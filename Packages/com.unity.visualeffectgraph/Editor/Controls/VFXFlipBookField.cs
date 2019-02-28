using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    class VFXFlipBookField : VFXControl<FlipBook>
    {
        VFXLabeledField<IntegerField, int> m_X;
        VFXLabeledField<IntegerField, int> m_Y;

        void CreateTextField()
        {
            m_X = new VFXLabeledField<IntegerField, int>("X");
            m_Y = new VFXLabeledField<IntegerField, int>("Y");

            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<int>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<int>>(OnYValueChanged);
        }

        void OnXValueChanged(ChangeEvent<int> e)
        {
            FlipBook newValue = value;
            newValue.x = (int)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<int> e)
        {
            FlipBook newValue = value;
            newValue.y = (int)m_Y.value;
            SetValueAndNotify(newValue);
        }

        public override bool indeterminate
        {
            get
            {
                return m_X.indeterminate;
            }
            set
            {
                m_X.indeterminate = value;
                m_Y.indeterminate = value;
            }
        }

        public VFXFlipBookField()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
        }

        protected override void ValueToGUI(bool force)
        {
            if (!m_X.control.HasFocus() || force)
                m_X.value = value.x;

            if (!m_Y.control.HasFocus() || force)
                m_Y.value = value.y;
        }
    }
}
