using Unity.Entities;

[DisableAutoCreation]
public class ResolvePlayerReference : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public ResolvePlayerReference(GameWorld world) : base(world) {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(PlayerState));
    }

    public void SetLocalPlayer(LocalPlayer localPlayer)
    {
        m_LocalPlayer = localPlayer;
    }

    protected override void OnUpdate()
    {
        if (m_LocalPlayer == null)
            return;
        
        // Find player with correct player id
        var playerStateArray = Group.GetComponentArray<PlayerState>();
        for(var playerIndex=0;playerIndex < playerStateArray.Length; playerIndex++)
        {
            if (playerStateArray[playerIndex].playerId == m_LocalPlayer.playerId)
            {
                m_LocalPlayer.playerState = playerStateArray[playerIndex];
                break;
            }
        }
    }

    LocalPlayer m_LocalPlayer;
}


// TODO (mogensh) rename this. Or can we get rid of it as it not only sets controlled entity on localPlayer?
[DisableAutoCreation]
public class UpdateServerEntityComponent : BaseComponentSystem<LocalPlayer>    
{
    public UpdateServerEntityComponent(GameWorld world) : base(world) {}

    protected override void Update(Entity entity, LocalPlayer localPlayer)
    {
        if (localPlayer.playerState == null)
            return;
        
        var player = localPlayer.playerState;
        
        if (player.controlledEntity != localPlayer.controlledEntity)
        {
            localPlayer.controlledEntity = player.controlledEntity;
        }
    }
}