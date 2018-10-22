using System;
using UnityEngine;


// TODO : Flip back to use ushort

public static class Sequence
{
    // In order to reduce the number of bits we use for sequence numbers we 
    // only send the lower 16 bits and then restore the integer sequence number
    // based on the baseline value. In order to partially protect against weird
    // errors we will only allow the diffs to be 1/4 of the size of ushort which
    // for an update rate of 60 is about 4 minutes

    public static uint invalid = 0xffffffff;


    public static ushort ToUInt16(int value)
    {
        GameDebug.Assert(value >= 0);
        return (ushort)(value & 0xffff);
    }

    // TODO : We can probably implement this more elegantly?
    public static int FromUInt16(ushort value, int baseline)
    {
        ushort b = ToUInt16(baseline);

        if (value <= b)
        {
            var diff = (b - value);
            if (diff < k_MaxUInt16Diff)
                return baseline - diff;
            else
            {
                var diff2 = k_Modulo - b + value;
                if (diff2 < k_MaxUInt16Diff)
                    return (int)(baseline + diff2);
                else
                    return -1;
            }
        }
        else
        {
            var diff = (value - b);
            if (diff < k_MaxUInt16Diff)
                return baseline + diff;
            else
            {
                var diff2 = k_Modulo - value + b;
                if (diff2 < k_MaxUInt16Diff)
                    return (int)(baseline - diff2);
                else
                    return -1;
            }
        }
    }

    const uint k_Modulo = (UInt16.MaxValue + 1);
    const ushort k_MaxUInt16Diff = (UInt16.MaxValue) / 4;
}


public class SequenceBuffer<T> where T : class
{
    public int Capacity { get { return m_Sequences.Length; } }

    public SequenceBuffer(int capacity, Func<T> factory)
    {
        m_Sequences = new int[capacity];
        m_Elements = new T[capacity];

        for (int i = 0; i < capacity; ++i)
        {
            m_Sequences[i] = -1;
            m_Elements[i] = factory();
        }
    }

    public T this[int sequence]
    {
        get
        {
            var index = sequence % m_Elements.Length;
            var elementSequence = m_Sequences[index];

            if (elementSequence != sequence)
                throw new ArgumentException("Invalid sequence. Looking for " + sequence + " but slot has " + elementSequence);

            return m_Elements[index];
        }
    }

    public T TryGetByIndex(int index, out int sequence)
    {
        GameDebug.Assert(index >= 0 && index < m_Elements.Length);
        if (m_Sequences[index] != -1)
        {
            sequence = m_Sequences[index];
            return m_Elements[index];
        }
        else
        {
            sequence = 0;
            return null;
        }
    }

    public bool TryGetValue(int sequence, out T result)
    {
        var index = sequence % m_Elements.Length;
        var elementSequence = m_Sequences[index];

        if (elementSequence == sequence)
        {
            result = m_Elements[index];
            return true;
        }
        else
        {
            result = default(T);
            return false;
        }
    }

    public void Clear()
    {
        for (int i = 0; i < m_Sequences.Length; ++i)
            m_Sequences[i] = -1;
    }

    public void Remove(int sequence)
    {
        var index = sequence % m_Sequences.Length;
        if (m_Sequences[index] == sequence)
            m_Sequences[index] = -1;
    }

    public bool Available(int sequence)
    {
        var index = sequence % m_Sequences.Length;
        return m_Sequences[index] == -1;
    }

    public bool Exists(int sequence)
    {
        var index = sequence % m_Sequences.Length;
        return m_Sequences[index] == sequence;
    }

    public T Acquire(int sequence)
    {
        var index = sequence % m_Sequences.Length;
        m_Sequences[index] = sequence;
        return m_Elements[index];
    }

    public int[] m_Sequences;
    public T[] m_Elements;

    public override string ToString()
    {
        return string.Join(",", m_Sequences);
    }
}