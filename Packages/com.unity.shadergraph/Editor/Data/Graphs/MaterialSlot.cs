using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

[assembly: InternalsVisibleTo("Unity.ShaderGraph.EditorTests")]

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public abstract class MaterialSlot : ISlot
    {
        const string k_NotInit =  "Not Initilaized";

        [SerializeField]
        int m_Id;

        [SerializeField]
        string m_DisplayName = k_NotInit;

        [SerializeField]
        SlotType m_SlotType = SlotType.Input;

        [SerializeField]
        int m_Priority = int.MaxValue;

        [SerializeField]
        bool m_Hidden;

        [SerializeField]
        string m_ShaderOutputName;

        [SerializeField]
        ShaderStageCapability m_StageCapability;

        bool m_HasError;

        protected MaterialSlot() {}

        protected MaterialSlot(int slotId, string displayName, string shaderOutputName, SlotType slotType, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
        {
            m_Id = slotId;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Hidden = hidden;
            m_ShaderOutputName = shaderOutputName;
            this.stageCapability = stageCapability;
        }

        protected MaterialSlot(int slotId, string displayName, string shaderOutputName, SlotType slotType, int priority, ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
        {
            m_Id = slotId;
            m_DisplayName = displayName;
            m_SlotType = slotType;
            m_Priority = priority;
            m_Hidden = hidden;
            m_ShaderOutputName = shaderOutputName;
            this.stageCapability = stageCapability;
        }

        public virtual VisualElement InstantiateControl()
        {
            return null;
        }

        static string ConcreteSlotValueTypeAsString(ConcreteSlotValueType type)
        {
            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                    return "(1)";
                case ConcreteSlotValueType.Vector2:
                    return "(2)";
                case ConcreteSlotValueType.Vector3:
                    return "(3)";
                case ConcreteSlotValueType.Vector4:
                    return "(4)";
                case ConcreteSlotValueType.Boolean:
                    return "(B)";
                case ConcreteSlotValueType.Matrix2:
                    return "(2x2)";
                case ConcreteSlotValueType.Matrix3:
                    return "(3x3)";
                case ConcreteSlotValueType.Matrix4:
                    return "(4x4)";
                case ConcreteSlotValueType.SamplerState:
                    return "(SS)";
                case ConcreteSlotValueType.Texture2D:
                    return "(T2)";
                case ConcreteSlotValueType.Texture2DArray:
                    return "(T2A)";
                case ConcreteSlotValueType.Texture3D:
                    return "(T3)";
                case ConcreteSlotValueType.Cubemap:
                    return "(C)";
                case ConcreteSlotValueType.Gradient:
                    return "(G)";
                default:
                    return "(E)";
            }
        }

        public virtual string displayName
        {
            get { return m_DisplayName + ConcreteSlotValueTypeAsString(concreteValueType); }
            set { m_DisplayName = value; }
        }

        public string RawDisplayName()
        {
            return m_DisplayName;
        }

        public static MaterialSlot CreateMaterialSlot(SlotValueType type, int slotId, string displayName, string shaderOutputName, SlotType slotType, Vector4 defaultValue, ShaderStageCapability shaderStageCapability = ShaderStageCapability.All, bool hidden = false)
        {
            switch (type)
            {
                case SlotValueType.SamplerState:
                    return new SamplerStateMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.DynamicMatrix:
                    return new DynamicMatrixMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Matrix4:
                    return new Matrix4MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Matrix3:
                    return new Matrix3MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Matrix2:
                    return new Matrix2MaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Texture2D:
                    return slotType == SlotType.Input
                        ? new Texture2DInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        : new Texture2DMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Texture2DArray:
                    return slotType == SlotType.Input
                        ? new Texture2DArrayInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        : new Texture2DArrayMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Texture3D:
                    return slotType == SlotType.Input
                        ? new Texture3DInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        : new Texture3DMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Cubemap:
                    return slotType == SlotType.Input
                        ? new CubemapInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        : new CubemapMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.Gradient:
                    return slotType == SlotType.Input
                        ? new GradientInputMaterialSlot(slotId, displayName, shaderOutputName, shaderStageCapability, hidden)
                        : new GradientMaterialSlot(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden);
                case SlotValueType.DynamicVector:
                    return new DynamicVectorMaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStageCapability, hidden);
                case SlotValueType.Vector4:
                    return new Vector4MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStageCapability, hidden: hidden);
                case SlotValueType.Vector3:
                    return new Vector3MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStageCapability, hidden: hidden);
                case SlotValueType.Vector2:
                    return new Vector2MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue, shaderStageCapability, hidden: hidden);
                case SlotValueType.Vector1:
                    return new Vector1MaterialSlot(slotId, displayName, shaderOutputName, slotType, defaultValue.x, shaderStageCapability, hidden: hidden);
                case SlotValueType.Dynamic:
                    return new DynamicValueMaterialSlot(slotId, displayName, shaderOutputName, slotType, new Matrix4x4(defaultValue, Vector4.zero, Vector4.zero, Vector4.zero), shaderStageCapability, hidden);
                case SlotValueType.Boolean:
                    return new BooleanMaterialSlot(slotId, displayName, shaderOutputName, slotType, false, shaderStageCapability, hidden);
            }

            throw new ArgumentOutOfRangeException("type", type, null);
        }

        public SlotReference slotReference
        {
            get { return new SlotReference(owner.guid, m_Id); }
        }

        public INode owner { get; set; }

        public bool hidden
        {
            get { return m_Hidden; }
            set { m_Hidden = value; }
        }

        public int id
        {
            get { return m_Id; }
        }

        public int priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
        }

        public bool isInputSlot
        {
            get { return m_SlotType == SlotType.Input; }
        }

        public bool isOutputSlot
        {
            get { return m_SlotType == SlotType.Output; }
        }

        public SlotType slotType
        {
            get { return m_SlotType; }
        }

        public bool isConnected
        {
            get
            {
                // node and graph respectivly
                if (owner == null || owner.owner == null)
                    return false;

                var graph = owner.owner;
                var edges = graph.GetEdges(slotReference);
                return edges.Any();
            }
        }

        public abstract SlotValueType valueType { get; }

        public abstract ConcreteSlotValueType concreteValueType { get; }

        public string shaderOutputName
        {
            get { return m_ShaderOutputName; }
            private set { m_ShaderOutputName = value; }
        }

        public ShaderStageCapability stageCapability
        {
            get { return m_StageCapability; }
            set { m_StageCapability = value; }
        }

        public bool hasError
        {
            get { return m_HasError; }
            set { m_HasError = value; }
        }

        bool IsCompatibleWithInputSlotType(SlotValueType inputType)
        {
            switch (valueType)
            {
                case SlotValueType.SamplerState:
                    return inputType == SlotValueType.SamplerState;
                case SlotValueType.DynamicMatrix:
                case SlotValueType.Matrix4:
                    return inputType == SlotValueType.Matrix4
                        || inputType == SlotValueType.Matrix3
                        || inputType == SlotValueType.Matrix2
                        || inputType == SlotValueType.DynamicMatrix
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Matrix3:
                    return inputType == SlotValueType.Matrix3
                        || inputType == SlotValueType.Matrix2
                        || inputType == SlotValueType.DynamicMatrix
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Matrix2:
                    return inputType == SlotValueType.Matrix2
                        || inputType == SlotValueType.DynamicMatrix
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Texture2D:
                    return inputType == SlotValueType.Texture2D;
                case SlotValueType.Texture2DArray:
                    return inputType == SlotValueType.Texture2DArray;
                case SlotValueType.Texture3D:
                    return inputType == SlotValueType.Texture3D;
                case SlotValueType.Cubemap:
                    return inputType == SlotValueType.Cubemap;
                case SlotValueType.Gradient:
                    return inputType == SlotValueType.Gradient;
                case SlotValueType.DynamicVector:
                case SlotValueType.Vector4:
                case SlotValueType.Vector3:
                case SlotValueType.Vector2:
                case SlotValueType.Vector1:
                    return inputType == SlotValueType.Vector4
                        || inputType == SlotValueType.Vector3
                        || inputType == SlotValueType.Vector2
                        || inputType == SlotValueType.Vector1
                        || inputType == SlotValueType.DynamicVector
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Dynamic:
                    return inputType == SlotValueType.Matrix4
                        || inputType == SlotValueType.Matrix3
                        || inputType == SlotValueType.Matrix2
                        || inputType == SlotValueType.DynamicMatrix
                        || inputType == SlotValueType.Vector4
                        || inputType == SlotValueType.Vector3
                        || inputType == SlotValueType.Vector2
                        || inputType == SlotValueType.Vector1
                        || inputType == SlotValueType.DynamicVector
                        || inputType == SlotValueType.Dynamic;
                case SlotValueType.Boolean:
                    return inputType == SlotValueType.Boolean;
            }
            return false;
        }

        public bool IsCompatibleWith(MaterialSlot otherSlot)
        {
            return otherSlot != null
                && otherSlot.owner != owner
                && otherSlot.isInputSlot != isInputSlot
                && ((isInputSlot
                     ? otherSlot.IsCompatibleWithInputSlotType(valueType)
                     : IsCompatibleWithInputSlotType(otherSlot.valueType)));
        }

        public bool IsCompatibleStageWith(MaterialSlot otherSlot)
        {
            var candidateStage = otherSlot.stageCapability;
            return stageCapability == ShaderStageCapability.All || candidateStage == stageCapability;
        }

        public virtual string GetDefaultValue(GenerationMode generationMode)
        {
            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            if (generationMode.IsPreview())
                return matOwner.GetVariableNameForSlot(id);

            return ConcreteSlotValueAsVariable(matOwner.precision);
        }

        protected virtual string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return "error";
        }

        public abstract void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode);

        protected static PropertyType ConvertConcreteSlotValueTypeToPropertyType(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Texture2D:
                    return PropertyType.Texture2D;
                case ConcreteSlotValueType.Texture2DArray:
                    return PropertyType.Texture2DArray;
                case ConcreteSlotValueType.Texture3D:
                    return PropertyType.Texture3D;
                case ConcreteSlotValueType.Cubemap:
                    return PropertyType.Cubemap;
                case ConcreteSlotValueType.Gradient:
                    return PropertyType.Gradient;
                case ConcreteSlotValueType.Boolean:
                    return PropertyType.Boolean;
                case ConcreteSlotValueType.Vector1:
                    return PropertyType.Vector1;
                case ConcreteSlotValueType.Vector2:
                    return PropertyType.Vector2;
                case ConcreteSlotValueType.Vector3:
                    return PropertyType.Vector3;
                case ConcreteSlotValueType.Vector4:
                    return PropertyType.Vector4;
                case ConcreteSlotValueType.Matrix2:
                    return PropertyType.Matrix2;
                case ConcreteSlotValueType.Matrix3:
                    return PropertyType.Matrix3;
                case ConcreteSlotValueType.Matrix4:
                    return PropertyType.Matrix4;
                case ConcreteSlotValueType.SamplerState:
                    return PropertyType.SamplerState;
                default:
                    return PropertyType.Vector4;
            }
        }

        public virtual void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            properties.Add(default(PreviewProperty));
        }

        public abstract void CopyValuesFrom(MaterialSlot foundSlot);

        bool Equals(MaterialSlot other)
        {
            return m_Id == other.m_Id && owner.guid.Equals(other.owner.guid);
        }

        public bool Equals(ISlot other)
        {
            return Equals(other as object);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MaterialSlot)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Id * 397) ^ (owner != null ? owner.GetHashCode() : 0);
            }
        }
    }
}
