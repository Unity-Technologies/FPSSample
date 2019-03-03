using UnityEngine;

namespace NetworkCompression
{
    public struct RawOutputStream : IOutputStream
    {
        public RawOutputStream(byte[] buffer, int bufferOffset, NetworkCompressionCapture capture)
        {
            m_Buffer = buffer;
            m_BufferOffset = bufferOffset;
            m_CurrentByteIndex = bufferOffset;
            m_Capture = capture;
        }

        public void Initialize(NetworkCompressionModel model, byte[] buffer, int bufferOffset, NetworkCompressionCapture capture)
        {
            this = new RawOutputStream(buffer, bufferOffset, capture);
        }
        
        public void WriteRawBits(uint value, int numbits)
        {
            for(int i = 0; i < numbits; i += 8)
            {
                m_Buffer[m_CurrentByteIndex++] = (byte)value;
                value >>= 8;
            }
        }

        unsafe public void WriteRawBytes(byte* value, int count)
        {
            for (int i = 0; i < count; i++)
                m_Buffer[m_CurrentByteIndex + i] = value[i];
            m_CurrentByteIndex += count;
        }

        public void WritePackedNibble(uint value, int context)
        {
            Debug.Assert(value < 16);
            if (m_Capture != null)
                m_Capture.AddNibble(context, value);

            m_Buffer[m_CurrentByteIndex++] = (byte)value;
        }

        public void WritePackedUInt(uint value, int context)
        {
            if (m_Capture != null)
                m_Capture.AddUInt(context, value);

            m_Buffer[m_CurrentByteIndex + 0] = (byte)value;
            m_Buffer[m_CurrentByteIndex + 1] = (byte)(value >> 8);
            m_Buffer[m_CurrentByteIndex + 2] = (byte)(value >> 16);
            m_Buffer[m_CurrentByteIndex + 3] = (byte)(value >> 24);
            m_CurrentByteIndex += 4;
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
            return (m_CurrentByteIndex - m_BufferOffset) * 8;
        }

        public NetworkCompressionModel GetModel()
        {
            return null;
        }

        public int Flush()
        {
            return m_CurrentByteIndex - m_BufferOffset;
        }
        
        NetworkCompressionCapture m_Capture;
        byte[] m_Buffer;
        int m_BufferOffset;
        int m_CurrentByteIndex;
    }
}