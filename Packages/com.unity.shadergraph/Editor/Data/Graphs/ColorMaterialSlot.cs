using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    public class ColorRGBAMaterialSlot : Vector4MaterialSlot
    {
        public ColorRGBAMaterialSlot() {}

        public ColorRGBAMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            Vector4 value,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, value, stageCapability, hidden: hidden)
        {
        }

        public override VisualElement InstantiateControl()
        {
            return new ColorRGBASlotControlView(this);
        }

        protected override string ConcreteSlotValueAsVariable(AbstractMaterialNode.OutputPrecision precision)
        {
            return string.Format("IsGammaSpace() ? {0}4({1}, {2}, {3}, {4}) : {0}4 (SRGBToLinear({0}3({1}, {2}, {3})), {4})"
                , precision
                , NodeUtils.FloatToShaderValue(value.x)
                , NodeUtils.FloatToShaderValue(value.y)
                , NodeUtils.FloatToShaderValue(value.z)
                , NodeUtils.FloatToShaderValue(value.w));
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
                value = value
            };
            properties.AddShaderProperty(property);
        }

        public override void GetPreviewProperties(List<PreviewProperty> properties, string name)
        {
            var pp = new PreviewProperty(PropertyType.Color)
            {
                name = name,
                colorValue = new Color(value.x, value.x, value.z, value.w),
            };
            properties.Add(pp);
        }
    }
}
