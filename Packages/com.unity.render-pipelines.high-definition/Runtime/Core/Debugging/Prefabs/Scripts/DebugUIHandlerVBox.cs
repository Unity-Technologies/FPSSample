namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerVBox : DebugUIHandlerWidget
    {
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Container = GetComponent<DebugUIHandlerContainer>();
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            if (!fromNext && !m_Container.IsDirectChild(previous))
            {
                var lastItem = m_Container.GetLastItem();
                DebugManager.instance.ChangeSelection(lastItem, false);
                return true;
            }

            return false;
        }

        public override DebugUIHandlerWidget Next()
        {
            if (m_Container == null)
                return base.Next();

            var firstChild = m_Container.GetFirstItem();

            if (firstChild == null)
                return base.Next();

            return firstChild;
        }
    }
}
