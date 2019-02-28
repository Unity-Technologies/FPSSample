using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability_ProjectileLauncher",menuName = "FPS Sample/Abilities/Ability_ProjectileLauncher")]
public class Ability_ProjectileLauncher : CharBehaviorFactory
{
    public enum Phase
    {
        Idle,
        Active,
        Cooldown,
    }
    
    public struct LocalState : IComponentData
    {
        public int lastFireTick;
    }
    
    [Serializable]
    public struct Settings : IComponentData
    {
        public UserCommand.Button activateButton;
        
        public float activationDuration;        
        public float cooldownDuration;
        public CharacterPredictedData.Action fireAction;
        public float projectileRange;
        public WeakAssetReference projectileAssetGuid;
    }

    public struct PredictedState : IPredictedComponent<PredictedState>, IComponentData
    {
        public int activeTick;

        public static IPredictedComponentSerializerFactory CreateSerializerFactory()
        {
            return new PredictedComponentSerializerFactory<PredictedState>();
        }
        
        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("activeTick", activeTick);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            activeTick = reader.ReadInt32();
        }
#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return activeTick == state.activeTick;
        }
#endif    
    }
    
    public struct InterpolatedState : IInterpolatedComponent<InterpolatedState>, IComponentData
    {
        public int fireTick;
        
        public static IInterpolatedComponentSerializerFactory CreateSerializerFactory()
        {
            return new InterpolatedComponentSerializerFactory<InterpolatedState>();
        }
        
        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("fireTick", fireTick);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            fireTick = reader.ReadInt32();
        }

        public void Interpolate(ref SerializeContext context, ref InterpolatedState first, ref InterpolatedState last,
            float t)
        {
            this = first;
        }
    }
    
    public Settings settings;

    public override Entity Create(EntityManager entityManager, List<Entity> entities)
    {
        var entity = CreateCharBehavior(entityManager);
        entities.Add(entity);
		
        // Ability components
        entityManager.AddComponentData(entity, settings);
        entityManager.AddComponentData(entity, new LocalState());
        entityManager.AddComponentData(entity, new PredictedState());
        entityManager.AddComponentData(entity, new InterpolatedState());
        return entity;
    }
}
                  
[DisableAutoCreation]
class ProjectileLauncher_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,
    Ability_ProjectileLauncher.PredictedState,Ability_ProjectileLauncher.Settings>
{
    public ProjectileLauncher_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_ProjectileLauncher.PredictedState predictedState, Ability_ProjectileLauncher.Settings settings)
    {
        if (abilityCtrl.behaviorState == AbilityControl.State.Active || abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
            return;
		
        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
        abilityCtrl.behaviorState = command.buttons.IsSet(settings.activateButton) ?  
            AbilityControl.State.RequestActive : AbilityControl.State.Idle;
        EntityManager.SetComponentData(entity, abilityCtrl);			
    }
}



[DisableAutoCreation]
class ProjectileLauncher_Update : BaseComponentDataSystem<AbilityControl,Ability_ProjectileLauncher.PredictedState,
    Ability_ProjectileLauncher.Settings>
{
    public ProjectileLauncher_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, AbilityControl abilityCtrl, Ability_ProjectileLauncher.PredictedState predictedState, Ability_ProjectileLauncher.Settings state)
    {
        var time = m_world.worldTime;
        switch (abilityCtrl.behaviorState)
        {
            case AbilityControl.State.RequestActive:
                if (abilityCtrl.active == 1)
                {
                    var charAbility = EntityManager.GetComponentData<CharBehaviour>(entity);
                    var character = EntityManager.GetComponentObject<Character>(charAbility.character);
                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
                    
                    abilityCtrl.behaviorState = AbilityControl.State.Active;
                    predictedState.activeTick = time.tick;
                    
//                    GameDebug.Log("Ability_ProjectileLauncher SetAction:" + state.fireAction + " tick:" + time.tick);
                    charPredictedState.SetAction(state.fireAction, time.tick);

                    // Only spawn once for each tick (so it does not fire again when re-predicting)
                    var localState = EntityManager.GetComponentData<Ability_ProjectileLauncher.LocalState>(entity);
                    if (time.tick > localState.lastFireTick)
                    {
                        localState.lastFireTick = time.tick;
                        EntityManager.SetComponentData(entity, localState);
                        
                        var eyePos = charPredictedState.position + Vector3.up*character.eyeHeight;
                        var interpolatedState = EntityManager.GetComponentData<Ability_ProjectileLauncher.InterpolatedState>(entity);
                        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character)
                            .command;
                        
                        var endPos = eyePos + command.lookDir * state.projectileRange;
                        ProjectileRequest.Create(PostUpdateCommands, time.tick, time.tick - command.renderTick,
                            state.projectileAssetGuid, charAbility.character, character.teamId, eyePos, endPos);

                        interpolatedState.fireTick = time.tick;
                        EntityManager.SetComponentData(entity, interpolatedState);
                    }
                    
                    EntityManager.SetComponentData(entity, abilityCtrl);
                    EntityManager.SetComponentData(entity, predictedState);
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);
                }
                break;
            case AbilityControl.State.Active:
            {
                var phaseDuration = time.DurationSinceTick(predictedState.activeTick);
                if (phaseDuration > state.activationDuration)
                {
                    var charAbility = EntityManager.GetComponentData<CharBehaviour>(entity);
                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

                    abilityCtrl.behaviorState = AbilityControl.State.Cooldown;
                    
//                    GameDebug.Log("Ability_ProjectileLauncher SetAction:" + CharPredictedStateData.Action.None + " tick:" + time.tick);
                    charPredictedState.SetAction(CharacterPredictedData.Action.None, time.tick);
                    
                    EntityManager.SetComponentData(entity, abilityCtrl);
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);
                }
                break;
            }
            case AbilityControl.State.Cooldown:
            {
                var phaseDuration = time.DurationSinceTick(predictedState.activeTick);
                if (phaseDuration > state.cooldownDuration)
                {
                    abilityCtrl.behaviorState = AbilityControl.State.Idle;
                    EntityManager.SetComponentData(entity, abilityCtrl);
                }
                break;
            }
        }

    }
}

