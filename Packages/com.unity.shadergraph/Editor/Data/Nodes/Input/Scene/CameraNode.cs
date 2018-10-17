using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Camera")]
    public class CameraNode : AbstractMaterialNode
    {
        const string kOutputSlotName = "Position";
        const string kOutputSlot1Name = "Direction";
        const string kOutputSlot2Name = "Orthographic";
        const string kOutputSlot3Name = "Near Plane";
        const string kOutputSlot4Name = "Far Plane";
        const string kOutputSlot5Name = "Z Buffer Sign";
        const string kOutputSlot6Name = "Width";
        const string kOutputSlot7Name = "Height";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;
        public const int OutputSlot2Id = 2;
        public const int OutputSlot3Id = 3;
        public const int OutputSlot4Id = 4;
        public const int OutputSlot5Id = 5;
        public const int OutputSlot6Id = 6;
        public const int OutputSlot7Id = 7;

        public CameraNode()
        {
            name = "Camera";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Camera-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlot2Id, kOutputSlot2Name, kOutputSlot2Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot3Id, kOutputSlot3Name, kOutputSlot3Name, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSlot4Id, kOutputSlot4Name, kOutputSlot4Name, SlotType.Output, 1));
            AddSlot(new Vector1MaterialSlot(OutputSlot5Id, kOutputSlot5Name, kOutputSlot5Name, SlotType.Output, 1));
            AddSlot(new Vector1MaterialSlot(OutputSlot6Id, kOutputSlot6Name, kOutputSlot6Name, SlotType.Output, 1));
            AddSlot(new Vector1MaterialSlot(OutputSlot7Id, kOutputSlot7Name, kOutputSlot7Name, SlotType.Output, 1));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id, OutputSlot2Id, OutputSlot3Id, OutputSlot4Id, OutputSlot5Id, OutputSlot6Id, OutputSlot7Id });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            switch (slotId)
            {
                case OutputSlot1Id:
                    return "-1 * mul(unity_ObjectToWorld, transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V)) [2].xyz)";
                case OutputSlot2Id:
                    return "unity_OrthoParams.w";
                case OutputSlot3Id:
                    return "_ProjectionParams.y";
                case OutputSlot4Id:
                    return "_ProjectionParams.z";
                case OutputSlot5Id:
                    return "_ProjectionParams.x";
                case OutputSlot6Id:
                    return "unity_OrthoParams.x";
                case OutputSlot7Id:
                    return "unity_OrthoParams.y";
                default:
                    return "_WorldSpaceCameraPos";
            }
        }
    }
}
