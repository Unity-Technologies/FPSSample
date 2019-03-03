using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability_Melee",menuName = "FPS Sample/Abilities/Ability_Melee")]
public class Ability_Melee : CharBehaviorFactory
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
        public UserCommand.Button activateButton;
        
        public float damage;
        public float damageDist;
        public float damageRadius;
        public float damageImpulse;
        public float impactTime;
        public int punchPerSecond;

        public CharacterPredictedData.Action punchAction;
    }

    public struct PredictedState : IPredictedComponent<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;

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
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
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
    
    public struct InterpolatedState : IInterpolatedComponent<InterpolatedState>, IComponentData
    {
        public int impactTick;
        
        public static IInterpolatedComponentSerializerFactory CreateSerializerFactory()
        {
            return new InterpolatedComponentSerializerFactory<InterpolatedState>();
        }

        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("impactTick", impactTick);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            impactTick = reader.ReadInt32();
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
class Melee_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,
    Ability_Melee.PredictedState,Ability_Melee.Settings>
{
    public Melee_RequestActive(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }

    protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_Melee.PredictedState predictedState, Ability_Melee.Settings settings)
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
class Melee_Update : BaseComponentDataSystem<CharBehaviour,AbilityControl, Ability_Melee.LocalState,
    Ability_Melee.PredictedState, Ability_Melee.Settings>
{
    public Melee_Update(GameWorld world) : base(world)
    {
        ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
    }
    
    protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
        Ability_Melee.LocalState localState, Ability_Melee.PredictedState predictedState, Ability_Melee.Settings settings)
    {
        var time = m_world.worldTime;

        if (abilityCtrl.active == 0)
        {
            if(predictedState.phase != Ability_Melee.Phase.Idle)
                predictedState.SetPhase(Ability_Melee.Phase.Idle, time.tick);
            EntityManager.SetComponentData(abilityEntity, predictedState);
            return;
        }
            
        switch (predictedState.phase)
        {
            case Ability_Melee.Phase.Idle:
                {
                    var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

                    abilityCtrl.behaviorState = AbilityControl.State.Active;
                    predictedState.SetPhase(Ability_Melee.Phase.Punch, time.tick);
                    charPredictedState.SetAction(settings.punchAction, time.tick);
                    
                    EntityManager.SetComponentData(abilityEntity, abilityCtrl);
                    EntityManager.SetComponentData(abilityEntity, predictedState);
                    EntityManager.SetComponentData(charAbility.character, charPredictedState);

                    break;
                }
            case Ability_Melee.Phase.Punch:
                {
                    var phaseDuration = time.DurationSinceTick(predictedState.phaseStartTick);
                    if (phaseDuration >= settings.impactTime)
                    {
                        var character = EntityManager.GetComponentObject<Character>(charAbility.character);
                        var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
                        var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
                        var viewDir = command.lookDir;
                        var eyePos = charPredictedState.position + Vector3.up*character.eyeHeight;

                        predictedState.SetPhase(Ability_Melee.Phase.Hold, time.tick);

                        var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
                        localState.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
                        {
                            origin = eyePos,
                            direction = viewDir,
                            distance = settings.damageDist,
                            ExcludeOwner = charAbility.character,
                            hitCollisionTestTick = command.renderTick,
                            radius = settings.damageRadius,
                            mask = ~0U,
                        });

                        EntityManager.SetComponentData(abilityEntity,localState);
                        EntityManager.SetComponentData(abilityEntity, predictedState);
                        
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
                        var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

                        abilityCtrl.behaviorState = AbilityControl.State.Idle;
                        predictedState.SetPhase(Ability_Melee.Phase.Idle, time.tick);
                        charPredictedState.SetAction(CharacterPredictedData.Action.None, time.tick);

                        EntityManager.SetComponentData(abilityEntity, abilityCtrl);
                        EntityManager.SetComponentData(abilityEntity, predictedState);
                        EntityManager.SetComponentData(charAbility.character, charPredictedState);

                        break;
                    }

                    break;
                }
        }
       
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
        RaySphereQueryReciever.QueryResult queryResult;
        queryReciever.GetResult(localState.rayQueryId, out query, out queryResult);
        localState.rayQueryId = -1;
        
        if (queryResult.hitCollisionOwner != Entity.Null)
        {
            var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);       
            var settings = EntityManager.GetComponentData<Ability_Melee.Settings>(abilityEntity);

            var damageEventBuffer = EntityManager.GetBuffer<DamageEvent>(queryResult.hitCollisionOwner);
            DamageEvent.AddEvent(damageEventBuffer, charAbility.character, settings.damage, query.direction, settings.damageImpulse);

            var interpolatedState = EntityManager.GetComponentData<Ability_Melee.InterpolatedState>(abilityEntity);
            interpolatedState.impactTick = m_world.worldTime.tick;            
            PostUpdateCommands.SetComponent(abilityEntity, interpolatedState);
        }
        
        EntityManager.SetComponentData(abilityEntity, localState);
    }
}