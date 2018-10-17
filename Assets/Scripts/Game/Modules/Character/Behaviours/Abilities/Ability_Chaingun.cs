using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

[RequireComponent(typeof(ReplicatedAbility))]
public class Ability_Chaingun : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        Firing,
        Reloading,
    }
    
    public struct LocalState : IComponentData
    {
        public int lastFireTick;
    }
    
    [Serializable]
    public struct Settings : IComponentData
    {
        public CharacterPredictedState.StateData.Action fireAction;
        public int clipSize;
        public float minFireRate;
        public float maxFireRate;
        public float fireRateAcceleration;
        public float reloadDuration;
        public float projectileRange;
        [NonSerialized] public uint projectileRegistryId;
    }


    public struct PredictedState : IPredictedData<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;
        public int ammoInClip;
        public int fireRequested;
        public int reloadRequested;
        public float fireRate;

        public void SetPhase(Phase phase, int tick)
        {
            this.phase = phase;
            this.phaseStartTick = tick;
        }

        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("phase", (int)phase);
            writer.WriteInt32("phaseStart", phaseStartTick);
            writer.WriteInt32("ammoInClip", ammoInClip);
            writer.WriteBoolean("reloadRequested", reloadRequested == 1);
            writer.WriteFloatQ("fireRate", fireRate,2);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            phase = (Phase)reader.ReadInt32();
            phaseStartTick = reader.ReadInt32();
            ammoInClip = reader.ReadInt32();
            reloadRequested = reader.ReadBoolean() ? 1 : 0;
            fireRate = reader.ReadFloatQ();
        }
        
#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return phase == state.phase
                   && phaseStartTick == state.phaseStartTick;
        }
#endif    
    }
    
    public struct InterpolatedState : IInterpolatedData<InterpolatedState>, IComponentData
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
    
    private void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        var entityManager = gameObjectEntity.EntityManager;
        var abilityEntity = gameObjectEntity.Entity;
        
        // Default components
        entityManager.AddComponentData(abilityEntity, new CharacterAbility());
        entityManager.AddComponentData(abilityEntity, new AbilityControl());

        // Ability components
        var predictedState = new PredictedState
        {
            ammoInClip = settings.clipSize,
            fireRate = settings.minFireRate
        };
        settings.projectileRegistryId = projectileType.registryId; 
        
        entityManager.AddComponentData(abilityEntity, settings);
        entityManager.AddComponentData(abilityEntity, new LocalState());
        entityManager.AddComponentData(abilityEntity, predictedState);
        entityManager.AddComponentData(abilityEntity, new InterpolatedState());

        // Setup replicated ability
        var replicatedAbility = entityManager.GetComponentObject<ReplicatedAbility>(abilityEntity);
        replicatedAbility.predictedHandlers = new IPredictedDataHandler[2];
        replicatedAbility.predictedHandlers[0] = new PredictedEntityHandler<AbilityControl>(entityManager, abilityEntity);
        replicatedAbility.predictedHandlers[1]= new PredictedEntityHandler<PredictedState>(entityManager, abilityEntity);
        replicatedAbility.interpolatedHandlers = new IInterpolatedDataHandler[1];
        replicatedAbility.interpolatedHandlers[0] = new InterpolatedEntityHandler<InterpolatedState>(entityManager, abilityEntity);
    }
}

[DisableAutoCreation]
class Chaingun_RequestActive : BaseComponentDataSystem<CharacterAbility,AbilityControl,Ability_Chaingun.PredictedState,Ability_Chaingun.Settings>
{
    public Chaingun_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharacterAbility charAbility, AbilityControl abilityCtrl, 
        Ability_Chaingun.PredictedState predictedState, Ability_Chaingun.Settings settings)
    {
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        
        var isAlive = character.State.locoState != CharacterPredictedState.StateData.LocoState.Dead;
        predictedState.fireRequested = (command.primaryFire && isAlive) ? 1 : 0;
        
        if (command.reload && isAlive && predictedState.ammoInClip < settings.clipSize)
        {
            predictedState.reloadRequested = 1;
        }

        var isIdle = predictedState.phase == Ability_Chaingun.Phase.Idle;
        if (isIdle)
        {
            if (predictedState.fireRequested == 1 && predictedState.ammoInClip == 0)
            {
                predictedState.reloadRequested = 1;
            }
        }
        
        abilityCtrl.requestsActive = !character.State.abilityActive && (!isIdle || predictedState.fireRequested == 1 
                                                                                || predictedState.reloadRequested == 1) ? 1 : 0;
        
        EntityManager.SetComponentData(entity, abilityCtrl);			
        EntityManager.SetComponentData(entity, predictedState);
    }
}


[DisableAutoCreation]
class Chaingun_Update : BaseComponentDataSystem<AbilityControl,Ability_Chaingun.PredictedState,Ability_Chaingun.Settings>
{
    public Chaingun_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, AbilityControl abilityCtrl, Ability_Chaingun.PredictedState predictedState, Ability_Chaingun.Settings settings)
    {
        var time = m_world.worldTime;

        // Handle state change

        if (predictedState.phase == Ability_Chaingun.Phase.Firing)
        {
            predictedState.fireRate += settings.fireRateAcceleration * time.tickDuration;
        }
        else
            predictedState.fireRate -= settings.fireRateAcceleration * time.tickDuration;
        predictedState.fireRate = Mathf.Clamp(predictedState.fireRate, settings.minFireRate, settings.maxFireRate);


        switch (predictedState.phase)
        {
            case Ability_Chaingun.Phase.Idle:
                {
                    if(abilityCtrl.activeAllowed == 0)
                        break;
                    
                    if (predictedState.reloadRequested == 1)
                    {
                        EnterReloadingPhase(abilityEntity, ref predictedState, time.tick);
                        break;
                    }

                    if (predictedState.fireRequested == 1)
                    {
                        EnterFiringPhase(abilityEntity, ref predictedState, ref settings, time.tick);
                        break;
                    }
                    break;
                }
            case Ability_Chaingun.Phase.Firing:
                {
                    var fireDuration = 1.0f / predictedState.fireRate;
                    var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                    if (phaseDuration > fireDuration)
                    {
                        if (predictedState.fireRequested == 1 && predictedState.ammoInClip > 0)
                            EnterFiringPhase(abilityEntity, ref predictedState, ref settings, time.tick);
                        else
                            EnterIdlePhase(abilityEntity, ref predictedState, time.tick);
                        
                        break;
                    }
                    break;
                }
            case Ability_Chaingun.Phase.Reloading:
                {
                    var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                    if (phaseDuration > settings.reloadDuration)
                    {
                        predictedState.reloadRequested = 0;
                        var neededInClip = settings.clipSize - predictedState.ammoInClip;
                        predictedState.ammoInClip += neededInClip;

                        EnterIdlePhase(abilityEntity, ref predictedState, time.tick);
                        break;
                    }
                    break;
                }
        }
        EntityManager.SetComponentData(abilityEntity, predictedState);
    }
    
    void EnterReloadingPhase(Entity abilityEntity, ref Ability_Chaingun.PredictedState state, int tick)
    {
        //GameDebug.Log("Chaingun.EnterReloadingPhase");
        
        state.SetPhase(Ability_Chaingun.Phase.Reloading, tick);
        var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        character.State.SetAction(CharacterPredictedState.StateData.Action.Reloading, tick);
        character.State.abilityActive = true;
    }

    void EnterIdlePhase(Entity abilityEntity, ref Ability_Chaingun.PredictedState state, int tick)
    {
        //GameDebug.Log("Chaingun.EnterIdlePhase");
        
        state.SetPhase(Ability_Chaingun.Phase.Idle, tick);
        var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        character.State.SetAction(CharacterPredictedState.StateData.Action.None, tick);
        character.State.abilityActive = false;
    }

    void EnterFiringPhase(Entity abilityEntity, ref Ability_Chaingun.PredictedState state, ref Ability_Chaingun.Settings settings, int tick)
    {
        //GameDebug.Log("Chaingun.EnterFiringPhase");

        state.SetPhase(Ability_Chaingun.Phase.Firing, tick);
        var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
        var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        var character = EntityManager.GetComponentObject<Character>(charAbility.character);
        charPredictedState.State.SetAction(CharacterPredictedState.StateData.Action.PrimaryFire, tick);
        charPredictedState.State.abilityActive = true;

        state.ammoInClip -= 1;

    
        // Only fire shot once for each tick (so it does not fire again when re-predicting)
        var localState = EntityManager.GetComponentData<Ability_Chaingun.LocalState>(abilityEntity);
        if (tick > localState.lastFireTick)
        {
            localState.lastFireTick = tick;

            var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
            var eyePos = charPredictedState.State.position + Vector3.up*character.eyeHeight; 
            var endPos = eyePos + command.lookDir * settings.projectileRange;

            //GameDebug.Log("Request Projectile. Tick:" + tick);
            ProjectileRequest.Create(PostUpdateCommands, tick, tick - command.renderTick,
                settings.projectileRegistryId, charAbility.character, charPredictedState.teamId, eyePos, endPos);

            // 
            var interpolatedState = EntityManager.GetComponentData<Ability_Chaingun.InterpolatedState>(abilityEntity);
            interpolatedState.fireTick = tick;
            
            EntityManager.SetComponentData(abilityEntity,interpolatedState);
            EntityManager.SetComponentData(abilityEntity,localState);
        }
    }
}
