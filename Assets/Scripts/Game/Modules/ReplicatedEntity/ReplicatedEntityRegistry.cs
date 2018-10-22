using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

interface IReplicatedEntityProvider
{
    WeakAssetReference GetReplicatedServerAsset();
    WeakAssetReference GetReplicatedClientAsset();
}

public abstract class ReplicatedEntityFactor : ScriptableObject
{
    public int typeId;
    public abstract Entity Create(EntityManager entityManager);
    public abstract INetworkSerializable[] CreateSerializables(EntityManager entityManager, Entity entity);
}


[CreateAssetMenu(fileName = "ReplicatedEntityRegistry",
    menuName = "FPS Sample/ReplicatedEntity/ReplicatedEntityRegistry")]
public class ReplicatedEntityRegistry : RegistryBase
{
    [Serializable]
    public struct Entry
    {
        // Each entry has either a asset reference or factory. Never both
        public WeakAssetReference prefab;
        public ReplicatedEntityFactor factory;
    }

    public bool server;
    public Entry[] entries;
    public Dictionary<string, int> guidToIndexMap = new Dictionary<string, int>();

    void OnEnable()
    {
        // Build guid to index map
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].factory != null)
                continue;
            
            string guid = entries[i].prefab.guid;
            if (guid != "")
            {
                GameDebug.Assert(!guidToIndexMap.ContainsKey(guid));
                guidToIndexMap.Add(guid, i);
            }
        }
    }

#if UNITY_EDITOR
    public override void UpdateRegistry(bool dry)
    {
        var newEntries = new List<Entry>();
        var handledGuids = new List<string>();

        // Handle all setups for server and client specific prefabs 
        var guids = AssetDatabase.FindAssets("t:" + typeof(ScriptableObject).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);

            if (!typeof(IReplicatedEntityProvider).IsAssignableFrom(type))
                continue;

            var provider = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) as IReplicatedEntityProvider;

            var serverAsset = provider.GetReplicatedServerAsset();
            var clientAsset = provider.GetReplicatedClientAsset();

            if (serverAsset.guid != "")
                handledGuids.Add(serverAsset.guid);
            if (clientAsset.guid != "")
                handledGuids.Add(clientAsset.guid);

            var entityGuid = server ? serverAsset.guid : clientAsset.guid;
            var entry = new Entry();
            entry.prefab = new WeakAssetReference()
            {
                guid = entityGuid
            };
            newEntries.Add(entry);
        }

        // Find all prefabs with replicated entity component that is not already registered
        guids = AssetDatabase.FindAssets("t:" + typeof(GameObject).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var replicatedEntity = go.GetComponent<ReplicatedEntity>();
            if (replicatedEntity == null)
                continue;


            // Make sure guid is correct so it can be used to when finding type id from instantiated entity
            if (replicatedEntity.guid != guid)
            {
                replicatedEntity.guid = guid;
                GameDebug.Log("Setting GUID for " + replicatedEntity + " to " + guid);
                EditorUtility.SetDirty(go);
            }

            if (handledGuids.Contains(guid))
                continue;

            if (dry) Debug.Log("  Adding " + path);


            var entry = new Entry();
            entry.prefab = new WeakAssetReference
            {
                guid = guid
            };
            newEntries.Add(entry);
        }
        
        // Find all factories
        guids = AssetDatabase.FindAssets("t:" + typeof(ReplicatedEntityFactor).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var factory = AssetDatabase.LoadAssetAtPath<ReplicatedEntityFactor>(path);
                
            var entry = new Entry
            {
                factory = factory
            };
            newEntries.Add(entry);
        }
        
        if (!dry)
            entries = newEntries.ToArray();

        // Make sure factories have correct typeId
        if (!dry)
        {
            for(var i=0;i<entries.Length;i++)
            {
                if (entries[i].factory == null)
                    continue;

                entries[i].factory.typeId = i;
                EditorUtility.SetDirty(entries[i].factory);
            }
        }
    }

    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        foreach (var entry in entries)
        {
            if (entry.factory != null)
                continue;
            guids.Add(entry.prefab.guid);
        }
    }

    public override bool Verify()
    {
        var verified = true;
        foreach (var entry in entries)
        {
            if (entry.factory != null)
                continue;
            
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

            if (replicatedEntity.guid != entry.prefab.guid)
            {
                Debug.Log(go + "GUID mixup. asset GUID:" + entry.prefab.guid + " GUID set in prefab:" +
                          replicatedEntity.guid);
                verified = false;
                continue;
            }
        }

        return verified;
    }
#endif
}