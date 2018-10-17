namespace UnityEditor.ShaderGraph
{
    /*public abstract class FunctionMultiInput : BaseMaterialNode, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "Output";
        private const string kBaseInputSlotName = "Input";

        public override bool hasPreview { get { return true; } }

        public override void OnCreate()
        {
            base.OnCreate();
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));

            AddSlot(new Slot(SlotType.InputSlot, GetInputSlotName(0)));
            AddSlot(new Slot(SlotType.InputSlot, GetInputSlotName(1)));
        }

        protected bool IsInputSlotConnected(int index)
        {
            var inputSlot = GetValidInputSlots().FirstOrDefault(x => x.name == GetInputSlotName(index));
            if (inputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return false;
            }

            return inputSlot.edges.Count > 0;
        }

        private static string GetInputSlotName(int index) { return kBaseInputSlotName + (index); }

        public override void InputEdgeChanged(Edge e)
        {
            base.InputEdgeChanged(e);

            int inputSlotCount = GetValidInputSlots().Count();

            if (IsInputSlotConnected(inputSlotCount - 1))
                AddSlot(new Slot(SlotType.InputSlot, GetInputSlotName(inputSlotCount)));
            else if (inputSlotCount > 2)
            {
                var lastSlot = inputSlots.FirstOrDefault(x => x.name == GetInputSlotName(inputSlotCount - 1));
                if (lastSlot != null)
                    RemoveSlot(lastSlot);
            }
        }

        protected abstract string GetFunctionName();

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = outputSlots.FirstOrDefault(x => x.name == kOutputSlotName);

            if (outputSlot == null)
            {
                Debug.LogError("Invalid slot configuration on node: " + name);
                return;
            }

            var inputSlots = GetValidInputSlots();
            int inputSlotCount = inputSlots.Count();

            // build up a list of the valid input connections
            var inputValues = new List<string>(inputSlotCount);
            MaterialWindow.DebugMaterialGraph("Generating On Node: " + GetOutputVariableNameForNode() + " - Preview is: " + generationMode);
            inputValues.AddRange(inputSlots.Select(inputSlot => GetSlotValue(inputSlot, generationMode)));
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(outputSlot, generationMode) + " = " + GetFunctionCallBody(inputValues) + ";", true);
        }

        protected virtual string GetFunctionCallBody(List<string> inputValues)
        {
            string functionCall = inputValues[0];
            for (int q = 1; q < inputValues.Count; ++q)
                functionCall = GetFunctionName() + " (" + functionCall + ", " + inputValues[q] + ")";
            return functionCall;
        }
    }*/
}
