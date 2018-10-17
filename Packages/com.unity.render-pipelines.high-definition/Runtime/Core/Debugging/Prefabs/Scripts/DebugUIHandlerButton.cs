using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerButton : DebugUIHandlerWidget
    {
        public Text nameLabel;

        DebugUI.Button m_Field;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.Button>();
            nameLabel.text = m_Field.displayName;
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            nameLabel.color = colorSelected;
            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        public override void OnAction()
        {
            if (m_Field.action != null)
                m_Field.action();
        }
    }
}
