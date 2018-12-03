using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

//
// A bunch of useful functions mapped to hotkeys by using the MenuItem attribute
//

public class HotKeys
{

    [MenuItem("FPS Sample/Hotkeys/Deselect All &d")]
    static void Deselect()
    {
        Selection.activeGameObject = null;
    }


    [MenuItem("FPS Sample/Hotkeys/Toggle Gizmos _%G")]
    static void ToggleGizmos()
    {
        var etype = typeof(Editor);

        var annotation = etype.Assembly.GetType("UnityEditor.Annotation");
        var scriptClass = annotation.GetField("scriptClass");
        var classID = annotation.GetField("classID");

        var annotation_util = etype.Assembly.GetType("UnityEditor.AnnotationUtility");
        var getAnnotations = annotation_util.GetMethod("GetAnnotations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var setGizmoEnable = annotation_util.GetMethod("SetGizmoEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var setIconEnabled = annotation_util.GetMethod("SetIconEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var iconSize = annotation_util.GetProperty("iconSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var showGrid = annotation_util.GetProperty("showGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var showSelectionOutline = annotation_util.GetProperty("showSelectionOutline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var showSelectionWire = annotation_util.GetProperty("showSelectionWire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var annotations = getAnnotations.Invoke(null, null) as System.Array;
        foreach(var a in annotations)
        {
            int cid = (int)classID.GetValue(a);
            string cls = (string)scriptClass.GetValue(a);
            setGizmoEnable.Invoke(null, new object[] { cid, cls, s_GizmoEnabled ? 1 : 0 });
            setIconEnabled.Invoke(null, new object[] { cid, cls, s_GizmoEnabled ? 1 : 0 });
        }
        s_GizmoEnabled = !s_GizmoEnabled;
        return;

        if (s_GizmoEnabled)
        {
            s_PreviewIconSize = (float)iconSize.GetValue(null, null);
            s_PreviewShowGrid = (bool)showGrid.GetValue(null, null);
            s_PreviewShowSelectionOutline = (bool)showSelectionOutline.GetValue(null, null);
            s_PreviewShowSelectionWire = (bool)showSelectionWire.GetValue(null, null);

            iconSize.SetValue(null, 0.0f, null);
            showGrid.SetValue(null, false, null);
            showSelectionOutline.SetValue(null, false, null);
            showSelectionWire.SetValue(null, false, null);
        }
        else
        {
            iconSize.SetValue(null, s_PreviewIconSize, null);
            showGrid.SetValue(null, s_PreviewShowGrid, null);
            showSelectionOutline.SetValue(null, s_PreviewShowSelectionOutline, null);
            showSelectionWire.SetValue(null, s_PreviewShowSelectionWire, null);
        }
        s_GizmoEnabled = !s_GizmoEnabled;
    }

    private static string k_EditorPrefScreenshotPath = "ScreenshotPath";
    [MenuItem("FPS Sample/Take screenshot")]
    public static void CaptureScreenshot()
    {
        var path = UnityEditor.EditorPrefs.GetString(k_EditorPrefScreenshotPath, Application.dataPath.BeforeLast("Assets"));
        var filename = EditorUtility.SaveFilePanel("Save screenshot", path, "sample_shot.png", "png");

        // Check if user cancelled
        if (filename == "")
            return;

        UnityEditor.EditorPrefs.SetString(k_EditorPrefScreenshotPath, System.IO.Path.GetDirectoryName(filename));
        ScreenCapture.CaptureScreenshot(filename, 1);
    }

    static bool s_GizmoEnabled = true;
    static float s_PreviewIconSize = 0.0f;
    static bool s_PreviewShowGrid = false;
    static bool s_PreviewShowSelectionOutline = false;
    static bool s_PreviewShowSelectionWire = false;
}
