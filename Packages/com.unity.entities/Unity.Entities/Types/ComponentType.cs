using System;

namespace Unity.Entities
{
    public struct SubtractiveComponent<T>
    {
    }

    public struct ComponentType
    {
        public enum AccessMode
        {
            ReadWrite,
            ReadOnly,
            Subtractive
        }

        public int TypeIndex;
        public AccessMode AccessModeType;
        public int BufferCapacity;
        
        public bool IsSystemStateComponent => TypeManager.IsSystemStateComponent(TypeIndex);
        public bool IsSystemStateSharedComponent => TypeManager.IsSystemStateSharedComponent(TypeIndex);
        public bool IsSharedComponent => TypeManager.IsSharedComponent(TypeIndex);
        public bool IsZeroSized => TypeManager.GetTypeInfo(TypeIndex).IsZeroSized;

        public static ComponentType Create<T>()
        {
            return FromTypeIndex(TypeManager.GetTypeIndex<T>());
        }

        public static ComponentType FromTypeIndex(int typeIndex)
        {
            TypeManager.TypeInfo ct = TypeManager.GetTypeInfo(typeIndex);

            ComponentType type;
            type.TypeIndex = typeIndex;
            type.AccessModeType = AccessMode.ReadWrite;
            type.BufferCapacity = ct.BufferCapacity;
            return type;
        }

        public static ComponentType ReadOnly(Type type)
        {
            ComponentType t = FromTypeIndex(TypeManager.GetTypeIndex(type));
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        public static ComponentType ReadOnly<T>()
        {
            ComponentType t = Create<T>();
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        public static ComponentType Subtractive(Type type)
        {
            ComponentType t = FromTypeIndex(TypeManager.GetTypeIndex(type));
            t.AccessModeType = AccessMode.Subtractive;
            return t;
        }

        public static ComponentType Subtractive<T>()
        {
            ComponentType t = Create<T>();
            t.AccessModeType = AccessMode.Subtractive;
            return t;
        }

        public ComponentType(Type type, AccessMode accessModeType = AccessMode.ReadWrite)
        {
            TypeIndex = TypeManager.GetTypeIndex(type);
            var ct = TypeManager.GetTypeInfo(TypeIndex);
            BufferCapacity = ct.BufferCapacity;
            AccessModeType = accessModeType;
        }

        internal bool RequiresJobDependency
        {
            get
            {
                if (AccessModeType == AccessMode.Subtractive)
                    return false;

                var type = GetManagedType();
                //@TODO: This is wrong... think about Entity array?
                return typeof(IComponentData).IsAssignableFrom(type) || typeof(IBufferElementData).IsAssignableFrom(type);
            }
        }

        public Type GetManagedType()
        {
            return TypeManager.GetType(TypeIndex);
        }

        public static implicit operator ComponentType(Type type)
        {
            return new ComponentType(type, AccessMode.ReadWrite);
        }

        public static bool operator <(ComponentType lhs, ComponentType rhs)
        {
            if (lhs.TypeIndex == rhs.TypeIndex)
                return lhs.BufferCapacity != rhs.BufferCapacity
                    ? lhs.BufferCapacity < rhs.BufferCapacity
                    : lhs.AccessModeType < rhs.AccessModeType;

            return lhs.TypeIndex < rhs.TypeIndex;
        }

        public static bool operator >(ComponentType lhs, ComponentType rhs)
        {
            return rhs < lhs;
        }

        public static bool operator ==(ComponentType lhs, ComponentType rhs)
        {
            return lhs.TypeIndex == rhs.TypeIndex && lhs.BufferCapacity == rhs.BufferCapacity &&
                   lhs.AccessModeType == rhs.AccessModeType;
        }

        public static bool operator !=(ComponentType lhs, ComponentType rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex || lhs.BufferCapacity != rhs.BufferCapacity ||
                   lhs.AccessModeType != rhs.AccessModeType;
        }

        internal static unsafe bool CompareArray(ComponentType* type1, int typeCount1, ComponentType* type2,
            int typeCount2)
        {
            if (typeCount1 != typeCount2)
                return false;
            for (var i = 0; i < typeCount1; ++i)
                if (type1[i] != type2[i])
                    return false;
            return true;
        }

        public bool IsFixedArray => BufferCapacity != -1;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public override string ToString()
        {
            var name = GetManagedType().Name;
            if (IsFixedArray)
                return $"{name}[B {BufferCapacity}]";
            if (AccessModeType == AccessMode.Subtractive)
                return $"{name} [S]";
            if (AccessModeType == AccessMode.ReadOnly)
                return $"{name} [RO]";
            if (TypeIndex == 0 && BufferCapacity == 0)
                return "None";
            return name;
        }
#endif

        public override bool Equals(object obj)
        {
            return obj is ComponentType && (ComponentType) obj == this;
        }

        public override int GetHashCode()
        {
            return (TypeIndex * 5813) ^ BufferCapacity;
        }
    }
}
