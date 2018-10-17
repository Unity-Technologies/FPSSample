using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ScreenPositionMaterialSlot : Vector4MaterialSlot, IMayRequireScreenPosition
    {
        [SerializeField]
        ScreenSpaceType m_ScreenSpaceType;

        public ScreenSpaceType screenSpaceType
        {
            get { return m_ScreenSpaceType; }
            set { m_ScreenSpaceType = value; }
        }

        public ScreenPositionMaterialSlot()
        {}

        public ScreenPositionMaterialSlot(int slotId, string displayName, string shaderOutputName, ScreenSpaceType screenSpaceType,
                                          ShaderStageCapability stageCapability = ShaderStageCapability.All, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, stageCapability, hidden: hidden)
        {
            this.screenSpaceType = screenSpaceType;
        }

        public override VisualElement InstantiateControl()
        {
            return new ScreenPositionSlotControlView(this);
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return m_ScreenSpaceType.ToValueAsVariable();
        }

        public bool RequiresScreenPosition(ShaderStageCapability stageCapability)
        {
            return !isConnected;
        }

        public override void CopyValuesFrom(MaterialSlot foundSlot)
        {
            var slot = foundSlot as ScreenPositionMaterialSlot;
            if (slot != null)
                screenSpaceType = slot.screenSpaceType;
        }
    }
}
