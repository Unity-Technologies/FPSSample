using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class DynamicMatrixMaterialSlot : MaterialSlot, IMaterialSlotHasValue<Matrix4x4>
    {
        [SerializeField]
        private Matrix4x4 m_Value = Matrix4x4.identity;

        [SerializeField]
        private Matrix4x4 m_DefaultValue = Matrix4x4.identity;

        private ConcreteSlotValueType m_ConcreteValueType = ConcreteSlotValueType.Matrix4;

        public DynamicMatrixMaterialSlot()
        {
        }

        public DynamicMatrixMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
        }

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView("Identity");
        }

        public Matrix4x4 defaultValue { get { return m_DefaultValue; } }

        public Matrix4x4 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override SlotValueType valueType { get { return SlotValueType.DynamicMatrix; } }

        public override ConcreteSlotValueType concreteValueType
        {
            get { return m_ConcreteValueType; }
        }

        public void SetConcreteType(ConcreteSlotValueType valueType)
        {
            m_ConcreteValueType = valueType;
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            var channelCount = (int)SlotValueHelper.GetMatrixDimension(concreteValueType);
            var values = "";
            bool isFirst = true;
            for (var r = 0; r < channelCount; r++)
            {
                for (var c = 0; c < channelCount; c++)
                {
                    if (!isFirst)
                        values += ", ";
                    isFirst = false;
                    values += value.GetRow(r)[c];
                }
            }
            return string.Format("{0}{1}x{1}({2})", precision, channelCount, values);
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            IShaderProperty property;
            switch (concreteValueType)
            {
                case ConcreteSlotValueType.Matrix4:
                    property = new Matrix4ShaderProperty();
                    break;
                case ConcreteSlotValueType.Matrix3:
                    property = new Matrix3ShaderProperty();
                    break;
                case ConcreteSlotValueType.Matrix2:
                    property = new Matrix2ShaderProperty();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            property.overrideReferenceName = matOwner.GetVariableNameForSlot(id);
            property.generatePropertyBlock = false;
            properties.AddShaderProperty(property);
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as DynamicMatrixMaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
