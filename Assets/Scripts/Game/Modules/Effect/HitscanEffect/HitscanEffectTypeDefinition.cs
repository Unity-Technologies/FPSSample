using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HitscanEffectTypeDefinition", menuName = "FPS Sample/Effect/HitscanEffectTypeDefinition")]
public class HitscanEffectTypeDefinition : DynamicEnum
{
    public WeakAssetReference prefab;
    public int poolSize = 16;
    
#if UNITY_EDITOR
    public override void GetAssetReferences(List<string> guids, bool server)
    {
        if (server)
            return;

        if (prefab != null)
            guids.Add(prefab.guid);
    }
#endif
}



