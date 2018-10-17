using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class VertexColorMaterialSlot : Vector4MaterialSlot, IMayRequireScreenPosition
    {
        public VertexColorMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                       ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, stageCapability, hidden: hidden)
        {}

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView("Vertex Color");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}", ShaderGeneratorNames.VertexColor);
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return !isConnected;
        }
    }
}
