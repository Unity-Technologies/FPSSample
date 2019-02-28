#define USE_MY_COLOR_FIELD

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using UnityEditor.VFX;
using UnityEditor.VFX.UIElements;
using Object = UnityEngine.Object;
using Type = System.Type;
using FloatField = UnityEditor.VFX.UIElements.VFXLabeledField<UnityEditor.Experimental.UIElements.FloatField, float>;


namespace UnityEditor.VFX.UI
{
    class ColorPropertyRM : PropertyRM<Color>
    {
        VisualElement m_MainContainer;
        public ColorPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            m_MainContainer = new VisualElement();

#if USE_MY_COLOR_FIELD
            m_ColorField = new UnityEditor.VFX.UIElements.VFXColorField(m_Label);
            m_ColorField.OnValueChanged = OnValueChanged;
#else
            m_ColorField = new LabeledField<UnityEditor.Experimental.UIElements.ColorField, Color>(m_Label);
            m_ColorField.RegisterCallback<ChangeEvent<Color>>(OnValueChanged);
#endif


            m_MainContainer.Add(m_ColorField);
            m_MainContainer.AddToClassList("maincontainer");

            VisualElement fieldContainer = new VisualElement();
            fieldContainer.AddToClassList("fieldContainer");

            m_RFloatField = new FloatField("R");
            m_RFloatField.RegisterCallback<ChangeEvent<float>>(OnValueChanged);

            m_GFloatField = new FloatField("G");
            m_GFloatField.RegisterCallback<ChangeEvent<float>>(OnValueChanged);

            m_BFloatField = new FloatField("B");
            m_BFloatField.RegisterCallback<ChangeEvent<float>>(OnValueChanged);

            m_AFloatField = new FloatField("A");
            m_AFloatField.RegisterCallback<ChangeEvent<float>>(OnValueChanged);

            fieldContainer.Add(m_RFloatField);
            fieldContainer.Add(m_GFloatField);
            fieldContainer.Add(m_BFloatField);
            fieldContainer.Add(m_AFloatField);

            m_MainContainer.Add(fieldContainer);

            Add(m_MainContainer);
        }

        public override float GetPreferredControlWidth()
        {
            return 200;
        }

        protected override void UpdateEnabled()
        {
            m_MainContainer.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_ColorField.indeterminate = indeterminate;
            m_RFloatField.indeterminate = indeterminate;
            m_GFloatField.indeterminate = indeterminate;
            m_BFloatField.indeterminate = indeterminate;
            m_AFloatField.indeterminate = indeterminate;
        }

        public void OnValueChanged(ChangeEvent<Color> e)
        {
            OnValueChanged(false);
        }

        public void OnValueChanged(ChangeEvent<float> e)
        {
            OnValueChanged(true);
        }

        void OnValueChanged()
        {
            OnValueChanged(false);
        }

        void OnValueChanged(bool fromField)
        {
            if (fromField)
            {
                Color newValue = new Color(m_RFloatField.value, m_GFloatField.value, m_BFloatField.value, m_AFloatField.value);
                if (newValue != m_Value)
                {
                    m_Value = newValue;
                    NotifyValueChanged();
                }
            }
            else
            {
                Color newValue = m_ColorField.value;
                if (newValue != m_Value)
                {
                    m_Value = newValue;
                    NotifyValueChanged();
                }
            }
        }

        FloatField m_RFloatField;
        FloatField m_GFloatField;
        FloatField m_BFloatField;
        FloatField m_AFloatField;

#if USE_MY_COLOR_FIELD
        UnityEditor.VFX.UIElements.VFXColorField m_ColorField;
#else
        LabeledField<UnityEditor.Experimental.UIElements.ColorField, Color> m_ColorField;
#endif

        public override void UpdateGUI(bool force)
        {
            m_ColorField.value = m_Value;
            m_RFloatField.SetValueWithoutNotify(m_Value.r);
            m_GFloatField.SetValueWithoutNotify(m_Value.g);
            m_BFloatField.SetValueWithoutNotify(m_Value.b);
            m_AFloatField.SetValueWithoutNotify(m_Value.a);
        }

        public override bool showsEverything { get { return true; } }
    }
}
