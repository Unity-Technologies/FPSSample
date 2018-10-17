using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public enum ConversionType
    {
        Position,
        Direction
    }

    [Serializable]
    public struct CoordinateSpaceConversion : IEnumConversion
    {
        public CoordinateSpace from;
        public CoordinateSpace to;

        public CoordinateSpaceConversion(CoordinateSpace from, CoordinateSpace to)
        {
            this.from = from;
            this.to = to;
        }

        Enum IEnumConversion.from
        {
            get { return from; }
            set { from = (CoordinateSpace)value; }
        }

        Enum IEnumConversion.to
        {
            get { return to; }
            set { to = (CoordinateSpace)value; }
        }
    }

    [Title("Math", "Vector", "Transform")]
    public class TransformNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireTangent, IMayRequireBitangent, IMayRequireNormal
    {
        private const int InputSlotId = 0;
        private const int OutputSlotId = 1;
        private const string kInputSlotName = "In";
        private const string kOutputSlotName = "Out";

        public TransformNode()
        {
            name = "Transform";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Transform-Node"; }
        }

        [SerializeField]
        CoordinateSpaceConversion m_Conversion = new CoordinateSpaceConversion(CoordinateSpace.Object, CoordinateSpace.World);

        [EnumConversionControl]
        public CoordinateSpaceConversion conversion
        {
            get { return m_Conversion; }
            set
            {
                if (Equals(m_Conversion, value))
                    return;
                m_Conversion = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        public ConversionType m_ConversionType = ConversionType.Position;

        [EnumControl("Type")]
        public ConversionType conversionType
        {
            get { return m_ConversionType; }
            set
            {
                if (Equals(m_ConversionType, value))
                    return;
                m_ConversionType = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlotId }, new[] { OutputSlotId });
            string inputValue = string.Format("{0}.xyz", GetSlotValue(InputSlotId, generationMode));
            string targetTransformString = "tangentTransform_" + conversion.from.ToString();
            string transposeTargetTransformString = "transposeTangent";
            string transformString = "";
            string tangentTransformSpace = conversion.from.ToString();
            bool requiresTangentTransform = false;
            bool requiresTransposeTangentTransform = false;

            if (conversion.from == CoordinateSpace.World)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToObjectDir({0})" : "TransformWorldToObject({0})", inputValue);
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = string.Format("TransformWorldToTangent({0}, {1})", inputValue, targetTransformString);
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToViewDir({0})" : "TransformWorldToView({0})", inputValue);
                }
            }
            else if (conversion.from == CoordinateSpace.Object)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformObjectToWorldDir({0})" : "TransformObjectToWorld({0})", inputValue);
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToTangent(TransformObjectToWorldDir({0}), {1})" : "TransformWorldToTangent(TransformObjectToWorld({0}), {1})", inputValue, targetTransformString);
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToViewDir(TransformObjectToWorldDir({0}))" : "TransformWorldToView(TransformObjectToWorld({0}))", inputValue);
                }
            }
            else if (conversion.from == CoordinateSpace.Tangent)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    requiresTransposeTangentTransform = true;
                    transformString = string.Format("mul({0}, {1}).xyz", inputValue, transposeTargetTransformString);
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    requiresTransposeTangentTransform = true;
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToObjectDir(mul({0}, {1}).xyz)" : "TransformWorldToObject(mul({0}, {1}).xyz)", inputValue, transposeTargetTransformString);
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    transformString = inputValue;
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    requiresTransposeTangentTransform = true;
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToViewDir(mul({0}, {1}).xyz)" : "TransformWorldToView(mul({0}, {1}).xyz)", inputValue, transposeTargetTransformString);
                }
            }
            else if (conversion.from == CoordinateSpace.View)
            {
                if (conversion.to == CoordinateSpace.World)
                {
                    transformString = string.Format("mul(UNITY_MATRIX_I_V, float4({0}, 1)).xyz", inputValue);
                }
                else if (conversion.to == CoordinateSpace.Object)
                {
                    transformString = string.Format(conversionType == ConversionType.Direction ? "TransformWorldToObjectDir(mul(UNITY_MATRIX_I_V, float4({0}, 1) ).xyz)" : "TransformWorldToObject(mul(UNITY_MATRIX_I_V, float4({0}, 1) ).xyz)", inputValue);
                }
                else if (conversion.to == CoordinateSpace.Tangent)
                {
                    requiresTangentTransform = true;
                    tangentTransformSpace = CoordinateSpace.World.ToString();
                    transformString = string.Format("TransformWorldToTangent(mul(UNITY_MATRIX_I_V, float4({0}, 1) ).xyz, {1})", inputValue, targetTransformString);
                }
                else if (conversion.to == CoordinateSpace.View)
                {
                    transformString = inputValue;
                }
            }
            if (requiresTransposeTangentTransform)
                visitor.AddShaderChunk(string.Format("float3x3 {0} = transpose(float3x3(IN.{1}SpaceTangent, IN.{1}SpaceBiTangent, IN.{1}SpaceNormal));", transposeTargetTransformString, CoordinateSpace.World.ToString()), true);
            else if (requiresTangentTransform)
                visitor.AddShaderChunk(string.Format("float3x3 {0} = float3x3(IN.{1}SpaceTangent, IN.{1}SpaceBiTangent, IN.{1}SpaceNormal);", targetTransformString, tangentTransformSpace), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                    GetVariableNameForSlot(OutputSlotId),
                    transformString), true);
        }

        bool RequiresWorldSpaceTangentTransform()
        {
            if (conversion.from == CoordinateSpace.View && conversion.to == CoordinateSpace.Tangent
                || conversion.from == CoordinateSpace.Tangent)
                return true;
            else
                return false;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability)
        {
            if (RequiresWorldSpaceTangentTransform())
                return NeededCoordinateSpace.World;
            return conversion.from.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresBitangent(ShaderStageCapability stageCapability)
        {
            if (RequiresWorldSpaceTangentTransform())
                return NeededCoordinateSpace.World;
            return conversion.from.ToNeededCoordinateSpace();
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            if (RequiresWorldSpaceTangentTransform())
                return NeededCoordinateSpace.World;
            return conversion.from.ToNeededCoordinateSpace();
        }
    }
}
