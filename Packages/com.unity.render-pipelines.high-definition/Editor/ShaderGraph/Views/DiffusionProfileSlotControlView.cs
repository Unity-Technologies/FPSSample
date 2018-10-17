using System;
using UnityEditor.Graphing;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class DiffusionProfileSlotControlView : VisualElement
    {
        DiffusionProfileInputMaterialSlot m_Slot;

        PopupField<string> popupField;

        public DiffusionProfileSlotControlView(DiffusionProfileInputMaterialSlot slot)
        {
            AddStyleSheetPath("DiffusionProfileSlotControlView");
            m_Slot = slot;
            popupField = new PopupField<string>(m_Slot.diffusionProfile.popupEntries, m_Slot.diffusionProfile.selectedEntry);
            popupField.OnValueChanged(OnValueChanged);
            Add(popupField);
        }

        void OnValueChanged(ChangeEvent<string> evt)
        {
            var selectedIndex = popupField.index;

           if (selectedIndex != m_Slot.diffusionProfile.selectedEntry)
           {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Diffusion Profile");

                PopupList popupList = m_Slot.diffusionProfile;
                popupList.selectedEntry = selectedIndex;
                m_Slot.diffusionProfile = popupList;
                m_Slot.owner.Dirty(ModificationScope.Graph);
           }
        }
    }
}
