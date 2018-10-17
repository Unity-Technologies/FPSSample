namespace UnityEditor.ShaderGraph
{
    public struct Identifier
    {
        uint m_Version;
        int m_Index;

        public Identifier(int index, uint version = 1)
        {
            m_Version = version;
            m_Index = index;
        }

        public void IncrementVersion()
        {
            if (m_Version == uint.MaxValue)
                m_Version = 1;
            else
                m_Version++;
        }

        public uint version
        {
            get { return m_Version; }
        }

        public int index
        {
            get { return m_Index; }
        }

        public bool valid
        {
            get { return m_Version != 0; }
        }
    }
}
