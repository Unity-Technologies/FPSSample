using System;
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

using VFXVector3Field = UnityEditor.VFX.UIElements.VFXVector3Field;
using VFXColorField = UnityEditor.VFX.UIElements.VFXColorField;

namespace UnityEditor.VFX.UI
{
    class Vector3PropertyRM : PropertyRM<Vector3>
    {
        VFXColorField m_ColorField;

        VFXVector3Field m_VectorField;

        public Vector3PropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            bool isColor = VFXPropertyAttribute.IsColor(m_Provider.attributes);

            if (isColor)
            {
                m_ColorField = new VFXColorField(m_Label);
                m_ColorField.OnValueChanged = OnColorValueChanged;
                m_ColorField.showAlpha = false;
                m_VectorField = new VFXVector3Field();
                m_VectorField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
                var mainContainer = new VisualElement() { name = "mainContainer" };
                mainContainer.AddToClassList("maincontainer");

                mainContainer.Add(m_ColorField);
                mainContainer.Add(m_VectorField);
                Add(mainContainer);
                m_VectorField.AddToClassList("fieldContainer");
            }
            else
            {
                var labeledField = new VFXLabeledField<VFXVector3Field, Vector3>(m_Label);
                m_VectorField = labeledField.control;
                labeledField.RegisterCallback<ChangeEvent<Vector3>>(OnValueChanged);
                Add(labeledField);
                labeledField.AddToClassList("fieldContainer");
            }
        }

        public override void UpdateGUI(bool force)
        {
            if (m_ColorField != null)
                m_ColorField.value = new Color(m_Value.x, m_Value.y, m_Value.z);

            m_VectorField.SetValueWithoutNotify(m_Value);
            if (force)
                m_VectorField.ForceUpdate();
        }

        void OnColorValueChanged()
        {
            m_Value = new Vector3(m_ColorField.value.r, m_ColorField.value.g, m_ColorField.value.b);

            NotifyValueChanged();
        }

        void OnValueChanged(ChangeEvent<Vector3> e)
        {
            m_Value = m_VectorField.value;

            NotifyValueChanged();
        }

        protected override void UpdateEnabled()
        {
            m_VectorField.SetEnabled(propertyEnabled);
            if (m_ColorField != null)
                m_ColorField.SetEnabled(propertyEnabled);
        }

        protected override void UpdateIndeterminate()
        {
            m_VectorField.indeterminate = indeterminate;
            if (m_ColorField != null)
                m_ColorField.indeterminate = indeterminate;
        }

        public override float GetPreferredControlWidth()
        {
            return 140;
        }

        public override bool IsCompatible(IPropertyRMProvider provider)
        {
            if (!base.IsCompatible(provider)) return false;

            bool isColor = VFXPropertyAttribute.IsColor(provider.attributes);

            return isColor == (m_ColorField != null);
        }

        public override bool showsEverything { get { return true; } }
    }
}
