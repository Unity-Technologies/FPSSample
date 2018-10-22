using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ItemTypeDefinition", menuName = "FPS Sample/Item/TypeDefinition")]
public class ItemTypeDefinition : DynamicEnum, IReplicatedEntityProvider
{
    public WeakAssetReference prefabServer;
    public WeakAssetReference prefabClient;
    public WeakAssetReference prefab1P;

    public WeakAssetReference GetReplicatedServerAsset()
    {
        return prefabServer;
    }

    public WeakAssetReference GetReplicatedClientAsset()
    {
        return prefabClient;
    }

    
#if UNITY_EDITOR
   
    public override void GetAssetReferences(List<string> guids, bool server)
    {
        if (server && prefabServer.guid != "")
            guids.Add(prefabServer.guid);
        if (!server && prefabClient.guid != "")
            guids.Add(prefabClient.guid);
        if (!server && prefab1P.guid != "")
            guids.Add(prefab1P.guid);
    }
    
#endif
    
}
