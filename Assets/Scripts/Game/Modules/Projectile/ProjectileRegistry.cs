using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Projectile/ProjectileRegistry", fileName = "ProjectileRegistry")]
public class ProjectileRegistry : RegistryBase
{
    [System.Serializable]
    public struct Entry
    {
        public ProjectileTypeDefinition definition;
    }

    public Entry[] entries;

    public int GetIndexByRegistryId(uint registryId)
    {
        return (int)registryId -1;
    }


#if UNITY_EDITOR
    public override void UpdateRegistry(bool dry)
    {
        List<Entry> newEntries = new List<Entry>();
        var guids = AssetDatabase.FindAssets("t:" + typeof(ProjectileTypeDefinition).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (dry) Debug.Log("  Adding " + path);
            var asset = AssetDatabase.LoadAssetAtPath(path, typeof(ProjectileTypeDefinition));
            var definition = asset as ProjectileTypeDefinition;

            Entry entry = new Entry();
            entry.definition = definition;
            newEntries.Add(entry);

            var registryId = (uint)newEntries.Count;
            if (definition.registryId != registryId)
            {
                definition.registryId = registryId;
                EditorUtility.SetDirty(definition);
            } 
        }
        if(!dry) 
            entries = newEntries.ToArray();
        EditorUtility.SetDirty(this);
    }
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        if (serverBuild)
            return;
        
        foreach (var entry in entries)
        {
            if (entry.definition.clientProjectilePrefab != null)
                guids.Add(entry.definition.clientProjectilePrefab.guid);
        }
    }
#endif

}
