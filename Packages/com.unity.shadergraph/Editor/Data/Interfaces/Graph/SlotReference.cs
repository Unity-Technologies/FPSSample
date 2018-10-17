using System;
using UnityEngine;

namespace UnityEditor.Graphing
{
    [Serializable]
    public struct SlotReference : ISerializationCallbackReceiver, IEquatable<SlotReference>
    {
        [SerializeField]
        private int m_SlotId;

        [NonSerialized]
        private Guid m_NodeGUID;

        [SerializeField]
        private string m_NodeGUIDSerialized;

        public SlotReference(Guid nodeGuid, int slotId)
        {
            m_NodeGUID = nodeGuid;
            m_SlotId = slotId;
            m_NodeGUIDSerialized = string.Empty;
        }

        public Guid nodeGuid
        {
            get { return m_NodeGUID; }
        }

        public int slotId
        {
            get { return m_SlotId; }
        }

        public void OnBeforeSerialize()
        {
            m_NodeGUIDSerialized = m_NodeGUID.ToString();
        }

        public void OnAfterDeserialize()
        {
            m_NodeGUID = new Guid(m_NodeGUIDSerialized);
        }

        public bool Equals(SlotReference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return m_SlotId == other.m_SlotId && m_NodeGUID.Equals(other.m_NodeGUID);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SlotReference)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_SlotId * 397) ^ m_NodeGUID.GetHashCode();
            }
        }
    }
}
