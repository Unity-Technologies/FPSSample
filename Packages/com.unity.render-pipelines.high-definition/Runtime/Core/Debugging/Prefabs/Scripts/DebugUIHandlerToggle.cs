using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerToggle : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public Toggle valueToggle;
        public Image checkmarkImage;

        DebugUI.BoolField m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.BoolField>();
            nameLabel.text = m_Field.displayName;
            UpdateValueLabel();
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            checkmarkImage.color = colorSelected;
            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
            checkmarkImage.color = colorDefault;
        }

        public override void OnAction()
        {
            bool value = !m_Field.GetValue();
            m_Field.SetValue(value);
            UpdateValueLabel();
        }

        void UpdateValueLabel()
        {
            if (valueToggle != null)
                valueToggle.isOn = m_Field.GetValue();
        }
    }
}
