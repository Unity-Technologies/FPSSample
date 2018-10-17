using UnityEngine;

public class FloatRollingAverage
{
    public float latest { get; private set; }
    public float max { get; private set; }
    public float min { get; private set; }
    public float average { get; private set; }
    public float stdDeviation { get; private set; }

    public CircularList<float> GetData()
    {
        return m_Entries;
    }

    public FloatRollingAverage(int windowSize = 64)
    {
        m_Entries = new CircularList<float>(windowSize);
    }

    public void Update(float value)
    {
        latest = value;

        if (m_Entries.Count == m_Entries.Capacity)
        {
            var oldValue = m_Entries[0];

            if(oldValue == min || oldValue == max)
            {
                // Recalculate min and max
                min = float.MaxValue;
                max = float.MinValue;
                for (int i = 0; i < m_Entries.Capacity; ++i)
                {
                    var entry = m_Entries[i];
                    if (entry < min)
                        min = m_Entries[i];
                    if (entry > max)
                        max = m_Entries[i];
                }
            }

            m_Sum -= oldValue;
            m_SqrSum -= oldValue * oldValue;
        }

        if (value > max)
            max = value;

        if (value < min)
            min = value;

        m_Entries.Add(value);
        var samples = m_Entries.Count;

        m_Sum += value;
        m_SqrSum += value * value;

        average = m_Sum / samples;
        float f = (m_SqrSum - m_Sum * m_Sum / samples) / samples;   
        stdDeviation = f >= 0 ? Mathf.Sqrt(f) : 0;
    }

    public void Reset()
    {
        m_Entries.Clear();
        latest = 0;
        average = 0;
        max = 0;
        min = 0;
        stdDeviation = 0;

        m_Sum = 0;
        m_SqrSum = 0;
    }

    float m_Sum;
    float m_SqrSum;

    CircularList<float> m_Entries;
}
