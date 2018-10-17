using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Artistic", "Adjustment", "Channel Mixer")]
    public class ChannelMixerNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public ChannelMixerNode()
        {
            name = "Channel Mixer";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Channel-Mixer-Node"; }
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
            return "Unity_ChannelMixer_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        [SerializeField]
        ChannelMixer m_ChannelMixer = new ChannelMixer(new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1));

        [Serializable]
        public struct ChannelMixer
        {
            public Vector3 outRed;
            public Vector3 outGreen;
            public Vector3 outBlue;

            public ChannelMixer(Vector3 red, Vector3 green, Vector3 blue)
            {
                outRed = red;
                outGreen = green;
                outBlue = blue;
            }
        }

        [ChannelMixerControl("")]
        public ChannelMixer channelMixer
        {
            get { return m_ChannelMixer; }
            set
            {
                if ((value.outRed == m_ChannelMixer.outRed) && (value.outGreen == m_ChannelMixer.outGreen) && (value.outBlue == m_ChannelMixer.outBlue))
                    return;
                m_ChannelMixer = value;
                Dirty(ModificationScope.Node);
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            sb.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId));
            if (!generationMode.IsPreview())
            {
                sb.AppendLine("{0}3 _{1}_Red = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(), channelMixer.outRed[0], channelMixer.outRed[1], channelMixer.outRed[2]);
                sb.AppendLine("{0}3 _{1}_Green = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(), channelMixer.outGreen[0], channelMixer.outGreen[1], channelMixer.outGreen[2]);
                sb.AppendLine("{0}3 _{1}_Blue = {0}3 ({2}, {3}, {4});", precision, GetVariableNameForNode(), channelMixer.outBlue[0], channelMixer.outBlue[1], channelMixer.outBlue[2]);
            }
            sb.AppendLine("{0}({1}, _{2}_Red, _{2}_Green, _{2}_Blue, {3});", GetFunctionName(), inputValue, GetVariableNameForNode(), outputValue);

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = string.Format("_{0}_Red", GetVariableNameForNode()),
                vector4Value = channelMixer.outRed
            });

            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = string.Format("_{0}_Green", GetVariableNameForNode()),
                vector4Value = channelMixer.outGreen
            });

            properties.Add(new PreviewProperty(PropertyType.Vector3)
            {
                name = string.Format("_{0}_Blue", GetVariableNameForNode()),
                vector4Value = channelMixer.outBlue
            });
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Red", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Green", GetVariableNameForNode()),
                generatePropertyBlock = false
            });

            properties.AddShaderProperty(new Vector4ShaderProperty()
            {
                overrideReferenceName = string.Format("_{0}_Blue", GetVariableNameForNode()),
                generatePropertyBlock = false
            });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0} ({1} In, {2}3 Red, {2}3 Green, {2}3 Blue, out {3} Out)",
                        GetFunctionName(),
                        FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType.ToString(precision),
                        precision,
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        s.AppendLine("Out = {0}(dot(In, Red), dot(In, Green), dot(In, Blue));",
                            FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    }
                });
        }
    }
}
