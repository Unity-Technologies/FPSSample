using System;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class Texture2DMaterialSlot : MaterialSlot
    {
        public Texture2DMaterialSlot()
        {}

        public Texture2DMaterialSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            SlotType slotType,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, slotType, stageCapability, hidden)
        {}

        public override SlotValueType valueType { get { return SlotValueType.Texture2D; } }
        public override ConcreteSlotValueType concreteValueType { get { return ConcreteSlotValueType.Texture2D; } }

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {}

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {}
    }
}
