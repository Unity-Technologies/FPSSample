using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal class ComponentGroupArrayStaticCache
    {
        public readonly Type CachedType;
        internal readonly int ComponentCount;
        internal readonly int ComponentDataCount;
        internal readonly int[] ComponentFieldOffsets;
        internal readonly ComponentGroup ComponentGroup;

        internal readonly ComponentType[] ComponentTypes;
        internal readonly ComponentJobSafetyManager SafetyManager;

        public ComponentGroupArrayStaticCache(Type type, EntityManager entityManager, ComponentSystemBase system)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var componentFieldOffsetsBuilder = new List<int>();
            var componentTypesBuilder = new List<ComponentType>();

            var componentDataFieldOffsetsBuilder = new List<int>();
            var componentDataTypesBuilder = new List<ComponentType>();

            var subtractiveComponentTypesBuilder = new List<ComponentType>();

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                var offset = UnsafeUtility.GetFieldOffset(field);

                if (fieldType.IsPointer)
                {
                    var isReadOnly = field.GetCustomAttributes(typeof(ReadOnlyAttribute), true).Length != 0;
                    var accessMode =
                        isReadOnly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite;

                    var elementType = fieldType.GetElementType();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (!typeof(IComponentData).IsAssignableFrom(elementType) && elementType != typeof(Entity))
                        throw new ArgumentException(
                            $"{type}.{field.Name} is a pointer type but not a IComponentData. Only IComponentData or Entity may be a pointer type for enumeration.");
#endif
                    componentDataFieldOffsetsBuilder.Add(offset);
                    componentDataTypesBuilder.Add(new ComponentType(elementType, accessMode));
                }
                else if (fieldType.IsSubclassOf(TypeManager.UnityEngineComponentType))
                {
                    componentFieldOffsetsBuilder.Add(offset);
                    componentTypesBuilder.Add(fieldType);
                }
                else if (fieldType.IsGenericType &&
                         fieldType.GetGenericTypeDefinition() == typeof(SubtractiveComponent<>))
                {
                    subtractiveComponentTypesBuilder.Add(ComponentType.Subtractive(fieldType.GetGenericArguments()[0]));
                }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                else if (typeof(IComponentData).IsAssignableFrom(fieldType))
                {
                    throw new ArgumentException(
                        $"{type}.{field.Name} must be an unsafe pointer to the {fieldType}. Like this: {fieldType}* {field.Name};");
                }
                else
                {
                    throw new ArgumentException($"{type}.{field.Name} can not be used in a component enumerator");
                }
#endif
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (componentTypesBuilder.Count + componentDataTypesBuilder.Count > ComponentGroupArrayData.kMaxStream)
                throw new ArgumentException(
                    $"{type} has too many component references. A ComponentGroup Array can have up to {ComponentGroupArrayData.kMaxStream}.");
#endif

            ComponentDataCount = componentDataTypesBuilder.Count;
            ComponentCount = componentTypesBuilder.Count;

            componentDataTypesBuilder.AddRange(componentTypesBuilder);
            componentDataTypesBuilder.AddRange(subtractiveComponentTypesBuilder);
            ComponentTypes = componentDataTypesBuilder.ToArray();

            componentDataFieldOffsetsBuilder.AddRange(componentFieldOffsetsBuilder);
            ComponentFieldOffsets = componentDataFieldOffsetsBuilder.ToArray();

            ComponentGroup = system.GetComponentGroupInternal(ComponentTypes);
            SafetyManager = entityManager.ComponentJobSafetyManager;

            CachedType = type;
        }
    }

    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    internal unsafe struct ComponentGroupArrayData
    {
        public const int kMaxStream = 6;

        private struct ComponentGroupStream
        {
            public byte* CachedPtr;
            public int SizeOf;
            public ushort FieldOffset;
            public ushort TypeIndexInArchetype;
        }

        private fixed byte m_Caches[16 * kMaxStream];

        private readonly int m_ComponentDataCount;
        private readonly int m_ComponentCount;

        // The following 3 fields must not be renamed, unless JobReflectionData.cpp is changed accordingly.
        // TODO: make JobDebugger logic more solid, either by using codegen proxies or attributes.
        public readonly int m_Length;
        public readonly int m_MinIndex;
        public readonly int m_MaxIndex;

        public int CacheBeginIndex;
        public int CacheEndIndex;

        private ComponentChunkIterator m_ChunkIterator;
        private fixed int m_IndexInComponentGroup[kMaxStream];
        private fixed bool m_IsWriting[kMaxStream];

        // The following fields must not be renamed, unless JobReflectionData.cpp is changed accordingly.
        // TODO: make JobDebugger logic more solid, either by using codegen proxies or attributes.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly int m_SafetyReadOnlyCount;
        private readonly int m_SafetyReadWriteCount;
#pragma warning disable 414
        private AtomicSafetyHandle m_Safety0;
        private AtomicSafetyHandle m_Safety1;
        private AtomicSafetyHandle m_Safety2;
        private AtomicSafetyHandle m_Safety3;
        private AtomicSafetyHandle m_Safety4;
        private AtomicSafetyHandle m_Safety5;
#pragma warning restore
#endif

        [NativeSetClassTypeToNullOnSchedule] private readonly ArchetypeManager m_ArchetypeManager;

        public ComponentGroupArrayData(ComponentGroupArrayStaticCache staticCache)
        {
            var length = 0;
            staticCache.ComponentGroup.GetComponentChunkIterator(out length, out m_ChunkIterator);
            m_ChunkIterator.IndexInComponentGroup = 0;

            m_Length = length;
            m_MinIndex = 0;
            m_MaxIndex = length - 1;

            CacheBeginIndex = 0;
            CacheEndIndex = 0;
            m_ArchetypeManager = staticCache.ComponentGroup.GetArchetypeManager();

            m_ComponentDataCount = staticCache.ComponentDataCount;
            m_ComponentCount = staticCache.ComponentCount;

            fixed (int* indexInComponentGroup = m_IndexInComponentGroup)
            fixed (byte* cacheBytes = m_Caches)
            fixed (bool* isWriting = m_IsWriting)
            {
                var streams = (ComponentGroupStream*) cacheBytes;

                for (var i = 0; i < staticCache.ComponentDataCount + staticCache.ComponentCount; i++)
                {
                    indexInComponentGroup[i] = staticCache.ComponentGroup.GetIndexInComponentGroup(staticCache.ComponentTypes[i].TypeIndex);
                    streams[i].FieldOffset = (ushort) staticCache.ComponentFieldOffsets[i];
                    isWriting[i] = staticCache.ComponentTypes[i].AccessModeType == ComponentType.AccessMode.ReadWrite;
                }
            }
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety0 = new AtomicSafetyHandle();
            m_Safety1 = new AtomicSafetyHandle();
            m_Safety2 = new AtomicSafetyHandle();
            m_Safety3 = new AtomicSafetyHandle();
            m_Safety4 = new AtomicSafetyHandle();
            m_Safety5 = new AtomicSafetyHandle();
            Assert.AreEqual(6, kMaxStream);

            m_SafetyReadWriteCount = 0;
            m_SafetyReadOnlyCount = 0;
            var safetyManager = staticCache.SafetyManager;
            fixed (AtomicSafetyHandle* safety = &m_Safety0)
            {
                for (var i = 0; i != staticCache.ComponentTypes.Length; i++)
                {
                    var type = staticCache.ComponentTypes[i];
                    if (type.AccessModeType != ComponentType.AccessMode.ReadOnly)
                        continue;

                    safety[m_SafetyReadOnlyCount] = safetyManager.GetSafetyHandle(type.TypeIndex, true);
                    m_SafetyReadOnlyCount++;
                }

                for (var i = 0; i != staticCache.ComponentTypes.Length; i++)
                {
                    var type = staticCache.ComponentTypes[i];
                    if (type.AccessModeType != ComponentType.AccessMode.ReadWrite)
                        continue;

                    safety[m_SafetyReadOnlyCount + m_SafetyReadWriteCount] =
                        safetyManager.GetSafetyHandle(type.TypeIndex, false);
                    m_SafetyReadWriteCount++;
                }
            }
#endif
        }

        public void UpdateCache(int index)
        {
            ComponentChunkCache cache;

            m_ChunkIterator.MoveToEntityIndex(index);

            fixed (int* indexInComponentGroup = m_IndexInComponentGroup)
            fixed (byte* cacheBytes = m_Caches)
            fixed (bool* isWriting = m_IsWriting)
            {
                var streams = (ComponentGroupStream*) cacheBytes;
                var totalCount = m_ComponentDataCount + m_ComponentCount;
                for (var i = 0; i < totalCount; i++)
                {
                    m_ChunkIterator.UpdateCacheToCurrentChunk(out cache, isWriting[i], indexInComponentGroup[i]);
                    CacheBeginIndex = cache.CachedBeginIndex;
                    CacheEndIndex = cache.CachedEndIndex;

                    int indexInArcheType = m_ChunkIterator.GetIndexInArchetypeFromCurrentChunk(indexInComponentGroup[i]);

                    streams[i].SizeOf = cache.CachedSizeOf;
                    streams[i].CachedPtr = (byte*) cache.CachedPtr;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (indexInArcheType > ushort.MaxValue)
                        throw new ArgumentException(
                            $"There is a maximum of {ushort.MaxValue} components on one entity.");
#endif
                    streams[i].TypeIndexInArchetype = (ushort) indexInArcheType;
                }
            }
        }

        public void CheckAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            fixed (AtomicSafetyHandle* safety = &m_Safety0)
            {
                for (var i = 0; i < m_SafetyReadOnlyCount; i++)
                    AtomicSafetyHandle.CheckReadAndThrow(safety[i]);

                for (var i = m_SafetyReadOnlyCount; i < m_SafetyReadOnlyCount + m_SafetyReadWriteCount; i++)
                    AtomicSafetyHandle.CheckWriteAndThrow(safety[i]);
            }
#endif
        }

        public void PatchPtrs(int index, byte* valuePtr)
        {
            fixed (byte* cacheBytes = m_Caches)
            {
                var streams = (ComponentGroupStream*) cacheBytes;
                for (var i = 0; i != m_ComponentDataCount; i++)
                {
                    var componentPtr = (void*) (streams[i].CachedPtr + streams[i].SizeOf * index);
                    var valuePtrOffsetted = (void**) (valuePtr + streams[i].FieldOffset);

                    *valuePtrOffsetted = componentPtr;
                }
            }
        }

        [BurstDiscard]
        public void PatchManagedPtrs(int index, byte* valuePtr)
        {
            fixed (byte* cacheBytes = m_Caches)
            {
                var streams = (ComponentGroupStream*) cacheBytes;
                for (var i = m_ComponentDataCount; i != m_ComponentDataCount + m_ComponentCount; i++)
                {
                    var component = m_ChunkIterator.GetManagedObject(m_ArchetypeManager,
                        streams[i].TypeIndexInArchetype, CacheBeginIndex, index);
                    var valuePtrOffsetted = valuePtr + streams[i].FieldOffset;
                    UnsafeUtility.CopyObjectAddressToPtr(component, valuePtrOffsetted);
                }
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [BurstDiscard]
        public void FailOutOfRangeError(int index)
        {
            /*
            //@TODO: Make error message utility and share with NativeArray...
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
                throw new IndexOutOfRangeException(string.Format(
                    "Index {0} is out of restricted component group array range [{1}...{2}] in ReadWriteBuffer.\n" +
                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                    "You can use double buffering strategies to avoid race conditions due to " +
                    "reading & writing in parallel to the same elements from a job.",
                    index, m_MinIndex, m_MaxIndex));
*/
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{m_Length}' Length.");
        }
#endif
    }

    public struct ComponentGroupArray<T> : IDisposable where T : struct
    {
        internal ComponentGroupArrayData m_Data;

        internal ComponentGroupArray(ComponentGroupArrayStaticCache cache)
        {
            m_Data = new ComponentGroupArrayData(cache);
        }

        public void Dispose()
        {
        }

        public int Length => m_Data.m_Length;

        public unsafe T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_Data.CheckAccess();
                if (index < m_Data.m_MinIndex || index > m_Data.m_MaxIndex)
                    m_Data.FailOutOfRangeError(index);
#endif

                if (index < m_Data.CacheBeginIndex || index >= m_Data.CacheEndIndex)
                    m_Data.UpdateCache(index);

                var value = default(T);
                var valuePtr = (byte*) UnsafeUtility.AddressOf(ref value);
                m_Data.PatchPtrs(index, valuePtr);
                m_Data.PatchManagedPtrs(index, valuePtr);
                return value;
            }
        }

        public ComponentGroupEnumerator<T> GetEnumerator()
        {
            return new ComponentGroupEnumerator<T>(m_Data);
        }

        public unsafe struct ComponentGroupEnumerator<U> : IEnumerator<U> where U : struct
        {
            private ComponentGroupArrayData m_Data;
            private int m_Index;

            internal ComponentGroupEnumerator(ComponentGroupArrayData arrayData)
            {
                m_Data = arrayData;
                m_Index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                m_Index++;

                if (m_Index >= m_Data.CacheBeginIndex && m_Index < m_Data.CacheEndIndex)
                    return true;

                if (m_Index >= m_Data.m_Length)
                    return false;

                m_Data.CheckAccess();
                m_Data.UpdateCache(m_Index);

                return true;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public U Current
            {
                get
                {
                    m_Data.CheckAccess();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (m_Index < m_Data.m_MinIndex || m_Index > m_Data.m_MaxIndex)
                        m_Data.FailOutOfRangeError(m_Index);
#endif

                    var value = default(U);
                    var valuePtr = (byte*) UnsafeUtility.AddressOf(ref value);
                    m_Data.PatchPtrs(m_Index, valuePtr);
                    m_Data.PatchManagedPtrs(m_Index, valuePtr);
                    return value;
                }
            }

            object IEnumerator.Current => Current;
        }
    }
}
