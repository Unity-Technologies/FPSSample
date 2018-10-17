using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    //
    // Unity can't serialize Dictionary so here's a custom wrapper that does. Note that you have to
    // extend it before it can be serialized as Unity won't serialized generic-based types either.
    //
    // Example:
    //   public sealed class MyDictionary : SerializedDictionary<KeyType, ValueType> {}
    //
    [Serializable]
    public class SerializedDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<K> m_Keys = new List<K>();

        [SerializeField]
        List<V> m_Values = new List<V>();

        public void OnBeforeSerialize()
        {
            m_Keys.Clear();
            m_Values.Clear();

            foreach (var kvp in this)
            {
                m_Keys.Add(kvp.Key);
                m_Values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            for (int i = 0; i < m_Keys.Count; i++)
                Add(m_Keys[i], m_Values[i]);

            m_Keys.Clear();
            m_Values.Clear();
        }
    }
}
