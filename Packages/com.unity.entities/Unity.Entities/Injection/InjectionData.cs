using System;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal struct InjectionData
    {
        public ComponentType ComponentType;
        public int IndexInComponentGroup;
        public readonly bool IsReadOnly;
        public readonly int FieldOffset;

        public InjectionData(FieldInfo field, Type genericType, bool isReadOnly)
        {
            IndexInComponentGroup = -1;
            FieldOffset = UnsafeUtility.GetFieldOffset(field);

            var accessMode = isReadOnly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite;
            ComponentType = new ComponentType(genericType, accessMode);
            IsReadOnly = isReadOnly;
        }
    }
}
