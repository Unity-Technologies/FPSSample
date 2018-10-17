using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 3")]
    public class Vector3Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector3 m_Value = Vector3.zero;

        const string kInputSlotXName = "X";
        const string kInputSlotYName = "Y";
        const string kInputSlotZName = "Z";
        const string kOutputSlotName = "Out";

        public const int OutputSlotId = 0;
        public const int InputSlotXId = 1;
        public const int InputSlotYId = 2;
        public const int InputSlotZId = 3;


        public Vector3Node()
        {
            name = "Vector 3";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Vector-3-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1MaterialSlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector1MaterialSlot(InputSlotZId, kInputSlotZName, kInputSlotZName, SlotType.Input, m_Value.z, label1: "Z"));
            AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId, InputSlotZId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var inputZValue = GetSlotValue(InputSlotZId, generationMode);
            var outputName = GetVariableNameForSlot(outputSlotId);

            var s = string.Format("{0}3 {1} = {0}3({2},{3},{4});",
                    precision,
                    outputName,
                    inputXValue,
                    inputYValue,
                    inputZValue);
            visitor.AddShaderChunk(s, false);
        }

        public IShaderProperty AsShaderProperty()
        {
            var slotX = FindInputSlot<Vector1MaterialSlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1MaterialSlot>(InputSlotYId);
            var slotZ = FindInputSlot<Vector1MaterialSlot>(InputSlotZId);
            return new Vector3ShaderProperty { value = new Vector3(slotX.value, slotY.value, slotZ.value) };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
