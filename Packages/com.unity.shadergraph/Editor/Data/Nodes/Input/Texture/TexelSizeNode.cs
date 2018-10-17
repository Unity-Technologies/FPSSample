using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    
    [Title("Input", "Texture", "Texel Size")]
    public class Texture2DPropertiesNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotWId = 0;
        public const int OutputSlotHId = 2;
        public const int TextureInputId = 1;
        const string kOutputSlotWName = "Width";
        const string kOutputSlotHName = "Height";
        const string kTextureInputName = "Texture";

        public override bool hasPreview { get { return false; } }

        public Texture2DPropertiesNode()
        {
            name = "Texel Size";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Texel-Size-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotWId, kOutputSlotWName, kOutputSlotWName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotHId, kOutputSlotHName, kOutputSlotHName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            RemoveSlotsNameNotMatching(new[] { OutputSlotWId, OutputSlotHId, TextureInputId });
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
			visitor.AddShaderChunk(string.Format("{0}2 {1} = {2}_TexelSize.z;", precision, GetVariableNameForSlot(OutputSlotWId), GetSlotValue(TextureInputId, generationMode)), true);
			visitor.AddShaderChunk(string.Format("{0}2 {1} = {2}_TexelSize.w;", precision, GetVariableNameForSlot(OutputSlotHId), GetSlotValue(TextureInputId, generationMode)), true);
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            return true;
        }
    }
}