using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// TODO (mogensh) currently only real purpose of this registry is to hand guids to bundlebuilding (same goes for itemregistry) Remove when we have addressable assets?
[CreateAssetMenu(menuName = "FPS Sample/Character/TypeRegistry", fileName = "CharacterTypeRegistry")]
public class CharacterTypeRegistry : ScriptableObjectRegistry<CharacterTypeDefinition>
{


#if UNITY_EDITOR
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