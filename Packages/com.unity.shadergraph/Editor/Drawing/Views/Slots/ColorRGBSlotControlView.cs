using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class ColorRGBSlotControlView : VisualElement
    {
        ColorRGBMaterialSlot m_Slot;

        public ColorRGBSlotControlView(ColorRGBMaterialSlot slot)
        {
            AddStyleSheetPath("Styles/Controls/ColorRGBSlotControlView");
            m_Slot = slot;
            var colorField = new ColorField
            {
                value = new Color(slot.value.x, slot.value.y, slot.value.z, 0),
                showEyeDropper = false, showAlpha = false, hdr = (slot.colorMode == ColorMode.HDR)
            };
            colorField.OnValueChanged(OnValueChanged);
            Add(colorField);
        }

        void OnValueChanged(ChangeEvent<Color> evt)
        {
            m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Color Change");
            m_Slot.value = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b);
            m_Slot.owner.Dirty(ModificationScope.Node);
        }
    }
}
