using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphing
{
    [Serializable]
    public class GraphDrawingData : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<string> m_SerializableSelection = new List<string>();

        [NonSerialized]
        private List<Guid> m_Selection = new List<Guid>();

        public IEnumerable<Guid> selection
        {
            get { return m_Selection; }
            set
            {
                m_Selection.Clear();
                m_Selection.AddRange(value);
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializableSelection.Clear();
            m_SerializableSelection.AddRange(m_Selection.Select(x => x.ToString()));
        }

        public void OnAfterDeserialize()
        {
            selection = m_SerializableSelection.Select(x => new Guid(x));
        }
    }
}
