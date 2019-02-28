using System;
using UnityEngine;


public struct BitInputStream
{
    public BitInputStream(byte[] buffer)
    {
        m_Buffer = buffer;
        m_CurrentBitIdx = 0;
        m_CurrentByteIdx = 0;
        m_BitStage = 0;
    }

    public void Initialize(byte[] buffer)
    {
        this = new BitInputStream(buffer);
    }
    
    public int GetBitPosition()
    {
        return m_CurrentByteIdx * 8 - m_CurrentBitIdx;
    }

    public long ReadUIntPacked()
    {
        int inputBits = 1;
        long value = 0;
        while (ReadBits(1) == 0)
        {
            value += (1L << inputBits);
            inputBits += 2;
        }

        if (inputBits > 32)
        {
            long low = ReadBits(32);
            long high = ReadBits(inputBits - 32);
            return value + (low | (high << 32));
        }
        else
            return value + ReadBits(inputBits);
    }

    public long ReadIntDelta(long baseline)
    {
        var mapped = ReadUIntPacked();
        if ((mapped & 1) != 0)
            return baseline + ((mapped + 1) >> 1);
        else
            return baseline - (mapped >> 1);
    }

    public uint ReadBits(int numbits)
    {
        GameDebug.Assert(numbits > 0 && numbits <= 32);

        while (m_CurrentBitIdx < 32)
        {
            m_BitStage |= (UInt64)m_Buffer[m_CurrentByteIdx++] << m_CurrentBitIdx;
            m_CurrentBitIdx += 8;
        }

        return ReadBitsInternal(numbits);
    }

    public void ReadBytes(byte[] dstBuffer, int dstIndex, int count)
    {
        Align();
        if (dstBuffer != null)
            NetworkUtils.MemCopy(m_Buffer, m_CurrentByteIdx, dstBuffer, dstIndex, count);

        m_CurrentByteIdx += count;
    }

    public int Align()
    {
        var remainder = m_CurrentBitIdx % 8;
        if (remainder > 0)
        {
            var value = ReadBitsInternal(remainder);
            GameDebug.Assert(value == 0);
        }

        m_CurrentByteIdx -= m_CurrentBitIdx / 8;
        m_CurrentBitIdx = 0;
        m_BitStage = 0;
        return m_CurrentByteIdx;
    }

    uint ReadBitsInternal(int numbits)
    {
        GameDebug.Assert(m_CurrentBitIdx >= numbits);
        var res = m_BitStage & (((UInt64)1 << numbits) - 1);
        m_BitStage >>= numbits;
        m_CurrentBitIdx -= numbits;
        return (UInt32)res;
    }

    byte[] m_Buffer;
    ulong m_BitStage;
    int m_CurrentBitIdx;
    int m_CurrentByteIdx;
}
