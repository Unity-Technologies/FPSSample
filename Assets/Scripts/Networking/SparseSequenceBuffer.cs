using System;
using UnityEngine;

public class SparseSequenceBuffer
{
    public SparseSequenceBuffer(int size, int snapSize)
    {
        m_Elements = new uint[size][];
        m_Sequences = new int[size];

        for (int i = 0; i < m_Elements.Length; ++i)
            m_Elements[i] = new uint[snapSize];
    }

    public uint[] Insert(int sequence)
    {
        if (m_Count == m_Sequences.Length)
            Remove(m_Sequences[0]);

        if (m_Count == 0 || m_Sequences[m_Count - 1] < sequence)
        {
            m_Sequences[m_Count] = sequence;
            var result = m_Elements[m_Count];
            ++m_Count;
            return result;
        }

        for(int i = 0; i < m_Count; ++i)
        {
            if(m_Sequences[i] == sequence)
                return m_Elements[i];
            else if(m_Sequences[i] > sequence)
            {
                var tmp = m_Elements[m_Count];
                for (int j = m_Count; j > i; --j)
                {
                    m_Sequences[j] = m_Sequences[j - 1];
                    m_Elements[j] = m_Elements[j - 1];
                }
                m_Elements[i] = tmp;
                ++m_Count;
                return tmp;
            }
        }

        // Should never reach this point
        throw new InvalidOperationException();
    }

    public bool Remove(int sequence)
    {
        for (int i = 0; i < m_Count; ++i)
        {
            if (m_Sequences[i] == sequence)
            {
                var tmpElement = m_Elements[i];
                for (var j = i; j < m_Count - 1; ++j)
                {
                    m_Sequences[j] = m_Sequences[j + 1];
                    m_Elements[j] = m_Elements[j + 1];
                }
                m_Elements[m_Count - 1] = tmpElement;
                --m_Count;
                return true;
            }
        }
        return false;
    }

    public uint[] FindMax(int sequence)
    {
        var index = -1;
        for (int i = 0; i < m_Count; ++i)
        {
            if (m_Sequences[i] <= sequence)
                index = i;
            else
                break;
        }
        return index != -1 ? m_Elements[index] : null;
    }

    public uint[] FindMin(int sequence)
    {
        var index = -1;
        for (int i = m_Count - 1; i >= 0; --i)
        {
            if (m_Sequences[i] >= sequence)
                index = i;
            else
                break;
        }
        return index != -1 ? m_Elements[index] : null;
    }

    public uint[] TryGetValue(int sequence)
    {
        for (int i = 0; i < m_Count; ++i)
        {
            if (m_Sequences[i] == sequence)
                return m_Elements[i];
            else if (m_Sequences[i] > sequence)
                return null;
        }
        return null;
    }


    public void Clear()
    {
        m_Count = 0;
    }

    public int GetSize()
    {
        return m_Count;
    }

    int m_Count;
    uint[][] m_Elements;
    int[] m_Sequences;
}
