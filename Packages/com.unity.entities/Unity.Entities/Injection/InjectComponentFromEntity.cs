using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal struct InjectFromEntityData
    {
        private readonly InjectionData[] m_InjectComponentDataFromEntity;
        private readonly InjectionData[] m_InjectBufferFromEntity;

        public InjectFromEntityData(InjectionData[] componentDataFromEntity, InjectionData[] bufferFromEntity)
        {
            m_InjectComponentDataFromEntity = componentDataFromEntity;
            m_InjectBufferFromEntity = bufferFromEntity;
        }

        public static bool SupportsInjections(FieldInfo field)
        {
            if (field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(ComponentDataFromEntity<>))
                return true;
            if (field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(BufferFromEntity<>))
                return true;
            return false;
        }

        public static void CreateInjection(FieldInfo field, EntityManager entityManager,
            List<InjectionData> componentDataFromEntity, List<InjectionData> bufferFromEntity)
        {
            var isReadOnly = field.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length != 0;

            if (field.FieldType.IsGenericType &&
                field.FieldType.GetGenericTypeDefinition() == typeof(ComponentDataFromEntity<>))
            {
                var injection = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                componentDataFromEntity.Add(injection);
            }
            else if (field.FieldType.IsGenericType &&
                     field.FieldType.GetGenericTypeDefinition() == typeof(BufferFromEntity<>))
            {
                var injection = new InjectionData(field, field.FieldType.GetGenericArguments()[0], isReadOnly);
                bufferFromEntity.Add(injection);
            }
            else
            {
                ComponentSystemInjection.ThrowUnsupportedInjectException(field);
            }
        }

        public unsafe void UpdateInjection(byte* pinnedSystemPtr, EntityManager entityManager)
        {
            for (var i = 0; i != m_InjectComponentDataFromEntity.Length; i++)
            {
                var array = entityManager.GetComponentDataFromEntity<ProxyComponentData>(
                    m_InjectComponentDataFromEntity[i].ComponentType.TypeIndex,
                    m_InjectComponentDataFromEntity[i].IsReadOnly);
                UnsafeUtility.CopyStructureToPtr(ref array,
                    pinnedSystemPtr + m_InjectComponentDataFromEntity[i].FieldOffset);
            }

            for (var i = 0; i != m_InjectBufferFromEntity.Length; i++)
            {
                var array = entityManager.GetBufferFromEntity<ProxyBufferElementData>(
                    m_InjectBufferFromEntity[i].ComponentType.TypeIndex,
                    m_InjectBufferFromEntity[i].IsReadOnly);
                UnsafeUtility.CopyStructureToPtr(ref array,
                    pinnedSystemPtr + m_InjectBufferFromEntity[i].FieldOffset);
            }
        }

        public void ExtractJobDependencyTypes(ComponentSystemBase system)
        {
            if (m_InjectComponentDataFromEntity != null)
                foreach (var injection in m_InjectComponentDataFromEntity)
                    system.AddReaderWriter(injection.ComponentType);

            if (m_InjectBufferFromEntity != null)
                foreach (var injection in m_InjectBufferFromEntity)
                    system.AddReaderWriter(injection.ComponentType);
        }
    }
}
