using Unity.Entities;

[DisableAutoCreation]
public class ResolvePlayerReference : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public ResolvePlayerReference(GameWorld world) : base(world) {}

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
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
            // Remove components added for previous controlled entity
            if (localPlayer.controlledEntity != Entity.Null)
            {
                var controlledEntity = localPlayer.controlledEntity;
                if(EntityManager.Exists(controlledEntity) && EntityManager.HasComponent<ServerEntity>(controlledEntity))
                    PostUpdateCommands.RemoveComponent<ServerEntity>(controlledEntity);
            }

            localPlayer.controlledEntity = player.controlledEntity;

            if (localPlayer.controlledEntity != Entity.Null)
            {
                var controlledEntity = localPlayer.controlledEntity;
                if(!EntityManager.HasComponent< ServerEntity>(controlledEntity))
                    PostUpdateCommands.AddComponent(controlledEntity, new ServerEntity());
            }
        }
    }
}