using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// TODO (mogensh) currently only real purpose of this registry is to hand guids to bundlebuilding (same goes for itemregistry) Remove when we have addressable assets?
[CreateAssetMenu(menuName = "FPS Sample/Character/TypeRegistry", fileName = "CharacterTypeRegistry")]
public class CharacterTypeRegistry : RegistryBase
{
    public List<CharacterTypeDefinition> entries = new List<CharacterTypeDefinition>();
    
#if UNITY_EDITOR
    
    public override void PrepareForBuild()
    {
        Debug.Log("ReplicatedEntityRegistry"); 

        entries.Clear();
        var guids = AssetDatabase.FindAssets("t:CharacterTypeDefinition");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<CharacterTypeDefinition>(path);
            Debug.Log("   Adding definition:" + definition);
            entries.Add(definition);
        }
        
        EditorUtility.SetDirty(this);
    }

    
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        foreach (var setup in entries)
        {
            if (serverBuild && setup.prefabServer.IsSet())
                guids.Add(setup.prefabServer.GetGuidStr());
            if (!serverBuild && setup.prefabClient.IsSet())
                guids.Add(setup.prefabClient.GetGuidStr());
            if (!serverBuild && setup.prefab1P.IsSet())
                guids.Add(setup.prefab1P.GetGuidStr());
        }
    }
#endif

}