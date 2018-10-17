using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{

    public enum OutputSpace
    {
        Tangent,
        World
    };

    [Title("Artistic", "Normal", "Normal From Height")]
    public class NormalFromHeightNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal, IMayRequirePosition
    {
        public NormalFromHeightNode()
        {
            name = "Normal From Height";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-From-Heightmap-Node"; }
        }

        [SerializeField]
        private OutputSpace m_OutputSpace = OutputSpace.Tangent;

        [EnumControl("Output Space")]
        public OutputSpace outputSpace
        {
            get { return m_OutputSpace; }
            set
            {
                if (m_OutputSpace == value)
                    return;

                m_OutputSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        const int InputSlotId = 0;
        const int OutputSlotId = 1;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        string GetFunctionName()
        {
            return string.Format("Unity_NormalFromHeight_{0}", outputSpace.ToString());
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, 0));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();

            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}3x3 _{1}_TangentMatrix = {0}3x3(IN.{2}SpaceTangent, IN.{2}SpaceBiTangent, IN.{2}SpaceNormal);", precision, GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            sb.AppendLine("{0}3 _{1}_Position = IN.{2}SpacePosition;", precision, GetVariableNameForNode(), NeededCoordinateSpace.World.ToString());
            
            sb.AppendLine("{0}({1},_{2}_Position,_{2}_TangentMatrix, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}({2} In, {1}3 Position, {1}3x3 TangentMatrix, out {3} Out)",
                        GetFunctionName(),
                        precision,
                        FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("{0}3 worldDirivativeX = ddx(Position * 100);", precision);
                        s.AppendLine("{0}3 worldDirivativeY = ddy(Position * 100);", precision);
                        s.AppendNewLine();
                        s.AppendLine("{0}3 crossX = cross(TangentMatrix[2].xyz, worldDirivativeX);", precision);
                        s.AppendLine("{0}3 crossY = cross(TangentMatrix[2].xyz, worldDirivativeY);", precision);
                        s.AppendLine("{0}3 d = abs(dot(crossY, worldDirivativeX));", precision);
                        s.AppendLine("{0}3 inToNormal = ((((In + ddx(In)) - In) * crossY) + (((In + ddy(In)) - In) * crossX)) * sign(d);", precision);
                        s.AppendLine("inToNormal.y *= -1.0;", precision);
                        s.AppendNewLine();
                        s.AppendLine("Out = normalize((d * TangentMatrix[2].xyz) - inToNormal);", precision);

                        if(outputSpace == OutputSpace.Tangent)
                            s.AppendLine("Out = TransformWorldToTangent(Out, TangentMatrix);");
                    }
                });
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }
        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.World;
        }
	}
}