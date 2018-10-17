using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [NativeContainer]
    public unsafe struct BufferFromEntity<T> where T : struct, IBufferElementData
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly AtomicSafetyHandle m_Safety;
        private readonly AtomicSafetyHandle m_ArrayInvalidationSafety;
#endif
        [NativeDisableUnsafePtrRestriction] private readonly EntityDataManager* m_Entities;
        private readonly int m_TypeIndex;
        private readonly bool m_IsReadOnly;
        readonly uint                    m_GlobalSystemVersion;
        int                              m_TypeLookupCache;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal BufferFromEntity(int typeIndex, EntityDataManager* entityData, bool isReadOnly,
            AtomicSafetyHandle safety, AtomicSafetyHandle arrayInvalidationSafety)
        {
            m_Safety = safety;
            m_ArrayInvalidationSafety = arrayInvalidationSafety;
            m_TypeIndex = typeIndex;
            m_Entities = entityData;
            m_IsReadOnly = isReadOnly;
            m_TypeLookupCache = 0;
            m_GlobalSystemVersion = entityData->GlobalSystemVersion;

            if (TypeManager.GetTypeInfo(m_TypeIndex).Category != TypeManager.TypeCategory.BufferData)
                throw new ArgumentException(
                    $"GetComponentBufferArray<{typeof(T)}> must be IBufferElementData");
        }
#else
        internal BufferFromEntity(int typeIndex, EntityDataManager* entityData, bool isReadOnly)
        {
            m_TypeIndex = typeIndex;
            m_Entities = entityData;
            m_IsReadOnly = isReadOnly;
            m_TypeLookupCache = 0;
            m_GlobalSystemVersion = entityData->GlobalSystemVersion;
        }
#endif

        public bool Exists(Entity entity)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            //@TODO: out of bounds index checks...

            return m_Entities->HasComponent(entity, m_TypeIndex);
        }

        public DynamicBuffer<T> this[Entity entity]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Note that this check is only for the lookup table into the entity manager
                // The native array performs the actual read only / write only checks
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

                m_Entities->AssertEntityHasComponent(entity, m_TypeIndex);
#endif

                // TODO(dep): We don't really have a way to mark the native array as read only.
                BufferHeader* header = (BufferHeader*) m_Entities->GetComponentDataWithTypeRW(entity, m_TypeIndex, m_GlobalSystemVersion, ref m_TypeLookupCache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                return new DynamicBuffer<T>(header, m_Safety, m_ArrayInvalidationSafety);
#else
                return new DynamicBuffer<T>(header);
#endif
            }
        }
    }
}
