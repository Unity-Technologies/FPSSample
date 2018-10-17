using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Split")]
    public class SplitNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        const string kInputSlotName = "In";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";

        public const int InputSlotId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;

        public SplitNode()
        {
            name = "Split";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Split-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new int[] { InputSlotId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId });
        }

        static int[] s_OutputSlots = {OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId};

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);

            var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);
            var numInputChannels = 0;
            if (inputSlot != null)
            {
                numInputChannels = SlotValueHelper.GetChannelCount(inputSlot.concreteValueType);
                if (numInputChannels > 4)
                    numInputChannels = 0;

                if (!owner.GetEdges(inputSlot.slotReference).Any())
                    numInputChannels = 0;
            }

            for (var i = 0; i < 4; i++)
            {
                var outputFormat = numInputChannels == 1 ? inputValue : string.Format("{0}[{1}]", inputValue, i);
                var outputValue = i >= numInputChannels ? "0" : outputFormat;
                visitor.AddShaderChunk(string.Format("{0} {1} = {2};", precision, GetVariableNameForSlot(s_OutputSlots[i]), outputValue), true);
            }
        }
    }
}
