using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    public class ColorRGBMaterialSlot : Vector3MaterialSlot
    {
        [SerializeField]
        ColorMode m_ColorMode = ColorMode.Default;

        public ColorRGBMaterialSlot() {}

        public ColorRGBMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Color value,
            ColorMode colorMode,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, (Vector4)value, stageCapability, hidden: hidden)
        {
            m_ColorMode = colorMode;
        }

        public ColorMode colorMode
        {
            get { return m_ColorMode; }
            set { m_ColorMode = value; }
        }

        public override VisualElement InstantiateControl()
        {
            return new ColorRGBSlotControlView(this);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return string.Format(m_ColorMode == ColorMode.Default ? "IsGammaSpace() ? {0}3({1}, {2}, {3}) : SRGBToLinear({0}3({1}, {2}, {3}))" : "{0}3({1}, {2}, {3})"
                , precision
                , NodeUtils.FloatToShaderValue(value.x)
                , NodeUtils.FloatToShaderValue(value.y)
                , NodeUtils.FloatToShaderValue(value.z));
        }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            var matOwner = owner as AbstractMaterialNode;
            if (matOwner == null)
                throw new Exception(string.Format("Slot {0} either has no owner, or the owner is not a {1}", this, typeof(AbstractMaterialNode)));

            var property = new ColorShaderProperty()
            {
                overrideReferenceName = matOwner.GetVariableNameForSlot(id),
                generatePropertyBlock = false,
                value = new Color(value.x, value.y, value.z)
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Color)
            {
                name = name,
                colorValue = new Color(value.x, value.y, value.z, 1),
            };
            properties.Add(pp);
        }
    }
}
