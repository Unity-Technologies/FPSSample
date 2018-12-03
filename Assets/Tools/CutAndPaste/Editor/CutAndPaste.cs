using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutAndPaste 
{
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

}
