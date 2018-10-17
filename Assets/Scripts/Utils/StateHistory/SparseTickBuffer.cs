using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SparseTickBuffer
{
    public SparseTickBuffer(int size)
    {
        m_ticks = new uint[size];
    }
    
    
    public int Register(uint tick)
    {
        uint lastSetTick = currentIndex != -1 ? m_ticks[currentIndex] : 0;

        if (tick != lastSetTick)
        {
            if (tick < lastSetTick)
            {
                // When recieving lower tick we clear all registered stated after tick
                while (m_ticks[currentIndex] > tick)
                {
                    m_ticks[currentIndex] = 0;

                    currentIndex--;
                    if (currentIndex < 0)
                        currentIndex += m_ticks.Length;
                }
            }
            else
                currentIndex = (currentIndex + 1) % m_ticks.Length;

            m_ticks[currentIndex] = tick;
        }
        
        return currentIndex;
    }

    public int GetIndex(uint tick)
    {
        for (int i = 0; i < m_ticks.Length; i++)      
        {
            if (m_ticks[i] == tick)
            {
                return i;
            }
        }
        return -1;
    }

    uint[] m_ticks;
    int currentIndex = -1;
}