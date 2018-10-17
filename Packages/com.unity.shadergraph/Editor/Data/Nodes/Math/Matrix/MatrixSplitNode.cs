using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum MatrixAxis
    {
        Row,
        Column
    }

    [Title("Math", "Matrix", "Matrix Split")]
    public class MatrixSplitNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        const string kInputSlotName = "In";
        const string kOutputSlotM0Name = "M0";
        const string kOutputSlotM1Name = "M1";
        const string kOutputSlotM2Name = "M2";
        const string kOutputSlotM3Name = "M3";

        public const int InputSlotId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;

        public MatrixSplitNode()
        {
            name = "Matrix Split";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Matrix-Split-Node"; }
        }

        [SerializeField]
        MatrixAxis m_Axis;

        [EnumControl("")]
        MatrixAxis axis
        {
            get { return m_Axis; }
            set
            {
                if (m_Axis.Equals(value))
                    return;
                m_Axis = value;
                Dirty(ModificationScope.Graph);
            }
        }

        static string[] s_ComponentList = new string[4] { "r", "g", "b", "a" };

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicMatrixMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotRId, kOutputSlotM0Name, kOutputSlotM0Name, SlotType.Output, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotGId, kOutputSlotM1Name, kOutputSlotM1Name, SlotType.Output, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotBId, kOutputSlotM2Name, kOutputSlotM2Name, SlotType.Output, Vector4.zero));
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotAId, kOutputSlotM3Name, kOutputSlotM3Name, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new int[] { InputSlotId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId });
        }

        static int[] s_OutputSlots = {OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId};

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotId, generationMode);

            var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);
            var numInputRows = 0;
            bool useIndentity = false;

            if (inputSlot != null)
            {
                numInputRows = SlotValueHelper.GetMatrixDimension(inputSlot.concreteValueType);
                if (numInputRows > 4)
                    numInputRows = 0;

                if (!owner.GetEdges(inputSlot.slotReference).Any())
                {
                    numInputRows = 0;
                    useIndentity = true;
                }
            }

            int concreteRowCount = useIndentity ? 2 : numInputRows;

            for (var r = 0; r < 4; r++)
            {
                string outputValue;
                if (r >= numInputRows)
                {
                    outputValue = string.Format("{0}{1}(", precision, concreteRowCount);
                    for (int c = 0; c < concreteRowCount; c++)
                    {
                        if (c != 0)
                            outputValue += ", ";
                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                    }
                    outputValue += ")";
                }
                else
                {
                    switch (m_Axis)
                    {
                        case MatrixAxis.Column:
                            outputValue = string.Format("{0}{1}(", precision, numInputRows);
                            for (int c = 0; c < numInputRows; c++)
                            {
                                if (c != 0)
                                    outputValue += ", ";
                                outputValue += string.Format("{0}[{1}].{2}", inputValue, c, s_ComponentList[r]);
                            }
                            outputValue += ")";
                            break;
                        default:
                            outputValue = string.Format("{0}[{1}]", inputValue, r);
                            break;
                    }
                }
                visitor.AddShaderChunk(string.Format("{0}{1} {2} = {3};", precision, concreteRowCount, GetVariableNameForSlot(s_OutputSlots[r]), outputValue), true);
            }
        }

        public override void ValidateNode()
        {
            var isInError = false;

            // all children nodes needs to be updated first
            // so do that here
            var slots = ListPool<MaterialSlot>.Get();
            GetInputSlots(slots);
            foreach (var inputSlot in slots)
            {
                inputSlot.hasError = false;

                var edges = owner.GetEdges(inputSlot.slotReference);
                foreach (var edge in edges)
                {
                    var fromSocketRef = edge.outputSlot;
                    var outputNode = owner.GetNodeFromGuid(fromSocketRef.nodeGuid);
                    if (outputNode == null)
                        continue;

                    outputNode.ValidateNode();
                    if (outputNode.hasError)
                        isInError = true;
                }
            }
            ListPool<MaterialSlot>.Release(slots);

            var dynamicInputSlotsToCompare = DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicVectorMaterialSlot>.Get();

            var dynamicMatrixInputSlotsToCompare = DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicMatrixSlots = ListPool<DynamicMatrixMaterialSlot>.Get();

            // iterate the input slots
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var inputSlot in s_TempSlots)
            {
                // if there is a connection
                var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                if (!edges.Any())
                {
                    if (inputSlot is DynamicVectorMaterialSlot)
                        skippedDynamicSlots.Add(inputSlot as DynamicVectorMaterialSlot);
                    if (inputSlot is DynamicMatrixMaterialSlot)
                        skippedDynamicMatrixSlots.Add(inputSlot as DynamicMatrixMaterialSlot);
                    continue;
                }

                // get the output details
                var outputSlotRef = edges[0].outputSlot;
                var outputNode = owner.GetNodeFromGuid(outputSlotRef.nodeGuid);
                if (outputNode == null)
                    continue;

                var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(outputSlotRef.slotId);
                if (outputSlot == null)
                    continue;

                if (outputSlot.hasError)
                {
                    inputSlot.hasError = true;
                    continue;
                }

                var outputConcreteType = outputSlot.concreteValueType;
                // dynamic input... depends on output from other node.
                // we need to compare ALL dynamic inputs to make sure they
                // are compatable.
                if (inputSlot is DynamicVectorMaterialSlot)
                {
                    dynamicInputSlotsToCompare.Add((DynamicVectorMaterialSlot)inputSlot, outputConcreteType);
                    continue;
                }
                else if (inputSlot is DynamicMatrixMaterialSlot)
                {
                    dynamicMatrixInputSlotsToCompare.Add((DynamicMatrixMaterialSlot)inputSlot, outputConcreteType);
                    continue;
                }

                // if we have a standard connection... just check the types work!
                if (!AbstractMaterialNode.ImplicitConversionExists(outputConcreteType, inputSlot.concreteValueType))
                    inputSlot.hasError = true;
            }

            // and now dynamic matrices
            var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicMatrixInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicMatrixInputSlotsToCompare)
                dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
            foreach (var skippedSlot in skippedDynamicMatrixSlots)
                skippedSlot.SetConcreteType(dynamicMatrixType);

            // we can now figure out the dynamic slotType
            // from here set all the
            var dynamicType = SlotValueHelper.ConvertMatrixToVectorType(dynamicMatrixType);
            foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                dynamicKvP.Key.SetConcreteType(dynamicType);
            foreach (var skippedSlot in skippedDynamicSlots)
                skippedSlot.SetConcreteType(dynamicType);

            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            var inputError = s_TempSlots.Any(x => x.hasError);

            // configure the output slots now
            // their slotType will either be the default output slotType
            // or the above dynanic slotType for dynamic nodes
            // or error if there is an input error
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            foreach (var outputSlot in s_TempSlots)
            {
                outputSlot.hasError = false;

                if (inputError)
                {
                    outputSlot.hasError = true;
                    continue;
                }

                if (outputSlot is DynamicVectorMaterialSlot)
                {
                    (outputSlot as DynamicVectorMaterialSlot).SetConcreteType(dynamicType);
                    continue;
                }
                else if (outputSlot is DynamicMatrixMaterialSlot)
                {
                    (outputSlot as DynamicMatrixMaterialSlot).SetConcreteType(dynamicMatrixType);
                    continue;
                }
            }

            isInError |= inputError;
            s_TempSlots.Clear();
            GetOutputSlots(s_TempSlots);
            isInError |= s_TempSlots.Any(x => x.hasError);
            isInError |= CalculateNodeHasError();
            hasError = isInError;

            if (!hasError)
            {
                ++version;
            }

            ListPool<DynamicVectorMaterialSlot>.Release(skippedDynamicSlots);
            DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);

            ListPool<DynamicMatrixMaterialSlot>.Release(skippedDynamicMatrixSlots);
            DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Release(dynamicMatrixInputSlotsToCompare);
        }
    }
}
