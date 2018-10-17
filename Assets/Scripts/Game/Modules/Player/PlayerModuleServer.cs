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
        var prefab = (GameObject)m_resourceSystem.LoadSingleAssetResource(m_settings.playerStatePrefab.guid);
        var playerState = m_world.Spawn<PlayerState>(prefab);
        playerState.playerId = playerId;
        playerState.playerName = playerName;

        // Mark the playerstate as 'owned' by ourselves so we can reduce amount of
        // data replicated out from server
        var re = playerState.GetComponent<ReplicatedEntity>();
        re.predictingPlayerId = playerId;

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
