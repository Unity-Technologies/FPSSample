using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class TextureSlotControlView : VisualElement
    {
        Texture2DInputMaterialSlot m_Slot;

        public TextureSlotControlView(Texture2DInputMaterialSlot slot)
        {
            m_Slot = slot;
            AddStyleSheetPath("Styles/Controls/TextureSlotControlView");
            var objectField = new ObjectField { objectType = typeof(Texture), value = m_Slot.texture };
            objectField.OnValueChanged(OnValueChanged);
            Add(objectField);
        }

        void OnValueChanged(ChangeEvent<Object> evt)
        {
            var texture = evt.newValue as Texture;
            if (texture != m_Slot.texture)
            {
                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Texture");
                m_Slot.texture = texture;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
