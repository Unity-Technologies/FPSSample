using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class CubemapMaterialSlot : MaterialSlot
    {
        public CubemapMaterialSlot()
        {}

        public CubemapMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {}

        public override SlotValueType valueType { get { return SlotValueType.Cubemap; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Cubemap; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}
    }
}
