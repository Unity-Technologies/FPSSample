using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerFoldout : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public UIFoldout valueToggle;

        DebugUI.Foldout m_Field;
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.Foldout>();
            m_Container = GetComponent<DebugUIHandlerContainer>();
            nameLabel.text = m_Field.displayName;
            UpdateValue();
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            if (fromNext || valueToggle.isOn == false)
            {
                nameLabel.color = colorSelected;
            }
            else if (valueToggle.isOn)
            {
                if (m_Container.IsDirectChild(previous))
                {
                    nameLabel.color = colorSelected;
                }
                else
                {
                    var lastItem = m_Container.GetLastItem();
                    DebugManager.instance.ChangeSelection(lastItem, false);
                }
            }

            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        public override void OnIncrement(bool fast)
        {
            m_Field.SetValue(true);
            UpdateValue();
        }

        public override void OnDecrement(bool fast)
        {
            m_Field.SetValue(false);
            UpdateValue();
        }

        public override void OnAction()
        {
            bool value = !m_Field.GetValue();
            m_Field.SetValue(value);
            UpdateValue();
        }

        void UpdateValue()
        {
            valueToggle.isOn = m_Field.GetValue();
        }

        public override DebugUIHandlerWidget Next()
        {
            if (!m_Field.GetValue() || m_Container == null)
                return base.Next();

            var firstChild = m_Container.GetFirstItem();

            if (firstChild == null)
                return base.Next();

            return firstChild;
        }
    }
}
