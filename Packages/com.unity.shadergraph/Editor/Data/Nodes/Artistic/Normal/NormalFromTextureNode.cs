using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [FormerName("UnityEditor.ShaderGraph.NormalCreateNode")]
    [Title("Artistic", "Normal", "Normal From Texture")]
    public class NormalFromTextureNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IGenerateProperties, IMayRequireMeshUV
    {
        public const int TextureInputId = 0;
        public const int UVInputId = 1;
        public const int SamplerInputId = 2;
        public const int OffsetInputId = 3;
        public const int StrengthInputId = 4;
        public const int OutputSlotId = 5;

        const string k_TextureInputName = "Texture";
        const string k_UVInputName = "UV";
        const string k_SamplerInputName = "Sampler";
        const string k_OffsetInputName = "Offset";
        const string k_StrengthInputName = "Strength";
        const string k_OutputSlotName = "Out";

        public NormalFromTextureNode()
        {
            name = "Normal From Texture";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-From-Texture-Node"; }
        }

        string GetFunctionName()
        {
            return string.Format("Unity_NormalFromTexture_{0}", precision);
        }

        public override bool hasPreview { get { return true; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, k_TextureInputName, k_TextureInputName));
            AddSlot(new UVMaterialSlot(UVInputId, k_UVInputName, k_UVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, k_SamplerInputName, k_SamplerInputName, SlotType.Input));
            AddSlot(new Vector1MaterialSlot(OffsetInputId, k_OffsetInputName, k_OffsetInputName, SlotType.Input, 0.5f));
            AddSlot(new Vector1MaterialSlot(StrengthInputId, k_StrengthInputName, k_StrengthInputName, SlotType.Input, 8f));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { TextureInputId, UVInputId, SamplerInputId, OffsetInputId, StrengthInputId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var textureValue = GetSlotValue(TextureInputId, generationMode);
            var uvValue = GetSlotValue(UVInputId, generationMode);
            var offsetValue = GetSlotValue(OffsetInputId, generationMode);
            var strengthValue = GetSlotValue(StrengthInputId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInputId);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);
            string samplerValue;
            if (edgesSampler.Any())
                samplerValue = GetSlotValue(SamplerInputId, generationMode);
            else
                samplerValue = string.Format("sampler{0}", GetSlotValue(TextureInputId, generationMode));

            var sb = new ShaderStringBuilder();
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6});", GetFunctionName(), textureValue, samplerValue, uvValue, offsetValue, strengthValue, outputValue);

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            sb.AppendLine("void {0}({1} Texture, {2} Sampler, {3} UV, {4} Offset, {5} Strength, out {6} Out)", GetFunctionName(),
                FindInputSlot<MaterialSlot>(TextureInputId).concreteValueType.ToString(precision),
                FindInputSlot<MaterialSlot>(SamplerInputId).concreteValueType.ToString(precision),
                FindInputSlot<MaterialSlot>(UVInputId).concreteValueType.ToString(precision),
                FindInputSlot<MaterialSlot>(OffsetInputId).concreteValueType.ToString(precision),
                FindInputSlot<MaterialSlot>(StrengthInputId).concreteValueType.ToString(precision),
                FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
            using (sb.BlockScope())
            {
                sb.AppendLine("Offset = pow(Offset, 3) * 0.1;");
                sb.AppendLine("{0}2 offsetU = float2(UV.x + Offset, UV.y);", precision);
                sb.AppendLine("{0}2 offsetV = float2(UV.x, UV.y + Offset);", precision);

                sb.AppendLine("{0} normalSample = Texture.Sample(Sampler, UV).x;", precision);
                sb.AppendLine("{0} uSample = Texture.Sample(Sampler, offsetU).x;", precision);
                sb.AppendLine("{0} vSample = Texture.Sample(Sampler, offsetV).x;", precision);

                sb.AppendLine("{0}3 va = float3(1, 0, (uSample - normalSample) * Strength);", precision);
                sb.AppendLine("{0}3 vb = float3(0, 1, (vSample - normalSample) * Strength);", precision);
                sb.AppendLine("Out = normalize(cross(va, vb));");
            }

            visitor.AddShaderChunk(sb.ToString(), true);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}({1} Texture, {2} Sampler, {3} UV, {4} Offset, {5} Strength, out {6} Out)", GetFunctionName(),
                        FindInputSlot<MaterialSlot>(TextureInputId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(SamplerInputId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(UVInputId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(OffsetInputId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(StrengthInputId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("Offset = pow(Offset, 3) * 0.1;");
                        s.AppendLine("{0}2 offsetU = float2(UV.x + Offset, UV.y);", precision);
                        s.AppendLine("{0}2 offsetV = float2(UV.x, UV.y + Offset);", precision);

                        s.AppendLine("{0} normalSample = Texture.Sample(Sampler, UV);", precision);
                        s.AppendLine("{0} uSample = Texture.Sample(Sampler, offsetU);", precision);
                        s.AppendLine("{0} vSample = Texture.Sample(Sampler, offsetV);", precision);

                        s.AppendLine("{0}3 va = float3(1, 0, (uSample - normalSample) * Strength);", precision);
                        s.AppendLine("{0}3 vb = float3(0, 1, (vSample - normalSample) * Strength);", precision);
                        s.AppendLine("Out = normalize(cross(va, vb));");
                    }
                });
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            foreach (var slot in this.GetInputSlots<MaterialSlot>().OfType<IMayRequireMeshUV>())
            {
                if (slot.RequiresMeshUV(channel))
                    return true;
            }
            return false;
        }
    }
}
