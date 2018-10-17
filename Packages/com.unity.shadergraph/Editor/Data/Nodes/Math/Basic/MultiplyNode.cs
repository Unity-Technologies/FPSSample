//using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Multiply")]
    public class MultiplyNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        public MultiplyNode()
        {
            name = "Multiply";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Multiply-Node"; }
        }

        const int Input1SlotId = 0;
        const int Input2SlotId = 1;
        const int OutputSlotId = 2;
        const string kInput1SlotName = "A";
        const string kInput2SlotName = "B";
        const string kOutputSlotName = "Out";

        public enum MultiplyType
        {
            Vector,
            Matrix,
            Mixed
        }

        MultiplyType m_MultiplyType;

        public override bool hasPreview
        {
            get { return m_MultiplyType != MultiplyType.Matrix; }
        }

        string GetFunctionHeader()
        {
            return string.Format("Unity_Multiply_{0}", precision);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicValueMaterialSlot(Input1SlotId, kInput1SlotName, kInput1SlotName, SlotType.Input, Matrix4x4.zero));
            AddSlot(new DynamicValueMaterialSlot(Input2SlotId, kInput2SlotName, kInput2SlotName, SlotType.Input, new Matrix4x4(new Vector4(2, 2, 2, 2), new Vector4(2, 2, 2, 2), new Vector4(2, 2, 2, 2), new Vector4(2, 2, 2, 2))));
            AddSlot(new DynamicValueMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Matrix4x4.zero));
            RemoveSlotsNameNotMatching(new[] { Input1SlotId, Input2SlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode)
        {
            var sb = new ShaderStringBuilder();
            var input1Value = GetSlotValue(Input1SlotId, generationMode);
            var input2Value = GetSlotValue(Input2SlotId, generationMode);
            var outputValue = GetSlotValue(OutputSlotId, generationMode);

            sb.AppendLine("{0} {1};", NodeUtils.ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId));
            sb.AppendLine("{0}({1}, {2}, {3});", GetFunctionHeader(), input1Value, input2Value, outputValue);

            visitor.AddShaderChunk(sb.ToString(), false);
        }

        string GetFunctionName()
        {
            return string.Format("{0}_{1}_{2}",
                GetFunctionHeader(),
                FindInputSlot<MaterialSlot>(Input1SlotId).concreteValueType.ToString(precision),
                FindInputSlot<MaterialSlot>(Input2SlotId).concreteValueType.ToString(precision));
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {
            registry.ProvideFunction(GetFunctionName(), s =>
                {
                    s.AppendLine("void {0} ({1} A, {2} B, out {3} Out)",
                        GetFunctionHeader(),
                        FindInputSlot<MaterialSlot>(Input1SlotId).concreteValueType.ToString(precision),
                        FindInputSlot<MaterialSlot>(Input2SlotId).concreteValueType.ToString(precision),
                        FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                    using (s.BlockScope())
                    {
                        switch (m_MultiplyType)
                        {
                            case MultiplyType.Vector:
                                s.AppendLine("Out = A * B;");
                                break;
                            default:
                                s.AppendLine("Out = mul(A, B);");
                                break;
                        }
                    }
                });
        }

        // Internal validation
        // -------------------------------------------------

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

            var dynamicInputSlotsToCompare = DictionaryPool<DynamicValueMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicValueMaterialSlot>.Get();

            // iterate the input slots
            s_TempSlots.Clear();
            GetInputSlots(s_TempSlots);
            foreach (var inputSlot in s_TempSlots)
            {
                // if there is a connection
                var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                if (!edges.Any())
                {
                    if (inputSlot is DynamicValueMaterialSlot)
                        skippedDynamicSlots.Add(inputSlot as DynamicValueMaterialSlot);
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
                if (inputSlot is DynamicValueMaterialSlot)
                {
                    dynamicInputSlotsToCompare.Add((DynamicValueMaterialSlot)inputSlot, outputConcreteType);
                    continue;
                }

                // if we have a standard connection... just check the types work!
                if (!ImplicitConversionExists(outputConcreteType, inputSlot.concreteValueType))
                    inputSlot.hasError = true;
            }

            m_MultiplyType = GetMultiplyType(dynamicInputSlotsToCompare.Values);

            // Resolve dynamics depending on matrix/vector configuration
            switch (m_MultiplyType)
            {
                // If all matrix resolve as per dynamic matrix
                case MultiplyType.Matrix:
                    var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                    foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                        dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
                    foreach (var skippedSlot in skippedDynamicSlots)
                        skippedSlot.SetConcreteType(dynamicMatrixType);
                    break;
                // If mixed handle differently:
                // Iterate all slots and set their concretes based on their edges
                // Find matrix slot and convert its type to a vector type
                // Reiterate all slots and set non matrix slots to the vector type
                case MultiplyType.Mixed:
                    foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                    {
                        SetConcreteValueTypeFromEdge(dynamicKvP.Key);
                    }
                    MaterialSlot matrixSlot = GetMatrixSlot();
                    ConcreteSlotValueType vectorType = SlotValueHelper.ConvertMatrixToVectorType(matrixSlot.concreteValueType);
                    foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                    {
                        if (dynamicKvP.Key != matrixSlot)
                            dynamicKvP.Key.SetConcreteType(vectorType);
                    }
                    foreach (var skippedSlot in skippedDynamicSlots)
                    {
                        skippedSlot.SetConcreteType(vectorType);
                    }
                    break;
                // If all vector resolve as per dynamic vector
                default:
                    var dynamicVectorType = ConvertDynamicInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                    foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                        dynamicKvP.Key.SetConcreteType(dynamicVectorType);
                    foreach (var skippedSlot in skippedDynamicSlots)
                        skippedSlot.SetConcreteType(dynamicVectorType);
                    break;
            }

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

                if (outputSlot is DynamicValueMaterialSlot)
                {
                    // Apply similar logic to output slot
                    switch (m_MultiplyType)
                    {
                        // As per dynamic matrix
                        case MultiplyType.Matrix:
                            var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                            (outputSlot as DynamicValueMaterialSlot).SetConcreteType(dynamicMatrixType);
                            break;
                        // Mixed configuration
                        // Find matrix slot and convert type to vector
                        // Set output concrete to vector
                        case MultiplyType.Mixed:
                            MaterialSlot matrixSlot = GetMatrixSlot();
                            ConcreteSlotValueType vectorType = SlotValueHelper.ConvertMatrixToVectorType(matrixSlot.concreteValueType);
                            (outputSlot as DynamicValueMaterialSlot).SetConcreteType(vectorType);
                            break;
                        // As per dynamic vector
                        default:
                            var dynamicVectorType = ConvertDynamicInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
                            (outputSlot as DynamicValueMaterialSlot).SetConcreteType(dynamicVectorType);
                            break;
                    }
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

            ListPool<DynamicValueMaterialSlot>.Release(skippedDynamicSlots);
            DictionaryPool<DynamicValueMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);
        }

        protected override bool CalculateNodeHasError()
        {
            if (m_MultiplyType == MultiplyType.Matrix)
            {
                foreach (var slot in this.GetOutputSlots<ISlot>())
                {
                    foreach (var edge in owner.GetEdges(slot.slotReference))
                    {
                        var inputNode = owner.GetNodeFromGuid(edge.inputSlot.nodeGuid);
                        List<MaterialSlot> slots = new List<MaterialSlot>();
                        inputNode.GetInputSlots(slots);
                        foreach (var s in slots)
                        {
                            foreach (var inputEdge in inputNode.owner.GetEdges(s.slotReference))
                            {
                                if (inputEdge == edge)
                                {
                                    if (s as DynamicValueMaterialSlot == null)
                                    {
                                        if (s.concreteValueType != ConcreteSlotValueType.Matrix4
                                            && s.concreteValueType != ConcreteSlotValueType.Matrix3
                                            && s.concreteValueType != ConcreteSlotValueType.Matrix2)
                                        {
                                            Debug.Log("ERROR: slot " + s.displayName + " cannot accept a Matrix type input");
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private MultiplyType GetMultiplyType(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as List<ConcreteSlotValueType> ?? inputTypes.ToList();
            int matrixCount = 0;
            int vectorCount = 0;
            for (int i = 0; i < concreteSlotValueTypes.Count; i++)
            {
                if (concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector4
                    || concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector3
                    || concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector2
                    || concreteSlotValueTypes[i] == ConcreteSlotValueType.Vector1)
                {
                    vectorCount++;
                }
                else if (concreteSlotValueTypes[i] == ConcreteSlotValueType.Matrix4
                         || concreteSlotValueTypes[i] == ConcreteSlotValueType.Matrix3
                         || concreteSlotValueTypes[i] == ConcreteSlotValueType.Matrix2)
                {
                    matrixCount++;
                }
            }
            if (matrixCount == 2)
                return MultiplyType.Matrix;
            else if (vectorCount == 2)
                return MultiplyType.Vector;
            else if (matrixCount == 1)
                return MultiplyType.Mixed;
            else
                return MultiplyType.Vector;
        }

        private MaterialSlot GetMatrixSlot()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            GetInputSlots(slots);
            for (int i = 0; i < slots.Count; i++)
            {
                var edges = owner.GetEdges(slots[i].slotReference).ToList();
                if (!edges.Any())
                    continue;
                var outputNode = owner.GetNodeFromGuid(edges[0].outputSlot.nodeGuid);
                var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edges[0].outputSlot.slotId);
                if (outputSlot.concreteValueType == ConcreteSlotValueType.Matrix4
                    || outputSlot.concreteValueType == ConcreteSlotValueType.Matrix3
                    || outputSlot.concreteValueType == ConcreteSlotValueType.Matrix2)
                    return slots[i];
            }
            return null;
        }

        private void SetConcreteValueTypeFromEdge(DynamicValueMaterialSlot slot)
        {
            var edges = owner.GetEdges(slot.slotReference).ToList();
            if (!edges.Any())
                return;
            var outputNode = owner.GetNodeFromGuid(edges[0].outputSlot.nodeGuid);
            var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(edges[0].outputSlot.slotId);
            slot.SetConcreteType(outputSlot.concreteValueType);
        }
    }
}
