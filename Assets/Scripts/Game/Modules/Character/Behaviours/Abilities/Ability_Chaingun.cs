using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "Ability_Chaingun",menuName = "FPS Sample/Abilities/Ability_Chaingun")]
public class Ability_Chaingun : CharBehaviorFactory
{
    public enum State
    {
        Idle,
        Fire,
        Reload,
    }
    
    public struct LocalState : IComponentData
    {
        public int lastFireTick;
    }
    
    [Serializable]
    public struct Settings : IComponentData
    {
        public UserCommand.Button fireButton;
        public UserCommand.Button reloadButton;

        public int clipSize;
        public float minFireRate;
        public float maxFireRate;
        public float fireRateAcceleration;
        public float reloadDuration;
        public float projectileRange;
        public WeakAssetReference projectileAssetGuid;
    }

    public struct PredictedState : IPredictedComponent<PredictedState>, IComponentData
    {
        public State state;
        public int actionStartTick;
        public int ammoInClip;
        public float fireRate;

        public void SetPhase(State action, int tick)
        {
            this.state = action;
            this.actionStartTick = tick;
        }
        
        public static IPredictedComponentSerializerFactory CreateSerializerFactory()
        {
            return new PredictedComponentSerializerFactory<PredictedState>();
        }

        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("phase", (int)state);
            writer.WriteInt32("phaseStart", actionStartTick);
            writer.WriteInt32("ammoInClip", ammoInClip);
            writer.WriteFloatQ("fireRate", fireRate,2);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            state = (State)reader.ReadInt32();
            actionStartTick = reader.ReadInt32();
            ammoInClip = reader.ReadInt32();
            fireRate = reader.ReadFloatQ();
        }
        
#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return this.state == state.state
                   && actionStartTick == state.actionStartTick;
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
        var predictedState = new PredictedState
        {
            ammoInClip = settings.clipSize,
            fireRate = settings.minFireRate
        };

        entityManager.AddComponentData(entity, settings);
        entityManager.AddComponentData(entity, new LocalState());
        entityManager.AddComponentData(entity, predictedState);
        entityManager.AddComponentData(entity, new InterpolatedState());
        return entity;
    }
    
    static public State GetPreferredState(ref PredictedState predictedState, ref Settings settings, 
        ref UserCommand command)
    {
        if (command.buttons.IsSet(settings.reloadButton) && predictedState.ammoInClip < settings.clipSize)
        {
            return State.Reload;
        }
		
        var isIdle = predictedState.state == State.Idle;
        if (isIdle)
        {
            if (command.buttons.IsSet(settings.fireButton) && predictedState.ammoInClip == 0)
            {
                return State.Reload;
            }
        }

        return command.buttons.IsSet(settings.fireButton) ? State.Fire : State.Idle;
    }
}

[DisableAutoCreation]
class Chaingun_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,Ability_Chaingun.PredictedState,Ability_Chaingun.Settings>
{
    public Chaingun_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_Chaingun.PredictedState predictedState, Ability_Chaingun.Settings settings)
    {
        if (abilityCtrl.behaviorState == AbilityControl.State.Active || abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
            return;
        
        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
        var request = Ability_Chaingun.GetPreferredState(ref predictedState, ref settings, ref command);
        abilityCtrl.behaviorState = request != Ability_Chaingun.State.Idle ?  AbilityControl.State.RequestActive : AbilityControl.State.Idle;
        EntityManager.SetComponentData(entity, abilityCtrl);			
    }
}


[DisableAutoCreation]
class Chaingun_Update : BaseComponentDataSystem<CharBehaviour, AbilityControl,Ability_Chaingun.PredictedState,Ability_Chaingun.Settings>
{
    public Chaingun_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, Ability_Chaingun.PredictedState predictedState, Ability_Chaingun.Settings settings)
    {
        var time = m_world.worldTime;

        // Adjust fire rate
        if (predictedState.state == Ability_Chaingun.State.Fire)
        {
            predictedState.fireRate += settings.fireRateAcceleration * time.tickDuration;
        }
        else
            predictedState.fireRate -= settings.fireRateAcceleration * time.tickDuration;
        predictedState.fireRate = Mathf.Clamp(predictedState.fireRate, settings.minFireRate, settings.maxFireRate);
        EntityManager.SetComponentData(abilityEntity, predictedState);

        if (abilityCtrl.active == 0)
        {
            return;
        }
            
        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
        var request = Ability_Chaingun.GetPreferredState(ref predictedState, ref settings, ref command);

        switch (predictedState.state)
        {
            case Ability_Chaingun.State.Idle:
                {
                    if (request == Ability_Chaingun.State.Reload)
                    {
                        EnterReloadingPhase(abilityEntity, ref abilityCtrl, ref predictedState, time.tick);
                        break;
                    }

                    if (request == Ability_Chaingun.State.Fire)
                    {
                        EnterFiringPhase(abilityEntity, ref abilityCtrl, ref predictedState, ref settings, time.tick);
                        break;
                    }
                    break;
                }
            case Ability_Chaingun.State.Fire:
                {
                    var fireDuration = 1.0f / predictedState.fireRate;
                    var phaseDuration = time.DurationSinceTick(predictedState.actionStartTick);
                    if (phaseDuration > fireDuration)
                    {
                        if (request == Ability_Chaingun.State.Fire && predictedState.ammoInClip > 0)
                            EnterFiringPhase(abilityEntity, ref abilityCtrl, ref predictedState, ref settings, time.tick);
                        else
                            EnterIdlePhase(abilityEntity, ref abilityCtrl, ref predictedState, time.tick);
                        
                        break;
                    }
                    break;
                }
            case Ability_Chaingun.State.Reload:
                {
                    var phaseDuration = time.DurationSinceTick(predictedState.actionStartTick);
                    if (phaseDuration > settings.reloadDuration)
                    {
                        var neededInClip = settings.clipSize - predictedState.ammoInClip;
                        predictedState.ammoInClip += neededInClip;

                        EnterIdlePhase(abilityEntity, ref abilityCtrl, ref predictedState, time.tick);
                        break;
                    }
                    break;
                }
        }
    }
    
    void EnterReloadingPhase(Entity abilityEntity, ref AbilityControl abilityCtrl, 
        ref Ability_Chaingun.PredictedState predictedState, int tick)
    {
//        GameDebug.Log("Chaingun.EnterReloadingPhase");

        var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
        var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

        abilityCtrl.behaviorState = AbilityControl.State.Active;
        predictedState.SetPhase(Ability_Chaingun.State.Reload, tick);
        charPredictedState.SetAction(CharacterPredictedData.Action.Reloading, tick);
        
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        EntityManager.SetComponentData(abilityEntity, predictedState);
        EntityManager.SetComponentData(charAbility.character, charPredictedState);
    }

    void EnterIdlePhase(Entity abilityEntity, ref AbilityControl abilityCtrl, 
        ref Ability_Chaingun.PredictedState predictedState, int tick)
    {
//        GameDebug.Log("Chaingun.EnterIdlePhase");
        
        var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
        var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
        
        abilityCtrl.behaviorState = AbilityControl.State.Idle;
        predictedState.SetPhase(Ability_Chaingun.State.Idle, tick);
        charPredictedState.SetAction(CharacterPredictedData.Action.None, tick);
        
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        EntityManager.SetComponentData(abilityEntity, predictedState);
        EntityManager.SetComponentData(charAbility.character, charPredictedState);
    }

    void EnterFiringPhase(Entity abilityEntity, ref AbilityControl abilityCtrl
        , ref Ability_Chaingun.PredictedState predictedState, ref Ability_Chaingun.Settings settings, int tick)
    {
//        GameDebug.Log("Chaingun.EnterFiringPhase");

        var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
        var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
        var character = EntityManager.GetComponentObject<Character>(charAbility.character);

        abilityCtrl.behaviorState = AbilityControl.State.Active;
        predictedState.SetPhase(Ability_Chaingun.State.Fire, tick);
        predictedState.ammoInClip -= 1;
        charPredictedState.SetAction(CharacterPredictedData.Action.PrimaryFire, tick);

        // Only fire shot once for each tick (so it does not fire again when re-predicting)
        var localState = EntityManager.GetComponentData<Ability_Chaingun.LocalState>(abilityEntity);
        if (tick > localState.lastFireTick)
        {
            localState.lastFireTick = tick;

            var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
            var eyePos = charPredictedState.position + Vector3.up*character.eyeHeight; 
            var endPos = eyePos + command.lookDir * settings.projectileRange;

            //GameDebug.Log("Request Projectile. Tick:" + tick);
            ProjectileRequest.Create(PostUpdateCommands, tick, tick - command.renderTick,
                settings.projectileAssetGuid, charAbility.character, character.teamId, eyePos, endPos);

            // 
            var interpolatedState = EntityManager.GetComponentData<Ability_Chaingun.InterpolatedState>(abilityEntity);
            interpolatedState.fireTick = tick;
            
            EntityManager.SetComponentData(abilityEntity,interpolatedState);
            EntityManager.SetComponentData(abilityEntity,localState);
        }
        
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        EntityManager.SetComponentData(abilityEntity, predictedState);
        EntityManager.SetComponentData(charAbility.character, charPredictedState);
    }
}
