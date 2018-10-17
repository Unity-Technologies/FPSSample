using System;
using System.Text;
using UnityEngine;

public struct ByteOutputStream
{
    public ByteOutputStream(byte[] buffer)
    {
        m_Buffer = buffer;
        m_CurrentByteIdx = 0;
    }

    public int GetBytePosition()
    {
        return m_CurrentByteIdx;
    }

    public void WriteBits(uint value, int numbits)
    {
        switch(numbits)
        {
            case 1:
            case 8:
                WriteUInt8((byte)value);
                break;
            case 16:
                WriteUInt16((ushort)value);
                break;
            case 32:
                WriteUInt32(value);
                break;
            default:
                GameDebug.Assert(false);
                break;
        }
    }

    public void WriteUInt8(byte value)
    {
        m_Buffer[m_CurrentByteIdx + 0] = value;
        m_CurrentByteIdx += 1;
    }

    public void WriteUInt16(ushort value)
    {
        m_Buffer[m_CurrentByteIdx + 0] = (byte)value;
        m_Buffer[m_CurrentByteIdx + 1] = (byte)(value >> 8);
        m_CurrentByteIdx += 2;
    }

    public void WriteUInt16_NBO(ushort value)
    {
        WriteUInt16((ushort)System.Net.IPAddress.HostToNetworkOrder((short)value));
    }

    public void WriteUInt32(uint value)
    {
        m_Buffer[m_CurrentByteIdx + 0] = (byte)value;
        m_Buffer[m_CurrentByteIdx + 1] = (byte)(value >> 8);
        m_Buffer[m_CurrentByteIdx + 2] = (byte)(value >> 16);
        m_Buffer[m_CurrentByteIdx + 3] = (byte)(value >> 24);
        m_CurrentByteIdx += 4;
    }
    public void WriteUInt32_NBO(uint value)
    {
        WriteUInt32((uint)System.Net.IPAddress.HostToNetworkOrder((int)value));
    }
    
    public void WriteBytes(byte[] data, int srcIndex, int length)
    {
        GameDebug.Assert(data != null);

        NetworkUtils.MemCopy(data, srcIndex, m_Buffer, m_CurrentByteIdx, length);
        m_CurrentByteIdx += length;
    }

    public void WriteBytesOffset(byte[] data, int srcIndex, int offset, int length)
    {
        GameDebug.Assert(data != null);

        NetworkUtils.MemCopy(data, srcIndex, m_Buffer, offset, length);
        m_CurrentByteIdx += length;
    }

    public void WriteByteArray(byte[] data, int srcIndex, int length, int maxCount)
    {
        GameDebug.Assert(length <= maxCount);
        m_Buffer[m_CurrentByteIdx + 0] = (byte)length;
        m_Buffer[m_CurrentByteIdx + 1] = (byte)(length >> 8);
        m_CurrentByteIdx += 2;

        int i = 0;
        for (; i < length; i++)
            m_Buffer[m_CurrentByteIdx + i] = data[srcIndex + i];

        for(; i < maxCount; i++)
            m_Buffer[m_CurrentByteIdx + i] = 0; //RUTODO: do we need to clear? do we ever reuse arrays?

        m_CurrentByteIdx += maxCount;
    }

    public void CopyByteArray<T>(ref T input, int maxCount, int context0) where T : NetworkCompression.IInputStream
    {
        int count = (int)input.ReadPackedUInt(context0);
        GameDebug.Assert(count <= maxCount);

        WriteUInt16((ushort)count);
        if (count > 0)
        {
            input.ReadRawBytes(m_Buffer, m_CurrentByteIdx, count);
        }
        for (int i = count; i < maxCount; i++)
            m_Buffer[m_CurrentByteIdx + i] = 0;
        m_CurrentByteIdx += maxCount;
    }

    public void CopyByteArray(ref ByteInputStream input, int maxCount)
    {
        int count = input.ReadUInt16();
        GameDebug.Assert(count <= maxCount);

        WriteUInt16((ushort)count);
        input.ReadBytes(m_Buffer, m_CurrentByteIdx, count, maxCount);
        for (int i = count; i < maxCount; i++)
            m_Buffer[m_CurrentByteIdx + i] = 0;
        m_CurrentByteIdx += maxCount;
    }

    public void CopyBytes(ref ByteInputStream input, int count)
    {
        input.ReadBytes(m_Buffer, m_CurrentByteIdx, count, count);
        m_CurrentByteIdx += count;
    }

    public void Flush()
    {
    }

    public void WriteString(string value, Encoding encoding)
    {
        var encoder = encoding.GetEncoder();

        var buffer = new byte[byte.MaxValue];
        var chars = value.ToCharArray();
        int charsUsed, bytesUsed;
        bool completed;

        encoder.Convert(chars, 0, chars.Length, buffer, 0, byte.MaxValue, true, out charsUsed, out bytesUsed, out completed);
        Debug.Assert(bytesUsed <= byte.MaxValue);

        WriteUInt8((byte)bytesUsed);
        WriteBytes(buffer, 0, bytesUsed);
    }

    byte[] m_Buffer;
    int m_CurrentByteIdx;
}
