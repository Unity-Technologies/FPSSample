using System;
using System.Collections.Generic;
using UnityEngine;

// TODO : Optimize internal data structures
public class NetworkObjectPool<T> where T : class, new()
{
    public int allocated { get { return m_Allocated.Count; } }
    public int capacity { get { return m_Allocated.Capacity; } }

    public NetworkObjectPool(int initialSize, Func<T> factory = null)
    {
        Grow(initialSize);
        m_Factory = factory;
    }

    public T Allocate()
    {
        if (m_Free.Count == 0)
            Grow(m_Free.Capacity * 2);

        var element = m_Free[m_Free.Count - 1];
        m_Free.RemoveAt(m_Free.Count - 1);
        m_Allocated.Add(element);
        return element;
    }

    public void Release(T t)
    {
        bool result = m_Allocated.Remove(t);
        GameDebug.Assert(result);
        m_Free.Add(t);
    }

    public void Reset()
    {
        foreach (var item in m_Allocated)
            m_Free.Add(item);
        m_Allocated.Clear();
    }

    void Grow(int count)
    {
        m_Free.Capacity += count;
        m_Allocated.Capacity += count;

        for (int i = 0; i < count; ++i)
            m_Free.Add(m_Factory != null ? m_Factory() : new T());
    }

    Func<T> m_Factory;
    List<T> m_Free = new List<T>();
    List<T> m_Allocated = new List<T>();
}