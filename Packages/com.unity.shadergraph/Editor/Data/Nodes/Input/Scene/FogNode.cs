using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Fog")]
    public class FogNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition
    {
        public FogNode()
        {
            name = "Fog";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Fog-Node"; }
        }

        const int OutputSlotId = 0;
        const int OutputSlot1Id = 1;
        const string k_OutputSlotName = "Color";
        const string k_OutputSlot1Name = "Density";

        public override bool hasPreview
        {
            get { return false; }
        }

        string GetFunctionName()
        {
            return string.Format("Unity_Fog_{0}", precision);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlot1Id, k_OutputSlot1Name, k_OutputSlot1Name, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(string.Format("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlotId)), false);
            visitor.AddShaderChunk(string.Format("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlot1Id)), false);
            visitor.AddShaderChunk(string.Format("{0}(IN.{1}, {2}, {3});", GetFunctionName(),
                    CoordinateSpace.Object.ToVariableName(InterpolatorType.Position),
                    GetVariableNameForSlot(OutputSlotId), GetVariableNameForSlot(OutputSlot1Id)), false);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}({1}3 ObjectSpacePosition, out {2} Color, out {3} Density)",
                        GetFunctionName(),
                        precision,
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("Color = unity_FogColor;");

                        s.AppendLine("{0} clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), ObjectSpacePosition)).z);", precision);
                        s.AppendLine("#if defined(FOG_LINEAR)");
                        using (s.IndentScope())
                        {
                            s.AppendLine("{0} fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);", precision);
                            s.AppendLine("Density = fogFactor;");
                        }
                        s.AppendLine("#elif defined(FOG_EXP)");
                        using (s.IndentScope())
                        {
                            s.AppendLine("{0} fogFactor = unity_FogParams.y * clipZ_01;", precision);
                            s.AppendLine("Density = saturate(exp2(-fogFactor));");
                        }
                        s.AppendLine("#elif defined(FOG_EXP2)");
                        using (s.IndentScope())
                        {
                            s.AppendLine("{0} fogFactor = unity_FogParams.x * clipZ_01;", precision);
                            s.AppendLine("Density = saturate(exp2(-fogFactor*fogFactor));");
                        }
                        s.AppendLine("#else");
                        using (s.IndentScope())
                        {
                            s.AppendLine("Density = 0.0h;");
                        }
                        s.AppendLine("#endif");
                    }
                });
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return CoordinateSpace.Object.ToNeededCoordinateSpace();
        }
    }
}
