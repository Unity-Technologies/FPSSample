using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerPanel : MonoBehaviour
    {
        public Text nameLabel;
        public ScrollRect scrollRect;
        public RectTransform viewport;

        RectTransform m_ScrollTransform;
        RectTransform m_ContentTransform;
        RectTransform m_MaskTransform;

        protected DebugUI.Panel m_Panel;

        void OnEnable()
        {
            m_ScrollTransform = scrollRect.GetComponent<RectTransform>();
            m_ContentTransform = GetComponent<DebugUIHandlerContainer>().contentHolder;
            m_MaskTransform = GetComponentInChildren<Mask>(true).rectTransform;
        }

        internal void SetPanel(DebugUI.Panel panel)
        {
            m_Panel = panel;
            nameLabel.text = "< " + panel.displayName + " >";
        }

        internal DebugUI.Panel GetPanel()
        {
            return m_Panel;
        }

        // TODO: Jumps around with foldouts and the likes, fix me
        internal void ScrollTo(DebugUIHandlerWidget target)
        {
            if (target == null)
                return;

            var targetTransform = target.GetComponent<RectTransform>();

            float itemY = GetYPosInScroll(targetTransform);
            float targetY = GetYPosInScroll(m_MaskTransform);
            float normalizedDiffY = (targetY - itemY) / (m_ContentTransform.rect.size.y - m_ScrollTransform.rect.size.y);
            float normalizedPosY = scrollRect.verticalNormalizedPosition - normalizedDiffY;
            normalizedPosY = Mathf.Clamp01(normalizedPosY);
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, normalizedPosY, Time.deltaTime * 10f);
        }

        float GetYPosInScroll(RectTransform target)
        {
            var pivotOffset = new Vector3(
                    (0.5f - target.pivot.x) * target.rect.size.x,
                    (0.5f - target.pivot.y) * target.rect.size.y,
                    0f
                    );
            var localPos = target.localPosition + pivotOffset;
            var worldPos = target.parent.TransformPoint(localPos);
            return m_ScrollTransform.TransformPoint(worldPos).y;
        }

        internal DebugUIHandlerWidget GetFirstItem()
        {
            return GetComponent<DebugUIHandlerContainer>()
                .GetFirstItem();
        }
    }
}
