using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "Ability_RocketJump",menuName = "FPS Sample/Abilities/Ability_RocketJump")]
public class Ability_RocketJump : CharBehaviorFactory
{
    public enum Phase
    {
        Idle,
        Prepare,
        Launch,
        Glide,
    }
    
    public struct LocalState : IComponentData
    {
        public int foo;
    }
    
    [Serializable]
    public struct Settings : IComponentData
    {
        public UserCommand.Button activateButton;
        public float launchDuration;
        public float launchSpeed;
        public float launchStartAngle;
        public float launchEndAngle;
    }

    public struct PredictedState : IPredictedComponent<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;
        public float3 groundDir;

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
            writer.WriteInt32("startTick", phaseStartTick);
            writer.WriteVector3Q("groundDir", groundDir);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            phase = (Phase)reader.ReadInt32();
            phaseStartTick = reader.ReadInt32();
            groundDir = reader.ReadVector3Q();
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
        public Phase phase;
        
        public static IInterpolatedComponentSerializerFactory CreateSerializerFactory()
        {
            return new InterpolatedComponentSerializerFactory<InterpolatedState>();
        }

        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("phase", (int)phase);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            phase = (Phase)reader.ReadInt32();
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
        var localState = new Ability_Melee.LocalState
        {
            rayQueryId = -1,
        };
        entityManager.AddComponentData(entity, localState);
        entityManager.AddComponentData(entity, new PredictedState());
        entityManager.AddComponentData(entity, new InterpolatedState());
        return entity;
    }
}


[DisableAutoCreation]
class RocketJump_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,
    Ability_RocketJump.PredictedState,Ability_RocketJump.Settings>
{
    public RocketJump_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_RocketJump.PredictedState predictedState, Ability_RocketJump.Settings settings)
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
class RocketJump_Update : BaseComponentDataSystem<CharBehaviour,AbilityControl, Ability_RocketJump.PredictedState, 
    Ability_RocketJump.InterpolatedState, Ability_RocketJump.Settings>
{
    public RocketJump_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_RocketJump.PredictedState predictedState, Ability_RocketJump.InterpolatedState interpolatedState, 
        Ability_RocketJump.Settings settings)
    {
        var time = m_world.worldTime;

        if (abilityCtrl.active == 0)
        {
            if(predictedState.phase != Ability_RocketJump.Phase.Idle)
                predictedState.SetPhase(Ability_RocketJump.Phase.Idle, time.tick);
            EntityManager.SetComponentData(abilityEntity, predictedState);
            return;
        }
            
        switch (predictedState.phase)
        {
            case Ability_RocketJump.Phase.Idle:
                {
                    abilityCtrl.behaviorState = AbilityControl.State.Active;
                    predictedState.SetPhase(Ability_RocketJump.Phase.Launch, time.tick);
                    interpolatedState.phase = Ability_RocketJump.Phase.Launch;
                    
                    var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
                    
                    var moveYawRotation = Quaternion.Euler(0, command.lookYaw, 0);
                    var forward = moveYawRotation * Vector3.forward;
                    predictedState.groundDir = forward;
                    
                    
                    
                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
                    charPredictedState.locoState = CharacterPredictedData.LocoState.InAir;
                    charPredictedState.locoStartTick = time.tick;
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);
                    
                    EntityManager.SetComponentData(abilityEntity, abilityCtrl);
                    EntityManager.SetComponentData(abilityEntity, predictedState);
                    EntityManager.SetComponentData(abilityEntity, interpolatedState);

                    break;
                }
            case Ability_RocketJump.Phase.Launch:
                {
                    var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                    if (phaseDuration >= settings.launchDuration)
                    {
                        predictedState.SetPhase(Ability_RocketJump.Phase.Idle, time.tick);
                        interpolatedState.phase = Ability_RocketJump.Phase.Idle;
                        abilityCtrl.behaviorState = AbilityControl.State.Idle;

                        
                        EntityManager.SetComponentData(abilityEntity,interpolatedState);
                        EntityManager.SetComponentData(abilityEntity, predictedState);
                        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
                        
                        break;
                    }

                    var deltaAngle = settings.launchEndAngle - settings.launchStartAngle;

                    var fraction = phaseDuration/settings.launchDuration;
                    var angle = settings.launchStartAngle + deltaAngle*fraction;
                    
                    var left = math.cross(predictedState.groundDir, new float3(0, 1, 0));

                    var rot = Quaternion.AngleAxis(angle, left);
                    var dir = rot * predictedState.groundDir;

                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
                    
                    var velocity = dir * settings.launchSpeed;
                    charPredictedState.velocity = velocity;
                    charPredictedState.position = charPredictedState.position + time.tickDuration*velocity;
        
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);
                    
                    break;
                }
        }
    }
}

