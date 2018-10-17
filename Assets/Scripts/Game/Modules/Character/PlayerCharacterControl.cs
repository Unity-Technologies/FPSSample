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
    public struct Players
    {
        public ComponentArray<PlayerCharacterControl> characterControls;
        public ComponentArray<PlayerState> playerStates;
    }

    [Inject] 
    public Players Group;

    public PlayerCharacterControlSystem(GameWorld gameWorld)
    {}

    
    protected override void OnUpdate()
    {
        for(var i=0;i< Group.characterControls.Length;i++)
        {
            var player = Group.playerStates[i];
            var controlledEntity = player.controlledEntity;

            if (controlledEntity == Entity.Null || !EntityManager.HasComponent<CharacterPredictedState>(controlledEntity))
                continue;
            
            var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(controlledEntity); 
            var character = EntityManager.GetComponentObject<Character>(controlledEntity);
            
            // Update character team
            charPredictedState.teamId = player.teamIndex;
            
            // Update hit collision
            if (EntityManager.HasComponent<HitCollisionOwner>(controlledEntity))
            {
                var hitCollisionOwner = EntityManager.GetComponentObject<HitCollisionOwner>(controlledEntity);
                hitCollisionOwner.colliderFlags = 1 << charPredictedState.teamId;
            }

            character.characterName = player.playerName;
        }
    }
}

