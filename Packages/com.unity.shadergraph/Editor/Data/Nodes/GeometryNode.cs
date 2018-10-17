using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    public abstract class GeometryNode : AbstractMaterialNode
    {
        [SerializeField]
        private CoordinateSpace m_Space = CoordinateSpace.World;

        [EnumControl("Space")]
        public CoordinateSpace space
        {
            get { return m_Space; }
            set
            {
                if (m_Space == value)
                    return;

                m_Space = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }
    }
}
