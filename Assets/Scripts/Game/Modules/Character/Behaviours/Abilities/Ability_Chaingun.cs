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
    public enum Action
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
        public CharPredictedStateData.Action fireAction;
        public int clipSize;
        public float minFireRate;
        public float maxFireRate;
        public float fireRateAcceleration;
        public float reloadDuration;
        public float projectileRange;
        [NonSerialized] public int projectileRegistryId;
    }

    public struct PredictedState : INetPredicted<PredictedState>, IComponentData
    {
        public Action action;
        public int actionStartTick;
        public int ammoInClip;
        public float fireRate;

        public void SetPhase(Action action, int tick)
        {
            this.action = action;
            this.actionStartTick = tick;
        }

        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("phase", (int)action);
            writer.WriteInt32("phaseStart", actionStartTick);
            writer.WriteInt32("ammoInClip", ammoInClip);
            writer.WriteFloatQ("fireRate", fireRate,2);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            action = (Action)reader.ReadInt32();
            actionStartTick = reader.ReadInt32();
            ammoInClip = reader.ReadInt32();
            fireRate = reader.ReadFloatQ();
        }
        
#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return action == state.action
                   && actionStartTick == state.actionStartTick;
        }
#endif    
    }
    
    public struct InterpolatedState : INetInterpolated<InterpolatedState>, IComponentData
    {
        public int fireTick;
        
        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("fireTick", fireTick);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            fireTick = reader.ReadInt32();
        }

        public void Interpolate(ref InterpolatedState first, ref InterpolatedState last, float t)
        {
            this = first;
        }
    }
    
    public Settings settings;
    public ProjectileTypeDefinition projectileType;

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
        settings.projectileRegistryId = projectileType.registryId;

        entityManager.AddComponentData(entity, settings);
        entityManager.AddComponentData(entity, new LocalState());
        entityManager.AddComponentData(entity, predictedState);
        entityManager.AddComponentData(entity, new InterpolatedState());
        return entity;
    }
    
    static public Action GetCommandRequest(ref PredictedState predictedState, ref Settings settings, 
        ref UserCommand command)
    {
        if (command.reload && predictedState.ammoInClip < settings.clipSize)
        {
            return Action.Reload;
        }
		
        var isIdle = predictedState.action == Action.Idle;
        if (isIdle)
        {
            if (command.primaryFire && predictedState.ammoInClip == 0)
            {
                return Action.Reload;
            }
        }

        return command.primaryFire ? Action.Fire : Action.Idle;
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
        
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        var request = Ability_Chaingun.GetCommandRequest(ref predictedState, ref settings, ref command);
        abilityCtrl.behaviorState = request != Ability_Chaingun.Action.Idle ?  AbilityControl.State.RequestActive : AbilityControl.State.Idle;
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
        if (predictedState.action == Ability_Chaingun.Action.Fire)
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
            
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        var request = Ability_Chaingun.GetCommandRequest(ref predictedState, ref settings, ref command);

        switch (predictedState.action)
        {
            case Ability_Chaingun.Action.Idle:
                {
                    if (request == Ability_Chaingun.Action.Reload)
                    {
                        EnterReloadingPhase(abilityEntity, ref abilityCtrl, ref predictedState, time.tick);
                        break;
                    }

                    if (request == Ability_Chaingun.Action.Fire)
                    {
                        EnterFiringPhase(abilityEntity, ref abilityCtrl, ref predictedState, ref settings, time.tick);
                        break;
                    }
                    break;
                }
            case Ability_Chaingun.Action.Fire:
                {
                    var fireDuration = 1.0f / predictedState.fireRate;
                    var phaseDuration = time.DurationSinceTick(predictedState.actionStartTick);
                    if (phaseDuration > fireDuration)
                    {
                        if (request == Ability_Chaingun.Action.Fire && predictedState.ammoInClip > 0)
                            EnterFiringPhase(abilityEntity, ref abilityCtrl, ref predictedState, ref settings, time.tick);
                        else
                            EnterIdlePhase(abilityEntity, ref abilityCtrl, ref predictedState, time.tick);
                        
                        break;
                    }
                    break;
                }
            case Ability_Chaingun.Action.Reload:
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
        var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(charAbility.character);

        abilityCtrl.behaviorState = AbilityControl.State.Active;
        predictedState.SetPhase(Ability_Chaingun.Action.Reload, tick);
        charPredictedState.SetAction(CharPredictedStateData.Action.Reloading, tick);
        
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        EntityManager.SetComponentData(abilityEntity, predictedState);
        EntityManager.SetComponentData(charAbility.character, charPredictedState);
    }

    void EnterIdlePhase(Entity abilityEntity, ref AbilityControl abilityCtrl, 
        ref Ability_Chaingun.PredictedState predictedState, int tick)
    {
//        GameDebug.Log("Chaingun.EnterIdlePhase");
        
        var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
        var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(charAbility.character);
        
        abilityCtrl.behaviorState = AbilityControl.State.Idle;
        predictedState.SetPhase(Ability_Chaingun.Action.Idle, tick);
        charPredictedState.SetAction(CharPredictedStateData.Action.None, tick);
        
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        EntityManager.SetComponentData(abilityEntity, predictedState);
        EntityManager.SetComponentData(charAbility.character, charPredictedState);
    }

    void EnterFiringPhase(Entity abilityEntity, ref AbilityControl abilityCtrl
        , ref Ability_Chaingun.PredictedState predictedState, ref Ability_Chaingun.Settings settings, int tick)
    {
//        GameDebug.Log("Chaingun.EnterFiringPhase");

        var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
        var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(charAbility.character);
        var character = EntityManager.GetComponentObject<Character>(charAbility.character);

        abilityCtrl.behaviorState = AbilityControl.State.Active;
        predictedState.SetPhase(Ability_Chaingun.Action.Fire, tick);
        predictedState.ammoInClip -= 1;
        charPredictedState.SetAction(CharPredictedStateData.Action.PrimaryFire, tick);

        // Only fire shot once for each tick (so it does not fire again when re-predicting)
        var localState = EntityManager.GetComponentData<Ability_Chaingun.LocalState>(abilityEntity);
        if (tick > localState.lastFireTick)
        {
            localState.lastFireTick = tick;

            var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
            var eyePos = charPredictedState.position + Vector3.up*character.eyeHeight; 
            var endPos = eyePos + command.lookDir * settings.projectileRange;

            //GameDebug.Log("Request Projectile. Tick:" + tick);
            ProjectileRequest.Create(PostUpdateCommands, tick, tick - command.renderTick,
                settings.projectileRegistryId, charAbility.character, character.teamId, eyePos, endPos);

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
