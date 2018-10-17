using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("Unity.Entities.Hybrid")]
[assembly: InternalsVisibleTo("Unity.Entities.Properties")]

namespace Unity.Entities
{
    //@TODO: There is nothing prevent non-main thread (non-job thread) access of EntityMnaager.
    //       Static Analysis or runtime checks?

    public class EntityArchetypeQuery
    {
        public ComponentType[] Any;
        public ComponentType[] None;
        public ComponentType[] All;
    }

    //@TODO: safety?
    public unsafe struct EntityArchetype : IEquatable<EntityArchetype>
    {
        [NativeDisableUnsafePtrRestriction] internal Archetype* Archetype;

        public bool Valid => Archetype != null;

        public static bool operator ==(EntityArchetype lhs, EntityArchetype rhs)
        {
            return lhs.Archetype == rhs.Archetype;
        }

        public static bool operator !=(EntityArchetype lhs, EntityArchetype rhs)
        {
            return lhs.Archetype != rhs.Archetype;
        }

        public override bool Equals(object compare)
        {
            return this == (EntityArchetype) compare;
        }

        public bool Equals(EntityArchetype entityArchetype)
        {
            return Archetype == entityArchetype.Archetype;
        }

        public override int GetHashCode()
        {
            return (int) Archetype;
        }

        public ComponentType[] ComponentTypes
        {
            get
            {
                var types = new ComponentType[Archetype->TypesCount];
                for (var i = 0; i < types.Length; ++i)
                    types[i] = Archetype->Types[i].ToComponentType();
                return types;
            }
        }

        public int ChunkCount => Archetype->ChunkCount;
    }

    public struct Entity : IEquatable<Entity>
    {
        public int Index;
        public int Version;

        public static bool operator ==(Entity lhs, Entity rhs)
        {
            return lhs.Index == rhs.Index && lhs.Version == rhs.Version;
        }

        public static bool operator !=(Entity lhs, Entity rhs)
        {
            return lhs.Index != rhs.Index || lhs.Version != rhs.Version;
        }

        public override bool Equals(object compare)
        {
            return this == (Entity) compare;
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public static Entity Null => new Entity();

        public bool Equals(Entity entity)
        {
            return entity.Index == Index && entity.Version == Version;
        }

        public override string ToString()
        {
            return $"Entity Index: {Index} Version: {Version}";
        }
    }

    [Preserve]
    public sealed unsafe class EntityManager : ScriptBehaviourManager
    {
        EntityDataManager*                m_Entities;

        ArchetypeManager                  m_ArchetypeManager;
        EntityGroupManager                m_GroupManager;

        internal SharedComponentDataManager        m_SharedComponentManager;

        ExclusiveEntityTransaction        m_ExclusiveEntityTransaction;

        ComponentType*                    m_CachedComponentTypeArray;
        ComponentTypeInArchetype*         m_CachedComponentTypeInArchetypeArray;

        internal object m_CachedComponentList;

        internal EntityDataManager* Entities
        {
            get => m_Entities;
            private set => m_Entities = value;
        }

        internal ArchetypeManager ArchetypeManager
        {
            get => m_ArchetypeManager;
            private set => m_ArchetypeManager = value;
        }

        public int Version => IsCreated ? m_Entities->Version : 0;

        public uint GlobalSystemVersion => IsCreated ? Entities->GlobalSystemVersion : 0;

        public bool IsCreated => m_CachedComponentTypeArray != null;

        public int EntityCapacity
        {
            get => Entities->Capacity;
            set
            {
                BeforeStructuralChange();
                Entities->Capacity = value;
            }
        }

        internal ComponentJobSafetyManager ComponentJobSafetyManager { get; private set; }

        public JobHandle ExclusiveEntityTransactionDependency
        {
            get => ComponentJobSafetyManager.ExclusiveTransactionDependency;
            set => ComponentJobSafetyManager.ExclusiveTransactionDependency = value;
        }

        EntityManagerDebug m_Debug;

        public EntityManagerDebug Debug => m_Debug ?? (m_Debug = new EntityManagerDebug(this));

        protected override void OnBeforeCreateManagerInternal(World world, int capacity)
        {
        }

        protected override void OnBeforeDestroyManagerInternal()
        {
        }

        protected override void OnAfterDestroyManagerInternal()
        {
        }

        protected override void OnCreateManager(int capacity)
        {
            TypeManager.Initialize();

            Entities = (EntityDataManager*) UnsafeUtility.Malloc(sizeof(EntityDataManager), 64, Allocator.Persistent);
            Entities->OnCreate(capacity);

            m_SharedComponentManager = new SharedComponentDataManager();

            ArchetypeManager = new ArchetypeManager(m_SharedComponentManager);
            ComponentJobSafetyManager = new ComponentJobSafetyManager();
            m_GroupManager = new EntityGroupManager(ComponentJobSafetyManager);

            m_ExclusiveEntityTransaction = new ExclusiveEntityTransaction(ArchetypeManager, m_GroupManager,
                m_SharedComponentManager, Entities);

            m_CachedComponentTypeArray =
                (ComponentType*) UnsafeUtility.Malloc(sizeof(ComponentType) * 32 * 1024, 16, Allocator.Persistent);
            m_CachedComponentTypeInArchetypeArray =
                (ComponentTypeInArchetype*) UnsafeUtility.Malloc(sizeof(ComponentTypeInArchetype) * 32 * 1024, 16,
                    Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            EndExclusiveEntityTransaction();

            ComponentJobSafetyManager.PreDisposeCheck();

            // Clean up all entities. This is needed to free all internal buffer allocations so memory is not leaked.
            using (var allEntities = GetAllEntities())
            {
                DestroyEntity(allEntities);
            }

            ComponentJobSafetyManager.Dispose();
            ComponentJobSafetyManager = null;

            Entities->OnDestroy();
            UnsafeUtility.Free(Entities, Allocator.Persistent);
            Entities = null;
            ArchetypeManager.Dispose();
            ArchetypeManager = null;
            m_GroupManager.Dispose();
            m_GroupManager = null;
            m_ExclusiveEntityTransaction.OnDestroyManager();

            m_SharedComponentManager.Dispose();

            UnsafeUtility.Free(m_CachedComponentTypeArray, Allocator.Persistent);
            m_CachedComponentTypeArray = null;

            UnsafeUtility.Free(m_CachedComponentTypeInArchetypeArray, Allocator.Persistent);
            m_CachedComponentTypeInArchetypeArray = null;
        }

        internal override void InternalUpdate()
        {
        }

        private int PopulatedCachedTypeArray(ComponentType* requiredComponents, int count)
        {
            m_CachedComponentTypeArray[0] = ComponentType.Create<Entity>();
            for (var i = 0; i < count; ++i)
                SortingUtilities.InsertSorted(m_CachedComponentTypeArray, i + 1, requiredComponents[i]);
            return count + 1;
        }

        private int PopulatedCachedTypeInArchetypeArray(ComponentType* requiredComponents, int count)
        {
            m_CachedComponentTypeInArchetypeArray[0] = new ComponentTypeInArchetype(ComponentType.Create<Entity>());
            for (var i = 0; i < count; ++i)
                SortingUtilities.InsertSorted(m_CachedComponentTypeInArchetypeArray, i + 1, requiredComponents[i]);
            return count + 1;
        }

        internal ComponentGroup CreateComponentGroup(ComponentType* requiredComponents, int count)
        {
            var typeArrayCount = PopulatedCachedTypeArray(requiredComponents, count);
            return m_GroupManager.CreateEntityGroup(ArchetypeManager, Entities, m_CachedComponentTypeArray, typeArrayCount);
        }

        internal ComponentGroup CreateComponentGroup(params ComponentType[] requiredComponents)
        {
            fixed (ComponentType* requiredComponentsPtr = requiredComponents)
            {
                return CreateComponentGroup(requiredComponentsPtr, requiredComponents.Length);
            }
        }

        internal EntityArchetype CreateArchetype(ComponentType* types, int count)
        {
            var cachedComponentCount = PopulatedCachedTypeInArchetypeArray(types, count);

            // Lookup existing archetype (cheap)
            EntityArchetype entityArchetype;
            entityArchetype.Archetype =
                ArchetypeManager.GetExistingArchetype(m_CachedComponentTypeInArchetypeArray, cachedComponentCount);
            if (entityArchetype.Archetype != null)
                return entityArchetype;

            // Creating an archetype invalidates all iterators / jobs etc
            // because it affects the live iteration linked lists...
            BeforeStructuralChange();

            entityArchetype.Archetype = ArchetypeManager.GetOrCreateArchetype(m_CachedComponentTypeInArchetypeArray,
                cachedComponentCount, m_GroupManager);
            return entityArchetype;
        }

        public EntityArchetype CreateArchetype(params ComponentType[] types)
        {
            fixed (ComponentType* typesPtr = types)
            {
                return CreateArchetype(typesPtr, types.Length);
            }
        }

        public void CreateEntity(EntityArchetype archetype, NativeArray<Entity> entities)
        {
            CreateEntityInternal(archetype, (Entity*) entities.GetUnsafePtr(), entities.Length);
        }

        public Entity CreateEntity(EntityArchetype archetype)
        {
            Entity entity;
            CreateEntityInternal(archetype, &entity, 1);
            return entity;
        }

        public Entity CreateEntity(params ComponentType[] types)
        {
            return CreateEntity(CreateArchetype(types));
        }

        internal void CreateEntityInternal(EntityArchetype archetype, Entity* entities, int count)
        {
            BeforeStructuralChange();
            Entities->CreateEntities(ArchetypeManager, archetype.Archetype, entities, count);
        }

        public void DestroyEntity(ComponentGroup componentGroupFilter)
        {
            BeforeStructuralChange();

            // @TODO: Don't copy entity array,
            // take advantage of inherent chunk structure to do faster destruction
            var entityGroupArray = componentGroupFilter.GetEntityArray();
            if (entityGroupArray.Length == 0)
                return;

            var entityArray = new NativeArray<Entity>(entityGroupArray.Length, Allocator.Temp,
                NativeArrayOptions.UninitializedMemory);
            entityGroupArray.CopyTo(entityArray);
            if (entityArray.Length != 0)
                Entities->TryRemoveEntityId((Entity*) entityArray.GetUnsafeReadOnlyPtr(), entityArray.Length,
                    ArchetypeManager, m_SharedComponentManager, m_GroupManager, m_CachedComponentTypeInArchetypeArray);

            entityArray.Dispose();
        }

        public void DestroyEntity(NativeArray<Entity> entities)
        {
            DestroyEntityInternal((Entity*) entities.GetUnsafeReadOnlyPtr(), entities.Length);
        }

        public void DestroyEntity(NativeSlice<Entity> entities)
        {
            DestroyEntityInternal((Entity*) entities.GetUnsafeReadOnlyPtr(), entities.Length);
        }

        public void DestroyEntity(Entity entity)
        {
            DestroyEntityInternal(&entity, 1);
        }

        private void DestroyEntityInternal(Entity* entities, int count)
        {
            BeforeStructuralChange();
            Entities->AssertEntitiesExist(entities, count);
            Entities->TryRemoveEntityId(entities, count, ArchetypeManager, m_SharedComponentManager, m_GroupManager,
                m_CachedComponentTypeInArchetypeArray);
        }

        public bool Exists(Entity entity)
        {
            return Entities->Exists(entity);
        }

        public bool HasComponent<T>(Entity entity)
        {
            return Entities->HasComponent(entity, ComponentType.Create<T>());
        }

        public bool HasComponent(Entity entity, ComponentType type)
        {
            return Entities->HasComponent(entity, type);
        }

        public Entity Instantiate(Entity srcEntity)
        {
            Entity entity;
            InstantiateInternal(srcEntity, &entity, 1);
            return entity;
        }

        public void Instantiate(Entity srcEntity, NativeArray<Entity> outputEntities)
        {
            InstantiateInternal(srcEntity, (Entity*) outputEntities.GetUnsafePtr(), outputEntities.Length);
        }

        internal void InstantiateInternal(Entity srcEntity, Entity* outputEntities, int count)
        {
            BeforeStructuralChange();
            if (!Entities->Exists(srcEntity))
                throw new ArgumentException("srcEntity is not a valid entity");

            Entities->InstantiateEntities(ArchetypeManager, m_SharedComponentManager, m_GroupManager, srcEntity, outputEntities,
                count, m_CachedComponentTypeInArchetypeArray);
        }

        public void AddComponent(Entity entity, ComponentType type)
        {
            BeforeStructuralChange();
            Entities->AssertEntitiesExist(&entity, 1);
            Entities->AddComponent(entity, type, ArchetypeManager, m_SharedComponentManager, m_GroupManager,
                m_CachedComponentTypeInArchetypeArray);
        }

        public void RemoveComponent(Entity entity, ComponentType type)
        {
            BeforeStructuralChange();
            Entities->AssertEntityHasComponent(entity, type);
            Entities->RemoveComponent(entity, type, ArchetypeManager, m_SharedComponentManager, m_GroupManager,
                m_CachedComponentTypeInArchetypeArray);

            var archetype = Entities->GetArchetype(entity);
            if (archetype->SystemStateCleanupComplete)
            {
                Entities->TryRemoveEntityId(&entity, 1, ArchetypeManager, m_SharedComponentManager, m_GroupManager,
                    m_CachedComponentTypeInArchetypeArray);
            }
        }

        public void RemoveComponent<T>(Entity entity)
        {
            RemoveComponent(entity, ComponentType.Create<T>());
        }

        public void AddComponentData<T>(Entity entity, T componentData) where T : struct, IComponentData
        {
            AddComponent(entity, ComponentType.Create<T>());
            SetComponentData(entity, componentData);
        }

        public void AddBuffer<T>(Entity entity) where T : struct, IBufferElementData
        {
            AddComponent(entity, ComponentType.Create<T>());
        }

        public ComponentDataFromEntity<T> GetComponentDataFromEntity<T>(bool isReadOnly = false)
            where T : struct, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return GetComponentDataFromEntity<T>(typeIndex, isReadOnly);
        }

        internal ComponentDataFromEntity<T> GetComponentDataFromEntity<T>(int typeIndex, bool isReadOnly)
            where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized)
                throw new ArgumentException($"GetComponentDataFromEntity<{typeof(T)}> cannot be called on zero-sized IComponentData");
            
            return new ComponentDataFromEntity<T>(typeIndex, Entities,
                ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly));
#else
            return new ComponentDataFromEntity<T>(typeIndex, m_Entities);
#endif
        }

        public BufferFromEntity<T> GetBufferFromEntity<T>(bool isReadOnly = false)
            where T : struct, IBufferElementData
        {
            return GetBufferFromEntity<T>(TypeManager.GetTypeIndex<T>(), isReadOnly);
        }

        public BufferFromEntity<T> GetBufferFromEntity<T>(int typeIndex, bool isReadOnly = false)
            where T : struct, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new BufferFromEntity<T>(typeIndex, Entities, isReadOnly,
                ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly),
                ComponentJobSafetyManager.GetBufferSafetyHandle(typeIndex));
#else
            return new BufferFromEntity<T>(typeIndex, m_Entities, isReadOnly);
#endif
        }

        public T GetComponentData<T>(Entity entity) where T : struct, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized)
                throw new ArgumentException($"GetComponentData<{typeof(T)}> cannot be called on zero-sized IComponentData");
#endif
                
            Entities->AssertEntityHasComponent(entity, typeIndex);
            ComponentJobSafetyManager.CompleteWriteDependency(typeIndex);

            var ptr = Entities->GetComponentDataWithTypeRO(entity, typeIndex);

            T value;
            UnsafeUtility.CopyPtrToStructure(ptr, out value);
            return value;
        }

        public void SetComponentData<T>(Entity entity, T componentData) where T : struct, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized)
                throw new ArgumentException($"GetComponentData<{typeof(T)}> cannot be called on zero-sized IComponentData");
#endif
            
            Entities->AssertEntityHasComponent(entity, typeIndex);

            ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);

            var ptr = Entities->GetComponentDataWithTypeRW(entity, typeIndex, Entities->GlobalSystemVersion);
            UnsafeUtility.CopyStructureToPtr(ref componentData, ptr);
        }

        internal void SetComponentObject(Entity entity, ComponentType componentType, object componentObject)
        {
            Entities->AssertEntityHasComponent(entity, componentType.TypeIndex);

            //@TODO
            Chunk* chunk;
            int chunkIndex;
            Entities->GetComponentChunk(entity, out chunk, out chunkIndex);
            ArchetypeManager.SetManagedObject(chunk, componentType, chunkIndex, componentObject);
        }

        public int GetSharedComponentCount()
        {
            return m_SharedComponentManager.GetSharedComponentCount();
        }

        public void GetAllUniqueSharedComponentData<T>(List<T> sharedComponentValues)
            where T : struct, ISharedComponentData
        {
            m_SharedComponentManager.GetAllUniqueSharedComponents(sharedComponentValues);
        }

        public void GetAllUniqueSharedComponentData<T>(List<T> sharedComponentValues, List<int> sharedComponentIndices)
            where T : struct, ISharedComponentData
        {
            m_SharedComponentManager.GetAllUniqueSharedComponents(sharedComponentValues, sharedComponentIndices);
        }

        public T GetSharedComponentData<T>(Entity entity) where T : struct, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            Entities->AssertEntityHasComponent(entity, typeIndex);

            var sharedComponentIndex = Entities->GetSharedComponentDataIndex(entity, typeIndex);
            return m_SharedComponentManager.GetSharedComponentData<T>(sharedComponentIndex);
        }

        public T GetSharedComponentData<T>(int sharedComponentIndex) where T : struct, ISharedComponentData
        {
            return m_SharedComponentManager.GetSharedComponentData<T>(sharedComponentIndex);
        }

        public void AddSharedComponentData<T>(Entity entity, T componentData) where T : struct, ISharedComponentData
        {
            //TODO: optimize this (no need to move the entity to a new chunk twice)
            AddComponent(entity, ComponentType.Create<T>());
            SetSharedComponentData(entity, componentData);
        }

        internal void AddSharedComponentDataBoxed(Entity entity, int typeIndex, int hashCode, object componentData)
        {
            //TODO: optimize this (no need to move the entity to a new chunk twice)
            AddComponent(entity, ComponentType.FromTypeIndex(typeIndex));
            SetSharedComponentDataBoxed(entity, typeIndex, hashCode, componentData);
        }

        public void SetSharedComponentData<T>(Entity entity, T componentData) where T : struct, ISharedComponentData
        {
            BeforeStructuralChange();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            Entities->AssertEntityHasComponent(entity, typeIndex);

            var newSharedComponentDataIndex = m_SharedComponentManager.InsertSharedComponent(componentData);
            Entities->SetSharedComponentDataIndex(ArchetypeManager, m_SharedComponentManager, entity, typeIndex,
                newSharedComponentDataIndex);
            m_SharedComponentManager.RemoveReference(newSharedComponentDataIndex);
        }

        internal void SetSharedComponentDataBoxed(Entity entity, int typeIndex, int hashCode, object componentData)
        {
            BeforeStructuralChange();

            Entities->AssertEntityHasComponent(entity, typeIndex);

            var newSharedComponentDataIndex = 0;
            if (componentData != null) // null means default
                newSharedComponentDataIndex = m_SharedComponentManager.InsertSharedComponentAssumeNonDefault(typeIndex,
                    hashCode, componentData, TypeManager.GetTypeInfo(typeIndex).FastEqualityTypeInfo);

            Entities->SetSharedComponentDataIndex(ArchetypeManager, m_SharedComponentManager, entity, typeIndex,
                newSharedComponentDataIndex);
            m_SharedComponentManager.RemoveReference(newSharedComponentDataIndex);
        }

        public DynamicBuffer<T> GetBuffer<T>(Entity entity) where T : struct, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Entities->AssertEntityHasComponent(entity, typeIndex);
            if (TypeManager.GetTypeInfo<T>().Category != TypeManager.TypeCategory.BufferData)
                throw new ArgumentException(
                    $"GetBuffer<{typeof(T)}> may not be IComponentData or ISharedComponentData; currently {TypeManager.GetTypeInfo<T>().Category}");
#endif

            ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);

            BufferHeader* header = (BufferHeader*) Entities->GetComponentDataWithTypeRW(entity, typeIndex, Entities->GlobalSystemVersion);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new DynamicBuffer<T>(header, ComponentJobSafetyManager.GetSafetyHandle(typeIndex, false), ComponentJobSafetyManager.GetBufferSafetyHandle(typeIndex));
#else
            return new DynamicBuffer<T>(header);
#endif
        }

        public NativeArray<Entity> GetAllEntities(Allocator allocator = Allocator.Temp)
        {
            BeforeStructuralChange();
            
            var enabledQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = Array.Empty<ComponentType>(),
            };
            var disabledQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(Disabled)}
            };
            var prefabQuery = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(Prefab)}
            };
            var archetypes = new NativeList<EntityArchetype>(Allocator.TempJob);
            
            AddMatchingArchetypes(enabledQuery, archetypes);
            AddMatchingArchetypes(disabledQuery, archetypes);
            AddMatchingArchetypes(prefabQuery, archetypes);
            
            var chunks = CreateArchetypeChunkArray(archetypes, Allocator.TempJob);
            var count = ArchetypeChunkArray.CalculateEntityCount(chunks);
            var array = new NativeArray<Entity>(count, allocator);
            var entityType = GetArchetypeChunkEntityType();
            var offset = 0;

            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var entities = chunk.GetNativeArray(entityType);
                array.Slice(offset, entities.Length).CopyFrom(entities);
                offset += entities.Length;
            }
            
            chunks.Dispose();
            archetypes.Dispose();
            
            return array;
        }

        public NativeArray<ComponentType> GetComponentTypes(Entity entity, Allocator allocator = Allocator.Temp)
        {
            Entities->AssertEntitiesExist(&entity, 1);

            var archetype = Entities->GetArchetype(entity);

            var components = new NativeArray<ComponentType>(archetype->TypesCount - 1, allocator);

            for (var i = 1; i < archetype->TypesCount; i++)
                components[i - 1] = archetype->Types[i].ToComponentType();

            return components;
        }

        internal void SetBufferRaw(Entity entity, int componentTypeIndex, BufferHeader* tempBuffer, int sizeInChunk)
        {
            Entities->AssertEntityHasComponent(entity, componentTypeIndex);

            ComponentJobSafetyManager.CompleteReadAndWriteDependency(componentTypeIndex);

            var ptr = Entities->GetComponentDataWithTypeRW(entity, componentTypeIndex, Entities->GlobalSystemVersion);

            BufferHeader.Destroy((BufferHeader*)ptr);

            UnsafeUtility.MemCpy(ptr, tempBuffer, sizeInChunk);
        }

        public int GetComponentCount(Entity entity)
        {
            Entities->AssertEntitiesExist(&entity, 1);
            var archetype = Entities->GetArchetype(entity);
            return archetype->TypesCount - 1;
        }

        internal int GetComponentTypeIndex(Entity entity, int index)
        {
            Entities->AssertEntitiesExist(&entity, 1);
            var archetype = Entities->GetArchetype(entity);

            if ((uint) index >= archetype->TypesCount) return -1;

            return archetype->Types[index + 1].TypeIndex;
        }

        internal void SetComponentDataRaw(Entity entity, int typeIndex, void* data, int size)
        {
            Entities->AssertEntityHasComponent(entity, typeIndex);

            ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);

            var ptr = Entities->GetComponentDataWithTypeRW(entity, typeIndex, Entities->GlobalSystemVersion);
            UnsafeUtility.MemCpy(ptr, data, size);
        }

        internal void* GetComponentDataRawRW(Entity entity, int typeIndex)
        {
            Entities->AssertEntityHasComponent(entity, typeIndex);

            ComponentJobSafetyManager.CompleteReadAndWriteDependency(typeIndex);

            var ptr = Entities->GetComponentDataWithTypeRW(entity, typeIndex, Entities->GlobalSystemVersion);
            return ptr;
        }

        internal object GetSharedComponentData(Entity entity, int typeIndex)
        {
            Entities->AssertEntityHasComponent(entity, typeIndex);

            var sharedComponentIndex = Entities->GetSharedComponentDataIndex(entity, typeIndex);
            return m_SharedComponentManager.GetSharedComponentDataBoxed(sharedComponentIndex);
        }

        public int GetComponentOrderVersion<T>()
        {
            return Entities->GetComponentTypeOrderVersion(TypeManager.GetTypeIndex<T>());
        }

        public int GetSharedComponentOrderVersion<T>(T sharedComponent) where T : struct, ISharedComponentData
        {
            return m_SharedComponentManager.GetSharedComponentVersion(sharedComponent);
        }

        public ExclusiveEntityTransaction BeginExclusiveEntityTransaction()
        {
            ComponentJobSafetyManager.BeginExclusiveTransaction();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_ExclusiveEntityTransaction.SetAtomicSafetyHandle(ComponentJobSafetyManager.ExclusiveTransactionSafety);
#endif
            return m_ExclusiveEntityTransaction;
        }

        public void EndExclusiveEntityTransaction()
        {
            ComponentJobSafetyManager.EndExclusiveTransaction();
        }

        private void BeforeStructuralChange()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (ComponentJobSafetyManager.IsInTransaction)
                throw new InvalidOperationException(
                    "Access to EntityManager is not allowed after EntityManager.BeginExclusiveEntityTransaction(); has been called.");
#endif
            ComponentJobSafetyManager.CompleteAllJobsAndInvalidateArrays();
        }

        //@TODO: Not clear to me what this method is really for...
        public void CompleteAllJobs()
        {
            ComponentJobSafetyManager.CompleteAllJobsAndInvalidateArrays();
        }

        public void MoveEntitiesFrom(EntityManager srcEntities)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (srcEntities == this)
                throw new ArgumentException("srcEntities must not be the same as this EntityManager.");

            if (!srcEntities.m_SharedComponentManager.AllSharedComponentReferencesAreFromChunks(srcEntities.ArchetypeManager))
                throw new ArgumentException("EntityManager.MoveEntitiesFrom failed - All ISharedComponentData references must be from EntityManager. (For example ComponentGroup.SetFilter with a shared component type is not allowed during EntityManager.MoveEntitiesFrom)");
#endif

            BeforeStructuralChange();
            srcEntities.BeforeStructuralChange();

            ArchetypeManager.MoveChunks(srcEntities, ArchetypeManager, m_GroupManager, Entities, m_SharedComponentManager, m_CachedComponentTypeInArchetypeArray);

            //@TODO: Need to incrmeent the component versions based the moved chunks...
        }

        public void CheckInternalConsistency()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS

            //@TODO: Validate from perspective of componentgroup...
            //@TODO: Validate shared component data refcounts...
            var entityCountEntityData = Entities->CheckInternalConsistency();
            var entityCountArchetypeManager = ArchetypeManager.CheckInternalConsistency();

            Assert.AreEqual(entityCountEntityData, entityCountArchetypeManager);
#endif
        }

        public List<Type> GetAssignableComponentTypes(Type interfaceType)
        {
            // #todo Cache this. It only can change when TypeManager.GetTypeCount() changes
            var componentTypeCount = TypeManager.GetTypeCount();
            var assignableTypes = new List<Type>();
            for (var i = 0; i < componentTypeCount; i++)
            {
                var type = TypeManager.GetType(i);
                if (interfaceType.IsAssignableFrom(type)) assignableTypes.Add(type);
            }

            return assignableTypes;
        }

        private bool TestMatchingArchetypeAny(Archetype* archetype, ComponentType* anyTypes, int anyCount)
        {
            if (anyCount == 0) return true;

            var componentTypes = archetype->Types;
            var componentTypesCount = archetype->TypesCount;
            for (var i = 0; i < componentTypesCount; i++)
            {
                var componentTypeIndex = componentTypes[i].TypeIndex;
                for (var j = 0; j < anyCount; j++)
                {
                    var anyTypeIndex = anyTypes[j].TypeIndex;
                    if (componentTypeIndex == anyTypeIndex) return true;
                }
            }

            return false;
        }

        private bool TestMatchingArchetypeNone(Archetype* archetype, ComponentType* noneTypes, int noneCount)
        {
            var componentTypes = archetype->Types;
            var componentTypesCount = archetype->TypesCount;
            for (var i = 0; i < componentTypesCount; i++)
            {
                var componentTypeIndex = componentTypes[i].TypeIndex;
                for (var j = 0; j < noneCount; j++)
                {
                    var noneTypeIndex = noneTypes[j].TypeIndex;
                    if (componentTypeIndex == noneTypeIndex) return false;
                }
            }

            return true;
        }

        private bool TestMatchingArchetypeAll(Archetype* archetype, ComponentType* allTypes, int allCount)
        {
            var componentTypes = archetype->Types;
            var componentTypesCount = archetype->TypesCount;
            var foundCount = 0;
            var disabledTypeIndex = TypeManager.GetTypeIndex<Disabled>();
            var prefabTypeIndex = TypeManager.GetTypeIndex<Prefab>();
            var requestedDisabled = false;
            var requestedPrefab = false;
            for (var i = 0; i < componentTypesCount; i++)
            {
                var componentTypeIndex = componentTypes[i].TypeIndex;
                for (var j = 0; j < allCount; j++)
                {
                    var allTypeIndex = allTypes[j].TypeIndex;
                    if (allTypeIndex == disabledTypeIndex)
                        requestedDisabled = true;
                    if (allTypeIndex == prefabTypeIndex)
                        requestedPrefab = true;
                    if (componentTypeIndex == allTypeIndex) foundCount++;
                }
            }

            if (archetype->Disabled && (!requestedDisabled))
                return false;
            if (archetype->Prefab && (!requestedPrefab))
                return false;

            return foundCount == allCount;
        }

        public void AddMatchingArchetypes(EntityArchetypeQuery query, NativeList<EntityArchetype> foundArchetypes)
        {
            var anyCount = query.Any.Length;
            var noneCount = query.None.Length;
            var allCount = query.All.Length;

            fixed (ComponentType* any = query.Any)
            {
                fixed (ComponentType* none = query.None)
                {
                    fixed (ComponentType* all = query.All)
                    {
                        for (var archetype = ArchetypeManager.m_LastArchetype;
                            archetype != null;
                            archetype = archetype->PrevArchetype)
                        {
                            if (archetype->EntityCount == 0)
                                continue;
                            if (!TestMatchingArchetypeAny(archetype, any, anyCount))
                                continue;
                            if (!TestMatchingArchetypeNone(archetype, none, noneCount))
                                continue;
                            if (!TestMatchingArchetypeAll(archetype, all, allCount))
                                continue;

                            var entityArchetype = new EntityArchetype {Archetype = archetype};
                            var found = foundArchetypes.Contains(entityArchetype);
                            if (!found)
                                foundArchetypes.Add(entityArchetype);
                        }
                    }
                }
            }
        }

        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(NativeList<EntityArchetype> archetypes,
            Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandle = AtomicSafetyHandle.Create();
            return ArchetypeChunkArray.Create(archetypes, allocator, safetyHandle);
#else
            return ArchetypeChunkArray.Create(archetypes, allocator);
#endif
        }

        public NativeArray<ArchetypeChunk> CreateArchetypeChunkArray(EntityArchetypeQuery query, Allocator allocator)
        {
            var foundArchetypes = new NativeList<EntityArchetype>(Allocator.TempJob);
            AddMatchingArchetypes(query, foundArchetypes);
            var chunkStream = CreateArchetypeChunkArray(foundArchetypes, allocator);
            foundArchetypes.Dispose();
            return chunkStream;
        }

        public ArchetypeChunkComponentType<T> GetArchetypeChunkComponentType<T>(bool isReadOnly)
            where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return new ArchetypeChunkComponentType<T>(
                ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly), isReadOnly,
                GlobalSystemVersion);
#else
            return new ArchetypeChunkComponentType<T>(isReadOnly, GlobalSystemVersion);
#endif
        }

        public ArchetypeChunkBufferType<T> GetArchetypeChunkBufferType<T>(bool isReadOnly)
            where T : struct, IBufferElementData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return new ArchetypeChunkBufferType<T>(
                ComponentJobSafetyManager.GetSafetyHandle(typeIndex, isReadOnly),
                ComponentJobSafetyManager.GetBufferSafetyHandle(typeIndex),
                isReadOnly, GlobalSystemVersion);
#else
            return new ArchetypeChunkBufferType<T>(isReadOnly,GlobalSystemVersion);
#endif
        }

        public ArchetypeChunkSharedComponentType<T> GetArchetypeChunkSharedComponentType<T>()
            where T : struct, ISharedComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ArchetypeChunkSharedComponentType<T>(
                ComponentJobSafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<T>(), true));
#else
            return new ArchetypeChunkSharedComponentType<T>(false);
#endif
        }

        public ArchetypeChunkEntityType GetArchetypeChunkEntityType()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ArchetypeChunkEntityType(
                ComponentJobSafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true));
#else
            return new ArchetypeChunkEntityType(false);
#endif
        }

        public void PrepareForDeserialize()
        {
            Assert.AreEqual(0, Debug.EntityCount);
            m_SharedComponentManager.PrepareForDeserialize();
        }

        public class EntityManagerDebug
        {
            private readonly EntityManager m_Manager;

            public EntityManagerDebug(EntityManager entityManager)
            {
                m_Manager = entityManager;
            }

            public void PoisonUnusedDataInAllChunks(EntityArchetype archetype, byte value)
            {
                for (var c = archetype.Archetype->ChunkList.Begin; c != archetype.Archetype->ChunkList.End; c = c->Next)
                    ChunkDataUtility.PoisonUnusedChunkData((Chunk*) c, value);
            }

            public void SetGlobalSystemVersion(uint version)
            {
                m_Manager.Entities->GlobalSystemVersion = version;
            }

            public bool IsSharedComponentManagerEmpty()
            {
                return m_Manager.m_SharedComponentManager.IsEmpty();
            }

            internal static string GetArchetypeDebugString(Archetype* a)
            {
                var buf = new StringBuilder();
                buf.Append("(");

                for (var i = 0; i < a->TypesCount; i++)
                {
                    var componentTypeInArchetype = a->Types[i];
                    if (i > 0)
                        buf.Append(", ");
                    buf.Append(componentTypeInArchetype.ToString());
                }

                buf.Append(")");
                return buf.ToString();
            }

            public int EntityCount
            {
                get
                {
                    var allEntities = m_Manager.GetAllEntities();
                    var count = allEntities.Length;
                    allEntities.Dispose();
                    return count;
                }
            }

            public void LogEntityInfo(Entity entity)
            {
                var archetype = m_Manager.Entities->GetArchetype(entity);
                Unity.Debug.Log($"Entity {entity.Index}.{entity.Version}");
                for (var i = 0; i < archetype->TypesCount; i++)
                {
                    var componentTypeInArchetype = archetype->Types[i];
                    Unity.Debug.Log($"  - {componentTypeInArchetype.ToString()}");
                }
            }
        }

    }
}
