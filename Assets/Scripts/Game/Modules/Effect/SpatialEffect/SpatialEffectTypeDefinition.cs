using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpatialEffectTypeDefinition", menuName = "FPS Sample/Effect/SpatialEffectTypeDefinition")]
public class SpatialEffectTypeDefinition : DynamicEnum
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

