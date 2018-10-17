using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [NativeContainer]
    public unsafe struct ExclusiveEntityTransaction
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
#endif
        [NativeDisableUnsafePtrRestriction] private GCHandle m_ArchetypeManager;

        [NativeDisableUnsafePtrRestriction] private GCHandle m_EntityGroupManager;

        [NativeDisableUnsafePtrRestriction] private GCHandle m_SharedComponentDataManager;

        [NativeDisableUnsafePtrRestriction] private EntityDataManager* m_Entities;

        [NativeDisableUnsafePtrRestriction]
        private readonly ComponentTypeInArchetype* m_CachedComponentTypeInArchetypeArray;

        internal SharedComponentDataManager SharedComponentDataManager
        {
            get
            {
                return (SharedComponentDataManager) m_SharedComponentDataManager.Target;
            }
        }

        internal ArchetypeManager ArchetypeManager
        {
            get
            {
                return (ArchetypeManager) m_ArchetypeManager.Target;
            }
        }

        
        
        internal ExclusiveEntityTransaction(ArchetypeManager archetypes, EntityGroupManager entityGroupManager,
            SharedComponentDataManager sharedComponentDataManager, EntityDataManager* data)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = new AtomicSafetyHandle();
#endif
            m_Entities = data;
            m_ArchetypeManager = GCHandle.Alloc(archetypes, GCHandleType.Weak);
            m_EntityGroupManager = GCHandle.Alloc(entityGroupManager, GCHandleType.Weak);
            m_SharedComponentDataManager = GCHandle.Alloc(sharedComponentDataManager, GCHandleType.Weak);

            m_CachedComponentTypeInArchetypeArray =
                (ComponentTypeInArchetype*) UnsafeUtility.Malloc(sizeof(ComponentTypeInArchetype) * 32 * 1024, 16,
                    Allocator.Persistent);
        }

        internal void OnDestroyManager()
        {
            UnsafeUtility.Free(m_CachedComponentTypeInArchetypeArray, Allocator.Persistent);
            m_ArchetypeManager.Free();
            m_EntityGroupManager.Free();
            m_SharedComponentDataManager.Free();
            m_Entities = null;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal void SetAtomicSafetyHandle(AtomicSafetyHandle safety)
        {
            m_Safety = safety;
        }
#endif

        private int PopulatedCachedTypeInArchetypeArray(ComponentType* requiredComponents, int count)
        {
            m_CachedComponentTypeInArchetypeArray[0] = new ComponentTypeInArchetype(ComponentType.Create<Entity>());
            for (var i = 0; i < count; ++i)
                SortingUtilities.InsertSorted(m_CachedComponentTypeInArchetypeArray, i + 1, requiredComponents[i]);
            return count + 1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        internal EntityArchetype CreateArchetype(ComponentType* types, int count)
        {
            CheckAccess();

            var groupManager = (EntityGroupManager) m_EntityGroupManager.Target;

            EntityArchetype type;
            type.Archetype = ArchetypeManager.GetOrCreateArchetype(m_CachedComponentTypeInArchetypeArray,
                PopulatedCachedTypeInArchetypeArray(types, count), groupManager);

            return type;
        }

        public EntityArchetype CreateArchetype(params ComponentType[] types)
        {
            fixed (ComponentType* typesPtr = types)
            {
                return CreateArchetype(typesPtr, types.Length);
            }
        }

        public Entity CreateEntity(EntityArchetype archetype)
        {
            CheckAccess();

            Entity entity;
            CreateEntityInternal(archetype, &entity, 1);
            return entity;
        }

        public void CreateEntity(EntityArchetype archetype, NativeArray<Entity> entities)
        {
            CreateEntityInternal(archetype, (Entity*) entities.GetUnsafePtr(), entities.Length);
        }

        public Entity CreateEntity(params ComponentType[] types)
        {
            return CreateEntity(CreateArchetype(types));
        }

        private void CreateEntityInternal(EntityArchetype archetype, Entity* entities, int count)
        {
            CheckAccess();
            m_Entities->CreateEntities(ArchetypeManager, archetype.Archetype, entities, count);
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

        private void InstantiateInternal(Entity srcEntity, Entity* outputEntities, int count)
        {
            CheckAccess();

            if (!m_Entities->Exists(srcEntity))
                throw new ArgumentException("srcEntity is not a valid entity");

            var groupManager = (EntityGroupManager) m_EntityGroupManager.Target;

            m_Entities->InstantiateEntities(ArchetypeManager, SharedComponentDataManager, groupManager, srcEntity, outputEntities,
                count, m_CachedComponentTypeInArchetypeArray);
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
            CheckAccess();
            m_Entities->AssertEntitiesExist(entities, count);

            var groupManager = (EntityGroupManager) m_EntityGroupManager.Target;

            m_Entities->TryRemoveEntityId(entities, count, ArchetypeManager, SharedComponentDataManager, groupManager,
                m_CachedComponentTypeInArchetypeArray);
        }

        public void AddComponent(Entity entity, ComponentType type)
        {
            CheckAccess();

            var groupManager = (EntityGroupManager) m_EntityGroupManager.Target;

            m_Entities->AssertEntitiesExist(&entity, 1);
            m_Entities->AddComponent(entity, type, ArchetypeManager, SharedComponentDataManager, groupManager,
                m_CachedComponentTypeInArchetypeArray);
        }

        public void RemoveComponent(Entity entity, ComponentType type)
        {
            CheckAccess();

            var groupManager = (EntityGroupManager) m_EntityGroupManager.Target;

            m_Entities->AssertEntityHasComponent(entity, type);
            m_Entities->RemoveComponent(entity, type, ArchetypeManager, SharedComponentDataManager, groupManager,
                m_CachedComponentTypeInArchetypeArray);
        }

        public bool Exists(Entity entity)
        {
            CheckAccess();

            return m_Entities->Exists(entity);
        }

        public T GetComponentData<T>(Entity entity) where T : struct, IComponentData
        {
            CheckAccess();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            m_Entities->AssertEntityHasComponent(entity, typeIndex);

            var ptr = m_Entities->GetComponentDataWithTypeRO(entity, typeIndex);

            T data;
            UnsafeUtility.CopyPtrToStructure(ptr, out data);
            return data;
        }

        public void SetComponentData<T>(Entity entity, T componentData) where T : struct, IComponentData
        {
            CheckAccess();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            m_Entities->AssertEntityHasComponent(entity, typeIndex);

            var ptr = m_Entities->GetComponentDataWithTypeRW(entity, typeIndex, m_Entities->GlobalSystemVersion);
            UnsafeUtility.CopyStructureToPtr(ref componentData, ptr);
        }

        public T GetSharedComponentData<T>(Entity entity) where T : struct, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            m_Entities->AssertEntityHasComponent(entity, typeIndex);

            var sharedComponentIndex = m_Entities->GetSharedComponentDataIndex(entity, typeIndex);
            return SharedComponentDataManager.GetSharedComponentData<T>(sharedComponentIndex);
        }

        public void SetSharedComponentData<T>(Entity entity, T componentData) where T : struct, ISharedComponentData
        {
            CheckAccess();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            m_Entities->AssertEntityHasComponent(entity, typeIndex);

            var archetypeManager = ArchetypeManager;
            var sharedComponentDataManager = SharedComponentDataManager;

            var newSharedComponentDataIndex = sharedComponentDataManager.InsertSharedComponent(componentData);
            m_Entities->SetSharedComponentDataIndex(archetypeManager, sharedComponentDataManager, entity, typeIndex,
                newSharedComponentDataIndex);
            sharedComponentDataManager.RemoveReference(newSharedComponentDataIndex);
        }

        internal void AllocateConsecutiveEntitiesForLoading(int count)
        {
            m_Entities->AllocateConsecutiveEntitiesForLoading(count);
        }


        internal void AddExistingChunk(Chunk* chunk)
        {
            ArchetypeManager.AddExistingChunk(chunk);
            m_Entities->AddExistingChunk(chunk);
        }
    }
}
