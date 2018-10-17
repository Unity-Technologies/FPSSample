using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class CubemapSlotControlView : VisualElement
    {
        CubemapInputMaterialSlot m_Slot;

        public CubemapSlotControlView(CubemapInputMaterialSlot slot)
        {
            AddStyleSheetPath("Styles/Controls/CubemapSlotControlView");
            m_Slot = slot;
            var objectField = new ObjectField { objectType = typeof(Cubemap), value = m_Slot.cubemap };
            objectField.OnValueChanged(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var cubemap = evt.newValue as Cubemap;
            if (cubemap != m_Slot.cubemap)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Cubemap");
                m_Slot.cubemap = cubemap;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
