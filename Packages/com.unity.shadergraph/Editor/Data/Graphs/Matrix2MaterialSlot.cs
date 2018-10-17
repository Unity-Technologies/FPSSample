using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Matrix2MaterialSlot : MaterialSlot, IMaterialSlotHasValue<Matrix4x4>
    {
        [SerializeField]
        private Matrix4x4 m_Value = Matrix4x4.identity;

        [SerializeField]
        private Matrix4x4 m_DefaultValue = Matrix4x4.identity;

        public Matrix2MaterialSlot()
        {
        }

        public Matrix2MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
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

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "2x2 (1,0,0,1)";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Matrix2ShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Matrix2)
            {
                name = name,
                vector4Value = new Vector4(value.GetRow(0).x, value.GetRow(0).y, 0, 0)
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Matrix2; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Matrix2; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Matrix2MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
