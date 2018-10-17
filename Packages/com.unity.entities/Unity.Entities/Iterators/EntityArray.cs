using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct EntityArray
    {
        private ComponentChunkIterator m_Iterator;
        private ComponentChunkCache m_Cache;

        private readonly int m_Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly AtomicSafetyHandle m_Safety;
#endif
        public int Length => m_Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal EntityArray(ComponentChunkIterator iterator, int length, AtomicSafetyHandle safety)
#else
        internal unsafe EntityArray(ComponentChunkIterator iterator, int length)
#endif
        {
            m_Length = length;
            m_Iterator = iterator;
            m_Cache = default(ComponentChunkCache);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = safety;
#endif
        }

        public Entity this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                if ((uint)index >= (uint)m_Length)
                    FailOutOfRangeError(index);
#endif

                if (index < m_Cache.CachedBeginIndex || index >= m_Cache.CachedEndIndex)
                    m_Iterator.MoveToEntityIndexAndUpdateCache(index, out m_Cache, false);

                return UnsafeUtility.ReadArrayElement<Entity>(m_Cache.CachedPtr, index);
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private void FailOutOfRangeError(int index)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
        }
#endif

        public NativeArray<Entity> GetChunkArray(int startIndex, int maxCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (startIndex < 0)
                FailOutOfRangeError(startIndex);
            else if (startIndex + maxCount > m_Length)
                FailOutOfRangeError(startIndex + maxCount);
#endif


            m_Iterator.MoveToEntityIndexAndUpdateCache(startIndex, out m_Cache, false);

            void* ptr = (byte*) m_Cache.CachedPtr + startIndex * m_Cache.CachedSizeOf;
            var count = Math.Min(maxCount, m_Cache.CachedEndIndex - startIndex);

            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>(ptr, count, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, m_Safety);
#endif

            return arr;
        }

        public void CopyTo(NativeSlice<Entity> dst, int startIndex = 0)
        {
            var copiedCount = 0;
            while (copiedCount < dst.Length)
            {
                var chunkArray = GetChunkArray(startIndex + copiedCount, dst.Length - copiedCount);
                dst.Slice(copiedCount, chunkArray.Length).CopyFrom(chunkArray);

                copiedCount += chunkArray.Length;
            }
        }

        public Entity[] ToArray()
        {
            var array = new Entity[Length];
            for (var i = 0; i != array.Length; i++)
                array[i] = this[i];
            return array;
        }
    }
}
