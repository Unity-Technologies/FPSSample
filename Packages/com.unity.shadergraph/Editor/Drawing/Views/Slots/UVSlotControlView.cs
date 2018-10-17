using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class UVSlotControlView : VisualElement
    {
        UVMaterialSlot m_Slot;

        public UVSlotControlView(UVMaterialSlot slot)
        {
            AddStyleSheetPath("Styles/Controls/UVSlotControlView");
            m_Slot = slot;
            var enumField = new EnumField(slot.channel);
            enumField.OnValueChanged(OnValueChanged);
            Add(enumField);
        }

        void OnValueChanged(ChangeEvent<Enum> evt)
        {
            var channel = (UVChannel)evt.newValue;
            if (channel != m_Slot.channel)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change UV Channel");
                m_Slot.channel = channel;
                m_Slot.owner.Dirty(ModificationScope.Graph);
            }
        }
    }
}
