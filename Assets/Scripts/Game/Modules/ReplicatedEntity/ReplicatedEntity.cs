using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
#endif





[Serializable]
public struct ReplicatedEntityData : IComponentData, IReplicatedComponent
{
    public WeakAssetReference assetGuid;    // Guid of asset this entity is created from
    [NonSerialized] public int id;
    [NonSerialized] public int predictingPlayerId;

    public ReplicatedEntityData(WeakAssetReference guid)
    {
        this.assetGuid = guid;
        id = -1;
        predictingPlayerId = -1;
    }
    
    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<ReplicatedEntityData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt32("predictingPlayerId",predictingPlayerId);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        predictingPlayerId = reader.ReadInt32();
    }     
}




[ExecuteAlways, DisallowMultipleComponent]
[RequireComponent(typeof(GameObjectEntity))]
public class ReplicatedEntity : ComponentDataProxy<ReplicatedEntityData>
{
    public byte[] netID;    // guid of instance. Used for identifying replicated entities from the scene

    private void Awake()
    {
        // Ensure replicatedEntityData is set to default
        var val = Value;
        val.id = -1;
        val.predictingPlayerId = -1;
        Value = val;
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            SetUniqueNetID();
#endif        
    }

#if UNITY_EDITOR

    public static Dictionary<byte[], ReplicatedEntity> netGuidMap = new Dictionary<byte[], ReplicatedEntity>(new ByteArrayComp());

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

        UpdateAssetGuid();
    }

    public bool SetAssetGUID(string guidStr)
    {
        var guid = new WeakAssetReference(guidStr);
        var val = Value;
        var currentGuid = val.assetGuid; 
        if (!guid.Equals(currentGuid))
        {
            val.assetGuid = guid;
            Value = val;
            PrefabUtility.SavePrefabAsset(gameObject);
            return true;
        }

        return false;
    }
    
    public void UpdateAssetGuid()
    {
        // Set type guid
        var stage = PrefabStageUtility.GetPrefabStage(gameObject);
        if (stage != null)
        {
            var guidStr = AssetDatabase.AssetPathToGUID(stage.prefabAssetPath);
            if(SetAssetGUID(guidStr))
                EditorSceneManager.MarkSceneDirty(stage.scene);
        }
    }
    
    private void SetUniqueNetID()
    {
        // Generate new if fresh object
        if (netID == null || netID.Length == 0)
        {
            var guid = System.Guid.NewGuid();
            netID = guid.ToByteArray();
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
    ComponentGroup RepEntityDataGroup;

    int m_localPlayerId;
    bool m_initialized;
    
    public UpdateReplicatedOwnerFlag(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        RepEntityDataGroup = GetComponentGroup(typeof(ReplicatedEntityData));
    }
    
    public void SetLocalPlayerId(int playerId)
    {
        m_localPlayerId = playerId;
        m_initialized = true;
    }
    
    protected override void OnUpdate()
    {
        var entityArray = RepEntityDataGroup.GetEntityArray();
        var repEntityDataArray = RepEntityDataGroup.GetComponentDataArray<ReplicatedEntityData>();
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
