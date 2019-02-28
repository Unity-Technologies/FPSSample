using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

public class ReplicatedEntityModuleClient : ISnapshotConsumer 
{
    public ReplicatedEntityModuleClient(GameWorld world, BundledResourceManager resourceSystem)
    {
        m_world = world;
        m_resourceSystem = resourceSystem;
        m_assetRegistry = resourceSystem.GetResourceRegistry<ReplicatedEntityRegistry>();
        m_entityCollection = new ReplicatedEntityCollection(m_world);
        
        m_UpdateReplicatedOwnerFlag = m_world.GetECSWorld().CreateManager<UpdateReplicatedOwnerFlag>(m_world);
        
        // Load all replicated entity resources
        m_assetRegistry.LoadAllResources(resourceSystem);

        if (world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("ReplicatedEntitySystem");
            m_SystemRoot.transform.SetParent(world.SceneRoot.transform);
        }
    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_UpdateReplicatedOwnerFlag);

        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }
    
    public void ProcessEntitySpawn(int servertick, int id, ushort typeId)
    {
        if (m_showInfo.IntValue > 0)
            GameDebug.Log("ProcessEntitySpawns. Server tick:" + servertick + " id:" + id + " typeid:" + typeId);
        
        // If this is a replicated entity from the scene it only needs to be registered (not instantiated)
        if(id < m_world.SceneEntities.Count)
        {
            var e = m_world.SceneEntities[id];
            var gameObjectEntity = e.gameObject.GetComponent<GameObjectEntity>();
            GameDebug.Assert(gameObjectEntity != null,"Replicated entity " + e.name + " has no GameObjectEntity component");

            m_entityCollection.Register(m_world.GetEntityManager(), id, gameObjectEntity.Entity);
            return;
        }
        

        int index = typeId;

        // If factory present it should be used to create entity
        
        GameDebug.Assert(index < m_assetRegistry.entries.Count,"TypeId:" +typeId + " not in range. Array Length:" + m_assetRegistry.entries.Count);

        var entity = m_resourceSystem.CreateEntity(m_assetRegistry.entries[index].guid);
        if (entity == Entity.Null)
        {
            GameDebug.LogError("Failed to create entity for index:" + index + " guid:" + m_assetRegistry.entries[index].guid);
            return;
        }
        
//        
//        var factory = m_assetRegistry.entries[index].factory;
//        if (factory)
//        {
//            var entity = factory.Create(m_world.GetEntityManager(), m_resourceSystem, m_world);
//
//            var replicatedDataEntity = m_world.GetEntityManager().GetComponentData<ReplicatedEntityData>(entity);
//            replicatedDataEntity.id = id;
//            m_world.GetEntityManager().SetComponentData(entity, replicatedDataEntity);
//
//            m_entityCollection.Register(m_world.GetEntityManager(),id, entity);
//            
//            return;
//        }
        
        {
            Profiler.BeginSample("ReplicatedEntitySystemClient.ProcessEntitySpawns()");
        
//            var prefabGuid = m_assetRegistry.entries[index].prefab.guid;
//            var prefab = (GameObject)m_resourceSystem.LoadSingleAssetResource(prefabGuid);
//
//            Entity entity;
//            var gameObject = m_world.SpawnInternal(prefab, new Vector3(), Quaternion.identity, out entity);
//            gameObject.name = prefab.name + "_" + id;
//        
//            if(m_SystemRoot != null)
//                gameObject.transform.SetParent(m_SystemRoot.transform);

            var replicatedDataEntity = m_world.GetEntityManager().GetComponentData<ReplicatedEntityData>(entity);
            replicatedDataEntity.id = id;
            m_world.GetEntityManager().SetComponentData(entity, replicatedDataEntity);
            
            m_entityCollection.Register(m_world.GetEntityManager(),id, entity);
            
            Profiler.EndSample();
        }
    }

    public void ProcessEntityUpdate(int serverTick, int id, ref NetworkReader reader)
    {
        if(m_showInfo.IntValue > 1)
            GameDebug.Log("ApplyEntitySnapshot. ServerTick:" + serverTick + " entityId:" + id);

        m_entityCollection.ProcessEntityUpdate(serverTick, id, ref reader);
    }

    public void ProcessEntityDespawns(int serverTime, List<int> despawns)
    {
        if (m_showInfo.IntValue > 0)
            GameDebug.Log("ProcessEntityDespawns. Server tick:" + serverTime + " ids:" + string.Join(",", despawns));

        foreach(var id in despawns)
        {
            var entity = m_entityCollection.Unregister(m_world.GetEntityManager(), id);

            if (m_world.GetEntityManager().HasComponent<ReplicatedEntity>(entity))
            {
                var replicatedEntity = m_world.GetEntityManager().GetComponentObject<ReplicatedEntity>(entity);            
                m_world.RequestDespawn(replicatedEntity.gameObject);
                continue;
            }
            
            m_world.RequestDespawn(entity);
        }
    }

    public void Rollback()
    {
        m_entityCollection.Rollback();
    }
    
    public void Interpolate(GameTime time)
    {
        m_entityCollection.Interpolate(time);
    }

    public void SetLocalPlayerId(int id)
    {
        m_UpdateReplicatedOwnerFlag.SetLocalPlayerId(id);
    }

    public void UpdateControlledEntityFlags()
    {
        m_UpdateReplicatedOwnerFlag.Update();
    }
    
#if UNITY_EDITOR

    public int GetEntityCount()
    {
        return m_entityCollection.GetEntityCount();
    }

    public int GetSampleCount()
    {
        return m_entityCollection.GetSampleCount();
    }

    public int GetSampleTick(int sampleIndex)
    {
        return m_entityCollection.GetSampleTick(sampleIndex);
    }
    
    public int GetLastServerTick(int sampleIndex)
    {
        return m_entityCollection.GetLastServerTick(sampleIndex);
    }

    public int GetNetIdFromEntityIndex(int entityIndex)
    {
        return m_entityCollection.GetNetIdFromEntityIndex(entityIndex);
    }    

    public ReplicatedEntityCollection.ReplicatedData GetReplicatedDataForNetId(int netId)
    {
        return m_entityCollection.GetReplicatedDataForNetId(netId);
    }
    
    
    public void StorePredictedState(int predictedTick, int finalTick)
    {
        m_entityCollection.StorePredictedState(predictedTick, finalTick);
    }

    public void FinalizedStateHistory(int tick, int lastServerTick, ref UserCommand command)
    {
        m_entityCollection.FinalizedStateHistory(tick, lastServerTick, ref command);
    }

    public int FindSampleIndexForTick(int tick)
    {
        return m_entityCollection.FindSampleIndexForTick(tick);
    }

    public bool IsPredicted(int entityIndex)
    {
        return m_entityCollection.IsPredicted(entityIndex);
    }

#endif

    readonly GameWorld m_world;
    readonly GameObject m_SystemRoot;
    readonly BundledResourceManager m_resourceSystem;
    readonly ReplicatedEntityRegistry m_assetRegistry;
    readonly ReplicatedEntityCollection m_entityCollection;
    readonly UpdateReplicatedOwnerFlag m_UpdateReplicatedOwnerFlag;

    [ConfigVar(Name = "replicatedentity.showclientinfo", DefaultValue = "0", Description = "Show replicated system info")]
    static ConfigVar m_showInfo;
}
