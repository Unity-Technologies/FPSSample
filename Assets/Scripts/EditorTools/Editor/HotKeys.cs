using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

//
// A bunch of useful functions mapped to hotkeys by using the MenuItem attribute
//

[InitializeOnLoad]
public class HotKeys
{
    static HotKeys()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    [MenuItem("FPS Sample/Hotkeys/Lookup asset guid %&l")]
    static void LookupAsset()
    {
        LookupAssetWindow.Open();
    }

    public static List<Object> objectsSelectedForCut;

    [MenuItem("FPS Sample/Hotkeys/Cut GameObjects _%#X")]
    static void Cut()
    {
        if (Selection.objects.Length > 0)
        {
            objectsSelectedForCut = new List<Object>(Selection.objects);
            foreach (var o in objectsSelectedForCut)
            {
                EditorUtility.SetDirty(o);
            }
            Debug.Log("Marked " + objectsSelectedForCut.Count + " for movement. Press Ctrl+V to move.");
        }
    }

    [MenuItem("FPS Sample/Hotkeys/Paste GameObjects _%#V")]
    static void Paste()
    {
        if (objectsSelectedForCut == null)
        {
            Debug.Log("Use Ctrl+Shift+X first to mark objects for moving.");
            return;
        }

        Transform newParent = null;
        var moveToDestScene = false;

        // Fill dest_scene with random stuff because it is a struct and hence non-nullable
        Scene destScene = SceneManager.GetActiveScene();

        if (Selection.activeGameObject != null && Selection.objects.Length == 1)
        {
            // In this case, we parent under another object
            newParent = Selection.activeGameObject.transform;
        }
        else if (Selection.activeGameObject == null && Selection.instanceIDs.Length == 1)
        {
            // In this case, we may have selected a scene
            var method = typeof(EditorSceneManager).GetMethod("GetSceneByHandle", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var obj = method.Invoke(null, new object[] { Selection.instanceIDs[0] });
            if (obj is Scene)
            {
                var scene = (Scene)obj;
                if (scene.isLoaded)
                {
                    destScene = scene;
                    moveToDestScene = true;
                }
            }
        }
        else
        {
            Debug.Log("You must select exactly one gameobject or one scene to be the parent of the pasted object(s).");
            return;
        }

        // Perform move
        foreach (var obj in objectsSelectedForCut)
        {
            GameObject go = obj as GameObject;
            if (go == null)
            {
                continue;
            }
            Undo.SetTransformParent(go.transform, newParent, "Moved objects");
            if (moveToDestScene)
            {
                // Moving to root of scene.
                SceneManager.MoveGameObjectToScene(go, destScene);
            }
        }
        objectsSelectedForCut = null;
    }

    [MenuItem("FPS Sample/Hotkeys/Deselect All &d")]
    static void Deselect()
    {
        Selection.activeGameObject = null;
    }

    static Ray lastMouseRay;
    static void OnSceneGUI(SceneView sceneview)
    {
        lastMouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
    }

    [MenuItem("FPS Sample/Hotkeys/Position under mouse %#q")]
    static void MousePlace()
    {
        var transforms = Selection.transforms;
        foreach (var myTransform in transforms)
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlace");
            var hit = FindClosestObjectUnderMouseNotSelected();
            myTransform.position = hit.point;
        }
    }

    static RaycastHit FindClosestObjectUnderMouseNotSelected()
    {
        var transforms = Selection.transforms;
        // Find closest object under mouse, not selected
        var hits = Physics.RaycastAll(lastMouseRay);
        RaycastHit hit = new RaycastHit();
        var closest_dist = float.MaxValue;
        foreach (var h in hits)
        {
            var skipit = false;
            foreach (var t in transforms)
            {
                if (h.collider.transform.IsChildOf(t))
                    skipit = true;
            }
            if (skipit)
                continue;
            if (h.distance < closest_dist)
            {
                hit = h;
                closest_dist = h.distance;
            }
        }
        return hit;
    }

    [MenuItem("FPS Sample/Hotkeys/Align and position under mouse %#z")]
    static void MousePlaceAndAlign()
    {
        var transforms = Selection.transforms;
        if (transforms.Length == 0)
            return;

        var hit = FindClosestObjectUnderMouseNotSelected();

        if (hit.distance == 0)
            return;

        foreach (var myTransform in transforms)
        {
            Undo.RegisterCompleteObjectUndo(Selection.transforms, "MousePlaceAndAlign");

            myTransform.position = hit.point;

            // Decide what is most up
            var xdot = Vector3.Dot(myTransform.right, Vector3.up);
            var ydot = Vector3.Dot(myTransform.up, Vector3.up);
            if (Mathf.Abs(xdot) > 0.7f)
            {
                var rot = Quaternion.FromToRotation(myTransform.right, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
            else if (Mathf.Abs(ydot) > 0.7f)
            {
                var rot = Quaternion.FromToRotation(myTransform.up, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
            else
            {
                var rot = Quaternion.FromToRotation(myTransform.forward, hit.normal);
                myTransform.rotation = rot * myTransform.rotation;
            }
        }
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

public class LookupAssetWindow : EditorWindow
{
    public static void Open()
    {
        LookupAssetWindow window = GetWindow<LookupAssetWindow>(false);
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 800, 150);
        window.ShowPopup();
        guid = "";
    }

    static string guid;
    static Object asset;
    static string path;

    void OnGUI()
    {
        EditorGUILayout.LabelField("Enter asset GUID below", EditorStyles.wordWrappedLabel);
        GUILayout.Space(20);

        EditorGUI.BeginChangeCheck();
        guid = EditorGUILayout.TextField("GUID", guid);
        var guidChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        path = EditorGUILayout.TextField("Asset", path);
        var pathChanged = EditorGUI.EndChangeCheck();

        EditorGUI.BeginChangeCheck();
        asset = EditorGUILayout.ObjectField("Asset", asset, typeof(Object), false);
        var assetChanged = EditorGUI.EndChangeCheck();

        GUILayout.Space(20);
        if (GUILayout.Button("Close"))
            this.Close();

        if(guidChanged)
        {
            path = AssetDatabase.GUIDToAssetPath(guid);
            asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
        }
        else if (pathChanged)
        {
            asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            guid = AssetDatabase.AssetPathToGUID(path);
        }
        else if (assetChanged)
        {
            path = AssetDatabase.GetAssetPath(asset);
            guid = AssetDatabase.AssetPathToGUID(path);
        }

    }
}

