using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Texture2DArrayMaterialSlot : MaterialSlot
    {
        public Texture2DArrayMaterialSlot()
        {}

        public Texture2DArrayMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden)
        {}

        public override SlotValueType valueType { get { return SlotValueType.Texture2DArray; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture2DArray; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}
    }
}
