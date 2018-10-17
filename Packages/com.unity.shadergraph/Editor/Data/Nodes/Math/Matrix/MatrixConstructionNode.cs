using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Matrix", "Matrix Construction")]
    public class MatrixConstructionNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        const string kInputSlotM0Name = "M0";
        const string kInputSlotM1Name = "M1";
        const string kInputSlotM2Name = "M2";
        const string kInputSlotM3Name = "M3";
        const string kOutput4x4SlotName = "4x4";
        const string kOutput3x3SlotName = "3x3";
        const string kOutput2x2SlotName = "2x2";

        public const int InputSlotM0Id = 0;
        public const int InputSlotM1Id = 1;
        public const int InputSlotM2Id = 2;
        public const int InputSlotM3Id = 3;
        public const int Output4x4SlotId = 4;
        public const int Output3x3SlotId = 5;
        public const int Output2x2SlotId = 6;

        public MatrixConstructionNode()
        {
            name = "Matrix Construction";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Matrix-Construction-Node"; }
        }

        [SerializeField]
        MatrixAxis m_Axis;

        [EnumControl("")]
        MatrixAxis axis
        {
            get { return m_Axis; }
            set
            {
                if (m_Axis.Equals(value))
                    return;
                m_Axis = value;
                Dirty(ModificationScope.Graph);
            }
        }

        string GetFunctionName()
        {
            return string.Format("Unity_MatrixConstruction_{0}", precision);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(InputSlotM0Id, kInputSlotM0Name, kInputSlotM0Name, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(InputSlotM1Id, kInputSlotM1Name, kInputSlotM1Name, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(InputSlotM2Id, kInputSlotM2Name, kInputSlotM2Name, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(InputSlotM3Id, kInputSlotM3Name, kInputSlotM3Name, SlotType.Input, Vector4.zero));
            AddSlot(new Matrix4MaterialSlot(Output4x4SlotId, kOutput4x4SlotName, kOutput4x4SlotName, SlotType.Output));
            AddSlot(new Matrix3MaterialSlot(Output3x3SlotId, kOutput3x3SlotName, kOutput3x3SlotName, SlotType.Output));
            AddSlot(new Matrix2MaterialSlot(Output2x2SlotId, kOutput2x2SlotName, kOutput2x2SlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new int[] { InputSlotM0Id, InputSlotM1Id, InputSlotM2Id, InputSlotM3Id, Output4x4SlotId, Output3x3SlotId, Output2x2SlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            var inputM0Value = GetSlotValue(InputSlotM0Id, generationMode);
            var inputM1Value = GetSlotValue(InputSlotM1Id, generationMode);
            var inputM2Value = GetSlotValue(InputSlotM2Id, generationMode);
            var inputM3Value = GetSlotValue(InputSlotM3Id, generationMode);

            sb.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(Output4x4SlotId).concreteValueType), GetVariableNameForSlot(Output4x4SlotId));
            sb.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(Output3x3SlotId).concreteValueType), GetVariableNameForSlot(Output3x3SlotId));
            sb.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(Output2x2SlotId).concreteValueType), GetVariableNameForSlot(Output2x2SlotId));
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6}, {7});",
                GetFunctionName(),
                inputM0Value,
                inputM1Value,
                inputM2Value,
                inputM3Value,
                GetVariableNameForSlot(Output4x4SlotId),
                GetVariableNameForSlot(Output3x3SlotId),
                GetVariableNameForSlot(Output2x2SlotId));

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0} ({1} M0, {1} M1, {1} M2, {1} M3, out {2} Out4x4, out {3} Out3x3, out {4} Out2x2)",
                        GetFunctionName(),
                        FindInputSlot<MaterialSlot>(InputSlotM0Id).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(Output4x4SlotId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(Output3x3SlotId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(Output2x2SlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        switch (m_Axis)
                        {
                            case MatrixAxis.Column:
                                s.AppendLine("Out4x4 = {0}4x4(M0.x, M1.x, M2.x, M3.x, M0.y, M1.y, M2.y, M3.y, M0.z, M1.z, M2.z, M3.z, M0.w, M1.w, M2.w, M3.w);", precision);
                                s.AppendLine("Out3x3 = {0}3x3(M0.x, M1.x, M2.x, M0.y, M1.y, M2.y, M0.z, M1.z, M2.z);", precision);
                                s.AppendLine("Out2x2 = {0}2x2(M0.x, M1.x, M0.y, M1.y);", precision);
                                break;
                            default:
                                s.AppendLine("Out4x4 = {0}4x4(M0.x, M0.y, M0.z, M0.w, M1.x, M1.y, M1.z, M1.w, M2.x, M2.y, M2.z, M2.w, M3.x, M3.y, M3.z, M3.w);", precision);
                                s.AppendLine("Out3x3 = {0}3x3(M0.x, M0.y, M0.z, M1.x, M1.y, M1.z, M2.x, M2.y, M2.z);", precision);
                                s.AppendLine("Out2x2 = {0}2x2(M0.x, M0.y, M1.x, M1.y);", precision);
                                break;
                        }
                    }
                });
        }
    }
}
