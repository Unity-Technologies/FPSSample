using UnityEngine;
using UnityEngine.Playables;

public class SimpleTranstion<U> where U : struct, IPlayable
{
    public SimpleTranstion(U target, params int[] ports) 
    {
        m_target = target;
        m_ports = ports;
    }

    public void Update(int activePort, float blendVelocity, float deltaTime)
    {
        // Update current state weight
        float weight = m_target.GetInputWeight(activePort);
        if (weight != 1.0f)
        {
            weight = Mathf.Clamp(weight + blendVelocity * deltaTime, 0, 1);
            m_target.SetInputWeight(activePort, weight);
        }

        // Adjust weight of other states and ensure total weight is 1
        float weighLeft = 1.0f - weight;
        float totalWeight = 0;
        for (int i = 0; i < m_ports.Length; i++)
        {
            int port = m_ports[i];
            if (port == activePort)
                continue;

            totalWeight += m_target.GetInputWeight(port);
        }
        if (totalWeight == 0)
            return;

        float fraction = weighLeft / totalWeight;
        for (int i = 0; i < m_ports.Length; i++)
        {
            int port = m_ports[i];
            if (port == activePort)
                continue;

            float w = m_target.GetInputWeight(port);
            w = w * fraction;
            m_target.SetInputWeight(port, w);
        }
    }

    int[] m_ports;
    U m_target;
}
