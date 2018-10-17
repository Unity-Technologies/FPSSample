using UnityEngine;

namespace NetworkCompression
{
    public struct RansInputStream : IInputStream
    {
        public RansInputStream(NetworkCompressionModel model, byte[] buffer, int bufferOffset)
        {
            m_Buffer = buffer;
            m_State = (uint)buffer[bufferOffset] | ((uint)buffer[bufferOffset + 1] << 8) | ((uint)buffer[bufferOffset + 2] << 16) | ((uint)buffer[bufferOffset + 3] << 24);
            m_CurrentByteIndex = bufferOffset + 4;
        }

        public void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset)
        {
            this = new RansInputStream(model, buffer, bufferOffset);
        }

        public uint ReadRawBits(int numBits)
        {
            Debug.Assert(numBits >= 0 && numBits <= 32);
            if(numBits > STATE_MIN_BITS)
            {
                int numTopBits = (numBits - STATE_MIN_BITS);
                uint topMask = (1u << numTopBits) - 1u;
                uint value = m_State & topMask;
                m_State >>= numTopBits;

                while (m_State < STATE_MIN_THRESHOLD)
                    m_State = (m_State << 8) | m_Buffer[m_CurrentByteIndex++];

                value = (value << STATE_MIN_BITS) | (m_State & STATE_MIN_MASK);
                m_State >>= STATE_MIN_BITS;

                while (m_State < STATE_MIN_THRESHOLD)
                    m_State = (m_State << 8) | m_Buffer[m_CurrentByteIndex++];

                return value;
            }
            else
            {
                uint mask = (1u << numBits) - 1u;
                uint value = m_State & mask;
                m_State >>= numBits;

                while (m_State < STATE_MIN_THRESHOLD)
                    m_State = (m_State << 8) | m_Buffer[m_CurrentByteIndex++];
                return value;
            }
        }

        public void ReadRawBytes(byte[] dstBuffer, int dstIndex, int count)
        {
            for (int i = 0; i < count; i++)
                dstBuffer[dstIndex + i] = (byte)ReadRawBits(8);
        }

        public void SkipRawBits(int numBits)
        {
            ReadRawBits(numBits);
        }

        public void SkipRawBytes(int count)
        {
            for (int i = 0; i < count; i++)
                SkipRawBits(8);
        }

        public uint ReadPackedNibble(int context)
        {
            uint prob = m_State & 0xFF;

            uint symbol = prob >> 4;
            uint start = symbol << 4;
            uint freq = 16;

            Advance(start, freq);

            return symbol;
        }

        public uint ReadPackedUInt(int context)
        {
            uint bucket = ReadPackedNibble(context);
            uint offset = NetworkCompressionConstants.k_BucketOffsets[bucket];
            int bits = NetworkCompressionConstants.k_BucketSizes[bucket];
            return ReadRawBits(bits) + offset;
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
            return 0;
        }

        public NetworkCompressionModel GetModel()
        {
            return null;
        }

        void Advance(uint start, uint freq)
        {
            m_State = freq * (m_State >> PROB_BITS) + (m_State & PROB_MASK) - start;

            // renormalize
            while(m_State < STATE_MIN_THRESHOLD)
            {
                m_State = (m_State << 8) | m_Buffer[m_CurrentByteIndex++];
            }
        }
        
        byte[] m_Buffer;
        uint m_State;
        int m_CurrentByteIndex;

        const int PROB_BITS = 8;
        const uint PROB_MASK = (1u << PROB_BITS) - 1u;
        const int STATE_MIN_BITS = 23;
        const int STATE_RENORM_BITS = 8;
        const int STATE_MAX_BITS = STATE_MIN_BITS + STATE_RENORM_BITS;
        const uint STATE_MIN_THRESHOLD = 1u << STATE_MIN_BITS;
        const uint STATE_MAX_THRESHOLD = STATE_MIN_THRESHOLD << STATE_RENORM_BITS;
        const uint STATE_MIN_MASK = STATE_MIN_THRESHOLD - 1u;
    }
}