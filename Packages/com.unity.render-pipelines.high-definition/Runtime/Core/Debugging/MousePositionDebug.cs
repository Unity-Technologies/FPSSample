using System;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering
{
    public class MousePositionDebug
    {
        // Singleton
        private static MousePositionDebug s_Instance = null;

        static public MousePositionDebug instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new MousePositionDebug();
                }

                return s_Instance;
            }
        }

        public int debugStep
        {
            get
            {
#if UNITY_EDITOR
                return m_DebugStep;
#else
                return 0;
#endif
            }
        }

#if UNITY_EDITOR
        [ExecuteAlways]
        class GameViewEventCatcher : MonoBehaviour
        {
            public static GameViewEventCatcher s_Instance = null;
            public static void Cleanup()
            {
                if (s_Instance != null)
                {
                    // Either we call DestroyImmediate or Destroy we get an error :(
                    // GameViewEventCatcher is only use for SSR debugging currently so comment this code and uncomment it if you want to debug SSR
                    //DestroyImmediate(s_Instance.gameObject);
                    //Destroy(s_Instance.gameObject);
                }
            }

            public static void Build()
            {
                Cleanup();
                var go = new GameObject("__GameViewEventCatcher");
                go.hideFlags = HideFlags.HideAndDontSave;
                s_Instance = go.AddComponent<GameViewEventCatcher>();
            }

            void Update()
            {
                if (Input.mousePosition.x < 0
                    || Input.mousePosition.y < 0
                    || Input.mousePosition.x > Screen.width
                    || Input.mousePosition.y > Screen.height)
                    return;

                instance.m_mousePosition = Input.mousePosition;
                instance.m_mousePosition.y = Screen.height - instance.m_mousePosition.y;
                if (Input.GetMouseButton(1))
                    instance.m_MouseClickPosition = instance.m_mousePosition;
                if (Input.GetKey(KeyCode.PageUp))
                    ++instance.m_DebugStep;
                if (Input.GetKey(KeyCode.PageDown))
                    instance.m_DebugStep = Mathf.Max(0, instance.m_DebugStep - 1);
                if (Input.GetKey(KeyCode.End))
                    instance.m_MouseClickPosition = instance.m_mousePosition;
            }
        }

        private Vector2 m_mousePosition = Vector2.zero;
        Vector2 m_MouseClickPosition = Vector2.zero;
        int m_DebugStep = 0;

        private void OnSceneGUI(UnityEditor.SceneView sceneview)
        {
            m_mousePosition = Event.current.mousePosition;
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    m_MouseClickPosition = m_mousePosition;
                    break;
                case EventType.KeyDown:
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.PageUp:
                            ++m_DebugStep;
                            sceneview.Repaint();
                            break;
                        case KeyCode.PageDown:
                            m_DebugStep = Mathf.Max(0, m_DebugStep - 1);
                            sceneview.Repaint();
                            break;
                        case KeyCode.End:
                            // Usefull we you don't want to change the scene viewport but still update the mouse click position
                            m_MouseClickPosition = m_mousePosition;
                            sceneview.Repaint();
                            break;
                    }
                    break;
            }
        }

#endif

        public void Build()
        {
#if UNITY_EDITOR
#if UNITY_2019_1_OR_NEWER
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
            UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
#else
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
            UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
            // Disabled as it cause error: GameViewEventCatcher is only use for SSR debugging currently so comment this code and uncomment it if you want to debug SSR
            //GameViewEventCatcher.Build();
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
#if UNITY_2019_1_OR_NEWER
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;            
#else
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
            // Disabled as it cause error: GameViewEventCatcher is only use for SSR debugging currently so comment this code and uncomment it if you want to debug SSR
            //GameViewEventCatcher.Cleanup();
#endif
        }

        // This function can either return the mouse position in the scene view
        // or in the game/game view.
        public Vector2 GetMousePosition(float ScreenHeight, bool sceneView)
        {
#if UNITY_EDITOR
            if (sceneView)
            {
                // In play mode, m_mousePosition the one in the scene view
                Vector2 mousePixelCoord = m_mousePosition;
                mousePixelCoord.y = (ScreenHeight - 1.0f) - mousePixelCoord.y;
                return mousePixelCoord;
            }
            else
            {
                // In play mode, Input.mousecoords matches the position in the game view
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return Input.mousePosition;
                }
                else
                {
                    // In non-play mode, only m_mousePosition is valid. 
                    // We force -1, -1 as a game view pixel pos to avoid 
                    // rendering un-wanted effects
                    return new Vector2(-1.0f, -1.0f);
                }
            }
#else
            // In app mode, we only use the Input.mousecoords
            return Input.mousePosition;
#endif
        }

        public Vector2 GetMouseClickPosition(float ScreenHeight)
        {
#if UNITY_EDITOR
            Vector2 mousePixelCoord = m_MouseClickPosition;
            mousePixelCoord.y = (ScreenHeight - 1.0f) - mousePixelCoord.y;
            return mousePixelCoord;
#else
            return Vector2.zero;
#endif
        }
    }
}
