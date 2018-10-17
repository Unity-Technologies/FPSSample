using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Registry<T> : RegistryBase  where T : DynamicEnum        
{
    public T[] entries;

    public T GetEntryById(uint registryId)
    {
        var index = GetIndexByRegistryId(registryId);
        return entries[index];
    }
    
    public int GetIndexByRegistryId(uint registryId)
    {
        return (int)registryId -1;
    }


#if UNITY_EDITOR
    public override void UpdateRegistry(bool dry)
    {
        var newEntries = new List<T>();
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (dry) Debug.Log("  Adding " + path);
            var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T));
            var entry = asset as T;

            newEntries.Add(entry);

            var registryId = (uint)newEntries.Count;
            if (entry.registryId != registryId)
            {
                entry.registryId = registryId;
                EditorUtility.SetDirty(entry);
            } 
        }
        
        if(!dry)
            entries = newEntries.ToArray();
        EditorUtility.SetDirty(this);
    }
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        foreach (var entry in entries)
        {
            entry.GetAssetReferences(guids, serverBuild);
        }
    }
#endif
}
