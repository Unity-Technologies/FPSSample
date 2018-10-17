using System;
using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerEnumField : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public Text valueLabel;
        DebugUI.EnumField m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.EnumField>();
            nameLabel.text = m_Field.displayName;
            UpdateValueLabel();
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            valueLabel.color = colorSelected;
            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            valueLabel.color = colorDefault;
        }

        public override void OnAction()
        {
            OnIncrement(false);
        }

        public override void OnIncrement(bool fast)
        {
            if (m_Field.enumValues.Length == 0)
                return;

            var array = m_Field.enumValues;
            int index = Array.IndexOf(array, m_Field.GetValue());

            if (index == array.Length - 1)
                index = 0;
            else
                index += 1;

            m_Field.SetValue(array[index]);
            UpdateValueLabel();
        }

        public override void OnDecrement(bool fast)
        {
            if (m_Field.enumValues.Length == 0)
                return;

            var array = m_Field.enumValues;
            int index = Array.IndexOf(array, m_Field.GetValue());

            if (index == 0)
                index = array.Length - 1;
            else
                index -= 1;

            m_Field.SetValue(array[index]);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            int index = Array.IndexOf(m_Field.enumValues, m_Field.GetValue());

            // Fallback just in case, we may be handling sub/sectionned enums here
            if (index < 0)
                index = 0;

            valueLabel.text = "< " + m_Field.enumNames[index].text + " >";
        }
    }
}
