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
        
        // Load all replicated entity resources
        for(var i=0;i< m_assetRegistry.entries.Length;i++)
        {
            if (m_assetRegistry.entries[i].factory != null)
                continue;

            var prefabGuid = m_assetRegistry.entries[i].prefab.guid;
            m_resourceSystem.LoadSingleAssetResource(prefabGuid);
        }

        if (world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("ReplicatedEntitySystem");
            m_SystemRoot.transform.SetParent(world.SceneRoot.transform);
        }
    }

    public void Shutdown()
    {
        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }
    
//    private List<INetworkSerializable> networkSerializables = new List<INetworkSerializable>(16);
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

            m_entityCollection.Register(id, gameObjectEntity.Entity, e.GetComponentsInChildren<INetworkSerializable>());
            return;
        }
        

        int index = typeId;

        // If factory present it should be used to create entity
        
        GameDebug.Assert(index < m_assetRegistry.entries.Length,"TypeId:" +typeId + " not in range. Array Length:" + m_assetRegistry.entries.Length);
        
        var factory = m_assetRegistry.entries[index].factory;
        if (factory)
        {
            var entity = factory.Create(m_world.GetEntityManager());

            var replicatedDataEntity = m_world.GetEntityManager().GetComponentData<ReplicatedDataEntity>(entity);
            replicatedDataEntity.id = id;
            
            var serializables = factory.CreateSerializables(m_world.GetEntityManager(), entity);
            m_entityCollection.Register(id, entity, serializables);
            
            m_world.GetEntityManager().SetComponentData(entity, replicatedDataEntity);
            return;
        }
        
        // Use prefab to create entity
        {
            Profiler.BeginSample("ReplicatedEntitySystemClient.ProcessEntitySpawns()");
        
            var prefabGuid = m_assetRegistry.entries[index].prefab.guid;
            var prefab = (GameObject)m_resourceSystem.LoadSingleAssetResource(prefabGuid);

            Entity entity;
            var gameObject = m_world.SpawnInternal(prefab, new Vector3(), Quaternion.identity, out entity);
            gameObject.name = prefab.name + "_" + id;
        
            if(m_SystemRoot != null)
                gameObject.transform.SetParent(m_SystemRoot.transform);

            var replicatedEntity = gameObject.GetComponent<ReplicatedEntity>();
            GameDebug.Assert(replicatedEntity != null, "GameObject has no ReplicatedEntity component");
            replicatedEntity.id = id;
        
            m_entityCollection.Register(id, entity, replicatedEntity.GetComponentsInChildren<INetworkSerializable>());

            Profiler.EndSample();
        }
    }

    public void ProcessEntityDespawn(int serverTick, int id)
    {
        if (m_showInfo.IntValue > 0)
        {
            if (m_despawnedEntities.Contains(id))
            {
                GameDebug.Log("RequestDespawnEntity failed as id already registerd for despawn. ServerTick:" + serverTick + " entityId:" + id);
                return;
            }
            
            if(m_showInfo.IntValue > 0)
                GameDebug.Log("RequestDespawnEntity. Tick:" + serverTick + " entityId:" + id);
        }

        GameDebug.Assert(!m_despawnedEntities.Contains(id), "Trying to request despawn for entity twice:{0}",id);

        m_despawnedEntities.Add(id);

    }

    public void ProcessEntityUpdate(int serverTick, int id, ref NetworkReader reader)
    {
        if(m_showInfo.IntValue > 1)
            GameDebug.Log("ApplyEntitySnapshot. ServerTick:" + serverTick + " entityId:" + id);

        m_entityCollection.ProcessEntityUpdate(serverTick, id, ref reader);
    }

    public void HandleEntityDespawns()
    {
        foreach(var id in m_despawnedEntities)
        {
            var entity = m_entityCollection.Unregister(id);

            if (m_world.GetEntityManager().HasComponent<ReplicatedEntity>(entity))
            {
                var replicatedEntity = m_world.GetEntityManager().GetComponentObject<ReplicatedEntity>(entity);            
                m_world.RequestDespawn(replicatedEntity.gameObject);
                continue;
            }
            
            m_world.GetEntityManager().AddComponent(entity, typeof(DespawningEntity));
        }

        m_despawnedEntities.Clear();
    }

    
    private readonly GameWorld m_world;
    private readonly GameObject m_SystemRoot;
    private readonly BundledResourceManager m_resourceSystem;
    private readonly ReplicatedEntityRegistry m_assetRegistry;
    private readonly ReplicatedEntityCollection m_entityCollection;

    private List<int> m_despawnedEntities = new List<int>();

    [ConfigVar(Name = "client.replicatedsysteminfo", DefaultValue = "0", Description = "Show replicated system info")]
    static ConfigVar m_showInfo;
}
