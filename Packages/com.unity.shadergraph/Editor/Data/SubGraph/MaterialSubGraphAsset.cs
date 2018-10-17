using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class MaterialSubGraphAsset : ScriptableObject
    {
        [SerializeField] private SubGraph m_MaterialSubGraph = new SubGraph();

        public SubGraph subGraph
        {
            get { return m_MaterialSubGraph; }
            set { m_MaterialSubGraph = value; }
        }
    }
}
