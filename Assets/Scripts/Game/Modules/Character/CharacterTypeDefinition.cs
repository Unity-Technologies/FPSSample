using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "CharacterTypeDefinition", menuName = "FPS Sample/Character/TypeDefinition")]
public class CharacterTypeDefinition : DynamicEnum, IReplicatedEntityProvider
{
    public WeakAssetReference prefabServer;
    public WeakAssetReference prefabClient;
    public WeakAssetReference prefab1P;
    public float eyeHeight = 1.8f;

    public CharacterMoveQuery.Settings characterMovementSettings;
    public HitCollisionHistory.Settings hitCollisionSettings;
    
    public WeakAssetReference GetReplicatedServerAsset()
    {
        return prefabServer;
    }

    public WeakAssetReference GetReplicatedClientAsset()
    {
        return prefabClient;
    }
}
