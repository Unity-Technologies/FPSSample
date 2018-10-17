using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal unsafe struct ChunkAllocator : IDisposable
    {
        private byte* m_FirstChunk;
        private byte* m_LastChunk;
        private int m_LastChunkUsedSize;
        private const int ms_ChunkSize = 64 * 1024;
        private const int ms_ChunkAlignment = 64;

        public void Dispose()
        {
            while (m_FirstChunk != null)
            {
                var nextChunk = ((byte**) m_FirstChunk)[0];
                UnsafeUtility.Free(m_FirstChunk, Allocator.Persistent);
                m_FirstChunk = nextChunk;
            }

            m_LastChunk = null;
        }

        public byte* Allocate(int size, int alignment)
        {
            var alignedChunkSize = (m_LastChunkUsedSize + alignment - 1) & ~(alignment - 1);
            if (m_LastChunk == null || size > ms_ChunkSize - alignedChunkSize)
            {
                // Allocate new chunk
                var newChunk = (byte*) UnsafeUtility.Malloc(ms_ChunkSize, ms_ChunkAlignment, Allocator.Persistent);
                ((byte**) newChunk)[0] = null;
                if (m_LastChunk != null)
                    ((byte**) m_LastChunk)[0] = newChunk;
                else
                    m_FirstChunk = newChunk;
                m_LastChunk = newChunk;
                m_LastChunkUsedSize = sizeof(byte*);
                alignedChunkSize = (m_LastChunkUsedSize + alignment - 1) & ~(alignment - 1);
            }

            var ptr = m_LastChunk + alignedChunkSize;
            m_LastChunkUsedSize = alignedChunkSize + size;
            return ptr;
        }

        public byte* Construct(int size, int alignment, void* src)
        {
            var res = Allocate(size, alignment);
            UnsafeUtility.MemCpy(res, src, size);
            return res;
        }
    }
}
