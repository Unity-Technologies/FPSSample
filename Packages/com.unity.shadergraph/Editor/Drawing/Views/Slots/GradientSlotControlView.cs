using System;
using UnityEditor.Experimental.UIElements;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Slots
{
    public class GradientSlotControlView : VisualElement
    {
        GradientInputMaterialSlot m_Slot;

        [SerializeField]
        GradientObject m_GradientObject;

        [SerializeField]
        SerializedObject m_SerializedObject;

        public GradientSlotControlView(GradientInputMaterialSlot slot)
        {
            m_Slot = slot;
            AddStyleSheetPath("Styles/Controls/GradientSlotControlView");

            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_SerializedObject = new SerializedObject(m_GradientObject);

            m_GradientObject.gradient.SetKeys(m_Slot.value.colorKeys, m_Slot.value.alphaKeys);
            m_GradientObject.gradient.mode = m_Slot.value.mode;

            var gradientField = new GradientField() { value = m_GradientObject.gradient };
            gradientField.OnValueChanged(OnValueChanged);
            Add(gradientField);
        }

        void OnValueChanged(ChangeEvent<Gradient> evt)
        {
            m_SerializedObject.Update();
            if (!evt.newValue.Equals(m_Slot.value))
            {
                m_GradientObject.gradient.SetKeys(evt.newValue.colorKeys, evt.newValue.alphaKeys);
                m_GradientObject.gradient.mode = evt.newValue.mode;
                m_SerializedObject.ApplyModifiedProperties();

                m_Slot.owner.owner.owner.RegisterCompleteObjectUndo("Change Gradient");
                m_Slot.value = m_GradientObject.gradient;
                m_Slot.owner.Dirty(ModificationScope.Node);
            }
        }
    }
}
