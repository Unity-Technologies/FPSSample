using System;
using UnityEngine;

namespace UnityEditor.Graphing
{
    [Serializable]
    public struct DrawState
    {
        [SerializeField]
        private bool m_Expanded;

        [SerializeField]
        private Rect m_Position;

        public bool expanded
        {
            get { return m_Expanded; }
            set { m_Expanded = value; }
        }

        public Rect position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }
    }
}
