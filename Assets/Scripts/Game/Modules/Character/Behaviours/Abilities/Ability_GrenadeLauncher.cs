using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability_GrenadeLauncher",menuName = "FPS Sample/Abilities/Ability_GrenadeLauncher")]
public class Ability_GrenadeLauncher : CharBehaviorFactory
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
        public float grenadeVelocity;
        public float grenadePitchAngle;
        public WeakAssetReference assetGuid;
    }


    public struct PredictedState : IPredictedComponent<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;
        public int fireRequestedTick;    

        public void SetPhase(Phase phase, int tick)
        {
            this.phase = phase;
            this.phaseStartTick = tick;
        }
        
        public static IPredictedComponentSerializerFactory CreateSerializerFactory()
        {
            return new PredictedComponentSerializerFactory<PredictedState>();
        }

        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("phase", (int)phase);
            writer.WriteInt32("phaseStart", phaseStartTick);
            writer.WriteInt32("fireRequestedTick", fireRequestedTick);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            phase = (Phase)reader.ReadInt32();
            phaseStartTick = reader.ReadInt32();
            fireRequestedTick = reader.ReadInt32();
        }
#if UNITY_EDITOR        
        public bool VerifyPrediction(ref PredictedState state)
        {
            return phase == state.phase
                   && phaseStartTick == state.phaseStartTick;
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
		
        entityManager.AddComponentData(entity, settings);
        entityManager.AddComponentData(entity, new LocalState());
        entityManager.AddComponentData(entity, new PredictedState());
        entityManager.AddComponentData(entity, new InterpolatedState());
        return entity;
    }

}


[DisableAutoCreation]
class GrenadeLauncher_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,
    Ability_GrenadeLauncher.PredictedState,Ability_GrenadeLauncher.Settings>
{
    public GrenadeLauncher_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_GrenadeLauncher.PredictedState predictedState, Ability_GrenadeLauncher.Settings settings)
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
class GrenadeLauncher_Update : BaseComponentDataSystem<AbilityControl,Ability_GrenadeLauncher.PredictedState,Ability_GrenadeLauncher.Settings>
{
    public GrenadeLauncher_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity entity, AbilityControl abilityCtrl, Ability_GrenadeLauncher.PredictedState predictedState, Ability_GrenadeLauncher.Settings state)
    {
        var time = m_world.worldTime;
        
        switch (predictedState.phase)
        {
            case Ability_GrenadeLauncher.Phase.Idle:
                if (abilityCtrl.active == 1)
                {
                    var charAbility = EntityManager.GetComponentData<CharBehaviour>(entity);
                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
                    var character = EntityManager.GetComponentObject<Character>(charAbility.character);
                    
                    abilityCtrl.behaviorState = AbilityControl.State.Active;
                    
                    predictedState.SetPhase(Ability_GrenadeLauncher.Phase.Active, time.tick);

                    charPredictedState.SetAction(state.fireAction, time.tick);

                    // Only spawn once for each tick (so it does not fire again when re-predicting)
                    var localState = EntityManager.GetComponentData<Ability_GrenadeLauncher.LocalState>(entity);
                    if (time.tick > localState.lastFireTick)
                    {
                        localState.lastFireTick = time.tick;
                        EntityManager.SetComponentData(entity, localState);
                        
                        var eyePos = charPredictedState.position + Vector3.up*character.eyeHeight;
                        
                        var interpolatedState = EntityManager.GetComponentData<Ability_GrenadeLauncher.InterpolatedState>(entity);
                        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character)
                            .command;
                        
                        var startDir = command.lookDir;
                        var right = math.cross(new float3(0, 1, 0),startDir);
                        var pitchRot = quaternion.AxisAngle(right,
                            -math.radians(state.grenadePitchAngle));
                        startDir = math.mul(pitchRot, startDir);
                            
                        var velocity = startDir*state.grenadeVelocity;

                        GrenadeSpawnRequest.Create(PostUpdateCommands, state.assetGuid, eyePos,
                            velocity, charAbility.character, character.teamId);

                        interpolatedState.fireTick = time.tick;
                        EntityManager.SetComponentData(entity, interpolatedState);
                    }
                    
                    EntityManager.SetComponentData(entity, abilityCtrl);
                    EntityManager.SetComponentData(entity, predictedState);
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);
                }
                break;
            case Ability_GrenadeLauncher.Phase.Active:
            {
                var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                if (phaseDuration > state.activationDuration)
                {
                    var charAbility = EntityManager.GetComponentData<CharBehaviour>(entity);
                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
                    
                    abilityCtrl.behaviorState = AbilityControl.State.Cooldown;
                    
                    predictedState.SetPhase(Ability_GrenadeLauncher.Phase.Cooldown, time.tick);
                    
                    charPredictedState.SetAction(CharacterPredictedData.Action.None, time.tick);
                    
                    EntityManager.SetComponentData(entity, abilityCtrl);
                    EntityManager.SetComponentData(entity, predictedState);
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);
                }
                break;
            }
            case Ability_GrenadeLauncher.Phase.Cooldown:
            {
                var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                if (phaseDuration > state.cooldownDuration)
                {
                    abilityCtrl.behaviorState = AbilityControl.State.Idle;

                    predictedState.SetPhase(Ability_GrenadeLauncher.Phase.Idle, time.tick);
                    
                    EntityManager.SetComponentData(entity, abilityCtrl);
                    EntityManager.SetComponentData(entity, predictedState);
                }
                break;
            }
        }

    }
}