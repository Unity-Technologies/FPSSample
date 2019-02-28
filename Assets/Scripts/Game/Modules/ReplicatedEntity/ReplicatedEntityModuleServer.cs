using Unity.Entities;
using UnityEngine;


[DisableAutoCreation]
public class HandleReplicatedEntityDataSpawn : InitializeComponentDataSystem<ReplicatedEntityData,HandleReplicatedEntityDataSpawn.Initialized>
{
    public struct Initialized : IComponentData{}
    
    public HandleReplicatedEntityDataSpawn(GameWorld world, NetworkServer network,
        ReplicatedEntityRegistry assetRegistry, ReplicatedEntityCollection entityCollection) : base(world)
    {
        m_assetRegistry = assetRegistry;
        m_entityCollection = entityCollection;
        m_network = network;
    }

    protected override void Initialize(Entity entity, ReplicatedEntityData spawned)
    {
        var typeId = m_assetRegistry.GetEntryIndex(spawned.assetGuid);
        spawned.id = m_network.RegisterEntity(spawned.id, (ushort)typeId, spawned.predictingPlayerId);

        m_entityCollection.Register(EntityManager, spawned.id, entity);

        PostUpdateCommands.SetComponent(entity, spawned);

        if(ReplicatedEntityModuleServer.m_showInfo.IntValue > 0)
            GameDebug.Log("HandleReplicatedEntityDataDespawn.Initialize entity:" + entity + " type:" + typeId + " id:" + spawned.id);
    }
    
    private readonly NetworkServer m_network;
    private readonly ReplicatedEntityRegistry m_assetRegistry;
    private readonly ReplicatedEntityCollection m_entityCollection;
}

[DisableAutoCreation]
public class HandleReplicatedEntityDataDespawn : DeinitializeComponentDataSystem<ReplicatedEntityData>
{
    public HandleReplicatedEntityDataDespawn(GameWorld world, NetworkServer network,
        ReplicatedEntityCollection entityCollection) : base(world)
    {
        m_entityCollection = entityCollection;
        m_network = network;
    }

    protected override void Deinitialize(Entity entity, ReplicatedEntityData component)
    {
        if(ReplicatedEntityModuleServer.m_showInfo.IntValue > 0)
            GameDebug.Log("HandleReplicatedEntityDataDespawn.Deinitialize entity:" + entity + " id:" + component.id);
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
        
        m_handleDataSpawn = m_world.GetECSWorld().CreateManager<HandleReplicatedEntityDataSpawn>(m_world, network,
            m_assetRegistry, m_entityCollection);

        m_handleDataDespawn = m_world.GetECSWorld().CreateManager<HandleReplicatedEntityDataDespawn>(m_world, network,
            m_entityCollection);
        
        
        m_UpdateReplicatedOwnerFlag = m_world.GetECSWorld().CreateManager<UpdateReplicatedOwnerFlag>(m_world);
        m_UpdateReplicatedOwnerFlag.SetLocalPlayerId(-1);
        
        // Load all replicated entity resources
        m_assetRegistry.LoadAllResources(resourceSystem);
    }
    
    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_handleDataSpawn);
        
        m_world.GetECSWorld().DestroyManager(m_handleDataDespawn);
        
        m_world.GetECSWorld().DestroyManager(m_UpdateReplicatedOwnerFlag);
            
        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }

    internal void ReserveSceneEntities(NetworkServer networkServer)
    {
        // TODO (petera) remove this
        for (var i = 0; i < m_world.SceneEntities.Count; i++)
        {
            var gameObjectEntity = m_world.SceneEntities[i].GetComponent<GameObjectEntity>();
            var repEntityData = m_world.GetEntityManager()
                .GetComponentData<ReplicatedEntityData>(gameObjectEntity.Entity);
            GameDebug.Assert(repEntityData.id == i, "Scene entities must be have the first network ids!");
        }

        networkServer.ReserveSceneEntities(m_world.SceneEntities.Count);
    }

    public void HandleSpawning()
    {
        m_handleDataSpawn.Update();
        m_UpdateReplicatedOwnerFlag.Update();
    }

    public void HandleDespawning()
    {
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

    private readonly HandleReplicatedEntityDataSpawn m_handleDataSpawn;
    
    private readonly HandleReplicatedEntityDataDespawn m_handleDataDespawn;
    
    readonly UpdateReplicatedOwnerFlag m_UpdateReplicatedOwnerFlag;
}