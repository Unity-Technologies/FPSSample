using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IStateHistoryBufferBase
{
    System.Object GetState(int tick);
}

public class StateHistoryBuffer<T>  where T : struct
{
    public StateHistoryBuffer(int size)
    {
        m_states = new T[size];
        m_ticks = new int[size];
    }

    public void SetState(int tick, ref T state)
    {
        int lastSetTick = currentIndex != -1 ? m_ticks[currentIndex] : 0;

        if(tick != lastSetTick)
        {
            if(tick < lastSetTick)
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
                currentIndex = (currentIndex + 1) % m_states.Length;
        }

        m_states[currentIndex] = state;
        m_ticks[currentIndex] = tick;
    }

    public bool IsTickSet(int tick)
    {
        for(int i=0;i< m_ticks.Length;i++)     
        {
            if (m_ticks[i] == tick)
                return true;
        }
        return false;
    }

    public System.Object GetState(int tick) 
    {
        for (int i = 0; i < m_ticks.Length; i++) 
        {
            if (m_ticks[i] == tick)
            {
                return m_states[i];
            }
        }
        return null;
    }

    T[] m_states;
    int[] m_ticks;
    int currentIndex = -1;
}