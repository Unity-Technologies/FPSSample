using UnityEngine;
using UnityEditor;


public static class GameViewTracker
{
    [MenuItem(k_MenuName, true)]
    public static bool ToggleGameViewTrackingValidate()
    {
        Menu.SetChecked(k_MenuName, s_Enabled);
        return true;
    }

    [MenuItem(k_MenuName)]
    public static void ToggleGameViewTracking()
    {
        SetEnabled(!s_Enabled);
    }

    static void SetEnabled(bool enabled)
    {
        if (enabled && !s_Enabled)
        {
            SceneView.onSceneGUIDelegate += sceneGUICallback;
            s_Enabled = true;
        }
        else if (!enabled && s_Enabled)
        {
            SceneView.onSceneGUIDelegate -= sceneGUICallback;
            s_Enabled = false;
        }
    }

    static void sceneGUICallback(SceneView s)
    {
        if (Camera.main == null)
            return;

        // Non ortho cams are placed slightly in front of scenecam to avoid seeing gizmo
        if(!s.camera.orthographic)
            Camera.main.transform.SetPositionAndRotation(s.camera.transform.position - 0.1f * s.camera.transform.forward, s.camera.transform.rotation);
    }

    static bool s_Enabled;
    const string k_MenuName = "FPS Sample/Hotkeys/Toggle game view tracking _%#K";
}
