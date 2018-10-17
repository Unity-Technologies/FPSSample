using UnityEngine;
using UnityEditor;

public static class EnableInstancingOnAllMaterials
{
    [MenuItem("fps.sample/Performance/Enable Instancing on All Materials")]

    static void DoIt()
    {
        var materialGuids = AssetDatabase.FindAssets("t:Material");
        foreach (var materialGuid in materialGuids)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuid));
            material.enableInstancing = true;
        }
    }
}