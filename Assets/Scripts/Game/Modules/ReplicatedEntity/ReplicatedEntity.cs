using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public interface IEntityReferenceSerializer    
{
    void SerializeReference(ref NetworkWriter writer, string name, Entity entity);
    void DeserializeReference(ref NetworkReader reader, ref Entity entity);
}

public interface INetworkSerializable
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
}

public struct ReplicatedDataEntity : IComponentData
{
    public int typeId;
    public int id;
    public int predictingPlayerId;    
}



[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(GameObjectEntity))]
public class ReplicatedEntity : MonoBehaviour
{
    public string guid;     // guid of asset. 
    public byte[] netID;    // guid of instance. Used for identifying replicated entities from the scene

    [NonSerialized] public int id = -1;     // network id, used to identify the entity for references and towards the network layer

    [NonSerialized]
    public int predictingPlayerId = -1; // The player id that is predicting this entity or -1 if none

    
#if UNITY_EDITOR

    public static Dictionary<byte[], ReplicatedEntity> netGuidMap = new Dictionary<byte[], ReplicatedEntity>(new ByteArrayComp());

    private void Awake()
    {
        if (EditorApplication.isPlaying)
            return;
        
        SetUniqueNetID();
    }

    private void OnValidate()
    {
        if (EditorApplication.isPlaying)
            return;

        PrefabType prefabType = PrefabUtility.GetPrefabType(this);
        if (prefabType == PrefabType.Prefab || prefabType == PrefabType.ModelPrefab)
        {
            netID = null;
        }
        else
            SetUniqueNetID();
    }

    private void SetUniqueNetID()
    {
        // Generate new if fresh object
        if (netID == null || netID.Length == 0)
        {
            netID = System.Guid.NewGuid().ToByteArray();
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        // If we are the first add us
        if (!netGuidMap.ContainsKey(netID))
        {
            netGuidMap[netID] = this;
            return;
        }

        // Our guid is known and in use by another object??
        var oldReg = netGuidMap[netID];
        if (oldReg != null && oldReg.GetInstanceID() != this.GetInstanceID() && ByteArrayComp.instance.Equals(oldReg.netID, netID))
        {
            // If actually *is* another ReplEnt that has our netID, *then* we give it up (usually happens because of copy / paste)
            netID = System.Guid.NewGuid().ToByteArray();
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        netGuidMap[netID] = this;
    }

#endif
}

