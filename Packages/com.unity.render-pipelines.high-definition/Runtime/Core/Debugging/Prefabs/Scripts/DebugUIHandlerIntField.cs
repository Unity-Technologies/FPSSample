using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerIntField : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public Text valueLabel;
        DebugUI.IntField m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.IntField>();
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

        public override void OnIncrement(bool fast)
        {
            ChangeValue(fast, 1);
        }

        public override void OnDecrement(bool fast)
        {
            ChangeValue(fast, -1);
        }

        void ChangeValue(bool fast, int multiplier)
        {
            int value = m_Field.GetValue();
            value += m_Field.incStep * (fast ? m_Field.intStepMult : 1) * multiplier;
            m_Field.SetValue(value);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            if (valueLabel != null)
                valueLabel.text = m_Field.GetValue().ToString("N0");
        }
    }
}
