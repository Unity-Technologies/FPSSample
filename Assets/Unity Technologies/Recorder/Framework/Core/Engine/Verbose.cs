using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace UnityEngine.Recorder
{
#if UNITY_EDITOR
    [InitializeOnLoad]
    public class Verbose
    {
        const string MENU_NAME = "Tools/Recorder/Debug mode";
        static bool m_cachedState;

        public static bool enabled
        {
            get { return m_cachedState; }
            set
            {
                EditorPrefs.SetBool(MENU_NAME, value);
                m_cachedState = value;
                var go = SceneHook.GetGameObject(false);
                if (go != null)
                {
                    go.hideFlags = value ? HideFlags.None : HideFlags.HideInHierarchy;
                }
            }
        }

        static Verbose()
        {
            enabled = EditorPrefs.GetBool(MENU_NAME, false);

            /// Delaying until first editor tick so that the menu  will be populated before setting check state, and  re-apply correct action
            EditorApplication.delayCall += () => PerformAction(enabled);
        }

        [MenuItem(MENU_NAME, false, Int32.MaxValue)]
        static void ToggleAction()
        {
            PerformAction(!enabled);
        }

        public static void PerformAction(bool newState)
        {
            Menu.SetChecked(MENU_NAME, newState);
            enabled = newState;
        }

    }

#else
    public class Verbose
    {
        static bool m_State;
        public static bool enabled
        {
            get { return m_State; }
            set
            {
                m_State = value;
                var go = SceneHook.GetGameObject(false);
                if (go != null)
                {
                    go.hideFlags = value ? HideFlags.None : HideFlags.HideInHierarchy;
                }
            }
        }
    }
#endif

}