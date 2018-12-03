using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public abstract class ScriptableObjectRegistry<T> : RegistryBase  
    where T : ScriptableObjectRegistryEntry        
{
    public List<T> entries = new List<T>();

    public T GetEntryById(int registryId)
    {
        return entries[registryId];
    }
    
#if UNITY_EDITOR    

    public int GetId(T scriptable)
    {
        if (scriptable == null)
            return -1;
        
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] == scriptable)
                return i;
        }
        return -1;
    }

    public void ClearAtId(int id)
    {
        GameDebug.Assert(entries[id] != null,"Trying to clear id that already is empty");
        entries[id] = null;
        EditorUtility.SetDirty(this);
    }

    public int Register(T scriptable)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i] == null)
            {
                entries[i] = scriptable;
                return i;
            }
        }

        var index = entries.Count;
        entries.Add(scriptable);
        EditorUtility.SetDirty(this);
        return index;
    }
    
    public override bool Verify()
    {
        var verified = true;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            if (entry == null)
            {
                Debug.Log("Free id:" + i);
                continue;
            }
            
            if (entry.registryId != i)
            {
                Debug.Log(entry + " - Index wrong. Registered as id:" + i + " but registryId is:" + entry.registryId);
                verified = false;
            }
        }

        return verified;
    }
#endif    
}

#if UNITY_EDITOR    

public class ScriptableObjectRegistryEntryEditor<RegType,EntryType> : Editor 
    where RegType : ScriptableObjectRegistry<EntryType>
    where EntryType : ScriptableObjectRegistryEntry
{

    public override void OnInspectorGUI()
    {
        var registry = GetReplicatedEntityRegistry();
        if (registry == null)
        {
            EditorGUILayout.HelpBox("Make sure you have a ReplicatedEntityRegistry in project", MessageType.Error);
            return;
        }
        
        var entry = target as EntryType;
        
        var registryIndex = registry != null ? registry.GetId(entry) : -1;

        if (registryIndex != entry.registryId)
        {
            EditorGUILayout.HelpBox("Factory index does not match client registry index", MessageType.Error);
        }
        
        GUILayout.Label("ScriptableObject registry index:" + entry.registryId);
        GUILayout.Label("Registrered index:" + registryIndex);

        if (registryIndex != -1 || entry.registryId != -1)
        {
            if (GUILayout.Button("Unregister"))
            {
                if (registryIndex != -1)
                    registry.ClearAtId(registryIndex);
                entry.registryId = -1;
                EditorUtility.SetDirty(target);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("NOT REGISTERED!", MessageType.Error);
            
            if (GUILayout.Button("Register"))
            {
                entry.registryId = registry.Register(entry);
                EditorUtility.SetDirty(target);
            }
        }
    }


    public static RegType GetReplicatedEntityRegistry()
    {
        var registryGuids = AssetDatabase.FindAssets("t:" + typeof(RegType));
        if (registryGuids == null || registryGuids.Length == 0)
        {
            GameDebug.LogError("Failed to find registry of type:" + typeof(RegType));
            return null;
        }
        if (registryGuids.Length > 1)
        {
            GameDebug.LogError("There should only be one registry in project of type:" + typeof(RegType));
            return null;
        }

        var guid = registryGuids[0];
        var registryPath = AssetDatabase.GUIDToAssetPath(guid);
        var registry = AssetDatabase.LoadAssetAtPath<RegType>(registryPath);
        return registry;
    }
}

#endif