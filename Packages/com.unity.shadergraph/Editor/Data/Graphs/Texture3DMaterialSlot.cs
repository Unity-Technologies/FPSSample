using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Texture3DMaterialSlot : MaterialSlot
    {
        public Texture3DMaterialSlot()
        {}

        public Texture3DMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability shaderStageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, shaderStageCapability, hidden)
        {}

        public override SlotValueType valueType { get { return SlotValueType.Texture3D; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture3D; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}
    }
}
