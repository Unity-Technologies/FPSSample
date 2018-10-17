using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionSystemSceneDictionary : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        class ObjectIDPair
        {
            public int Key;
            public Object Value;
        }

        [SerializeField]
        List<ObjectIDPair> m_ObjectList = new List<ObjectIDPair>();

        Dictionary<Object, int> m_ObjectIndex = new Dictionary<Object, int>();
        Dictionary<int, Object> m_IDIndex = new Dictionary<int, Object>();

        public int GetIdFor(PlanarReflectionProbe probe)
        {
            if (m_ObjectIndex.ContainsKey(probe))
                return m_ObjectIndex[probe];

            var id = FindNextId();
            m_ObjectList.Add(new ObjectIDPair
            {
                Key = id,
                Value = probe
            });

            m_ObjectIndex[probe] = id;
            m_IDIndex[id] = probe;

            return id;
        }

        public void OnBeforeSerialize()
        {
            for (var i = m_ObjectList.Count - 1; i >= 0; --i)
            {
                if (m_ObjectList[i].Value == null)
                    m_ObjectList.RemoveAt(i);
            }
        }

        public void OnAfterDeserialize()
        {
            for (int i = 0; i < m_ObjectList.Count; i++)
            {
                if (m_IDIndex.ContainsKey(m_ObjectList[i].Key))
                    Debug.LogErrorFormat(this, "ID {0} is a duplicated in ReflectionSystemSceneDictionary ({1}) for {2}", m_ObjectList[i].Key, this, m_ObjectList[i].Value);

                m_ObjectIndex[m_ObjectList[i].Value] = m_ObjectList[i].Key;
                m_IDIndex[m_ObjectList[i].Key] = m_ObjectList[i].Value;
            }
        }

        int FindNextId()
        {
            var id = 0;
            while (m_IDIndex.ContainsKey(id)) ++id;
            return id;
        }
    }
}
