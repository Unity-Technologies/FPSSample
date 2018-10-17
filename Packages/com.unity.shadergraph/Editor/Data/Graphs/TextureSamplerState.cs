using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class TextureSamplerState
    {
        public enum FilterMode
        {
            Linear,
            Point,
            Trilinear
        }

        public enum WrapMode
        {
            Repeat,
            Clamp,
            Mirror,
            MirrorOnce
        }

        [SerializeField] private FilterMode m_filter = FilterMode.Linear;

        public FilterMode filter
        {
            get { return m_filter; }
            set
            {
                if (m_filter == value)
                    return;

                m_filter = value;
            }
        }

        [SerializeField] private WrapMode m_wrap = WrapMode.Repeat;

        public WrapMode wrap
        {
            get { return m_wrap; }
            set
            {
                if (m_wrap == value)
                    return;

                m_wrap = value;
            }
        }
    }
}
