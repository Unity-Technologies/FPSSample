using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Profiling;

namespace Unity.Entities
{
    public unsafe class ComponentGroup : IDisposable
    {
        readonly ComponentJobSafetyManager m_SafetyManager;
        readonly EntityGroupData*          m_GroupData;
        readonly EntityDataManager*        m_EntityDataManager;
        ComponentGroupFilter               m_Filter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal string                    DisallowDisposing = null;
#endif

        // TODO: this is temporary, used to cache some state to avoid recomputing the TransformAccessArray. We need to improve this.
        internal IDisposable               m_CachedState;

        internal ComponentGroup(EntityGroupData* groupData, ComponentJobSafetyManager safetyManager,
            ArchetypeManager typeManager, EntityDataManager* entityDataManager)
        {
            m_GroupData = groupData;
            m_EntityDataManager = entityDataManager;
            m_Filter = default(ComponentGroupFilter);
            m_SafetyManager = safetyManager;
            ArchetypeManager = typeManager;
            EntityDataManager = entityDataManager;
        }

        internal EntityDataManager* EntityDataManager { get; }

        public bool IsEmptyIgnoreFilter
        {
            get
            {
                for (var match = m_GroupData->FirstMatchingArchetype; match != null; match = match->Next)
                    if (match->Archetype->EntityCount > 0)
                        return false;

                return true;
            }
        }
        
        public ComponentType[] Types
        {
            get
            {
                var types = new List<ComponentType>();
                for (var i = 0; i < m_GroupData->RequiredComponentsCount; ++i)
                    types.Add(m_GroupData->RequiredComponents[i]);

                return types.ToArray();
            }
        }
        internal ArchetypeManager ArchetypeManager { get; }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (DisallowDisposing != null)
                throw new ArgumentException(DisallowDisposing);
#endif

            if (m_CachedState != null)
                m_CachedState.Dispose();

            ResetFilter();
        }
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle GetSafetyHandle(int indexInComponentGroup)
        {
            var type = m_GroupData->RequiredComponents + indexInComponentGroup;
            var isReadOnly = type->AccessModeType == ComponentType.AccessMode.ReadOnly;
            return m_SafetyManager.GetSafetyHandle(type->TypeIndex, isReadOnly);
        }

        internal AtomicSafetyHandle GetBufferSafetyHandle(int indexInComponentGroup)
        {
            var type = m_GroupData->RequiredComponents + indexInComponentGroup;
            return m_SafetyManager.GetBufferSafetyHandle(type->TypeIndex);
        }
#endif

        bool GetIsReadOnly(int indexInComponentGroup)
        {
            var type = m_GroupData->RequiredComponents + indexInComponentGroup;
            var isReadOnly = type->AccessModeType == ComponentType.AccessMode.ReadOnly;
            return isReadOnly;
        }

        public int CalculateLength()
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            return length;
        }

        internal void GetComponentChunkIterator(out int outLength, out ComponentChunkIterator outIterator)
        {
            outLength = ComponentChunkIterator.CalculateLength(m_GroupData->FirstMatchingArchetype, ref m_Filter);
            outIterator = new ComponentChunkIterator(m_GroupData->FirstMatchingArchetype, m_EntityDataManager->GlobalSystemVersion, ref m_Filter);
        }

        internal void GetComponentChunkIterator(out ComponentChunkIterator outIterator)
        {
            outIterator = new ComponentChunkIterator(m_GroupData->FirstMatchingArchetype, m_EntityDataManager->GlobalSystemVersion, ref m_Filter);
        }
        
        internal int GetIndexInComponentGroup(int componentType)
        {
            var componentIndex = 0;
            while (componentIndex < m_GroupData->RequiredComponentsCount && m_GroupData->RequiredComponents[componentIndex].TypeIndex != componentType)
                ++componentIndex;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentIndex >= m_GroupData->RequiredComponentsCount)
                throw new InvalidOperationException( $"Trying to get iterator for {TypeManager.GetType(componentType)} but the required component type was not declared in the EntityGroup.");
#endif
            return componentIndex;
        }
        
        internal void GetComponentDataArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup,
            int length, out ComponentDataArray<T> output) where T : struct, IComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized)
                throw new ArgumentException($"GetComponentDataArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
#endif
            
            iterator.IndexInComponentGroup = indexInComponentGroup;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new ComponentDataArray<T>(iterator, length, GetSafetyHandle(indexInComponentGroup));
#else
			output = new ComponentDataArray<T>(iterator, length);
#endif
        }

        public ComponentDataArray<T> GetComponentDataArray<T>() where T : struct, IComponentData
        {
            Profiler.BeginSample("GetComponentDataArray");
            
            var typeIndex = TypeManager.GetTypeIndex<T>();
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            if (componentType.IsZeroSized)
                throw new ArgumentException($"GetComponentDataArray<{typeof(T)}> cannot be called on zero-sized IComponentData");
#endif
            
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            var indexInComponentGroup = GetIndexInComponentGroup(typeIndex);

            ComponentDataArray<T> res;
            GetComponentDataArray(ref iterator, indexInComponentGroup, length, out res);
            
            Profiler.EndSample();
            return res;
        }

        int ComponentTypeIndex(int indexInComponentGroup)
        {
            return m_GroupData->RequiredComponents[indexInComponentGroup].TypeIndex;
        }

        internal void GetSharedComponentDataArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup,
            int length, out SharedComponentDataArray<T> output) where T : struct, ISharedComponentData
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var typeIndex = ComponentTypeIndex(indexInComponentGroup);
            output = new SharedComponentDataArray<T>(ArchetypeManager.GetSharedComponentDataManager(),
                indexInComponentGroup, iterator, length, m_SafetyManager.GetSafetyHandle(typeIndex, true));
#else
			output = new SharedComponentDataArray<T>(ArchetypeManager.GetSharedComponentDataManager(),
                                                     indexInComponentGroup, iterator, length);
#endif
        }

        public SharedComponentDataArray<T> GetSharedComponentDataArray<T>() where T : struct, ISharedComponentData
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            var indexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>());

            SharedComponentDataArray<T> res;
            GetSharedComponentDataArray(ref iterator, indexInComponentGroup, length, out res);
            return res;
        }

        internal void GetBufferArray<T>(ref ComponentChunkIterator iterator, int indexInComponentGroup, int length,
            out BufferArray<T> output) where T : struct, IBufferElementData
        {
            iterator.IndexInComponentGroup = indexInComponentGroup;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new BufferArray<T>(iterator, length, GetIsReadOnly(indexInComponentGroup),
                GetSafetyHandle(indexInComponentGroup),
                GetBufferSafetyHandle(indexInComponentGroup));
#else
			output = new BufferArray<T>(iterator, length, GetIsReadOnly(indexInComponentGroup));
#endif
        }

        public BufferArray<T> GetBufferArray<T>() where T : struct, IBufferElementData
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);
            var indexInComponentGroup = GetIndexInComponentGroup(TypeManager.GetTypeIndex<T>());

            BufferArray<T> res;
            GetBufferArray(ref iterator, indexInComponentGroup, length, out res);
            return res;
        }

        internal void GetEntityArray(ref ComponentChunkIterator iterator, int length, out EntityArray output)
        {
            iterator.IndexInComponentGroup = 0;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            output = new EntityArray(iterator, length, m_SafetyManager.GetSafetyHandle(TypeManager.GetTypeIndex<Entity>(), true));
#else
			output = new EntityArray(iterator, length);
#endif
        }

        public EntityArray GetEntityArray()
        {
            int length;
            ComponentChunkIterator iterator;
            GetComponentChunkIterator(out length, out iterator);

            EntityArray res;
            GetEntityArray(ref iterator, length, out res);
            return res;
        }
        
        public bool CompareComponents(ComponentType[] componentTypes)
        {
            fixed (ComponentType* ptr = componentTypes)
            {
                return CompareComponents(ptr, componentTypes.Length);
            }
        }

        internal bool CompareComponents(ComponentType* componentTypes, int count)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (var k = 0; k < count; ++k)
                if (componentTypes[k].TypeIndex == TypeManager.GetTypeIndex<Entity>())
                    throw new ArgumentException(
                        "ComponentGroup.CompareComponents may not include typeof(Entity), it is implicit");
#endif

            // ComponentGroups are constructed including the Entity ID
            var requiredCount = m_GroupData->RequiredComponentsCount;
            if (count != requiredCount - 1)
                return false;

            for (var k = 0; k < count; ++k)
            {
                int i;
                for (i = 1; i < requiredCount; ++i)
                    if (m_GroupData->RequiredComponents[i] == componentTypes[k])
                        break;

                if (i == requiredCount)
                    return false;
            }

            return true;
        }

        public void ResetFilter()
        {
            if (m_Filter.Type == FilterType.SharedComponent)
            {
                var filteredCount = m_Filter.Shared.Count;

                var sm = ArchetypeManager.GetSharedComponentDataManager();
                fixed (int* sharedComponentIndexPtr = m_Filter.Shared.SharedComponentIndex)
                {
                    for (var i = 0; i < filteredCount; ++i)
                        sm.RemoveReference(sharedComponentIndexPtr[i]);
                }
            }

            m_Filter.Type = FilterType.None;
        }

        void SetFilter(ref ComponentGroupFilter filter)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            filter.AssertValid();
#endif
            var version = m_Filter.RequiredChangeVersion;
            ResetFilter();
            m_Filter = filter;
            m_Filter.RequiredChangeVersion = version;
        }

        public void SetFilter<SharedComponent1>(SharedComponent1 sharedComponent1)
            where SharedComponent1 : struct, ISharedComponentData
        {
            var sm = ArchetypeManager.GetSharedComponentDataManager();

            var filter = new ComponentGroupFilter();
            filter.Type = FilterType.SharedComponent;
            filter.Shared.Count = 1;
            filter.Shared.IndexInComponentGroup[0] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            filter.Shared.SharedComponentIndex[0] = sm.InsertSharedComponent(sharedComponent1);

            SetFilter(ref filter);
        }

        public void SetFilter<SharedComponent1, SharedComponent2>(SharedComponent1 sharedComponent1,
            SharedComponent2 sharedComponent2)
            where SharedComponent1 : struct, ISharedComponentData
            where SharedComponent2 : struct, ISharedComponentData
        {
            var sm = ArchetypeManager.GetSharedComponentDataManager();

            var filter = new ComponentGroupFilter();
            filter.Type = FilterType.SharedComponent;
            filter.Shared.Count = 2;
            filter.Shared.IndexInComponentGroup[0] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent1>());
            filter.Shared.SharedComponentIndex[0] = sm .InsertSharedComponent(sharedComponent1);

            filter.Shared.IndexInComponentGroup[1] = GetIndexInComponentGroup(TypeManager.GetTypeIndex<SharedComponent2>());
            filter.Shared.SharedComponentIndex[1] = sm.InsertSharedComponent(sharedComponent2);

            SetFilter(ref filter);
        }

        public void SetFilterChanged(ComponentType componentType)
        {
            var filter = new ComponentGroupFilter();
            filter.Type = FilterType.Changed;
            filter.Changed.Count = 1;
            filter.Changed.IndexInComponentGroup[0] = GetIndexInComponentGroup(componentType.TypeIndex);

            SetFilter(ref filter);
        }

        internal void SetFilterChangedRequiredVersion(uint requiredVersion)
        {
            m_Filter.RequiredChangeVersion = requiredVersion;
        }

        public void SetFilterChanged(ComponentType[] componentType)
        {
            if (componentType.Length > ComponentGroupFilter.ChangedFilter.Capacity)
                throw new ArgumentException(
                    $"ComponentGroup.SetFilterChanged accepts a maximum of {ComponentGroupFilter.ChangedFilter.Capacity} component array length");
            if (componentType.Length <= 0)
                throw new ArgumentException(
                    $"ComponentGroup.SetFilterChanged component array length must be larger than 0");

            var filter = new ComponentGroupFilter();
            filter.Type = FilterType.Changed;
            filter.Changed.Count = componentType.Length;
            for (var i = 0; i != componentType.Length; i++)
                filter.Changed.IndexInComponentGroup[i] = GetIndexInComponentGroup(componentType[i].TypeIndex);

            SetFilter(ref filter);
        }

        public void CompleteDependency()
        {
            m_SafetyManager.CompleteDependenciesNoChecks(m_GroupData->ReaderTypes, m_GroupData->ReaderTypesCount,
                m_GroupData->WriterTypes, m_GroupData->WriterTypesCount);
        }

        public JobHandle GetDependency()
        {
            return m_SafetyManager.GetDependency(m_GroupData->ReaderTypes, m_GroupData->ReaderTypesCount,
                m_GroupData->WriterTypes, m_GroupData->WriterTypesCount);
        }

        public void AddDependency(JobHandle job)
        {
            m_SafetyManager.AddDependency(m_GroupData->ReaderTypes, m_GroupData->ReaderTypesCount,
                m_GroupData->WriterTypes, m_GroupData->WriterTypesCount, job);
        }

        public int GetCombinedComponentOrderVersion()
        {
            var version = 0;

            for (var i = 0; i < m_GroupData->RequiredComponentsCount; ++i)
                version += m_EntityDataManager->GetComponentTypeOrderVersion(m_GroupData->RequiredComponents[i].TypeIndex);

            return version;
        }
        

        internal ArchetypeManager GetArchetypeManager()
        {
            return ArchetypeManager;
        }

        internal int CalculateNumberOfChunksWithoutFiltering()
        {
            return ComponentChunkIterator.CalculateNumberOfChunksWithoutFiltering(m_GroupData->FirstMatchingArchetype);
        }
    }
}
