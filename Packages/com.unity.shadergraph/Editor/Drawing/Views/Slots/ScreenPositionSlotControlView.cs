using System;
using UnityEditor.Graphing;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class ScreenPositionSlotControlView : VisualElement
    {
        ScreenPositionMaterialSlot m_Slot;

        public ScreenPositionSlotControlView(ScreenPositionMaterialSlot slot)
        {
            AddStyleSheetPath("Styles/Controls/ScreenPositionSlotControlView");
            m_Slot = slot;
            var enumField = new EnumField(slot.screenSpaceType);
            enumField.OnValueChanged(OnValueChanged);
            Add(enumField);
        }

        void OnValueChanged(ChangeEvent<Enum> evt)
        {
            var screenSpaceType = (ScreenSpaceType)evt.newValue;
            if (screenSpaceType != m_Slot.screenSpaceType)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Screen Space Type");
                m_Slot.screenSpaceType = screenSpaceType;
                m_Slot.owner.Dirty(ModificationScope.Graph);
            }
        }
    }
}
