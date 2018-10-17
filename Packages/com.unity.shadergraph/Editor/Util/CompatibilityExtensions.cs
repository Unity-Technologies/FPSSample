using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
#if UNITY_2018_3_OR_NEWER
using ContextualMenu = UnityEngine.Experimental.UIElements.DropdownMenu;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    public static class CompatibilityExtensions
    {
        public static void AppendAction(this ContextualMenu contextualMenu, string actionName, Action action, Func<ContextualMenu.MenuAction.StatusFlags> actionStatusCallback)
        {
            Debug.Assert(action != null);
            Debug.Assert(actionStatusCallback != null);
            contextualMenu.AppendAction(actionName, e => action(), e => actionStatusCallback());
        }

        public static void AppendAction(this ContextualMenu contextualMenu, string actionName, Action action, ContextualMenu.MenuAction.StatusFlags statusFlags)
        {
            Debug.Assert(action != null);
            contextualMenu.AppendAction(actionName, e => action(), e => statusFlags);
        }

#if !UNITY_2018_3_OR_NEWER
        public static void MarkDirtyRepaint(this VisualElement element)
        {
            element.Dirty(ChangeType.Repaint);
        }
#endif

#if !UNITY_2018_3_OR_NEWER
        public static void CaptureMouse(this VisualElement element)
        {
            element.TakeMouseCapture();
        }

        public static void ReleaseMouse(this VisualElement element)
        {
            element.ReleaseMouseCapture();
        }
#endif

        public static void OnToggleChanged(this Toggle toggle, EventCallback<ChangeEvent<bool>> callback)
        {
#if UNITY_2018_3_OR_NEWER
            toggle.OnValueChanged(callback);
#else
            toggle.OnToggle(() => callback(ChangeEvent<bool>.GetPooled(!toggle.value, toggle.value)));
#endif
        }
    }

    static class TrickleDownEnum
    {
#if UNITY_2018_3_OR_NEWER
        public static readonly TrickleDown NoTrickleDown = TrickleDown.NoTrickleDown;
        public static readonly TrickleDown TrickleDown = TrickleDown.TrickleDown;
#else
        public static readonly Capture NoTrickleDown = Capture.NoCapture;
        public static readonly Capture TrickleDown = Capture.Capture;
#endif
    }
}
