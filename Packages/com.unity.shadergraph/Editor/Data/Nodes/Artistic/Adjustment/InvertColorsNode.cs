using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Invert Colors")]
    public class InvertColorsNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public InvertColorsNode()
        {
            name = "Invert Colors";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Invert-Colors-Node"; }
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
            return "Unity_InvertColors_" + NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        int channelCount { get { return SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType); } }

        [SerializeField]
        private bool m_RedChannel;

        [ToggleControl("Red")]
        public ToggleData redChannel
        {
            get { return new ToggleData(m_RedChannel, channelCount > 0); }
            set
            {
                if (m_RedChannel == value.isOn)
                    return;
                m_RedChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private bool m_GreenChannel;

        [ToggleControl("Green")]
        public ToggleData greenChannel
        {
            get { return new ToggleData(m_GreenChannel, channelCount > 1); }
            set
            {
                if (m_GreenChannel == value.isOn)
                    return;
                m_GreenChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        private bool m_BlueChannel;

        [ToggleControl("Blue")]
        public ToggleData blueChannel
        {
            get { return new ToggleData(m_BlueChannel, channelCount > 2); }
            set
            {
                if (m_BlueChannel == value.isOn)
                    return;
                m_BlueChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        private bool m_AlphaChannel;

        [ToggleControl("Alpha")]
        public ToggleData alphaChannel
        {
            get { return new ToggleData(m_AlphaChannel, channelCount > 3); }
            set
            {
                if (m_AlphaChannel == value.isOn)
                    return;
                m_AlphaChannel = value.isOn;
                Dirty(ModificationScope.Node);
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();

            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlotId));

            if (!generationMode.IsPreview())
            {
                sb.AppendLine("{0} _{1}_InvertColors = {0} ({2}",
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision),
                    GetVariableNameForNode(),
                    Convert.ToInt32(m_RedChannel));
                if (channelCount > 1)
                    sb.Append(", {0}", Convert.ToInt32(m_GreenChannel));
                if (channelCount > 2)
                    sb.Append(", {0}", Convert.ToInt32(m_BlueChannel));
                if (channelCount > 3)
                    sb.Append(", {0}", Convert.ToInt32(m_AlphaChannel));
                sb.Append(");");
            }

            sb.AppendLine("{0}({1}, _{2}_InvertColors, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Vector4)
            {
                name = string.Format("_{0}_InvertColors", GetVariableNameForNode()),
                vector4Value = new Vector4(Convert.ToInt32(m_RedChannel), Convert.ToInt32(m_GreenChannel), Convert.ToInt32(m_BlueChannel), Convert.ToInt32(m_AlphaChannel)),
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector4ShaderProperty
            {
                overrideReferenceName = string.Format("_{0}_InvertColors", GetVariableNameForNode()),
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            sb.AppendLine("void {0}({1} In, {2} InvertColors, out {3} Out)",
                GetFunctionName(),
                FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision),
                FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision),
                FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
            using (sb.BlockScope())
            {
                sb.AppendLine("Out = abs(InvertColors - In);");
            }
            visitor.AddShaderChunk(sb.ToString(), true);
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0}({1} In, {2} InvertColors, out {3} Out)",
                        GetFunctionName(),
                        FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("Out = abs(InvertColors - In);");
                    }
                });
        }
    }
}
