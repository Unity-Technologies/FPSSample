//#define USE_BURST_DESTROY

using System;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal struct CleanupEntity : IComponentData
    {
    }

    internal unsafe struct EntityDataManager
    {
#if USE_BURST_DESTROY
        private delegate Chunk* DeallocateDataEntitiesInChunkDelegate(EntityDataManager* entityDataManager, Entity* entities, int count, out int indexInChunk, out int batchCount);
        static DeallocateDataEntitiesInChunkDelegate ms_DeallocateDataEntitiesInChunkDelegate;
#endif

        private struct EntityData
        {
            public int Version;
            public Archetype* Archetype;
            public Chunk* Chunk;
            public int IndexInChunk;
        }

        private EntityData* m_Entities;
        private int m_EntitiesCapacity;
        private int m_EntitiesFreeIndex;

        private int* m_ComponentTypeOrderVersion;
        public uint GlobalSystemVersion;

        public int Version => GetComponentTypeOrderVersion(TypeManager.GetTypeIndex<Entity>());

        public void IncrementGlobalSystemVersion()
        {
            ChangeVersionUtility.IncrementGlobalSystemVersion(ref GlobalSystemVersion);
        }

        public void OnCreate(int capacity)
        {
            m_EntitiesCapacity = capacity;
            m_Entities = (EntityData*) UnsafeUtility.Malloc(m_EntitiesCapacity * sizeof(EntityData), 64, Allocator.Persistent);
            m_EntitiesFreeIndex = 0;
            GlobalSystemVersion = ChangeVersionUtility.InitialGlobalSystemVersion;
            InitializeAdditionalCapacity(0);

#if USE_BURST_DESTROY
            if (ms_DeallocateDataEntitiesInChunkDelegate == null)
            {
                ms_DeallocateDataEntitiesInChunkDelegate = DeallocateDataEntitiesInChunk;
                ms_DeallocateDataEntitiesInChunkDelegate =
 Burst.BurstDelegateCompiler.CompileDelegate(ms_DeallocateDataEntitiesInChunkDelegate);
            }
#endif

            const int componentTypeOrderVersionSize = sizeof(int) * TypeManager.MaximumTypesCount;
            m_ComponentTypeOrderVersion = (int*) UnsafeUtility.Malloc(componentTypeOrderVersionSize,
                UnsafeUtility.AlignOf<int>(), Allocator.Persistent);
            UnsafeUtility.MemClear(m_ComponentTypeOrderVersion, componentTypeOrderVersionSize);
        }

        public void OnDestroy()
        {
            UnsafeUtility.Free(m_Entities, Allocator.Persistent);
            m_Entities = null;
            m_EntitiesCapacity = 0;

            UnsafeUtility.Free(m_ComponentTypeOrderVersion, Allocator.Persistent);
            m_ComponentTypeOrderVersion = null;
        }

        private void InitializeAdditionalCapacity(int start)
        {
            for (var i = start; i != m_EntitiesCapacity; i++)
            {
                m_Entities[i].IndexInChunk = i + 1;
                m_Entities[i].Version = 1;
                m_Entities[i].Chunk = null;
            }

            // Last entity indexInChunk identifies that we ran out of space...
            m_Entities[m_EntitiesCapacity - 1].IndexInChunk = -1;
        }

        private void IncreaseCapacity()
        {
            Capacity = 2 * Capacity;
        }

        public int Capacity
        {
            get => m_EntitiesCapacity;
            set
            {
                if (value <= m_EntitiesCapacity)
                    return;

                var newEntities = (EntityData*) UnsafeUtility.Malloc(value * sizeof(EntityData),
                    64, Allocator.Persistent);
                UnsafeUtility.MemCpy(newEntities, m_Entities, m_EntitiesCapacity * sizeof(EntityData));
                UnsafeUtility.Free(m_Entities, Allocator.Persistent);

                var startNdx = m_EntitiesCapacity - 1;
                m_Entities = newEntities;
                m_EntitiesCapacity = value;

                InitializeAdditionalCapacity(startNdx);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateEntity(Entity entity)
        {
            if ((uint)entity.Index >= (uint)m_EntitiesCapacity)
                throw new ArgumentException(
                    "All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
        }

        public bool Exists(Entity entity)
        {
            int index = entity.Index;

            ValidateEntity(entity);

            var versionMatches = m_Entities[index].Version == entity.Version;
            var hasChunk = m_Entities[index].Chunk != null;

            return versionMatches && hasChunk;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntitiesExist(Entity* entities, int count)
        {
            for (var i = 0; i != count; i++)
            {
                var entity = entities + i;
                int index = entity->Index;

                if ((uint)index >= (uint)m_EntitiesCapacity)
                    throw new ArgumentException(
                        "All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");

                var exists = m_Entities[index].Version == entity->Version;
                if (!exists)
                    throw new ArgumentException(
                        "All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntityHasComponent(Entity entity, ComponentType componentType)
        {
            if (HasComponent(entity, componentType))
                return;

            if (!Exists(entity))
                throw new ArgumentException("The Entity does not exist");

            if (HasComponent(entity, componentType.TypeIndex))
                throw new ArgumentException(
                    $"The component typeof({componentType.GetManagedType()}) exists on the entity but the exact type {componentType} does not");

            throw new ArgumentException($"{componentType} component has not been added to the entity.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntityHasComponent(Entity entity, int componentType)
        {
            if (HasComponent(entity, componentType))
                return;

            if (!Exists(entity))
                throw new ArgumentException("The entity does not exist");

            throw new ArgumentException("The component has not been added to the entity.");
        }

        private static Chunk* EntityChunkBatch(EntityDataManager* entityDataManager, Entity* entities, int count,
            out int indexInChunk, out int batchCount)
        {
            /// This is optimized for the case where the array of entities are allocated contigously in the chunk
            /// Thus the compacting of other elements can be batched

            // Calculate baseEntityIndex & chunk
            var baseEntityIndex = entities[0].Index;

            var chunk = entityDataManager->m_Entities[baseEntityIndex].Chunk;
            indexInChunk = entityDataManager->m_Entities[baseEntityIndex].IndexInChunk;
            batchCount = 0;

            var entityDatas = entityDataManager->m_Entities;

            while (batchCount < count)
            {
                var entityIndex = entities[batchCount].Index;
                var data = entityDatas + entityIndex;

                if (data->Chunk != chunk || data->IndexInChunk != indexInChunk + batchCount)
                    break;

                batchCount++;
            }

            return chunk;
        }

        private static void DeallocateDataEntitiesInChunk(EntityDataManager* entityDataManager, Entity* entities,
            Chunk* chunk, int indexInChunk, int batchCount)
        {
            DeallocateBuffers(entityDataManager, entities, chunk, batchCount);

            var freeIndex = entityDataManager->m_EntitiesFreeIndex;
            var entityDatas = entityDataManager->m_Entities;

            for (var i = batchCount - 1; i >= 0; --i)
            {
                var entityIndex = entities[i].Index;
                var data = entityDatas + entityIndex;

                data->Chunk = null;
                data->Version++;
                data->IndexInChunk = freeIndex;
                freeIndex = entityIndex;
            }

            entityDataManager->m_EntitiesFreeIndex = freeIndex;

            // Compute the number of things that need to moved and patched.
            int patchCount = Math.Min(batchCount, chunk->Count - indexInChunk - batchCount);

            if (0 == patchCount)
                return;

            // updates EntitityData->indexInChunk to point to where the components will be moved to
            //Assert.IsTrue(chunk->archetype->sizeOfs[0] == sizeof(Entity) && chunk->archetype->offsets[0] == 0);
            var movedEntities = (Entity*) chunk->Buffer + (chunk->Count - patchCount);
            for (var i = 0; i != patchCount; i++)
                entityDataManager->m_Entities[movedEntities[i].Index].IndexInChunk = indexInChunk + i;

            // Move component data from the end to where we deleted components
            ChunkDataUtility.Copy(chunk, chunk->Count - patchCount, chunk, indexInChunk, patchCount);
        }

        private static void DeallocateBuffers(EntityDataManager* entityDataManager, Entity* entities, Chunk* chunk, int batchCount)
        {
            var archetype = chunk->Archetype;

            for (var ti = 0; ti < archetype->TypesCount; ++ti)
            {
                var type = archetype->Types[ti];

                if (!type.IsBuffer)
                    continue;

                var basePtr = chunk->Buffer + archetype->Offsets[ti];
                var stride = archetype->SizeOfs[ti];

                for (int i = 0; i < batchCount; ++i)
                {
                    Entity e = entities[i];
                    EntityData ed = entityDataManager->m_Entities[e.Index];
                    int indexInChunk = ed.IndexInChunk;
                    byte* bufferPtr = basePtr + stride * indexInChunk;
                    BufferHeader.Destroy((BufferHeader*)bufferPtr);
                }
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public int CheckInternalConsistency()
        {
            var aliveEntities = 0;
            var entityType = TypeManager.GetTypeIndex<Entity>();

            for (var i = 0; i != m_EntitiesCapacity; i++)
            {
                if (m_Entities[i].Chunk == null)
                    continue;

                aliveEntities++;
                var archetype = m_Entities[i].Archetype;
                Assert.AreEqual(entityType, archetype->Types[0].TypeIndex);
                var entity =
                    *(Entity*) ChunkDataUtility.GetComponentDataRO(m_Entities[i].Chunk, m_Entities[i].IndexInChunk, 0);
                Assert.AreEqual(i, entity.Index);
                Assert.AreEqual(m_Entities[i].Version, entity.Version);

                Assert.IsTrue(Exists(entity));
            }

            return aliveEntities;
        }
#endif

        public void AllocateConsecutiveEntitiesForLoading(int count)
        {
            int newCapacity = count + 1; // make room for Entity.Null
            Capacity = newCapacity;
            m_EntitiesFreeIndex = Capacity == newCapacity ? -1 : newCapacity;
            for (int i = 1; i < newCapacity; ++i)
            {
                if (m_Entities[i].Chunk != null)
                {
                    throw new ArgumentException("loading into non-empty entity manager is not supported");
                }

                m_Entities[i].IndexInChunk = 0;
                m_Entities[i].Version = 0;
            }
        }

        internal void AddExistingChunk(Chunk* chunk)
        {
            for (int iEntity = 0; iEntity < chunk->Count; ++iEntity)
            {
                var entity = (Entity*)ChunkDataUtility.GetComponentDataRO(chunk, iEntity, 0);
                m_Entities[entity->Index].Chunk = chunk;
                m_Entities[entity->Index].IndexInChunk = iEntity;
                m_Entities[entity->Index].Archetype = chunk->Archetype;
            }
        }

        public void AllocateEntities(Archetype* arch, Chunk* chunk, int baseIndex, int count, Entity* outputEntities)
        {
            Assert.AreEqual(chunk->Archetype->Offsets[0], 0);
            Assert.AreEqual(chunk->Archetype->SizeOfs[0], sizeof(Entity));

            var entityInChunkStart = (Entity*) chunk->Buffer + baseIndex;

            for (var i = 0; i != count; i++)
            {
                var entity = m_Entities + m_EntitiesFreeIndex;
                if (entity->IndexInChunk == -1)
                {
                    IncreaseCapacity();
                    entity = m_Entities + m_EntitiesFreeIndex;
                }

                outputEntities[i].Index = m_EntitiesFreeIndex;
                outputEntities[i].Version = entity->Version;

                var entityInChunk = entityInChunkStart + i;

                entityInChunk->Index = m_EntitiesFreeIndex;
                entityInChunk->Version = entity->Version;

                m_EntitiesFreeIndex = entity->IndexInChunk;

                entity->IndexInChunk = baseIndex + i;
                entity->Archetype = arch;
                entity->Chunk = chunk;
            }
        }

        public void AllocateEntitiesForRemapping(EntityDataManager * srcEntityDataManager, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            var srcEntityData = srcEntityDataManager->m_Entities;
            var count = srcEntityDataManager->m_EntitiesCapacity;
            for (var i = 0; i != count; i++)
            {
                if (srcEntityData[i].Chunk != null)
                {
                    var entity = m_Entities + m_EntitiesFreeIndex;
                    if (entity->IndexInChunk == -1)
                    {
                        IncreaseCapacity();
                        entity = m_Entities + m_EntitiesFreeIndex;
                    }
                    EntityRemapUtility.AddEntityRemapping(ref entityRemapping, new Entity { Version = srcEntityData[i].Version, Index = i }, new Entity { Version = entity->Version, Index = m_EntitiesFreeIndex });
                    m_EntitiesFreeIndex = entity->IndexInChunk;
                }
            }
        }

        public void RemapChunk(Archetype* arch, Chunk* chunk, int baseIndex, int count, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            Assert.AreEqual(chunk->Archetype->Offsets[0], 0);
            Assert.AreEqual(chunk->Archetype->SizeOfs[0], sizeof(Entity));

            var entityInChunkStart = (Entity*)(chunk->Buffer) + baseIndex;

            for (var i = 0; i != count; i++)
            {
                var entityInChunk = entityInChunkStart + i;
                var target = EntityRemapUtility.RemapEntity(ref entityRemapping, *entityInChunk);
                var entity = m_Entities + target.Index;
                Assert.AreEqual(entity->Version, target.Version);

                entityInChunk->Index = target.Index;
                entityInChunk->Version = entity->Version;
                entity->IndexInChunk = baseIndex + i;
                entity->Archetype = arch;
                entity->Chunk = chunk;
            }
        }

        public void FreeAllEntities()
        {
            for (var i = 0; i != m_EntitiesCapacity; i++)
            {
                m_Entities[i].IndexInChunk = i + 1;
                m_Entities[i].Version += 1;
                m_Entities[i].Chunk = null;
            }

            // Last entity indexInChunk identifies that we ran out of space...
            m_Entities[m_EntitiesCapacity - 1].IndexInChunk = -1;

            m_EntitiesFreeIndex = 0;
        }

        public bool HasComponent(Entity entity, int type)
        {
            if (!Exists(entity))
                return false;

            var archetype = m_Entities[entity.Index].Archetype;
            return ChunkDataUtility.GetIndexInTypeArray(archetype, type) != -1;
        }

        public bool HasComponent(Entity entity, ComponentType type)
        {
            if (!Exists(entity))
                return false;

            var archetype = m_Entities[entity.Index].Archetype;

            return ChunkDataUtility.GetIndexInTypeArray(archetype, type.TypeIndex) != -1;
        }

        public byte* GetComponentDataWithTypeRO(Entity entity, int typeIndex)
        {
            var entityData = m_Entities + entity.Index;
            return ChunkDataUtility.GetComponentDataWithTypeRO(entityData->Chunk, entityData->IndexInChunk, typeIndex);
        }

        public byte* GetComponentDataWithTypeRW(Entity entity, int typeIndex, uint globalVersion)
        {
            var entityData = m_Entities + entity.Index;
            return ChunkDataUtility.GetComponentDataWithTypeRW(entityData->Chunk, entityData->IndexInChunk, typeIndex,
                globalVersion);
        }

        public byte* GetComponentDataWithTypeRO(Entity entity, int typeIndex, ref int typeLookupCache)
        {
            var entityData = m_Entities + entity.Index;
            return ChunkDataUtility.GetComponentDataWithTypeRO(entityData->Chunk, entityData->IndexInChunk, typeIndex,
                ref typeLookupCache);
        }

        public byte* GetComponentDataWithTypeRW(Entity entity, int typeIndex, uint globalVersion,
            ref int typeLookupCache)
        {
            var entityData = m_Entities + entity.Index;
            return ChunkDataUtility.GetComponentDataWithTypeRW(entityData->Chunk, entityData->IndexInChunk, typeIndex,
                globalVersion, ref typeLookupCache);
        }

        public Chunk* GetComponentChunk(Entity entity)
        {
            var entityData = m_Entities + entity.Index;
            return entityData->Chunk;
        }

        public void GetComponentChunk(Entity entity, out Chunk* chunk, out int chunkIndex)
        {
            var entityData = m_Entities + entity.Index;
            chunk = entityData->Chunk;
            chunkIndex = entityData->IndexInChunk;
        }

        public Archetype* GetArchetype(Entity entity)
        {
            return m_Entities[entity.Index].Archetype;
        }
        
        public Archetype* GetInstantiableArchetype(Entity entity, ArchetypeManager archetypeManager, EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            var srcArchetype = GetArchetype(entity);
            return srcArchetype->InstantiableArchetype;
        }

        public void SetArchetype(ArchetypeManager typeMan, Entity entity, Archetype* archetype,
            int* sharedComponentDataIndices)
        {
            var chunk = typeMan.GetChunkWithEmptySlots(archetype, sharedComponentDataIndices);
            var chunkIndex = typeMan.AllocateIntoChunk(chunk);

            var oldArchetype = m_Entities[entity.Index].Archetype;
            var oldChunk = m_Entities[entity.Index].Chunk;
            var oldChunkIndex = m_Entities[entity.Index].IndexInChunk;
            ChunkDataUtility.Convert(oldChunk, oldChunkIndex, chunk, chunkIndex);
            if (chunk->ManagedArrayIndex >= 0 && oldChunk->ManagedArrayIndex >= 0)
                ChunkDataUtility.CopyManagedObjects(typeMan, oldChunk, oldChunkIndex, chunk, chunkIndex, 1);

            m_Entities[entity.Index].Archetype = archetype;
            m_Entities[entity.Index].Chunk = chunk;
            m_Entities[entity.Index].IndexInChunk = chunkIndex;

            var lastIndex = oldChunk->Count - 1;
            // No need to replace with ourselves
            if (lastIndex != oldChunkIndex)
            {
                var lastEntity = (Entity*) ChunkDataUtility.GetComponentDataRO(oldChunk, lastIndex, 0);
                m_Entities[lastEntity->Index].IndexInChunk = oldChunkIndex;

                ChunkDataUtility.Copy(oldChunk, lastIndex, oldChunk, oldChunkIndex, 1);
                if (oldChunk->ManagedArrayIndex >= 0)
                    ChunkDataUtility.CopyManagedObjects(typeMan, oldChunk, lastIndex, oldChunk, oldChunkIndex, 1);
            }

            if (oldChunk->ManagedArrayIndex >= 0)
                ChunkDataUtility.ClearManagedObjects(typeMan, oldChunk, lastIndex, 1);

            --oldArchetype->EntityCount;
            typeMan.SetChunkCount(oldChunk, lastIndex);
        }

        public void AddComponent(Entity entity, ComponentType type, ArchetypeManager archetypeManager,
            SharedComponentDataManager sharedComponentDataManager,
            EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            var componentType = new ComponentTypeInArchetype(type);
            var archetype = GetArchetype(entity);

            var t = 0;
            while (t < archetype->TypesCount && archetype->Types[t] < componentType)
            {
                componentTypeInArchetypeArray[t] = archetype->Types[t];
                ++t;
            }

            componentTypeInArchetypeArray[t] = componentType;
            while (t < archetype->TypesCount)
            {
                componentTypeInArchetypeArray[t + 1] = archetype->Types[t];
                ++t;
            }

            var newType = archetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray,
                archetype->TypesCount + 1, groupManager);

            int* sharedComponentDataIndices = null;
            if (newType->NumSharedComponents > 0)
            {
                var oldSharedComponentDataIndices = GetComponentChunk(entity)->SharedComponentValueArray;
                if (type.IsSharedComponent)
                {
                    int* stackAlloced = stackalloc int[newType->NumSharedComponents];
                    sharedComponentDataIndices = stackAlloced;

                    if (archetype->SharedComponentOffset == null)
                    {
                        sharedComponentDataIndices[0] = 0;
                    }
                    else
                    {
                        t = 0;
                        var sharedIndex = 0;
                        while (t < archetype->TypesCount && archetype->Types[t] < componentType)
                        {
                            if (archetype->SharedComponentOffset[t] != -1)
                            {
                                sharedComponentDataIndices[sharedIndex] = oldSharedComponentDataIndices[sharedIndex];
                                ++sharedIndex;
                            }

                            ++t;
                        }

                        sharedComponentDataIndices[sharedIndex] = 0;
                        while (t < archetype->TypesCount)
                        {
                            if (archetype->SharedComponentOffset[t] != -1)
                            {
                                sharedComponentDataIndices[sharedIndex + 1] =
                                    oldSharedComponentDataIndices[sharedIndex];
                                ++sharedIndex;
                            }

                            ++t;
                        }
                    }
                }
                else
                {
                    // reuse old sharedComponentDataIndices
                    sharedComponentDataIndices = oldSharedComponentDataIndices;
                }
            }

            SetArchetype(archetypeManager, entity, newType, sharedComponentDataIndices);
            IncrementComponentOrderVersion(newType, GetComponentChunk(entity), sharedComponentDataManager);
        }

        public void TryRemoveEntityId(Entity* entities, int count, ArchetypeManager archetypeManager,
            SharedComponentDataManager sharedComponentDataManager,
            EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            var entityIndex = 0;
            while (entityIndex != count)
            {
                int indexInChunk;
                int batchCount;
                fixed (EntityDataManager* manager = &this)
                {
                    var chunk = EntityChunkBatch(manager, entities + entityIndex, count - entityIndex, out indexInChunk,
                        out batchCount);
                    var archetype = GetArchetype(entities[entityIndex]);
                    if (!archetype->SystemStateCleanupNeeded)
                    {
                        DeallocateDataEntitiesInChunk(manager, entities + entityIndex, chunk, indexInChunk, batchCount);
                        IncrementComponentOrderVersion(chunk->Archetype, chunk, sharedComponentDataManager);

                        if (chunk->ManagedArrayIndex >= 0)
                        {
                            // We can just chop-off the end, no need to copy anything
                            if (chunk->Count != indexInChunk + batchCount)
                                ChunkDataUtility.CopyManagedObjects(archetypeManager, chunk, chunk->Count - batchCount,
                                    chunk,
                                    indexInChunk, batchCount);

                            ChunkDataUtility.ClearManagedObjects(archetypeManager, chunk, chunk->Count - batchCount,
                                batchCount);
                        }

                        chunk->Archetype->EntityCount -= batchCount;
                        archetypeManager.SetChunkCount(chunk, chunk->Count - batchCount);
                    }
                    else
                    {
                        for (var batchEntityIndex = 0; batchEntityIndex < batchCount; batchEntityIndex++)
                        {
                            var entity = entities[entityIndex + batchEntityIndex];
                            var removedTypes = 0;
                            var removedComponentIsShared = false;
                            for (var t = 1; t < archetype->TypesCount; ++t)
                            {
                                var type = archetype->Types[t];
                                
                                if (!(type.IsSystemStateComponent||type.IsSystemStateSharedComponent))
                                {
                                    ++removedTypes;
                                    removedComponentIsShared |= type.IsSharedComponent;
                                }
                                else
                                {
                                    componentTypeInArchetypeArray[t - removedTypes] = archetype->Types[t];
                                }
                            }

                            componentTypeInArchetypeArray[archetype->TypesCount - removedTypes] =
                                new ComponentTypeInArchetype(ComponentType.Create<CleanupEntity>());

                            var newType = archetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray,
                                archetype->TypesCount - removedTypes + 1, groupManager);

                            int* sharedComponentDataIndices = null;
                            if (newType->NumSharedComponents > 0)
                            {
                                var oldSharedComponentDataIndices =
                                    GetComponentChunk(entity)->SharedComponentValueArray;
                                if (removedComponentIsShared)
                                {
                                    int* tempAlloc = stackalloc int[newType->NumSharedComponents];
                                    sharedComponentDataIndices = tempAlloc;

                                    var srcIndex = 0;
                                    var dstIndex = 0;
                                    for (var t = 0; t < archetype->TypesCount; ++t)
                                    {
                                        if (archetype->SharedComponentOffset[t] != -1)
                                        {
                                            var typeIndex = archetype->Types[t].TypeIndex;
                                            var systemStateType = typeof(ISystemStateComponentData).IsAssignableFrom(TypeManager.GetType(typeIndex));
                                            var systemStateSharedType = typeof(ISystemStateSharedComponentData).IsAssignableFrom(TypeManager.GetType(typeIndex));
                                            if (!(systemStateType||systemStateSharedType))
                                            {
                                                srcIndex++;
                                            }
                                            else
                                            {
                                                sharedComponentDataIndices[dstIndex] =
                                                    oldSharedComponentDataIndices[srcIndex];
                                                srcIndex++;
                                                dstIndex++;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // reuse old sharedComponentDataIndices
                                    sharedComponentDataIndices = oldSharedComponentDataIndices;
                                }
                            }

                            IncrementComponentOrderVersion(archetype, GetComponentChunk(entity),
                                sharedComponentDataManager);
                            SetArchetype(archetypeManager, entity, newType, sharedComponentDataIndices);
                        }
                    }
                }

                entityIndex += batchCount;
            }
        }

        public void RemoveComponent(Entity entity, ComponentType type, ArchetypeManager archetypeManager,
            SharedComponentDataManager sharedComponentDataManager,
            EntityGroupManager groupManager, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            var componentType = new ComponentTypeInArchetype(type);

            var archetype = GetArchetype(entity);

            var removedTypes = 0;
            for (var t = 0; t < archetype->TypesCount; ++t)
                if (archetype->Types[t].TypeIndex == componentType.TypeIndex)
                    ++removedTypes;
                else
                    componentTypeInArchetypeArray[t - removedTypes] = archetype->Types[t];

            var newType = archetypeManager.GetOrCreateArchetype(componentTypeInArchetypeArray,
                archetype->TypesCount - removedTypes, groupManager);

            int* sharedComponentDataIndices = null;
            if (newType->NumSharedComponents > 0)
            {
                var oldSharedComponentDataIndices = GetComponentChunk(entity)->SharedComponentValueArray;
                if (type.IsSharedComponent)
                {
                    int* tempAlloc = stackalloc int[newType->NumSharedComponents];
                    sharedComponentDataIndices = tempAlloc;

                    var srcIndex = 0;
                    var dstIndex = 0;
                    for (var t = 0; t < archetype->TypesCount; ++t)
                        if (archetype->SharedComponentOffset[t] != -1)
                        {
                            if (archetype->Types[t].TypeIndex == componentType.TypeIndex)
                            {
                                srcIndex++;
                            }
                            else
                            {
                                sharedComponentDataIndices[dstIndex] = oldSharedComponentDataIndices[srcIndex];
                                srcIndex++;
                                dstIndex++;
                            }
                        }
                }
                else
                {
                    // reuse old sharedComponentDataIndices
                    sharedComponentDataIndices = oldSharedComponentDataIndices;
                }
            }

            IncrementComponentOrderVersion(archetype, GetComponentChunk(entity), sharedComponentDataManager);

            SetArchetype(archetypeManager, entity, newType, sharedComponentDataIndices);
        }

        public void MoveEntityToChunk(ArchetypeManager typeMan, Entity entity, Chunk* newChunk, int newChunkIndex)
        {
            var oldChunk = m_Entities[entity.Index].Chunk;
            Assert.IsTrue(oldChunk->Archetype == newChunk->Archetype);

            var oldChunkIndex = m_Entities[entity.Index].IndexInChunk;

            ChunkDataUtility.Copy(oldChunk, oldChunkIndex, newChunk, newChunkIndex, 1);

            if (oldChunk->ManagedArrayIndex >= 0)
                ChunkDataUtility.CopyManagedObjects(typeMan, oldChunk, oldChunkIndex, newChunk, newChunkIndex, 1);

            m_Entities[entity.Index].Chunk = newChunk;
            m_Entities[entity.Index].IndexInChunk = newChunkIndex;

            var lastIndex = oldChunk->Count - 1;
            // No need to replace with ourselves
            if (lastIndex != oldChunkIndex)
            {
                var lastEntity = (Entity*) ChunkDataUtility.GetComponentDataRO(oldChunk, lastIndex, 0);
                m_Entities[lastEntity->Index].IndexInChunk = oldChunkIndex;

                ChunkDataUtility.Copy(oldChunk, lastIndex, oldChunk, oldChunkIndex, 1);
                if (oldChunk->ManagedArrayIndex >= 0)
                    ChunkDataUtility.CopyManagedObjects(typeMan, oldChunk, lastIndex, oldChunk, oldChunkIndex, 1);
            }

            if (oldChunk->ManagedArrayIndex >= 0)
                ChunkDataUtility.ClearManagedObjects(typeMan, oldChunk, lastIndex, 1);

            newChunk->Archetype->EntityCount--;
            typeMan.SetChunkCount(oldChunk, oldChunk->Count - 1);
        }

        public void CreateEntities(ArchetypeManager archetypeManager, Archetype* archetype, Entity* entities, int count)
        {
            int* sharedComponentDataIndices = stackalloc int[archetype->NumSharedComponents];
            UnsafeUtility.MemClear(sharedComponentDataIndices, archetype->NumSharedComponents*sizeof(int));

            while (count != 0)
            {
                var chunk = archetypeManager.GetChunkWithEmptySlots(archetype, sharedComponentDataIndices);
                int allocatedIndex;
                var allocatedCount = archetypeManager.AllocateIntoChunk(chunk, count, out allocatedIndex);
                AllocateEntities(archetype, chunk, allocatedIndex, allocatedCount, entities);
                ChunkDataUtility.InitializeComponents(chunk, allocatedIndex, allocatedCount);

                entities += allocatedCount;
                count -= allocatedCount;
            }

            IncrementComponentTypeOrderVersion(archetype);
        }

        public void InstantiateEntities(ArchetypeManager archetypeManager,
            SharedComponentDataManager sharedComponentDataManager, EntityGroupManager groupManager, Entity srcEntity, Entity* outputEntities, int count, ComponentTypeInArchetype* componentTypeInArchetypeArray)
        {
            var srcIndex = m_Entities[srcEntity.Index].IndexInChunk;
            var srcChunk = m_Entities[srcEntity.Index].Chunk;
            var srcArchetype = GetArchetype(srcEntity);
            var dstArchetype = GetInstantiableArchetype(srcEntity,archetypeManager, groupManager, componentTypeInArchetypeArray);
            var srcSharedComponentDataIndices = GetComponentChunk(srcEntity)->SharedComponentValueArray;

            while (count != 0)
            {
                var chunk = archetypeManager.GetChunkWithEmptySlots(dstArchetype, srcSharedComponentDataIndices);
                int indexInChunk;
                var allocatedCount = archetypeManager.AllocateIntoChunk(chunk, count, out indexInChunk);

                ChunkDataUtility.ReplicateComponents(srcChunk, srcIndex, chunk, indexInChunk, allocatedCount);

                AllocateEntities(dstArchetype, chunk, indexInChunk, allocatedCount, outputEntities);

                outputEntities += allocatedCount;
                count -= allocatedCount;
            }

            IncrementComponentOrderVersion(dstArchetype, srcChunk, sharedComponentDataManager);
        }

        public int GetSharedComponentDataIndex(Entity entity, int typeIndex)
        {
            var archetype = GetArchetype(entity);
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);

            var chunk = m_Entities[entity.Index].Chunk;
            var sharedComponentValueArray = chunk->SharedComponentValueArray;
            var sharedComponentOffset = m_Entities[entity.Index].Archetype->SharedComponentOffset[indexInTypeArray];
            return sharedComponentValueArray[sharedComponentOffset];
        }

        public void SetSharedComponentDataIndex(ArchetypeManager archetypeManager,
            SharedComponentDataManager sharedComponentDataManager, Entity entity, int typeIndex,
            int newSharedComponentDataIndex)
        {
            var archetype = GetArchetype(entity);

            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);

            var srcChunk = GetComponentChunk(entity);
            var srcSharedComponentValueArray = srcChunk->SharedComponentValueArray;
            var sharedComponentOffset = archetype->SharedComponentOffset[indexInTypeArray];
            var oldSharedComponentDataIndex = srcSharedComponentValueArray[sharedComponentOffset];

            if (newSharedComponentDataIndex == oldSharedComponentDataIndex)
                return;

            var sharedComponentIndices = stackalloc int[archetype->NumSharedComponents];
            var srcSharedComponentDataIndices = srcChunk->SharedComponentValueArray;

            UnsafeUtility.MemCpy(sharedComponentIndices, srcSharedComponentDataIndices, archetype->NumSharedComponents*sizeof(int));

            sharedComponentIndices[sharedComponentOffset] = newSharedComponentDataIndex;

            var newChunk = archetypeManager.GetChunkWithEmptySlots(archetype, sharedComponentIndices);
            var newChunkIndex = archetypeManager.AllocateIntoChunk(newChunk);

            IncrementComponentOrderVersion(archetype, srcChunk, sharedComponentDataManager);

            MoveEntityToChunk(archetypeManager, entity, newChunk, newChunkIndex);
        }

        internal void IncrementComponentOrderVersion(Archetype* archetype, Chunk* chunk,
            SharedComponentDataManager sharedComponentDataManager)
        {
            // Increment shared component version
            var sharedComponentDataIndices = chunk->SharedComponentValueArray;
            for (var i = 0; i < archetype->NumSharedComponents; i++)
                sharedComponentDataManager.IncrementSharedComponentVersion(sharedComponentDataIndices[i]);

            IncrementComponentTypeOrderVersion(archetype);
        }

        internal void IncrementComponentTypeOrderVersion(Archetype* archetype)
        {
            // Increment type component version
            for (var t = 0; t < archetype->TypesCount; ++t)
            {
                var typeIndex = archetype->Types[t].TypeIndex;
                m_ComponentTypeOrderVersion[typeIndex]++;
            }
        }

        public int GetComponentTypeOrderVersion(int typeIndex)
        {
            return m_ComponentTypeOrderVersion[typeIndex];
        }
    }
}
