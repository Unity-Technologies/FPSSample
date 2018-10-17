using System;
using System.Text;
using UnityEngine;


public struct ByteInputStream
{
    public ByteInputStream(byte[] buffer)
    {
        m_Buffer = buffer;
        m_CurrentByteIdx = 0;
    }
    
    public int GetBytePosition()
    {
        return m_CurrentByteIdx;
    }

    public uint ReadBits(int numbits)
    {
        switch (numbits)
        {
            case 1:
            case 8:
                return ReadUInt8();
            case 16:
                return ReadUInt16();
            case 32:
                return ReadUInt32();
            default:
                return 0;
        }
    }
    public byte ReadUInt8()
    {
        return m_Buffer[m_CurrentByteIdx++];
    }

    public ushort ReadUInt16()
    {
        ushort value = (ushort)(m_Buffer[m_CurrentByteIdx] | (m_Buffer[m_CurrentByteIdx + 1] << 8));
        m_CurrentByteIdx += 2;
        return value;
    }

    public ushort ReadUInt16_NBO()
    {
        return (ushort)System.Net.IPAddress.NetworkToHostOrder((short)ReadUInt16());
    }

    public uint ReadUInt32()
    {
        uint value = (uint)(m_Buffer[m_CurrentByteIdx] | (m_Buffer[m_CurrentByteIdx + 1] << 8) | (m_Buffer[m_CurrentByteIdx + 2] << 16) | (m_Buffer[m_CurrentByteIdx + 3] << 24));
        m_CurrentByteIdx += 4;
        return value;
    }

    public uint ReadUInt32_NBO()
    {
        return (uint)System.Net.IPAddress.NetworkToHostOrder((int)ReadUInt32());
    }

    public void GetByteArray(out byte[] buffer, out int srcIndex, out int count, int maxCount)
    {
        count = ReadUInt16();
        if (count > 0)
        {
            srcIndex = m_CurrentByteIdx;
            buffer = m_Buffer;
        }
        else
        {
            srcIndex = -1;
            buffer = null;
        }
        m_CurrentByteIdx += maxCount;

    }

    public void ReadBytes(byte[] dstBuffer, int dstIndex, int count, int maxCount)
    {
        if (dstBuffer != null)
            NetworkUtils.MemCopy(m_Buffer, m_CurrentByteIdx, dstBuffer, dstIndex, count);

        m_CurrentByteIdx += maxCount;
    }

    public int ReadByteArray(byte[] dstBuffer, int dstIndex, int maxCount)
    {
        int count = (int)ReadUInt16();
        ReadBytes(dstBuffer, dstIndex, count, maxCount);
        return count;
    }

    public void SkipBytes(int count)
    {
        m_CurrentByteIdx += count;
    }

    public void SkipByteArray(int maxCount)
    {
        m_CurrentByteIdx += 2 + maxCount;
    }

    public void Reset()
    {
        m_CurrentByteIdx = 0;
    }

    public string ReadString(Encoding encoding)
    {
        var length = ReadUInt8();
        var buffer = new byte[byte.MaxValue];
        ReadBytes(buffer, 0, length, length);
        return encoding.GetString(buffer, 0, length);
    }

    byte[] m_Buffer;
    int m_CurrentByteIdx;
}
