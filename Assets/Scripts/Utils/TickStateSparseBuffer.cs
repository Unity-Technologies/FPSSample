using UnityEngine;


public class TickStateSparseBuffer<T>
{
    public TickStateSparseBuffer(int size)
    {
        m_Elements = new T[size];
        m_Ticks = new int[size];
    }

    public int Count { get { return m_Count; } }
    public int Capacity { get { return m_Ticks.Length; } }

    public T this[int index]
    {
        get
        {
            Debug.Assert(index >= 0 && index < m_Count);
            return m_Elements[index];
        }
    }

    public int FirstTick()
    {
        return m_Count > 0 ? m_Ticks[m_First] : -1;
    }

    public int LastTick()
    {
        return m_Count > 0 ? m_Ticks[(m_First + m_Count - 1) % m_Ticks.Length] : -1;
    }

    public T First()
    {
        Debug.Assert(m_Count > 0);
        return m_Elements[m_First];
    }

    public T Last()
    {
        Debug.Assert(m_Count > 0);
        return m_Elements[(m_First + m_Count - 1) % m_Ticks.Length];
    }

    public bool TryGetValue(int tick, out T result)
    {
        var index = m_First;
        for (int i = 0; i < m_Count; ++i, ++index)
        {
            if (index == m_Ticks.Length)
                index = 0;

            if (m_Ticks[index] == tick)
            {
                result = m_Elements[index];
                return true;
            }
        }

        result = default(T);
        return false;
    }

    public bool GetStates(int tick, float fraction, ref int lowIndex, ref int highIndex, ref float outputFraction)
    {
        lowIndex = GetValidIndexLower(tick);
        highIndex = GetValidIndexHigher(tick + 1);

        if (lowIndex == -1 || highIndex == -1)
            return false;

        int lowTick = GetTickByIndex(lowIndex);
        int highTick = GetTickByIndex(highIndex);

        float total = (float)(highTick - lowTick);
        float relativeTime = tick - lowTick + fraction;

        outputFraction = relativeTime / total;

        return true;
    }

    int GetValidIndexLower(int tick)
    {
        var index = m_First;

        int bestResultIndex = -1;
        for (int i = 0; i < m_Count; ++i, ++index)
        {
            if (index == m_Ticks.Length)
                index = 0;

            if (m_Ticks[index] == tick)
            {
                bestResultIndex = index;
                break;
            }

            if (m_Ticks[index] < tick)
            {
                bestResultIndex = index;
            }
        }

        return bestResultIndex;
    }

    int GetValidIndexHigher(int tick)
    {
        var index = m_First;

        int bestResultIndex = -1;
        for (int i = 0; i < m_Count; ++i, ++index)
        {
            if (index == m_Ticks.Length)
                index = 0;

            if (m_Ticks[index] == tick || m_Ticks[index] > tick)
            {
                bestResultIndex = index;
                break;
            }
        }

        return bestResultIndex;
    }

    public int GetTickByIndex(int index)
    {
        Debug.Assert(index >= 0 && index < m_Count);
        return m_Ticks[index];
    }

    public void Add(int tick, T element)    
    {
        var last = LastTick();
        if (last != -1 && last >= tick)
            throw new System.InvalidOperationException(string.Format("Ticks must be increasing when adding (last = {0}, trying to add {1})", last, tick));

        var index = (m_First + m_Count) % m_Ticks.Length;

        m_Ticks[index] = tick;
        m_Elements[index] = element;

        if (m_Count < m_Ticks.Length)
            m_Count++;
        else
            m_First = (m_First + 1) % m_Ticks.Length;
    }

    public void Clear()
    {
        m_First = 0;
        m_Count = 0;

        for (int i = 0; i < m_Ticks.Length; ++i)
            m_Elements[i] = default(T);

        for (int i = 0; i < m_Ticks.Length; ++i)
            m_Ticks[i] = 0;
    }

    int m_First;
    int m_Count;

    T[] m_Elements;
    int[] m_Ticks;
}
