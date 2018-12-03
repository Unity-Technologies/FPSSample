using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Effect/SpatialEffectRegistry", fileName = "SpatialEffectRegistry")]
public class SpatialEffectRegistry : ScriptableObjectRegistry<SpatialEffectTypeDefinition>
{
#if UNITY_EDITOR
    public override void GetSingleAssetGUIDs(List<string> guids, bool server)
    {
        if (server)
            return;
        
        foreach (var entry in entries)
        {
            if (entry.prefab != null)
                guids.Add(entry.prefab.guid);
        }
    }
#endif    
}
