using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;


namespace UnityEditor.VFX.UIElements
{
    class VFXEnumField : ValueControl<int>
    {
        Label m_DropDownButton;
        System.Type m_EnumType;

        void CreateButton()
        {
            m_DropDownButton = new Label();
            m_DropDownButton.AddToClassList("PopupButton");
            m_DropDownButton.AddManipulator(new DownClickable(OnClick));
        }

        void OnClick()
        {
            GenericMenu menu = new GenericMenu();

            foreach (string val in System.Enum.GetNames(m_EnumType))
            {
                int valueInt = (int)System.Enum.Parse(m_EnumType, val);

                menu.AddItem(new GUIContent(val), valueInt == m_Value, ChangeValue, valueInt);
            }
            menu.DropDown(m_DropDownButton.worldBound);
        }

        void ChangeValue(object val)
        {
            SetValue((int)val);
            if (OnValueChanged != null)
            {
                OnValueChanged();
            }
        }

        public VFXEnumField(string label, System.Type enumType) : base(label)
        {
            CreateButton();

            if (!enumType.IsEnum)
            {
                Debug.LogError("The type passed To enumfield must be an enumType");
            }
            m_EnumType = enumType;

            style.flexDirection = FlexDirection.Row;
            Add(m_DropDownButton);
        }

        public VFXEnumField(Label existingLabel, System.Type enumType) : base(existingLabel)
        {
            CreateButton();
            if (!enumType.IsEnum)
            {
                Debug.LogError("The type passed To enum field must be an enumType");
            }
            m_EnumType = enumType;
            Add(m_DropDownButton);
        }

        protected override void ValueToGUI(bool force)
        {
            m_DropDownButton.text = System.Enum.GetName(m_EnumType, m_Value);
        }
    }
}
