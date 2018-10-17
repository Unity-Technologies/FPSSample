using UnityEngine;
using System.Collections.Generic;

namespace NetworkCompression
{
    // rANS
    public struct RansOutputStream : IOutputStream
    {
        public RansOutputStream(byte[] buffer, int bufferOffset, NetworkCompressionCapture capture)
        {
            m_Entries = new List<uint>();
            m_Buffer = buffer;
            m_BufferOffset = bufferOffset;
        }

        public void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset, NetworkCompressionCapture capture)
        {
            this = new RansOutputStream(buffer, bufferOffset, capture);
        }
        
        public void WriteRawBits(uint value, int numBits)
        {
            Debug.Assert(numBits <= 32);
            if(numBits > STATE_MIN_BITS)
            {
                m_Entries.Add(value & STATE_MIN_MASK);
                value >>= STATE_MIN_BITS;
            }
            Debug.Assert(value < STATE_MIN_THRESHOLD);
            m_Entries.Add(((uint)(numBits << STATE_MIN_BITS) | value));
        }

        public void WriteRawBytes(byte[] value, int srcIndex, int count)
        {
            for (int i = 0; i < count; i++)
                WriteRawBits(value[srcIndex + i], 8);   //TODO: this sucks
        }

        public void WritePackedNibble(uint value, int context)
        {
            Debug.Assert(value < 16);
            uint start = value * 16;
            uint freq = 16;
            m_Entries.Add((uint)((1 << 31) | (start << 8) | freq));
        }

        public void WritePackedUInt(uint value, int context)
        {
            int bucket = NetworkCompressionUtils.CalculateBucket(value);
            uint offset = NetworkCompressionConstants.k_BucketOffsets[bucket];
            int bits = NetworkCompressionConstants.k_BucketSizes[bucket];

            WritePackedNibble((uint)bucket, context);
            if (bits > 0)
                WriteRawBits(value - offset, bits);
        }

        public void WritePackedIntDelta(int value, int baseline, int context)
        {
            WritePackedUIntDelta((uint)value, (uint)baseline, context);
        }

        public void WritePackedUIntDelta(uint value, uint baseline, int context)
        {
            int diff = (int)(baseline - value);
            uint interleaved = (uint)((diff >> 31) ^ (diff << 1));      // interleave negative values between positive values: 0, -1, 1, -2, 2
            WritePackedUInt(interleaved, context);
        }

        public int GetBitPosition2()
        {
            return 0;
        }

        public NetworkCompressionModel GetModel()
        {
            return null;
        }

        public int Flush()
        {
            int numEntries = m_Entries.Count;
            
            uint x = STATE_MIN_THRESHOLD;

            int writePos = m_Buffer.Length;
            for (int i = numEntries - 1; i >= 0; )
            {
                uint entry = m_Entries[i--];
                if((entry & 0x80000000) != 0)
                {
                    // Packed
                    uint start = (uint)((entry >> 8) & 0xFF);
                    uint freq = (uint)(entry & 0xFF);
                    
                    // Renormalize
                    uint x_max = freq << STATE_MIN_BITS;
                    while (x >= x_max)
                    {
                        m_Buffer[--writePos] = (byte)x;
                        x >>= 8;
                    }

                    // Code
                    x = ((x / freq) << PROB_BITS) + (x % freq) + start;
                }
                else
                {
                    // Raw
                    int numBits = (int)(entry >> STATE_MIN_BITS);
                    uint value = entry & STATE_MIN_MASK;
                    if (numBits > STATE_MIN_BITS)
                    {
                        uint x_max2 = STATE_MAX_THRESHOLD >> STATE_MIN_BITS;
                        while (x >= x_max2)
                        {
                            m_Buffer[--writePos] = (byte)x;
                            x >>= 8;
                        }

                        x = (x << STATE_MIN_BITS) | m_Entries[i--];
                        numBits -= STATE_MIN_BITS;
                    }

                    Debug.Assert(value < (1u << numBits));

                    uint x_max = STATE_MAX_THRESHOLD >> numBits;
                    while (x >= x_max)
                    {
                        m_Buffer[--writePos] = (byte)x;
                        x >>= 8;
                    }

                    x = (x << numBits) | value;
                }
            }

            m_Entries.Clear();

            int numBytes = m_Buffer.Length - writePos;
            int compressedSize = 4 + numBytes;

            // Write state and move output bytes to start of buffer
            Debug.Assert(writePos >= compressedSize);
            m_Buffer[m_BufferOffset + 0] = (byte)x;
            m_Buffer[m_BufferOffset + 1] = (byte)(x >> 8);
            m_Buffer[m_BufferOffset + 2] = (byte)(x >> 16);
            m_Buffer[m_BufferOffset + 3] = (byte)(x >> 24);
            for (int i = 0; i < numBytes; i++)
                m_Buffer[m_BufferOffset + 4 + i] = m_Buffer[writePos + i];

            m_BufferOffset += compressedSize;
            return compressedSize;
        }

        List<uint> m_Entries;   //TODO: can we cram this into ushort? We need probabilities to be at least 8bit, so signaling and encoding raw bytes is a little tricky
        byte[] m_Buffer;
        int m_BufferOffset;

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