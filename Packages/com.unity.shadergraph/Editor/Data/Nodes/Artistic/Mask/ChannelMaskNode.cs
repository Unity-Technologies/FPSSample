using System;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum TextureChannel
    {
        Red,
        Green,
        Blue,
        Alpha
    }

    [Title("Artistic", "Mask", "Channel Mask")]
    public class ChannelMaskNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public ChannelMaskNode()
        {
            name = "Channel Mask";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Channel-Mask-Node"; }
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
            string channelSum = "None";
            if (channelMask != 0)
            {
                bool red = (channelMask & 1) != 0;
                bool green = (channelMask & 2) != 0;
                bool blue = (channelMask & 4) != 0;
                bool alpha = (channelMask & 8) != 0;
                channelSum = string.Format("{0}{1}{2}{3}", red ? "Red" : "", green ? "Green" : "", blue ? "Blue" : "", alpha ? "Alpha" : "");
            }
            return string.Format("Unity_ChannelMask_{0}_{1}", channelSum, precision);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector3.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public TextureChannel channel;

        [SerializeField]
        private int m_ChannelMask = -1;

        [ChannelEnumMaskControl("Channels")]
        public int channelMask
        {
            get { return m_ChannelMask; }
            set
            {
                if (m_ChannelMask == value)
                    return;

                m_ChannelMask = value;
                Dirty(ModificationScope.Graph);
            }
        }

        void ValidateChannelCount()
        {
            int channelCount = SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
            if (channelMask >= 1 << channelCount)
                channelMask = -1;
        }

        string GetFunctionPrototype(string argIn, string argOut)
        {
            return string.Format("void {0} ({1} {2}, out {3} {4})", GetFunctionName(), NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<DynamicVectorMaterialSlot>(InputSlotId).concreteValueType), argIn, NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<DynamicVectorMaterialSlot>(OutputSlotId).concreteValueType), argOut);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            ValidateChannelCount();
            string inputValue = GetSlotValue(InputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlotId, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(GetFunctionCallBody(inputValue, outputValue), true);
        }

        string GetFunctionCallBody(string inputValue, string outputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", " + outputValue + ");";
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            ValidateChannelCount();
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    int channelCount = SlotValueHelper.GetChannelCount(FindSlot<MaterialSlot>(InputSlotId).concreteValueType);
                    s.AppendLine(GetFunctionPrototype("In", "Out"));
                    using (s.BlockScope())
                    {
                        if (channelMask == 0)
                            s.AppendLine("Out = 0;");
                        else if (channelMask == -1)
                            s.AppendLine("Out = In;");
                        else
                        {
                            bool red = (channelMask & 1) != 0;
                            bool green = (channelMask & 2) != 0;
                            bool blue = (channelMask & 4) != 0;
                            bool alpha = (channelMask & 8) != 0;

                            switch (channelCount)
                            {
                                case 1:
                                    s.AppendLine("Out = In.r;");
                                    break;
                                case 2:
                                    s.AppendLine(string.Format("Out = {0}2({1}, {2});", precision,
                                        red ? "In.r" : "0", green ? "In.g" : "0"));
                                    break;
                                case 3:
                                    s.AppendLine(string.Format("Out = {0}3({1}, {2}, {3});", precision,
                                        red ? "In.r" : "0", green ? "In.g" : "0", blue ? "In.b" : "0"));
                                    break;
                                case 4:
                                    s.AppendLine(string.Format("Out = {0}4({1}, {2}, {3}, {4});", precision,
                                        red ? "In.r" : "0", green ? "In.g" : "0", blue ? "In.b" : "0", alpha ? "In.a" : "0"));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                });
        }
    }
}
