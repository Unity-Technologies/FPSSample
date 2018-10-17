using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector3MaterialSlot : MaterialSlot, IMaterialSlotHasValue<Vector3>
    {
        [SerializeField]
        private Vector3 m_Value;

        [SerializeField]
        private Vector3 m_DefaultValue = Vector3.zero;

        [SerializeField]
        string[] m_Labels;

        public Vector3MaterialSlot()
        {
        }

        public Vector3MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector3 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = "X",
            string label2 = "Y",
            string label3 = "Z",
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2, label3 };
        }

        public Vector3 defaultValue { get { return m_DefaultValue; } }

        public Vector3 value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public override VisualElement InstantiateControl()
        {
            return new MultiFloatSlotControlView(owner, m_Labels, () => value, (newValue) => value = newValue);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return precision + "3 (" + NodeUtils.FloatToShaderValue(value.x) + "," + NodeUtils.FloatToShaderValue(value.y) + "," + NodeUtils.FloatToShaderValue(value.z) + ")";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Vector3ShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector3)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, value.z, 0)
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector3; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector3; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector3MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
