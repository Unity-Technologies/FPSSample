using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    [ExecuteAlways]
    public class UIFoldout : Toggle
    {
        public GameObject content;
        public GameObject arrowOpened;
        public GameObject arrowClosed;

        protected override void Start()
        {
            base.Start();
            onValueChanged.AddListener(SetState);
            SetState(isOn);
        }

#pragma warning disable 108,114
        void OnValidate()
        {
            SetState(isOn, false);
        }

#pragma warning restore 108,114

        public void SetState(bool state)
        {
            SetState(state, true);
        }

        public void SetState(bool state, bool rebuildLayout)
        {
            if (arrowOpened == null || arrowClosed == null || content == null)
                return;

            if (arrowOpened.activeSelf != state)
                arrowOpened.SetActive(state);

            if (arrowClosed.activeSelf == state)
                arrowClosed.SetActive(!state);

            if (content.activeSelf != state)
                content.SetActive(state);

            if (rebuildLayout)
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
        }
    }
}
