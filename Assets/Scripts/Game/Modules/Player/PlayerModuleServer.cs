using Unity.Entities;
using UnityEngine;

public class PlayerModuleServer
{
    public PlayerModuleServer(GameWorld gameWorld, BundledResourceManager resourceSystem)
    {
        m_settings = Resources.Load<PlayerModuleSettings>("PlayerModuleSettings");
        m_resourceSystem = resourceSystem;

        m_world = gameWorld;
    }

    public void Shutdown()
    {
        Resources.UnloadAsset(m_settings);
    }

    public PlayerState CreatePlayer(GameWorld world, int playerId, string playerName, bool isReady)
    {
        var prefab = (GameObject)m_resourceSystem.GetSingleAssetResource(m_settings.playerStatePrefab);
        
        
        var gameObjectEntity = m_world.Spawn<GameObjectEntity>(prefab);
        var entityManager = gameObjectEntity.EntityManager;
        var entity = gameObjectEntity.Entity;
        
        var playerState = entityManager.GetComponentObject<PlayerState>(entity);
        playerState.playerId = playerId;
        playerState.playerName = playerName;

        // Mark the playerstate as 'owned' by ourselves so we can reduce amount of
        // data replicated out from server
        var re = entityManager.GetComponentData<ReplicatedEntityData>(entity);
        re.predictingPlayerId = playerId;
        entityManager.SetComponentData(entity,re);
            
        return playerState;
    }

    public void CleanupPlayer(PlayerState player)
    {
        m_world.RequestDespawn(player.gameObject);
    }

    readonly GameWorld m_world;
    readonly BundledResourceManager m_resourceSystem;
    readonly PlayerModuleSettings m_settings;
}
