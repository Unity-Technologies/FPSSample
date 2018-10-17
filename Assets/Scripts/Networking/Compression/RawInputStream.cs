using UnityEngine;

namespace NetworkCompression
{
    public struct RawInputStream : IInputStream
    {
        public RawInputStream(byte[] buffer, int bufferOffset)
        {
            m_Buffer = buffer;
            m_BufferOffset = bufferOffset;
            m_CurrentByteIndex = bufferOffset;
        }

        public void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset)
        {
            this = new RawInputStream(buffer, bufferOffset);
        }

        public uint ReadRawBits(int numbits)
        {
            uint value = 0;
            for (int i = 0; i < numbits; i += 8)
                value |= (uint)m_Buffer[m_CurrentByteIndex++] << i;
            return value;
        }

        public void ReadRawBytes(byte[] dstBuffer, int dstIndex, int count)
        {
            for (int i = 0; i < count; i++)
                dstBuffer[dstIndex + i] = m_Buffer[m_CurrentByteIndex + i];
            m_CurrentByteIndex += count;
        }

        public void SkipRawBits(int numbits)
        {
            m_CurrentByteIndex += (numbits + 7) >> 3;
        }

        public void SkipRawBytes(int count)
        {
            m_CurrentByteIndex += count;
        }

        public uint ReadPackedNibble(int context)
        {
            return m_Buffer[m_CurrentByteIndex++];
        }

        public uint ReadPackedUInt(int context)
        {
            uint value = (uint)m_Buffer[m_CurrentByteIndex + 0] | ((uint)m_Buffer[m_CurrentByteIndex + 1] << 8) | ((uint)m_Buffer[m_CurrentByteIndex + 2] << 16) | ((uint)m_Buffer[m_CurrentByteIndex + 3] << 24);
            m_CurrentByteIndex += 4;
            return value;
        }

        public int ReadPackedIntDelta(int baseline, int context)
        {
            return (int)ReadPackedUIntDelta((uint)baseline, context);
        }

        public uint ReadPackedUIntDelta(uint baseline, int context)
        {
            uint folded = ReadPackedUInt(context);
            uint delta = (folded >> 1) ^ (uint)-(int)(folded & 1);    // Deinterleave values from [0, -1, 1, -2, 2...] to [..., -2, -1, -0, 1, 2, ...]
            return baseline - delta;
        }

        public int GetBitPosition2()
        {
            return (m_CurrentByteIndex - m_BufferOffset) * 8;
        }

        public NetworkCompressionModel GetModel()
        {
            return null;
        }
        
        byte[] m_Buffer;
        int m_BufferOffset;
        int m_CurrentByteIndex;
    }
}