using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerPersistentCanvas : MonoBehaviour
    {
        public RectTransform panel;
        public RectTransform valuePrefab;

        List<DebugUIHandlerValue> m_Items = new List<DebugUIHandlerValue>();

        internal void Toggle(DebugUI.Value widget)
        {
            int index = m_Items.FindIndex(x => x.GetWidget() == widget);

            // Remove
            if (index > -1)
            {
                var item = m_Items[index];
                CoreUtils.Destroy(item.gameObject);
                m_Items.RemoveAt(index);
                return;
            }

            // Add
            var go = Instantiate(valuePrefab, panel, false).gameObject;
            go.name = widget.displayName;
            var uiHandler = go.GetComponent<DebugUIHandlerValue>();
            uiHandler.SetWidget(widget);
            m_Items.Add(uiHandler);
        }

        internal void Clear()
        {
            if (m_Items == null)
                return;

            foreach (var item in m_Items)
                CoreUtils.Destroy(item.gameObject);

            m_Items.Clear();
        }
    }
}
