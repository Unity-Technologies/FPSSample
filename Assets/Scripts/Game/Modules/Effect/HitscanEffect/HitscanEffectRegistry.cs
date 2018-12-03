using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS Sample/Effect/HitscanEffectRegistry", fileName = "HitscanEffectRegistry")]
public class HitscanEffectRegistry : ScriptableObjectRegistry<HitscanEffectTypeDefinition>
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
