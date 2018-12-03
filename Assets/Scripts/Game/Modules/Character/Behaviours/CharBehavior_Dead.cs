using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "Dead",menuName = "FPS Sample/Abilities/Dead")]
public class CharBehavior_Dead : CharBehaviorFactory
{
    public struct InternalState : IComponentData
    {
        public int active;
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

        if (internalState.active != -1)
        {
            internalState.active = 1;
            EntityManager.SetComponentData(abilityEntity,internalState);

            var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(charAbility.character);
            charPredictedState.cameraProfile = CameraProfile.ThirdPerson;
            EntityManager.SetComponentData(charAbility.character, charPredictedState);
        }
    }
}