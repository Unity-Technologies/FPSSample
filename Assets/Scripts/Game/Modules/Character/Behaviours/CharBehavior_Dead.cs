using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "Dead",menuName = "FPS Sample/Abilities/Dead")]
public class CharBehavior_Dead : CharBehaviorFactory
{
    public struct InternalState : IComponentData
    {
        public int foo;
    }
    
    public override Entity Create(EntityManager entityManager, List<Entity> entities)
    {
        var entity = CreateCharBehavior(entityManager);
        entities.Add(entity);
		
        // Ability components
        entityManager.AddComponentData(entity, new InternalState());

        return entity;
    }
}


[DisableAutoCreation]
class Dead_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,
    CharBehavior_Dead.InternalState>
{
    public Dead_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        CharBehavior_Dead.InternalState internalState)
    {
        if (abilityCtrl.behaviorState == AbilityControl.State.Active || abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
            return;

        if (abilityCtrl.active == 0)
        {
            var healthState = EntityManager.GetComponentData<HealthStateData>(charAbility.character);
            if (healthState.health <= 0)
            {
                abilityCtrl.behaviorState = AbilityControl.State.RequestActive;
                EntityManager.SetComponentData(entity, abilityCtrl);			
            }
        }
    }
}


[DisableAutoCreation]
class Dead_Update : BaseComponentDataSystem<CharBehaviour, AbilityControl, CharBehavior_Dead.InternalState>
{
    public Dead_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        CharBehavior_Dead.InternalState internalState)
    {
        if (abilityCtrl.active == 0)
            return;

        if (abilityCtrl.behaviorState != AbilityControl.State.Active)
        {
            abilityCtrl.behaviorState = AbilityControl.State.Active;
            EntityManager.SetComponentData(abilityEntity,abilityCtrl);

            var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
            charPredictedState.cameraProfile = CameraProfile.ThirdPerson;
            EntityManager.SetComponentData(charAbility.character, charPredictedState);
        }
    }
}