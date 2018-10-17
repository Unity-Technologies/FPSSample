using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


    

public class LODErrorFinder
{
    [MenuItem("fps.sample/FIND BROKEN LODS")]
    static void FIND()
    {
        var found = new List<GameObject>();
        
        var lodGroups = GameObject.FindObjectsOfType<LODGroup>();

        foreach(var lodGroup in lodGroups)
        {
            var lods = lodGroup.GetLODs();

            foreach (var lod in lods)
            {
                foreach (var renderer in lod.renderers)
                {
                    if (renderer == null)
                    {
                        if (!found.Contains(lodGroup.gameObject))
                            found.Add(lodGroup.gameObject);
                    }                    
                }
            }
        }

        Selection.objects = found.ToArray();
    }
    
    [MenuItem("fps.sample/FIND AND SELECT WITH LOD OVERRIDE")]
    static void FINDLODOverride()
    {
        var found = new List<GameObject>();
        
        var lodGroups = GameObject.FindObjectsOfType<LODGroup>();

        foreach(var lodGroup in lodGroups)
        {
            if (lodGroup == null || lodGroup.gameObject == null)
                continue;

            if (!PrefabUtility.IsPartOfPrefabInstance(lodGroup.gameObject))
                continue;
            
            var overrides = PrefabUtility.GetObjectOverrides(lodGroup.gameObject);
            if (overrides == null)
                continue;
               
            foreach (var objectOverride in overrides)
            {
                if (objectOverride.instanceObject == lodGroup)
                {
                    Debug.Log("Found on:" + lodGroup.gameObject.name);
                    found.Add(lodGroup.gameObject);
                    break;
                }
            }
        }

        Debug.Log("Found:" + found.Count);
        Selection.objects = found.ToArray();
    }

    
    

    [MenuItem("fps.sample/REMOVE LOD OVERRIDE OF SElECTED SCENE OBJECTS")]
    static void REmoveOverride()
    {
        foreach (var selected in Selection.gameObjects)
        {
            var lodGroup = selected.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                Debug.Log("No LOD on:" + selected);
                continue;
            }

            var overrides = PrefabUtility.GetObjectOverrides(lodGroup.gameObject);
            foreach (var objectOverride in overrides)
            {
                if (objectOverride.instanceObject == lodGroup)
                {
                    Debug.Log("Removed from:" + selected);
                    PrefabUtility.RevertObjectOverride(objectOverride.instanceObject, InteractionMode.AutomatedAction);
                    break;
                }
            }

            EditorUtility.SetDirty(lodGroup);
            EditorUtility.SetDirty(selected);
        }        
    }
    

    [MenuItem("fps.sample/FIX LOD OVERRIDE")]
    static void FixLODOverride()
    {
        foreach (var selected in Selection.gameObjects)
        {

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selected);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.Log("No PrefabFound");
                continue;
            }

            var lodGroup = selected.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                Debug.Log("No LOD");
                continue;
            }
            
            
            var prefabLodGroup = prefab.GetComponent<LODGroup>();
            if (prefabLodGroup == null)
            {
                Debug.Log("No prefab LOD");
                continue;
            }

            if (lodGroup.lodCount != prefabLodGroup.lodCount)
            {
                Debug.Log("Diff LOD count");
                continue;
            }

            var lodEntries = lodGroup.GetLODs();
            var fadeMode = lodGroup.fadeMode;
            var animateCrossFading = lodGroup.animateCrossFading;
            
//            var overrides = PrefabUtility.GetObjectOverrides(lodGroup.gameObject);
//            foreach (var objectOverride in overrides)
//            {
//                if (objectOverride.instanceObject == lodGroup)
//                {
//                    Debug.Log("Found");
//                    PrefabUtility.RevertObjectOverride(objectOverride.instanceObject, InteractionMode.AutomatedAction);
//                    break;
//                }
//            }


            var prefabLods = prefabLodGroup.GetLODs();
            var newLods = lodGroup.GetLODs();
            for (var i = 0; i < newLods.Length; i++)
            {
                newLods[i].renderers = prefabLods[i].renderers;
//                newLods[i].fadeTransitionWidth = lodEntries[i].fadeTransitionWidth;
//                newLods[i].screenRelativeTransitionHeight = lodEntries[i].screenRelativeTransitionHeight;
            }
            lodGroup.SetLODs(newLods);
//            lodGroup.fadeMode = fadeMode;
//            lodGroup.animateCrossFading = animateCrossFading;

            EditorUtility.SetDirty(lodGroup);
            EditorUtility.SetDirty(selected);
        }        
    }
    
}
