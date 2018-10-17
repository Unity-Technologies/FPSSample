using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Vector2MaterialSlot : MaterialSlot, IMaterialSlotHasValue<Vector2>
    {
        [SerializeField]
        Vector2 m_Value;

        [SerializeField]
        Vector2 m_DefaultValue = Vector2.zero;

        [SerializeField]
        string[] m_Labels;

        public Vector2MaterialSlot()
        {
        }

        public Vector2MaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector2 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            string label1 = "X",
            string label2 = "Y",
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {
            m_Value = value;
            m_Labels = new[] { label1, label2 };
        }

        public Vector2 defaultValue { get { return m_DefaultValue; } }

        public Vector2 value
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
            return precision + "2 (" + NodeUtils.FloatToShaderValue(value.x) + "," + NodeUtils.FloatToShaderValue(value.y) + ")";
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new Vector2ShaderProperty
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Vector2)
            {
                name = name,
                vector4Value = new Vector4(value.x, value.y, 0, 0),
            };
            properties.Add(pp);
        }

        public override SlotValueType valueType { get { return SlotValueType.Vector2; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Vector2; } }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as Vector2MaterialSlot;
            if (slot != null)
                value = slot.value;
        }
    }
}
