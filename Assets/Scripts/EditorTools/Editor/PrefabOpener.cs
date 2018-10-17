using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public static class AssetTools
{
    [MenuItem("Assets/OpenPrefabInNewScene")]
    static void OnOpenPrefabInNewScene()
    {
        var selobj = Selection.activeObject;

        if (selobj == null)
            return;

        if (PrefabUtility.GetPrefabAssetType(selobj) != PrefabAssetType.Regular)
        {
            Debug.Log(PrefabUtility.GetPrefabAssetType(selobj));
            return;
        }

        var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(s);
        Selection.activeObject = PrefabUtility.InstantiatePrefab(selobj, s);
        SceneView.FrameLastActiveSceneView();
    }

    [MenuItem("Assets/FindEmptyFolders")]
    static void OnFindEmptyFolders()
    {
        var empties = new List<UnityEngine.Object>();
        foreach(var d in System.IO.Directory.GetDirectories("Assets", "*", System.IO.SearchOption.AllDirectories))
        {
            if(System.IO.Directory.GetFiles(d).Length == 0)
            {
                empties.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(d));
            }
        }
        Selection.objects = empties.ToArray();
    }
}
