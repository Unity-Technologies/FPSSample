public class TickStateDenseBuffer<T> where T : struct
{
    public TickStateDenseBuffer(int capasity)
    {
        m_Elements = new T[capasity];
    }

    public int LastTick()
    {
        return m_Size > 0 ? m_Tick : -1;
    }

    public int FirstTick()
    {
        return m_Size > 0 ? m_Tick - m_Size + 1 : -1;
    }

    public void Clear()
    {
        m_Size = 0;
    }

    public bool TryGetValue(int tick, ref T result)
    {
        if (IsValidTick(tick))
        {
            result = m_Elements[tick % m_Elements.Length];
            return true;
        }
        else
            return false;
    }

    public void Add(ref T value, int tick)
    {
        m_Elements[tick % m_Elements.Length] = value;

        // Reset the buffer if we receive non consecutive tick
        if (tick - m_Tick != 1)
            m_Size = 1;
        else if(m_Size < m_Elements.Length)
            ++m_Size;

        m_Tick = tick;
    }

    public void Set(ref T value, int tick)
    {
        m_Elements[tick % m_Elements.Length] = value;
    }

    public bool IsValidTick(int tick)
    {
        return tick <= m_Tick && tick > m_Tick - m_Size;
    }

    T[] m_Elements;
    int m_Tick;
    int m_Size;
}
