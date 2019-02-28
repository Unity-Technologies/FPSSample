using UnityEngine;
using Unity.Entities;


public class PlayerCharacterControl : MonoBehaviour 
{
    public int characterType = -1; 
    public int requestedCharacterType = -1;
}

[DisableAutoCreation]
public class PlayerCharacterControlSystem : ComponentSystem
{
    ComponentGroup Group;

    public PlayerCharacterControlSystem(GameWorld gameWorld)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(PlayerCharacterControl), typeof(PlayerState));
    }

    protected override void OnUpdate()
    {
        var playerCharControlArray = Group.GetComponentArray<PlayerCharacterControl>();
        var playerStateArray = Group.GetComponentArray<PlayerState>();
        
        for(var i=0;i< playerCharControlArray.Length;i++)
        {
            var player = playerStateArray[i];
            var controlledEntity = player.controlledEntity;

            if (controlledEntity == Entity.Null || !EntityManager.HasComponent<Character>(controlledEntity))
                continue;
            
            var character = EntityManager.GetComponentObject<Character>(controlledEntity);
            
            // Update character team
            character.teamId = player.teamIndex;
            
            // Update hit collision
            if (EntityManager.HasComponent<HitCollisionOwnerData>(controlledEntity))
            {
                var hitCollisionOwner = EntityManager.GetComponentData<HitCollisionOwnerData>(controlledEntity);
                hitCollisionOwner.colliderFlags = 1U << character.teamId;
                EntityManager.SetComponentData(controlledEntity,hitCollisionOwner);
            }

            character.characterName = player.playerName;
        }
    }
}

