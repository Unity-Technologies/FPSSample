using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class HandleReplicatedEntitySpawn : InitializeComponentSystem<ReplicatedEntity>
{
    public HandleReplicatedEntitySpawn(GameWorld world, GameObject systemRoot, NetworkServer network,
        ReplicatedEntityRegistry assetRegistry, ReplicatedEntityCollection entityCollection) : base(world)
    {
        m_systemRoot = systemRoot;
        m_assetRegistry = assetRegistry;
        m_entityCollection = entityCollection;
        m_network = network;
    }

    protected override void Initialize(Entity entity, ReplicatedEntity spawned)
    {
//        GameDebug.Assert(m_assetRegistry.guidToIndexMap.ContainsKey(spawned.guid), "An entity was spawned but we have no type for that guid?");

        var typeId = (ushort)spawned.registryId;
        spawned.id = m_network.RegisterEntity(spawned.id, typeId, spawned.predictingPlayerId);
        spawned.name = spawned.name + "_" + spawned.id;
        m_entityCollection.Register(EntityManager, spawned.id, entity);
        
#if UNITY_EDITOR
        if(m_systemRoot != null)
            spawned.transform.SetParent(m_systemRoot.transform);
#endif
    }
    
    private readonly GameObject m_systemRoot;
    private readonly NetworkServer m_network;
    private readonly ReplicatedEntityRegistry m_assetRegistry;
    private readonly ReplicatedEntityCollection m_entityCollection;
}


[DisableAutoCreation]
public class HandleReplicatedEntityDataSpawn : InitializeComponentDataSystem<ReplicatedDataEntity,HandleReplicatedEntityDataSpawn.Initialized>
{
    public struct Initialized : IComponentData{}
    
    public HandleReplicatedEntityDataSpawn(GameWorld world, NetworkServer network,
        ReplicatedEntityRegistry assetRegistry, ReplicatedEntityCollection entityCollection) : base(world)
    {
        m_assetRegistry = assetRegistry;
        m_entityCollection = entityCollection;
        m_network = network;
    }

    protected override void Initialize(Entity entity, ReplicatedDataEntity spawned)
    {

        var typeId = spawned.typeId;
        spawned.id = m_network.RegisterEntity(spawned.id, (ushort)typeId, spawned.predictingPlayerId);
//        spawned.name = spawned.name + "_" + spawned.id;

        GameDebug.Assert(typeId < m_assetRegistry.entries.Count,"TypeId:{0} outside range Length:{1}", typeId, m_assetRegistry.entries.Count);
        GameDebug.Assert(m_assetRegistry.entries[typeId].factory != null,"No valid factory for replicated type:{0}", typeId);

        m_entityCollection.Register(EntityManager, spawned.id, entity);

        PostUpdateCommands.SetComponent(entity, spawned);

        if(ReplicatedEntityModuleServer.m_showInfo.IntValue > 0)
            GameDebug.Log("HandleReplicatedEntityDataDespawn.Initialize entity:" + entity + " type:" + spawned.typeId + " id:" + spawned.id);
    }
    
    private readonly NetworkServer m_network;
    private readonly ReplicatedEntityRegistry m_assetRegistry;
    private readonly ReplicatedEntityCollection m_entityCollection;
}


[DisableAutoCreation]
public class HandleReplicatedEntityDespawn : DeinitializeComponentSystem<ReplicatedEntity>
{
    public HandleReplicatedEntityDespawn(GameWorld world, NetworkServer network,
        ReplicatedEntityCollection entityCollection) : base(world)
    {
        m_entityCollection = entityCollection;
        m_network = network;
    }

    protected override void Deinitialize(Entity entity, ReplicatedEntity component)
    {
        m_entityCollection.Unregister(EntityManager, component.id);
        m_network.UnregisterEntity(component.id);
    }

    private readonly NetworkServer m_network;
    private readonly ReplicatedEntityCollection m_entityCollection;
}

[DisableAutoCreation]
public class HandleReplicatedEntityDataDespawn : DeinitializeComponentDataSystem<ReplicatedDataEntity>
{
    public HandleReplicatedEntityDataDespawn(GameWorld world, NetworkServer network,
        ReplicatedEntityCollection entityCollection) : base(world)
    {
        m_entityCollection = entityCollection;
        m_network = network;
    }

    protected override void Deinitialize(Entity entity, ReplicatedDataEntity component)
    {
        if(ReplicatedEntityModuleServer.m_showInfo.IntValue > 0)
            GameDebug.Log("HandleReplicatedEntityDataDespawn.Deinitialize entity:" + entity + " type:" + component.typeId + " id:" + component.id);
        m_entityCollection.Unregister(EntityManager, component.id);
        m_network.UnregisterEntity(component.id);
    }

    private readonly NetworkServer m_network;
    private readonly ReplicatedEntityCollection m_entityCollection;
}

public class ReplicatedEntityModuleServer
{
    [ConfigVar(Name = "server.replicatedsysteminfo", DefaultValue = "0", Description = "Show replicated system info")]
    public static ConfigVar m_showInfo;
    
    public ReplicatedEntityModuleServer(GameWorld world, BundledResourceManager resourceSystem, NetworkServer network)
    {
        m_world = world;
        m_assetRegistry = resourceSystem.GetResourceRegistry<ReplicatedEntityRegistry>();
        m_entityCollection = new ReplicatedEntityCollection(m_world);

        if (world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("ReplicatedEntitySystem");
            m_SystemRoot.transform.SetParent(world.SceneRoot.transform);
        }
        
        m_handleSpawn = m_world.GetECSWorld().CreateManager<HandleReplicatedEntitySpawn>(m_world, m_SystemRoot, network,
            m_assetRegistry, m_entityCollection);
        m_handleDataSpawn = m_world.GetECSWorld().CreateManager<HandleReplicatedEntityDataSpawn>(m_world, network,
            m_assetRegistry, m_entityCollection);

        m_handleDespawn = m_world.GetECSWorld().CreateManager<HandleReplicatedEntityDespawn>(m_world, network,
            m_entityCollection);
        m_handleDataDespawn = m_world.GetECSWorld().CreateManager<HandleReplicatedEntityDataDespawn>(m_world, network,
            m_entityCollection);
        
        
        m_UpdateReplicatedOwnerFlag = m_world.GetECSWorld().CreateManager<UpdateReplicatedOwnerFlag>(m_world);
        m_UpdateReplicatedOwnerFlag.SetLocalPlayerId(-1);
        
        // Make sure all replicated entities are streamed in
        for (var i = 0; i < m_assetRegistry.entries.Count; i++)
        {
            if (m_assetRegistry.entries[i].factory != null)
                continue;
            if (m_assetRegistry.entries[i].prefab.guid == "")
                continue;
            resourceSystem.LoadSingleAssetResource(m_assetRegistry.entries[i].prefab.guid);
        }
    }
    
    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_handleSpawn);
        m_world.GetECSWorld().DestroyManager(m_handleDataSpawn);
        
        m_world.GetECSWorld().DestroyManager(m_handleDespawn);
        m_world.GetECSWorld().DestroyManager(m_handleDataDespawn);
        
        m_world.GetECSWorld().DestroyManager(m_UpdateReplicatedOwnerFlag);
            
        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }

    internal void ReserveSceneEntities(NetworkServer networkServer)
    {
        // TODO (petera) remove this
        for (var i = 0; i < m_world.SceneEntities.Count; i++)
            GameDebug.Assert(m_world.SceneEntities[i].id == i, "Scene entities must be have the first network ids!");

        networkServer.ReserveSceneEntities(m_world.SceneEntities.Count);
    }

    public void HandleSpawning()
    {
        m_handleSpawn.Update();
        m_handleDataSpawn.Update();
        m_UpdateReplicatedOwnerFlag.Update();
    }

    public void HandleDespawning()
    {
        m_handleDespawn.Update();
        m_handleDataDespawn.Update();
    }

    public void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer)
    {
        m_entityCollection.GenerateEntitySnapshot(entityId, ref writer);
    }

    public string GenerateName(int entityId)
    {
        return m_entityCollection.GenerateName(entityId);
    }



    private readonly GameWorld m_world;

    private readonly GameObject m_SystemRoot;
    private readonly ReplicatedEntityRegistry m_assetRegistry;
    private readonly ReplicatedEntityCollection m_entityCollection;

    private readonly HandleReplicatedEntitySpawn m_handleSpawn;
    private readonly HandleReplicatedEntityDataSpawn m_handleDataSpawn;
    
    private readonly HandleReplicatedEntityDespawn m_handleDespawn;
    private readonly HandleReplicatedEntityDataDespawn m_handleDataDespawn;
    
    readonly UpdateReplicatedOwnerFlag m_UpdateReplicatedOwnerFlag;
}