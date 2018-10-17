using System;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerWidget : MonoBehaviour
    {
        [HideInInspector]
        public Color colorDefault = new Color(0.8f, 0.8f, 0.8f, 1f);

        [HideInInspector]
        public Color colorSelected = new Color(0.25f, 0.65f, 0.8f, 1f);

        public DebugUIHandlerWidget parentUIHandler { get; set; }
        public DebugUIHandlerWidget previousUIHandler { get; set; }
        public DebugUIHandlerWidget nextUIHandler { get; set; }

        protected DebugUI.Widget m_Widget;

        protected virtual void OnEnable() {}

        internal virtual void SetWidget(DebugUI.Widget widget)
        {
            m_Widget = widget;
        }

        internal DebugUI.Widget GetWidget()
        {
            return m_Widget;
        }

        protected T CastWidget<T>()
            where T : DebugUI.Widget
        {
            var casted = m_Widget as T;
            string typeName = m_Widget == null ? "null" : m_Widget.GetType().ToString();

            if (casted == null)
                throw new InvalidOperationException("Can't cast " + typeName + " to " + typeof(T));

            return casted;
        }

        // Returns `true` if selection is allowed, `false` to skip to the next/previous item
        public virtual bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            return true;
        }

        public virtual void OnDeselection() {}

        public virtual void OnAction() {}

        public virtual void OnIncrement(bool fast) {}

        public virtual void OnDecrement(bool fast) {}

        public virtual DebugUIHandlerWidget Previous()
        {
            if (previousUIHandler != null)
                return previousUIHandler;

            if (parentUIHandler != null)
                return parentUIHandler;

            return null;
        }

        public virtual DebugUIHandlerWidget Next()
        {
            if (nextUIHandler != null)
                return nextUIHandler;

            if (parentUIHandler != null)
            {
                var p = parentUIHandler;
                while (p != null)
                {
                    var n = p.nextUIHandler;

                    if (n != null)
                        return n;

                    p = p.parentUIHandler;
                }
            }

            return null;
        }
    }
}
