using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Entities
{
    public static unsafe class TypeManager
    {
        public enum TypeCategory
        {
            ComponentData,
            BufferData,
            ISharedComponentData,
            EntityData,
            Class
        }

        public const int MaximumTypesCount = 1024 * 10;
        private static TypeInfo[] s_Types;
        private static volatile int s_Count;
        private static SpinLock s_CreateTypeLock;
        public static int ObjectOffset;
        internal static readonly Type UnityEngineComponentType = typeof(Component);

        private struct StaticTypeLookup<T>
        {
            public static int typeIndex;
        }

        public struct EntityOffsetInfo
        {
            public int Offset;
        }
        
        // https://stackoverflow.com/a/27851610
        static bool IsZeroSizeStruct(Type t)
        {
            return t.IsValueType && !t.IsPrimitive && 
                   t.GetFields((BindingFlags)0x34).All(fi => IsZeroSizeStruct(fi.FieldType));
        }

        public struct TypeInfo
        {
            public TypeInfo(Type type, int size, TypeCategory category, FastEquality.TypeInfo typeInfo, EntityOffsetInfo[] entityOffsets, UInt64 memoryOrdering, int bufferCapacity, int elementSize)
            {
                Type = type;
                SizeInChunk = size;
                Category = category;
                FastEqualityTypeInfo = typeInfo;
                EntityOffsets = entityOffsets;
                MemoryOrdering = memoryOrdering;
                BufferCapacity = bufferCapacity;
                ElementSize = elementSize;
                IsSystemStateSharedComponent = typeof(ISystemStateSharedComponentData).IsAssignableFrom(type);
                IsSystemStateComponent = typeof(ISystemStateComponentData).IsAssignableFrom(type);
            }

            public readonly Type Type;
            // Note that this includes internal capacity and header overhead for buffers.
            public readonly int SizeInChunk;
            // Normally the same as SizeInChunk (for components), but for buffers means size of an individual element.
            public readonly int ElementSize;
            public readonly int BufferCapacity;
            public readonly FastEquality.TypeInfo FastEqualityTypeInfo;
            public readonly TypeCategory Category;
            public readonly EntityOffsetInfo[] EntityOffsets;
            public readonly UInt64 MemoryOrdering;
            public readonly bool IsSystemStateSharedComponent;
            public readonly bool IsSystemStateComponent;
            public bool IsZeroSized => SizeInChunk == 0;
        }

        // TODO: this creates a dependency on UnityEngine, but makes splitting code in separate assemblies easier. We need to remove it during the biggere refactor.
        private struct ObjectOffsetType
        {
#pragma warning disable 0169 // "never used" warning
            private void* v0;
            private void* v1;
#pragma warning restore 0169
        }

        public static void Initialize()
        {
            if (s_Types != null)
                return;

            ObjectOffset = UnsafeUtility.SizeOf<ObjectOffsetType>();
            s_CreateTypeLock = new SpinLock();
            s_Types = new TypeInfo[MaximumTypesCount];
            s_Count = 0;

            s_Types[s_Count++] = new TypeInfo(null, 0, TypeCategory.ComponentData, FastEquality.TypeInfo.Null, null, 0, -1, 0);
            // This must always be first so that Entity is always index 0 in the archetype
            s_Types[s_Count++] = new TypeInfo(typeof(Entity), sizeof(Entity), TypeCategory.EntityData,
            FastEquality.CreateTypeInfo(typeof(Entity)), EntityRemapUtility.CalculateEntityOffsets(typeof(Entity)), 0, -1, sizeof(Entity));
        }


        public static int GetTypeIndex<T>()
        {
            var typeIndex = StaticTypeLookup<T>.typeIndex;
            if (typeIndex != 0)
                return typeIndex;

            typeIndex = GetTypeIndex(typeof(T));
            StaticTypeLookup<T>.typeIndex = typeIndex;
            return typeIndex;
        }

        public static int GetTypeIndex(Type type)
        {
            var index = FindTypeIndex(type, s_Count);
            return index != -1 ? index : CreateTypeIndexThreadSafe(type);
        }

        private static int FindTypeIndex(Type type, int count)
        {
            for (var i = 0; i != count; i++)
            {
                var c = s_Types[i];
                if (c.Type == type)
                    return i;
            }

            return -1;
        }

#if UNITY_EDITOR
        public static int TypesCount => s_Count;

        public static IEnumerable<TypeInfo> AllTypes()
        {
            return Enumerable.Take(s_Types, s_Count);
        }
#endif //UNITY_EDITOR

        private static int CreateTypeIndexThreadSafe(Type type)
        {
            var lockTaken = false;
            try
            {
                s_CreateTypeLock.Enter(ref lockTaken);

                // After taking the lock, make sure the type hasn't been created
                // after doing the non-atomic FindTypeIndex
                var index = FindTypeIndex(type, s_Count);
                if (index != -1)
                    return index;

                var componentType = BuildComponentType(type);

                index = s_Count++;
                s_Types[index] = componentType;

                return index;
            }
            finally
            {
                if (lockTaken)
                    s_CreateTypeLock.Exit(true);
            }
        }

        static UInt64 CalculateMemoryOrdering(Type type)
        {
            if (type == typeof(Entity))
                return 0;

            var hash = new System.Security.Cryptography.SHA1Managed().ComputeHash(System.Text.Encoding.UTF8.GetBytes(type.AssemblyQualifiedName));
            var hash64 = new byte[8];
            Array.Copy(hash, 0, hash64, 0, 8);

            UInt64 result = 0;
            for (int i = 0; i < 8; ++i)
            {
                result = result * 256 + hash64[i];
            }
            return (result != 0) ? result : 1;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly Type[] s_SingularInterfaces =
        {
            typeof(IComponentData),
            typeof(IBufferElementData),
            typeof(ISharedComponentData),
        };
#endif

        public static TypeInfo BuildComponentType(Type type)
        {
            var componentSize = 0;
            TypeCategory category;
            var typeInfo = FastEquality.TypeInfo.Null;
            EntityOffsetInfo[] entityOffsets = null;
            int bufferCapacity = -1;
            var memoryOrdering = CalculateMemoryOrdering(type);
            int elementSize = 0;
            if (typeof(IComponentData).IsAssignableFrom(type))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an IComponentData, and thus must be a struct.");
                if (!UnsafeUtility.IsBlittable(type))
                    throw new ArgumentException(
                        $"{type} is an IComponentData, and thus must be blittable (No managed object is allowed on the struct).");
#endif

                category = TypeCategory.ComponentData;
                if (TypeManager.IsZeroSizeStruct(type))
                    componentSize = 0;
                else
                    componentSize = UnsafeUtility.SizeOf(type);
                
                typeInfo = FastEquality.CreateTypeInfo(type);
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);
            }
            else if (typeof(IBufferElementData).IsAssignableFrom(type))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an IBufferElementData, and thus must be a struct.");
                if (!UnsafeUtility.IsBlittable(type))
                    throw new ArgumentException(
                        $"{type} is an IBufferElementData, and thus must be blittable (No managed object is allowed on the struct).");
#endif

                category = TypeCategory.BufferData;
                elementSize = UnsafeUtility.SizeOf(type);

                var capacityAttribute = (InternalBufferCapacityAttribute) type.GetCustomAttribute(typeof(InternalBufferCapacityAttribute));
                if (capacityAttribute != null)
                    bufferCapacity = capacityAttribute.Capacity;
                else
                    bufferCapacity = 128 / elementSize; // Rather than 2*cachelinesize, to make it cross platform deterministic

                componentSize = sizeof(BufferHeader) + bufferCapacity * elementSize;
                typeInfo = FastEquality.CreateTypeInfo(type);
                entityOffsets = EntityRemapUtility.CalculateEntityOffsets(type);
             }
            else if (typeof(ISharedComponentData).IsAssignableFrom(type))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!type.IsValueType)
                    throw new ArgumentException($"{type} is an ISharedComponentData, and thus must be a struct.");
#endif

                category = TypeCategory.ISharedComponentData;
                typeInfo = FastEquality.CreateTypeInfo(type);
            }
            else if (type.IsClass)
            {
                category = TypeCategory.Class;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (type.FullName == "Unity.Entities.GameObjectEntity")
                    throw new ArgumentException(
                        "GameObjectEntity can not be used from EntityManager. The component is ignored when creating entities for a GameObject.");
#endif
            }
            else
            {
                throw new ArgumentException($"'{type}' is not a valid component");
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            {
                int typeCount = 0;
                foreach (Type t in s_SingularInterfaces)
                {
                    if (t.IsAssignableFrom(type))
                        ++typeCount;
                }

                if (typeCount > 1)
                    throw new ArgumentException($"Component {type} can only implement one of IComponentData, ISharedComponentData and IBufferElementData");
            }
#endif
            return new TypeInfo(type, componentSize, category, typeInfo, entityOffsets, memoryOrdering, bufferCapacity, elementSize > 0 ? elementSize : componentSize);
        }

        public static TypeInfo GetTypeInfo(int typeIndex)
        {
            return s_Types[typeIndex];
        }

        public static TypeInfo GetTypeInfo<T>()
        {
            return s_Types[GetTypeIndex<T>()];
        }

        public static Type GetType(int typeIndex)
        {
            return s_Types[typeIndex].Type;
        }

        public static int GetTypeCount()
        {
            return s_Count;
        }

        public static bool IsSystemStateComponent(int typeIndex) => GetTypeInfo(typeIndex).IsSystemStateComponent;
        public static bool IsSystemStateSharedComponent(int typeIndex) => GetTypeInfo(typeIndex).IsSystemStateSharedComponent;
        public static bool IsSharedComponent(int typeIndex) => TypeCategory.ISharedComponentData == GetTypeInfo(typeIndex).Category;
    }
}
