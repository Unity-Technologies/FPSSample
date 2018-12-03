using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Boo.Lang.Environments;
using UnityEditor;
using UnityEngine;

public class CreateVariants 
{
    [MenuItem("Assets/Create/Prefab Variants", false, 210)]
    static void CreateVariantsMenu()
    {
        var newSelection = new List<GameObject>(Selection.gameObjects.Length);
        foreach (var go in Selection.gameObjects)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
                continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(go);
            var path = AssetDatabase.GetAssetPath(go);

            path = System.IO.Path.ChangeExtension(path,".prefab");
            
            PrefabUtility.SaveAsPrefabAsset(instance,path);
            
            GameObject.DestroyImmediate(instance);

            var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            newSelection.Add(newPrefab);
        }

        Selection.objects = newSelection.ToArray();
    }
}
