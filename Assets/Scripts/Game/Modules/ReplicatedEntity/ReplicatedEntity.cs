using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

// TODO (mogensh) non generic base interface for predicted and interpolated data handlers. Used to find correct 
// serializers when replicated entity is registered. Someone with more C# generics and reflection knowledge should
// be able to get rid of these
public interface IPredictedDataBase
{}
public interface IInterpolatedDataBase
{}


// Interface used for components that should always be serialized from server to client
public interface INetSerialized
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
}

// Interface for components that are replicated only to predicting clients
public interface INetPredicted<T> : IPredictedDataBase
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
#if UNITY_EDITOR    
    bool VerifyPrediction(ref T state);
#endif    
}

// Interface for components that are replicated to all non-predicting clients
public interface INetInterpolated<T> : IInterpolatedDataBase
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
    void Interpolate(ref T first, ref T last, float t);
}

public interface IEntityReferenceSerializer    
{
    void SerializeReference(ref NetworkWriter writer, string name, Entity entity);
    void DeserializeReference(ref NetworkReader reader, ref Entity entity);
}

public struct ReplicatedDataEntity : IComponentData, INetSerialized
{
    public int typeId;
    public int id;
    public int predictingPlayerId;
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("predictingPlayerId",predictingPlayerId);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        predictingPlayerId = reader.ReadInt32();
    }     
}


[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(GameObjectEntity))]
public class ReplicatedEntity : MonoBehaviour, INetSerialized
{
    public byte[] netID;    // guid of instance. Used for identifying replicated entities from the scene
    public int registryId = -1;

    [NonSerialized] public int id = -1;     // network id, used to identify the entity for references and towards the network layer
    [NonSerialized] public int predictingPlayerId = -1; // The player id that is predicting this entity or -1 if none
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("predictingPlayerId",predictingPlayerId);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        predictingPlayerId = reader.ReadInt32();
    }    
    
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



[DisableAutoCreation]
public class UpdateReplicatedOwnerFlag : BaseComponentSystem
{
    ComponentGroup RepEntityGroup;
    ComponentGroup RepEntityDataGroup;

    int m_localPlayerId;
    bool m_initialized;
    
    public UpdateReplicatedOwnerFlag(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        RepEntityGroup = GetComponentGroup(typeof(ReplicatedEntity));
        RepEntityDataGroup = GetComponentGroup(typeof(ReplicatedDataEntity));
    }
    
    public void SetLocalPlayerId(int playerId)
    {
        m_localPlayerId = playerId;
        m_initialized = true;
    }
    
    protected override void OnUpdate()
    {
        var entityArray = RepEntityGroup.GetEntityArray(); 
        var replicatedArray = RepEntityGroup.GetComponentArray<ReplicatedEntity>();

        for (int i = 0; i < entityArray.Length; i++)
        {
            var repEntity = replicatedArray[i];
            var locallyControlled = m_localPlayerId == -1 || repEntity.predictingPlayerId == m_localPlayerId;

            SetFlagAndChildFlags(entityArray[i], locallyControlled);
        }

        entityArray = RepEntityDataGroup.GetEntityArray();
        var repEntityDataArray = RepEntityDataGroup.GetComponentDataArray<ReplicatedDataEntity>();
        for (int i = 0; i < entityArray.Length; i++)
        {
            var repDataEntity = repEntityDataArray[i];
            var locallyControlled = m_localPlayerId == -1 || repDataEntity.predictingPlayerId == m_localPlayerId;

            SetFlagAndChildFlags(entityArray[i], locallyControlled);
        }  
    }

    void SetFlagAndChildFlags(Entity entity, bool set)
    {
        SetFlag(entity, set);
        
        if (EntityManager.HasComponent<EntityGroupChildren>(entity))
        {
            var buffer = EntityManager.GetBuffer<EntityGroupChildren>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                SetFlag(buffer[i].entity, set);
            }
        } 
    }
    
    void SetFlag(Entity entity, bool set)
    {
        var flagSet = EntityManager.HasComponent<ServerEntity>(entity);
        if (flagSet != set)
        {
            if (set)
                PostUpdateCommands.AddComponent(entity, new ServerEntity());
            else
                PostUpdateCommands.RemoveComponent<ServerEntity>(entity);
        }  
    }
}
