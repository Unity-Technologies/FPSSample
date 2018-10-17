using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Geometry", "Screen Position")]
    public class ScreenPositionNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireScreenPosition
    {
        public ScreenPositionNode()
        {
            name = "Screen Position";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Screen-Position-Node"; }
        }

        [SerializeField]
        private ScreenSpaceType m_ScreenSpaceType = ScreenSpaceType.Default;

        [EnumControl("Mode")]
        public ScreenSpaceType screenSpaceType
        {
            get { return m_ScreenSpaceType; }
            set
            {
                if (m_ScreenSpaceType == value)
                    return;

                m_ScreenSpaceType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        private const int kOutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(string.Format("{0}4 {1} = {2};", precision, GetVariableNameForSlot(kOutputSlotId), m_ScreenSpaceType.ToValueAsVariable()), true);
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}
