using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class ReplicatedEntityFactory : ScriptableObjectRegistryEntry
{
    public abstract Entity Create(EntityManager entityManager, int predictingPlayerId);
}


[CreateAssetMenu(fileName = "ReplicatedEntityRegistry",
    menuName = "FPS Sample/ReplicatedEntity/ReplicatedEntityRegistry")]
public class ReplicatedEntityRegistry : RegistryBase
{
    [Serializable]
    public class Entry
    {
        // Each entry has either a asset reference or factory. Never both
        public WeakAssetReference prefab = new WeakAssetReference();
        public ReplicatedEntityFactory factory;
    }

    public List<Entry> entries = new List<Entry>();

#if UNITY_EDITOR

    public int GetId(string guid)
    {
        if (guid == null || guid == "")
            return -1;
        
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].prefab.guid == guid)
                return i;
        }
        return -1;
    }
    
    public int GetId(ReplicatedEntityFactory factory)
    {
        if (factory == null)
            return -1;
        
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].factory == factory)
                return i;
        }
        return -1;
    }

    public void ClearAtId(int index)
    {
        entries[index].prefab.guid = "";
        entries[index].factory = null;
        
        EditorUtility.SetDirty(this);
    }

    public int FindFreeId()
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].prefab.guid == "" && entries[i].factory == null)
                return i;
        }

        var index = entries.Count;
        entries.Add(new Entry());
        return index;
    }

    public void SetPrefab(int registryId, string guid)
    {
        GameDebug.Assert(entries[registryId].prefab.guid == "","GUID already set");
        GameDebug.Assert(entries[registryId].factory == null,"Factory already set");

        entries[registryId].prefab.guid = guid;
        EditorUtility.SetDirty(this);
    }
    
    public void SetFactory(int registryId, ReplicatedEntityFactory factory)
    {
        GameDebug.Assert(entries[registryId].prefab.guid == "","GUID already set");
        GameDebug.Assert(entries[registryId].factory == null,"Factory already set");

        entries[registryId].factory = factory;
        EditorUtility.SetDirty(this);
    }
    
    public static ReplicatedEntityRegistry GetReplicatedEntityRegistry()
    {
        var registryGuids = AssetDatabase.FindAssets("t:ReplicatedEntityRegistry");
        if (registryGuids == null || registryGuids.Length == 0)
        {
            GameDebug.LogError("Failed to find ReplicatedEntityRegistry");
            return null;
        }
        if (registryGuids.Length > 1)
        {
            GameDebug.LogError("There should only be one ReplicatedEntityRegistry in project");
            return null;
        }

        var guid = registryGuids[0];
        var registryPath = AssetDatabase.GUIDToAssetPath(guid);
        var registry = AssetDatabase.LoadAssetAtPath<ReplicatedEntityRegistry>(registryPath);
        return registry;
    }
    
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        foreach (var entry in entries)
        {
            if (entry.factory != null)
                continue;
            
            if (entry.prefab.guid == "")
                continue;
            
            guids.Add(entry.prefab.guid);
        }
    }

    public override bool Verify()
    {
        var verified = true;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (entry.prefab.guid == "" && entry.factory == null)
            {
                Debug.Log("Entry:" + i + " is free");
                continue;
            }

            if (entry.prefab.guid != "" && entry.factory != null)
            {
                Debug.Log("Entry:" + i + " registered with both prefab and factory");
                verified = false;
                continue;
            }
            
            if (entry.factory != null)
            {
                if (entry.factory.registryId != i)
                {
                    Debug.Log(entry.factory + " - Index wrong. Registered as id:" + i + " but registryId is:" +
                              entry.factory.registryId);
                    verified = false;
                    continue;
                }
            }
            else
            {
                var p = AssetDatabase.GUIDToAssetPath(entry.prefab.guid);
                if (p == null || p == "")
                {
                    Debug.Log("Cant find path for guid:" + entry.prefab.guid);
                    verified = false;
                    continue;
                }

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null)
                {
                    Debug.Log("Cant load asset for guid:" + entry.prefab.guid + " path:" + p);
                    verified = false;
                    continue;
                }

                var replicatedEntity = go.GetComponent<ReplicatedEntity>();
                if (go == null)
                {
                    Debug.Log(go + " has no ReplicatedEntity component");
                    verified = false;
                    continue;
                }

                if (replicatedEntity.registryId != i)
                {
                    Debug.Log(go + "Id wrong. Registered as:" + i + " but registryId is:" +
                              replicatedEntity.registryId);
                    verified = false;
                    continue;
                }
            }
        }

        return verified;
    }
#endif
}