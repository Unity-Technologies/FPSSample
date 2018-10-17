using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class BooleanSlotControlView : VisualElement
    {
        BooleanMaterialSlot m_Slot;

        public BooleanSlotControlView(BooleanMaterialSlot slot)
        {
            AddStyleSheetPath("Styles/Controls/BooleanSlotControlView");
            m_Slot = slot;
            var toggleField = new Toggle();
            toggleField.OnToggleChanged(OnChangeToggle);
            Add(toggleField);
        }

        void OnChangeToggle(ChangeEvent<bool> evt)
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Toggle Change");
            var value = m_Slot.value;
            value = evt.newValue;
            m_Slot.value = value;
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
