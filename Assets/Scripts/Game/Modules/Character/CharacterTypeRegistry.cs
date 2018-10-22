using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Character/TypeRegistry", fileName = "CharacterTypeRegistry")]
public class CharacterTypeRegistry : RegistryBase
{
    public CharacterTypeDefinition[] entries;

    public CharacterTypeDefinition GetEntryById(uint registryId)
    {
        var index = FindIndexByRigistryId(registryId);
        return entries[index];
    }
    
    public int FindIndexByRigistryId(uint registryId)
    {
        return (int)registryId - 1;
    }
   
    
    public int GetIndexByClientGUID(string guid)
    {
        for (var i = 0; i < entries.Length; i++)
        {
            if (entries[i].prefabClient.guid == guid)
                return i;
        }

        return -1;
    }
    

#if UNITY_EDITOR
    public override void UpdateRegistry(bool dry)
    {
        var newEntries = new List<CharacterTypeDefinition>();
        var guids = AssetDatabase.FindAssets("t:" + typeof(CharacterTypeDefinition).Name);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (dry) Debug.Log("  Adding " + path);
            var asset = AssetDatabase.LoadAssetAtPath(path, typeof(CharacterTypeDefinition));
            var definition = asset as CharacterTypeDefinition;

            newEntries.Add(definition);

            // Update registry ID
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
        foreach (var setup in entries)
        {
            if (serverBuild && setup.prefabServer.guid != "")
                guids.Add(setup.prefabServer.guid);
            if (!serverBuild && setup.prefabClient.guid != "")
                guids.Add(setup.prefabClient.guid);
            if (!serverBuild && setup.prefab1P.guid != "")
                guids.Add(setup.prefab1P.guid);
        }
    }
#endif

}