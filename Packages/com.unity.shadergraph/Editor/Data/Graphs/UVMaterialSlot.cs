using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class UVMaterialSlot : Vector2MaterialSlot, IMayRequireMeshUV
    {
        [SerializeField]
        UVChannel m_Channel = UVChannel.UV0;

        public UVChannel channel
        {
            get { return m_Channel; }
            set { m_Channel = value; }
        }

        public UVMaterialSlot()
        {}

        public UVMaterialSlot(int slotId, string displayName, string shaderOutputName, UVChannel channel,
                              ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector2.zero, stageCapability, hidden: hidden)
        {
            this.channel = channel;
        }

        public override VisualElement InstantiateControl()
        {
            return new UVSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}.xy", channel.GetUVName());
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            if (isConnected)
                return false;

            return m_Channel == channel;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as UVMaterialSlot;
            if (slot != null)
                channel = slot.channel;
        }
    }
}
