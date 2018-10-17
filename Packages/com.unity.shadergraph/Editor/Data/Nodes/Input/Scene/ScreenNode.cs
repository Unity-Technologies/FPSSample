using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Screen")]
    public sealed class ScreenNode : AbstractMaterialNode
    {
        const string kOutputSlotName = "Width";
        const string kOutputSlot1Name = "Height";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;

        public ScreenNode()
        {
            name = "Screen";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Screen-Node"; }
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return "_ScreenParams.y";
                default:
                    return "_ScreenParams.x";
            }
        }
    }
}
