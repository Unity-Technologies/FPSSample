using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System;

namespace UnityEditor.VFX.UIElements
{
    class VFXStringFieldProvider : ValueControl<string>
    {
        Label m_DropDownButton;
        Func<string[]> m_fnStringProvider;


        public Func<string[]> stringProvider
        {
            get { return m_fnStringProvider; }
        }

        void CreateButton()
        {
            m_DropDownButton = new Label();
            m_DropDownButton.AddToClassList("PopupButton");
            m_DropDownButton.AddManipulator(new DownClickable(OnClick));
        }

        void OnClick()
        {
            var menu = new GenericMenu();
            var allString = m_fnStringProvider();
            foreach (var val in allString)
            {
                menu.AddItem(new GUIContent(val), val == m_Value, ChangeValue, val);
            }
            menu.DropDown(m_DropDownButton.worldBound);
        }

        void ChangeValue(object val)
        {
            SetValue((string)val);
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public VFXStringFieldProvider(string label, Func<string[]> stringProvider) : base(label)
        {
            m_fnStringProvider = stringProvider;
            CreateButton();
            style.flexDirection = FlexDirection.Row;
            Add(m_DropDownButton);
        }

        public VFXStringFieldProvider(Label existingLabel, Func<string[]> stringProvider) : base(existingLabel)
        {
            m_fnStringProvider = stringProvider;
            CreateButton();
            Add(m_DropDownButton);
        }

        bool m_Indeterminate;

        public bool indeterminate
        {
            get
            {
                return m_Indeterminate;
            }
            set
            {
                m_Indeterminate = value;
                ValueToGUI(true);
            }
        }


        protected override void ValueToGUI(bool force)
        {
            m_DropDownButton.SetEnabled(indeterminate);
            m_DropDownButton.text = indeterminate ? "_" : m_Value;
        }
    }
}
