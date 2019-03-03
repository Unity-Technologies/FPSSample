using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



[CreateAssetMenu(fileName = "ReplicatedEntityRegistry",
    menuName = "FPS Sample/ReplicatedEntity/ReplicatedEntityRegistry")]
public class ReplicatedEntityRegistry : RegistryBase
{
    [Serializable]
    public class Entry
    {
        public WeakAssetReference guid;
        // Each entry has either a asset reference or factory. Never both
        public WeakAssetReference prefab = new WeakAssetReference();
        public ReplicatedEntityFactory factory;
    }

    [SerializeField]
    public List<Entry> entries = new List<Entry>();

    public void LoadAllResources(BundledResourceManager resourceManager)
    {
        for(var i=0;i< entries.Count;i++)
        {
            resourceManager.GetSingleAssetResource(entries[i].guid);
        }
    }

    public Entity Create(EntityManager entityManager, BundledResourceManager resourceManager, 
        GameWorld world, ReplicatedEntity repEntity)
    {
        var prefab = repEntity.gameObject;
        
        if (prefab == null)
        {
            GameDebug.LogError("Cant create. Not gameEntityType. GameEntityTypeDefinition:" + name);
            return Entity.Null;
        }

        var gameObjectEntity = world.Spawn<GameObjectEntity>(prefab);
        gameObjectEntity.name = string.Format("{0}",prefab.name);
        var entity = gameObjectEntity.Entity;
        
        return entity;
    }

    public Entry GetEntry(WeakAssetReference guid)
    {
        var index = GetEntryIndex(guid);
        if (index != -1)
            return entries[index];
        return null;
    }

    public int GetEntryIndex(WeakAssetReference guid)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].guid.Equals(guid))
            {
                return i;
            }
        }
        return -1;
    }
    
    
#if UNITY_EDITOR


    public override void PrepareForBuild()
    {
        Debug.Log("ReplicatedEntityRegistry"); 
        
        entries.Clear();
        
        var guids = AssetDatabase.FindAssets("t:GameObject");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var replicated = go.GetComponent<ReplicatedEntity>();
            if (replicated == null)
                continue;

            
////            PrefabUtility.LoadPrefabContents()
//            var stage =  PrefabStageUtility.GetPrefabStage(go);
//            stage.prefabAssetPath          
            
            replicated.SetAssetGUID(guid);
            
            Debug.Log("   Adding guid:" + guid + " prefab:" + path);
            
            var guidData = new WeakAssetReference(guid);
            entries.Add(new Entry
            {
                guid = guidData,
                prefab = new WeakAssetReference(guid)
            });
        }
        
        guids = AssetDatabase.FindAssets("t:ReplicatedEntityFactory");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var factory = AssetDatabase.LoadAssetAtPath<ReplicatedEntityFactory>(path);

            factory.SetAssetGUID(guid);
            
            Debug.Log("   Adding guid:" + guid + " factory:" + factory);
            
            var guidData = new WeakAssetReference(guid);
            entries.Add(new Entry
            {
                guid = guidData,
                factory = factory
            });
        }
        
        EditorUtility.SetDirty(this);
    }

    
//
//    public int GetId(string guid)
//    {
//        if (guid == null || guid == "")
//            return -1;
//        
//        for (int i = 0; i < entries.Count; i++)
//        {
//            if (entries[i].prefab.guid == guid)
//                return i;
//        }
//        return -1;
//    }
//    
//    public int GetId(ReplicatedEntityFactory factory)
//    {
//        if (factory == null)
//            return -1;
//        
//        for (int i = 0; i < entries.Count; i++)
//        {
//            if (entries[i].factory == factory)
//                return i;
//        }
//        return -1;
//    }
//
//    public void ClearAtId(int index)
//    {
//        entries[index].prefab.guid = "";
//        entries[index].factory = null;
//        
//        EditorUtility.SetDirty(this);
//    }
//
//    public int FindFreeId()
//    {
//        for (var i = 0; i < entries.Count; i++)
//        {
//            if (entries[i].prefab.guid == "" && entries[i].factory == null)
//                return i;
//        }
//
//        var index = entries.Count;
//        entries.Add(new Entry());
//        return index;
//    }
//
//    public void SetPrefab(int registryId, string guid)
//    {
//        GameDebug.Assert(entries[registryId].prefab.guid == "","GUID already set");
//        GameDebug.Assert(entries[registryId].factory == null,"Factory already set");
//
//        entries[registryId].prefab.guid = guid;
//        EditorUtility.SetDirty(this);
//    }
//    
//    public void SetFactory(int registryId, ReplicatedEntityFactory factory)
//    {
//        GameDebug.Assert(entries[registryId].prefab.guid == "","GUID already set");
//        GameDebug.Assert(entries[registryId].factory == null,"Factory already set");
//
//        entries[registryId].factory = factory;
//        EditorUtility.SetDirty(this);
//    }
//    
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
            {
                var factoryPath = AssetDatabase.GetAssetPath(entry.factory);
                var factoryGuid = AssetDatabase.AssetPathToGUID(factoryPath);
                guids.Add(factoryGuid);

                continue;
            }
                
            
            if (!entry.prefab.IsSet())
                continue;
            
            guids.Add(entry.prefab.GetGuidStr());
        }
    }

    public override bool Verify()
    {
        var verified = true;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (!entry.prefab.IsSet())
            {
                Debug.Log("Entry:" + i + " is free");
                continue;
            }

            if (entry.prefab.IsSet() && entry.factory != null)
            {
                Debug.Log("Entry:" + i + " registered with both prefab and factory");
                verified = false;
                continue;
            }
            
            if (entry.factory != null)
            {
                var factoryPath = AssetDatabase.GetAssetPath(entry.factory);
                if (factoryPath == null || factoryPath == "")
                {
                    Debug.Log("Cant find path for factory:" + entry.factory);
                    verified = false;
                }
                
//                var guids = new List<string>();
//                entry.factory.GetSingleAssetGUIDs(guids,false);
//                entry.factory.GetSingleAssetGUIDs(guids,true);
//                foreach (var guid in guids)
//                {
//                    var p = AssetDatabase.GUIDToAssetPath(guid);
//                    if (p == null || p == "")
//                    {
//                        Debug.Log("Cant find path for guid:" + entry.prefab.guid);
//                        verified = false;
//                    }
//                }
//                
////                if (entry.factory.registryId != i)
////                {
////                    Debug.Log(entry.factory + " - Index wrong. Registered as id:" + i + " but registryId is:" +
////                              entry.factory.registryId);
////                    verified = false;
////                }
                
                if (!verified)
                    continue;
            }
            else
            {
                var p = AssetDatabase.GUIDToAssetPath(entry.prefab.GetGuidStr());
                if (p == null || p == "")
                {
                    Debug.Log("Cant find path for guid:" + entry.prefab.GetGuidStr());
                    verified = false;
                    continue;
                }

                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go == null)
                {
                    Debug.Log("Cant load asset for guid:" + entry.prefab.GetGuidStr() + " path:" + p);
                    verified = false;
                    continue;
                }

                var repEntity = go.GetComponent<ReplicatedEntity>();
                if (repEntity == null)
                {
                    Debug.Log(go + " has no GameEntityType component");
                    verified = false;
                    continue;
                }

//                if (repEntity.Value.registryId != i)
//                {
//                    Debug.Log(go + "Id wrong. Registered as:" + i + " but registryId is:" +
//                              repEntity.Value.registryId);
//                    verified = false;
//                    continue;
//                }
            }
        }

        return verified;
    }
#endif
}