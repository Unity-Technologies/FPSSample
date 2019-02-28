using UnityEngine;

namespace NetworkCompression
{
    public struct HuffmanOutputStream : IOutputStream
    {
        public HuffmanOutputStream(NetworkCompressionModel model, byte[] buffer, int bufferOffset, NetworkCompressionCapture capture)
        {
            m_Model = model;
            m_Buffer = buffer;
            m_BufferOffset = bufferOffset;
            m_CurrentBitIndex = 0;
            m_CurrentByteIndex = bufferOffset;
            m_BitBuffer = 0;
            m_Capture = capture;
        }

        public void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset, NetworkCompressionCapture capture)
        {
            this = new HuffmanOutputStream(model, buffer, bufferOffset, capture);
        }
        
        public void WriteRawBits(uint value, int numbits)
        {
            WriteRawBitsInternal(value, numbits);
            FlushBits();
        }

        unsafe public void WriteRawBytes(byte* value, int count)
        {
            for (int i = 0; i < count; i++)
                WriteRawBits(value[i], 8);  //TODO: only flush every n bytes
        }

        public void WritePackedNibble(uint value, int context)
        {
            if(value >= 16)
                Debug.Assert(false, "Nibble bigger than 15");
            if (m_Capture != null)
                m_Capture.AddNibble(context, value);

            ushort encodeEntry = m_Model.encodeTable[context, value];
            WriteRawBitsInternal((uint)(encodeEntry >> 8), encodeEntry & 0xFF);
            FlushBits();
        }

        public void WritePackedUInt(uint value, int context)
        {
            if (m_Capture != null)
                m_Capture.AddUInt(context, value);

            //int bucket = NetworkCompressionUtils.CalculateBucket(value); // Manually inlined
            int bucket = 0;
            while (bucket + 1 < NetworkCompressionConstants.k_NumBuckets && value >= NetworkCompressionConstants.k_BucketOffsets[bucket + 1])
                bucket++;
            uint offset = NetworkCompressionConstants.k_BucketOffsets[bucket];
            int bits = NetworkCompressionConstants.k_BucketSizes[bucket];
            ushort encodeEntry = m_Model.encodeTable[context, bucket];
            WriteRawBitsInternal((uint)(encodeEntry >> 8), encodeEntry & 0xFF);
            WriteRawBitsInternal(value - offset, bits);
            FlushBits();
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
            return (m_CurrentByteIndex - m_BufferOffset) * 8 - m_CurrentBitIndex;
        }

        public NetworkCompressionModel GetModel()
        {
            return m_Model;
        }

        public int Flush()
        {
            while (m_CurrentBitIndex > 0)
            {
                m_Buffer[m_CurrentByteIndex++] = (byte)m_BitBuffer;
                m_CurrentBitIndex -= 8;
                m_BitBuffer >>= 8;
            }
            m_CurrentBitIndex = 0;
            return m_CurrentByteIndex - m_BufferOffset;
        }

        void WriteRawBitsInternal(uint value, int numbits)
        {
#if UNITY_EDITOR
            Debug.Assert(numbits >= 0 && numbits <= 32);
            Debug.Assert(value < (1UL << numbits));
#endif

            m_BitBuffer |= ((ulong)value << m_CurrentBitIndex);
            m_CurrentBitIndex += numbits;
        }

        void FlushBits()
        {
            while (m_CurrentBitIndex >= 8)
            {
                m_Buffer[m_CurrentByteIndex++] = (byte)m_BitBuffer;
                m_CurrentBitIndex -= 8;
                m_BitBuffer >>= 8;
            }
        }

        NetworkCompressionCapture m_Capture;
        NetworkCompressionModel m_Model;
        byte[] m_Buffer;
        int m_BufferOffset;
        ulong m_BitBuffer;
        int m_CurrentBitIndex;
        int m_CurrentByteIndex;
    }
}