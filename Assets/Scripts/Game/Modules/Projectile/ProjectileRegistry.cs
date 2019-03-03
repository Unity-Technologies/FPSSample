using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Projectile/ProjectileRegistry", fileName = "ProjectileRegistry")]
public class ProjectileRegistry : RegistryBase
{
    [Serializable]
    public class Entry
    {
        public WeakAssetReference assetGuid;
        public ProjectileTypeDefinition definition;
    }

    public List<Entry> entries = new List<Entry>();

    public int FindIndex(WeakAssetReference guid)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].assetGuid.Equals(guid))
                return i;
        }

        return -1;
    }
    
#if UNITY_EDITOR

    
    public override void PrepareForBuild()
    {
        Debug.Log("ProjectileRegistry"); 

        entries.Clear();
        var guids = AssetDatabase.FindAssets("t:ProjectileTypeDefinition");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<ProjectileTypeDefinition>(path);
            
            definition.SetAssetGUID(guid);
            
            Debug.Log("   Adding definition:" + definition);
            entries.Add(new Entry
            {
                definition =  definition,
                assetGuid = definition.guid,
            });
        }
        
        EditorUtility.SetDirty(this);
    }
    
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        if (serverBuild)
            return;
        
        foreach (var entry in entries)
        {
            if (entry.definition.clientProjectilePrefab.IsSet())
                guids.Add(entry.definition.clientProjectilePrefab.GetGuidStr());
        }
    }
#endif

}
