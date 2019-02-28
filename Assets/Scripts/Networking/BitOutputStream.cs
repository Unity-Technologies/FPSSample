using System;
using UnityEngine;

public struct BitOutputStream
{
    public BitOutputStream(byte[] buffer)
    {
        m_Buffer = buffer;
        m_CurrentBitIdx = 0;
        m_CurrentByteIdx = 0;
        m_BitStage = 0;
    }

    public int GetBitPosition()
    {
        return m_CurrentByteIdx * 8 + m_CurrentBitIdx;
    }
    public void WriteUIntPacked(long value)
    {
        GameDebug.Assert(value >= 0);

        int outputBits = 1;
        int numPrefixBits = 0;
        while (value >= (1L << outputBits))  // RUTODO: Unroll this and merge with bit output. How do we actually verify inlining in C#?
        {
            value -= (1L << outputBits);
            outputBits += 2;
            numPrefixBits++;
        }
        WriteBits(1u << numPrefixBits, numPrefixBits + 1);

        if (outputBits > 32)
        {
            WriteBits((uint)value, 32);
            WriteBits((uint)(value >> 32), outputBits - 32);
        }
        else
            WriteBits((uint)value, outputBits);
    }

    public void WriteIntDelta(long value, long baseline)
    {
        var diff = baseline - value;
        if (diff < 0)
            diff = (-diff << 1) - 1;
        else
            diff = diff << 1;

        WriteUIntPacked(diff);
    }
    public void WriteIntDeltaNonZero(long value, long baseline)
    {
        var diff = value - baseline;
        GameDebug.Assert(diff != 0);

        if (diff < 0)
            diff = (-diff << 1) - 1;
        else
            diff = (diff << 1) - 2;

        WriteUIntPacked(diff);
    }

    public void WriteBits(uint value, int numbits)
    {
        GameDebug.Assert(numbits > 0 && numbits <= 32);
        GameDebug.Assert((UInt64.MaxValue << numbits & value) == 0);

        m_BitStage |= ((ulong)value << m_CurrentBitIdx);
        m_CurrentBitIdx += numbits;

        while (m_CurrentBitIdx >= 8)
        {
            m_Buffer[m_CurrentByteIdx++] = (byte)m_BitStage;
            m_CurrentBitIdx -= 8;
            m_BitStage >>= 8;
        }
    }

    public void WriteBytes(byte[] value, int srcIndex, int count)
    {
        Align();
        NetworkUtils.MemCopy(value, srcIndex, m_Buffer, m_CurrentByteIdx, count);
        m_CurrentByteIdx += count;
    }

    public int Align()
    {
        if (m_CurrentBitIdx > 0)
            WriteBits(0, 8 - m_CurrentBitIdx);
        return m_CurrentByteIdx;
    }

    public int Flush()
    {
        Align();
        return m_CurrentByteIdx;
    }

    public void SkipBytes(int bytes)
    {
        Debug.Assert(m_CurrentBitIdx == 0);
        m_CurrentByteIdx += bytes;
    }

    byte[] m_Buffer;
    ulong m_BitStage;
    int m_CurrentBitIdx;
    int m_CurrentByteIdx;
}
