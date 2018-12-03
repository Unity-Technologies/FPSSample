using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class FindWindow
{
    public bool findGameobjects = true;
    public bool searchScenes = true;
    public bool searchAssets = false;
    public bool prefabOnly = true;

    [MenuItem("GameObject/Find/Instances of prefab", false, -100)]
    static void InstancesOfPrefabInScene()
    {
        var selected = Selection.activeGameObject;

        if (selected == null)
            return;

        if (!PrefabUtility.IsPartOfPrefabInstance(selected))
            return;

        if (!PrefabUtility.IsAnyPrefabInstanceRoot(selected))
            return;
        
        var selectedPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected);
        FindInstancesOfPrefab(selectedPrefabPath);
    }
    
    static void FindInstancesOfPrefab(string prefabPath)
    {
        var matches = new List<GameObject>();
        var gameObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (var gameObject in gameObjects)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
                continue;
            
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                continue;
            
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (!path.Equals(prefabPath))
                continue;

            matches.Add(gameObject);
        }

        Selection.objects = matches.ToArray();
    }
    
    
    [MenuItem("GameObject/Find/Instances with same components", false, -101)]
    static void FindSameComponents()
    {
        var selected = Selection.activeGameObject;
        var components = selected.GetComponents<Component>();

        // Transform is component 0
        var primaryComponentIndex = components.Length == 1 ? 0 : 1;

        var primaryComponent = components[primaryComponentIndex];
        
//        Debug.Log("Finding all with component:" + primaryComponent);

        var foundObjects = GameObject.FindObjectsOfType(primaryComponent.GetType());

        var count = foundObjects.Length;
//        Debug.Log("Found with primary component;" + count);

        var matches = new List<GameObject>(count);

        foreach (var foundObject in foundObjects)
        {
            var component = foundObject as Component;
            var gameObject = component.gameObject;

            // Test of rest of components are present
            var fail = false;
            for (var i = primaryComponentIndex + 1; i < components.Length; i++)
            {
                var comp = gameObject.GetComponent(components[i].GetType());
                if (comp == null)
                {
                    fail = true;
                    break;
                }
            }

            if (fail)
                continue;
            
            matches.Add(gameObject);
        }

 //       Debug.Log("Found;" + matches.Count);
        Selection.objects = matches.ToArray();
    }
}