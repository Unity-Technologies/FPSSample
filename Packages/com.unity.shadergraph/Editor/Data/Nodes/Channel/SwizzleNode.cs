using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Swizzle")]
    public class SwizzleNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public SwizzleNode()
        {
            name = "Swizzle";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Swizzle-Node"; }
        }

        const int InputSlotId = 0;
        const int OutputSlotId = 1;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        static Dictionary<TextureChannel, string> s_ComponentList = new Dictionary<TextureChannel, string>
        {
            {TextureChannel.Red, "r" },
            {TextureChannel.Green, "g" },
            {TextureChannel.Blue, "b" },
            {TextureChannel.Alpha, "a" },
        };

        [SerializeField]
        TextureChannel m_RedChannel;

        [ChannelEnumControl("Red Out")]
        public TextureChannel redChannel
        {
            get { return m_RedChannel; }
            set
            {
                if (m_RedChannel == value)
                    return;

                m_RedChannel = value;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        TextureChannel m_GreenChannel;

        [ChannelEnumControl("Green Out")]
        public TextureChannel greenChannel
        {
            get { return m_GreenChannel; }
            set
            {
                if (m_GreenChannel == value)
                    return;

                m_GreenChannel = value;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        TextureChannel m_BlueChannel;

        [ChannelEnumControl("Blue Out")]
        public TextureChannel blueChannel
        {
            get { return m_BlueChannel; }
            set
            {
                if (m_BlueChannel == value)
                    return;

                m_BlueChannel = value;
                Dirty(ModificationScope.Node);
            }
        }

        [SerializeField]
        TextureChannel m_AlphaChannel;

        [ChannelEnumControl("Alpha Out")]
        public TextureChannel alphaChannel
        {
            get { return m_AlphaChannel; }
            set
            {
                if (m_AlphaChannel == value)
                    return;

                m_AlphaChannel = value;
                Dirty(ModificationScope.Node);
            }
        }

        void ValidateChannelCount()
        {
            var channelCount = SlotValueHelper.GetChannelCount(FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType);
            if ((int)redChannel >= channelCount)
                redChannel = TextureChannel.Red;
            if ((int)greenChannel >= channelCount)
                greenChannel = TextureChannel.Red;
            if ((int)blueChannel >= channelCount)
                blueChannel = TextureChannel.Red;
            if ((int)alphaChannel >= channelCount)
                alphaChannel = TextureChannel.Red;
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            ValidateChannelCount();
            var outputSlotType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision);
            var outputName = GetVariableNameForSlot(OutputSlotId);
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var inputValueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            if (inputValueType == ConcreteSlotValueType.Vector1)
                visitor.AddShaderChunk(string.Format("{0} {1} = {2};", outputSlotType, outputName, inputValue), false);
            else if (generationMode == GenerationMode.ForReals)
                visitor.AddShaderChunk(string.Format("{0} {1} = {2}.{3}{4}{5}{6};",
                        outputSlotType,
                        outputName,
                        inputValue,
                        s_ComponentList[m_RedChannel].ToString(CultureInfo.InvariantCulture),
                        s_ComponentList[m_GreenChannel].ToString(CultureInfo.InvariantCulture),
                        s_ComponentList[m_BlueChannel].ToString(CultureInfo.InvariantCulture),
                        s_ComponentList[m_AlphaChannel].ToString(CultureInfo.InvariantCulture)), false);
            else
                visitor.AddShaderChunk(string.Format("{0} {1} = {0}({3}[((int){2} >> 0) & 3], {3}[((int){2} >> 2) & 3], {3}[((int){2} >> 4) & 3], {3}[((int){2} >> 6) & 3]);",
                        outputSlotType,
                        outputName,
                        GetVariableNameForNode(), // Name of the uniform we encode swizzle values into
                        inputValue), false);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);
            if (generationMode != GenerationMode.Preview)
                return;
            properties.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            // Encode swizzle values into an integer
            var value = ((int)redChannel) | ((int)greenChannel << 2) | ((int)blueChannel << 4) | ((int)alphaChannel << 6);
            properties.Add(new PreviewProperty(PropertyType.Vector1)
            {
                name = GetVariableNameForNode(),
                floatValue = value
            });
        }
    }
}
