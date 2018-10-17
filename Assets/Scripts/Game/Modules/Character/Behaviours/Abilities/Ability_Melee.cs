using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(ReplicatedAbility))]
public class Ability_Melee : MonoBehaviour
{
    public enum Phase
    {
        Idle,
        Punch,
        Hold,
    }
    
    public struct LocalState : IComponentData
    {
        public int lastHitCheckTick;
        public int rayQueryId;
    }
    
    [Serializable]
    public struct Settings : IComponentData
    {
        public float damage;
        public float damageDist;
        public float damageRadius;
        public float damageImpulse;
        public float impactTime;
        public int punchPerSecond;

        public CharacterPredictedState.StateData.Action punchAction;
    }

    public struct PredictedState : IPredictedData<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;

        public void SetPhase(Phase phase, int tick)
        {
            this.phase = phase;
            this.phaseStartTick = tick;
        }

        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("phase", (int)phase);
            writer.WriteInt32("startTick", phaseStartTick);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            phase = (Phase)reader.ReadInt32();
            phaseStartTick = reader.ReadInt32();
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
        public int impactTick;
        
        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("impactTick", impactTick);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            impactTick = reader.ReadInt32();
        }

        public void Interpolate(ref InterpolatedState first, ref InterpolatedState last, float t)
        {
            this = first;
        }
    }

    public Settings settings;

    private void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        var entityManager = gameObjectEntity.EntityManager;
        var abilityEntity = gameObjectEntity.Entity;

        // Default components
        entityManager.AddComponentData(abilityEntity, new CharacterAbility());
        entityManager.AddComponentData(abilityEntity, new AbilityControl());

        // Ability components
        var localState = new LocalState
        {
            rayQueryId = -1,
        };
        entityManager.AddComponentData(abilityEntity, localState);
        entityManager.AddComponentData(abilityEntity, new PredictedState());
        entityManager.AddComponentData(abilityEntity, settings);
        entityManager.AddComponentData(abilityEntity, new InterpolatedState());
        
        // Setup replicated ability
        var replicatedAbility = entityManager.GetComponentObject<ReplicatedAbility>(abilityEntity);
        replicatedAbility.predictedHandlers = new IPredictedDataHandler[2];
        replicatedAbility.predictedHandlers[0] = new PredictedEntityHandler<AbilityControl>(entityManager, abilityEntity);
        replicatedAbility.predictedHandlers[1] = new PredictedEntityHandler<PredictedState>(entityManager, abilityEntity);
        
        replicatedAbility.interpolatedHandlers = new IInterpolatedDataHandler[1];
        replicatedAbility.interpolatedHandlers[0] = new InterpolatedEntityHandler<InterpolatedState>(entityManager, abilityEntity);
    }
}


[DisableAutoCreation]
class Melee_RequestActive : BaseComponentDataSystem<CharacterAbility,AbilityControl,Ability_Melee.PredictedState>
{
    public Melee_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharacterAbility charAbility, AbilityControl abilityCtrl, Ability_Melee.PredictedState predictedState)
    {
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
			
        var isAlive = character.State.locoState != CharacterPredictedState.StateData.LocoState.Dead;
        var fireRequested = command.melee && isAlive;

        var isActive = predictedState.phase == Ability_Melee.Phase.Punch;

        abilityCtrl.requestsActive = !character.State.abilityActive && (isActive || fireRequested) ? 1 : 0;
        
        
        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
        EntityManager.SetComponentData(abilityEntity, predictedState);
    }
}

[DisableAutoCreation]
class Melee_Update : BaseComponentDataSystem<CharacterAbility,AbilityControl, Ability_Melee.LocalState,
    Ability_Melee.PredictedState, Ability_Melee.Settings>
{
    public Melee_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharacterAbility charAbility, AbilityControl abilityCtrl, 
        Ability_Melee.LocalState localState, Ability_Melee.PredictedState predictedState, Ability_Melee.Settings settings)
    {
        var time = m_world.worldTime;
        
        switch (predictedState.phase)
        {
            case Ability_Melee.Phase.Idle:
                {
                    if (abilityCtrl.activeAllowed == 1)
                    {
                        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
                        predictedState.SetPhase(Ability_Melee.Phase.Punch, time.tick);
                        character.State.SetAction(settings.punchAction, time.tick);
                        character.State.abilityActive = true;
                    }

                    break;
                }
            case Ability_Melee.Phase.Punch:
                {
                    var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                    if (phaseDuration >= settings.impactTime)
                    {
                        var character = EntityManager.GetComponentObject<Character>(charAbility.character);
                        var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
                        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
                        var viewDir = command.lookDir;
                        var eyePos = charPredictedState.State.position + Vector3.up*character.eyeHeight;

                        predictedState.SetPhase(Ability_Melee.Phase.Hold, time.tick);

                        var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
                        localState.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
                        {
                            origin = eyePos,
                            direction = viewDir,
                            distance = settings.damageDist,
                            sphereCastExcludeOwner = charAbility.character,
                            hitCollisionTestTick = command.renderTick,
                            testAgainsEnvironment = 0,
                            sphereCastRadius = settings.damageRadius,
                            sphereCastMask = ~0,
                        });

                        EntityManager.SetComponentData(abilityEntity,localState);
                        
                        break;
                    }
                    break;
                }
            case Ability_Melee.Phase.Hold:
                {
                    var holdEndDuration = 1.0f / settings.punchPerSecond - settings.impactTime;   
                    var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                    if (phaseDuration > holdEndDuration)
                    {
                        predictedState.phase = Ability_Melee.Phase.Idle;
                        predictedState.phaseStartTick = time.tick;
                        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
                        
                        character.State.SetAction(CharacterPredictedState.StateData.Action.None, time.tick);
                        break;
                    }

                    break;
                }
        }
       
        EntityManager.SetComponentData(abilityEntity, predictedState);
    }
}


[DisableAutoCreation]
class Melee_HandleCollision : BaseComponentDataSystem<Ability_Melee.LocalState>
{
    public Melee_HandleCollision(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, Ability_Melee.LocalState localState)
    {
        if (localState.rayQueryId == -1)
            return;
        
        var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
        
        RaySphereQueryReciever.Query query;
        RaySphereQueryReciever.Result result;
        queryReciever.GetResult(localState.rayQueryId, out query, out result);
        localState.rayQueryId = -1;
        
        if (result.hitCollisionOwner != Entity.Null)
        {
            var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);       
            var settings = EntityManager.GetComponentData<Ability_Melee.Settings>(abilityEntity);

            var hitCollisionOwner =
                EntityManager.GetComponentObject<HitCollisionOwner>(result.hitCollisionOwner);
            hitCollisionOwner.damageEvents.Add(new DamageEvent(charAbility.character, settings.damage, query.direction, 
                settings.damageImpulse));

            var interpolatedState = EntityManager.GetComponentData<Ability_Melee.InterpolatedState>(abilityEntity);
            interpolatedState.impactTick = m_world.worldTime.tick;            
            PostUpdateCommands.SetComponent(abilityEntity, interpolatedState);
        }
        
        EntityManager.SetComponentData(abilityEntity, localState);
    }
}