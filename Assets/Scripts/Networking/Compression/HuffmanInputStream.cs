using UnityEngine;

namespace NetworkCompression
{
    public struct HuffmanInputStream : IInputStream
    {
        public HuffmanInputStream(NetworkCompressionModel model, byte[] buffer, int bufferOffset)
        {
            m_Model = model;
            m_Buffer = buffer;
            m_CurrentBitIndex = 0;
            m_CurrentByteIndex = bufferOffset;
            m_BitBuffer = 0;
        }

        public void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset)
        {
            this = new HuffmanInputStream(model, buffer, bufferOffset);
        }

        public uint ReadRawBits(int numbits)
        {
            FillBitBuffer();
            return ReadRawBitsInternal(numbits);
        }

        public void ReadRawBytes(byte[] dstBuffer, int dstIndex, int count)
        {
            for (int i = 0; i < count; i++)
                dstBuffer[dstIndex + i] = (byte)ReadRawBits(8);
        }

        public void SkipRawBits(int numbits)
        {
            // TODO: implement this properly
            while (numbits >= 32)
            {
                ReadRawBits(32);
                numbits -= 32;
            }
            ReadRawBits(numbits);
        }

        public void SkipRawBytes(int count)
        {
            SkipRawBits(count * 8);
        }

        public uint ReadPackedNibble(int context)
        {
            FillBitBuffer();
            uint peekMask = (1u << NetworkCompressionConstants.k_MaxHuffmanSymbolLength) - 1u;
            uint peekBits = (uint)m_BitBuffer & peekMask;
            ushort huffmanEntry = m_Model.decodeTable[context, peekBits];
            int symbol = huffmanEntry >> 8;
            int length = huffmanEntry & 0xFF;

            // Skip Huffman bits
            m_BitBuffer >>= length;
            m_CurrentBitIndex -= length;
            return (uint)symbol;
        }

        public uint ReadPackedUInt(int context)
        {
            FillBitBuffer();
            uint peekMask = (1u << NetworkCompressionConstants.k_MaxHuffmanSymbolLength) - 1u;
            uint peekBits = (uint)m_BitBuffer & peekMask;
            ushort huffmanEntry = m_Model.decodeTable[context, peekBits];
            int symbol = huffmanEntry >> 8;
            int length = huffmanEntry & 0xFF;

            // Skip Huffman bits
            m_BitBuffer >>= length;
            m_CurrentBitIndex -= length;
        
            uint offset = NetworkCompressionConstants.k_BucketOffsets[symbol];
            int bits = NetworkCompressionConstants.k_BucketSizes[symbol];
            return ReadRawBitsInternal(bits) + offset;
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
            return m_CurrentByteIndex * 8 - m_CurrentBitIndex;
        }
        
        public NetworkCompressionModel GetModel()
        {
            return m_Model;
        }


        uint ReadRawBitsInternal(int numbits)
        {
            Debug.Assert(numbits >= 0 && numbits <= 32);    //TODO: change back to Debug.Assert
            Debug.Assert(m_CurrentBitIndex >= numbits);
            uint res = (uint)(m_BitBuffer & ((1UL << numbits) - 1UL));
            m_BitBuffer >>= numbits;
            m_CurrentBitIndex -= numbits;
            return res;
        }

        void FillBitBuffer()
        {
            while (m_CurrentBitIndex <= 56)
            {
                m_BitBuffer |= (ulong)m_Buffer[m_CurrentByteIndex++] << m_CurrentBitIndex;
                m_CurrentBitIndex += 8;
            }
        }

        NetworkCompressionModel m_Model;
        byte[] m_Buffer;
        ulong m_BitBuffer;
        int m_CurrentBitIndex;
        int m_CurrentByteIndex;
    }
}