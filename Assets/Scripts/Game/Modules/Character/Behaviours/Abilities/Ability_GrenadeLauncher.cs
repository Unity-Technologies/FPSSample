using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(ReplicatedAbility))]
public class Ability_GrenadeLauncher : MonoBehaviour
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
        public float activationDuration;        
        public float cooldownDuration;
        public CharacterPredictedState.StateData.Action fireAction;
        public float grenadeVelocity;
        public float grenadePitchAngle;
        [NonSerialized] public unsafe fixed byte grenadePrefabGUID[16];
        public void SetGrenadePrefabGuid(byte[] guid)
        {
            unsafe
            {
                fixed(byte* p = grenadePrefabGUID) {
                    for (var i = 0; i < guid.Length; i++)
                    {
                        p[i] = guid[i];
                    }
                }
            }
        }
    }


    public struct PredictedState : IPredictedData<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;
        public int fireRequestedTick;    

        public void SetPhase(Phase phase, int tick)
        {
            this.phase = phase;
            this.phaseStartTick = tick;
        }

        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerialize)
        {
            writer.WriteInt32("phase", (int)phase);
            writer.WriteInt32("phaseStart", phaseStartTick);
            writer.WriteInt32("fireRequestedTick", fireRequestedTick);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
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
    public WeakAssetReference grenadePrefab;

    private void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        var entityManager = gameObjectEntity.EntityManager;
        var abilityEntity = gameObjectEntity.Entity;

        // Default components
        entityManager.AddComponentData(abilityEntity, new CharacterAbility());
        entityManager.AddComponentData(abilityEntity, new AbilityControl());

        // Ability components
        
        var guid = Guid.Parse(grenadePrefab.guid);
        var byteArray = guid.ToByteArray();
        GameDebug.Assert(byteArray.Length == 16,"Guid byte array length:{0}. SHould be 16", byteArray.Length);
        settings.SetGrenadePrefabGuid(byteArray);
        entityManager.AddComponentData(abilityEntity, settings);
        entityManager.AddComponentData(abilityEntity, new LocalState());
        entityManager.AddComponentData(abilityEntity, new PredictedState());
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
class GrenadeLauncher_RequestActive : BaseComponentDataSystem<CharacterAbility,AbilityControl, Ability_GrenadeLauncher.PredictedState>
{
    public GrenadeLauncher_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharacterAbility charAbility, AbilityControl abilityCtrl, Ability_GrenadeLauncher.PredictedState predictedState)
    {
        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
        var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
        
        var isAlive = character.State.locoState != CharacterPredictedState.StateData.LocoState.Dead;
        var fireRequested = command.secondaryFire && isAlive;

        var isActive = predictedState.phase == Ability_GrenadeLauncher.Phase.Active;

        abilityCtrl.requestsActive = !character.State.abilityActive && (isActive || fireRequested) ? 1 : 0;
        
        EntityManager.SetComponentData(entity, abilityCtrl);			
        EntityManager.SetComponentData(entity, predictedState);
    }
}


[DisableAutoCreation]
class GrenadeLauncher_Update : BaseComponentDataSystem<AbilityControl,Ability_GrenadeLauncher.PredictedState,Ability_GrenadeLauncher.Settings>
{
    public GrenadeLauncher_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity entity, AbilityControl abilityCtrl, Ability_GrenadeLauncher.PredictedState predictedState, Ability_GrenadeLauncher.Settings settings)
    {
        var time = m_world.worldTime;
        
        switch (predictedState.phase)
        {
            case Ability_GrenadeLauncher.Phase.Idle:
                if (abilityCtrl.activeAllowed == 1)
                {
                    var charAbility = EntityManager.GetComponentData<CharacterAbility>(entity);
                    var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
                    var character = EntityManager.GetComponentObject<Character>(charAbility.character);
                    predictedState.SetPhase(Ability_GrenadeLauncher.Phase.Active, time.tick);

                    charPredictedState.State.SetAction(settings.fireAction, time.tick);

                    // Only spawn once for each tick (so it does not fire again when re-predicting)
                    var localState = EntityManager.GetComponentData<Ability_GrenadeLauncher.LocalState>(entity);
                    if (time.tick > localState.lastFireTick)
                    {
                        localState.lastFireTick = time.tick;
                        EntityManager.SetComponentData(entity, localState);
                        
                        var eyePos = charPredictedState.State.position + Vector3.up*character.eyeHeight;
                        
                        var interpolatedState = EntityManager.GetComponentData<Ability_GrenadeLauncher.InterpolatedState>(entity);
                        var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character)
                            .command;
                        
                        var startDir = command.lookDir;
                        var right = math.cross(new float3(0, 1, 0),startDir);
                        var pitchRot = quaternion.axisAngle(right,
                            -math.radians(settings.grenadePitchAngle));
                        startDir = math.mul(pitchRot, startDir);
                            
                        var velocity = startDir*settings.grenadeVelocity;

                        unsafe
                        {
                            GrenadeSpawnRequest.Create(PostUpdateCommands, settings.grenadePrefabGUID, eyePos,
                                velocity, charAbility.character, charPredictedState.teamId);
                        }

                        interpolatedState.fireTick = time.tick;
                        EntityManager.SetComponentData(entity, interpolatedState);
                    }
                }
                break;
            case Ability_GrenadeLauncher.Phase.Active:
            {
                var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                if (phaseDuration > settings.activationDuration)
                {
                    var charAbility = EntityManager.GetComponentData<CharacterAbility>(entity);
                    var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
                    
                    predictedState.SetPhase(Ability_GrenadeLauncher.Phase.Cooldown, time.tick);
                    character.State.SetAction(CharacterPredictedState.StateData.Action.None, time.tick);
                }
                break;
            }
            case Ability_GrenadeLauncher.Phase.Cooldown:
            {
                var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                if (phaseDuration > settings.cooldownDuration)
                {
                    predictedState.SetPhase(Ability_GrenadeLauncher.Phase.Idle, time.tick);
                }
                break;
            }
        }

        EntityManager.SetComponentData(entity, predictedState);
    }
}