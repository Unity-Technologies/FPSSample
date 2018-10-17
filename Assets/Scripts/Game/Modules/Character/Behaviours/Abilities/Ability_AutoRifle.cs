using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;


[RequireComponent(typeof(ReplicatedAbility))]
public class Ability_AutoRifle : MonoBehaviour
{
	public enum Phase
	{
		Idle,
		Firing,
		Reloading,
	}
	
	public enum ImpactType
	{
		None,
		Environment,
		Character
	}	

    [Serializable]
    public struct Settings : IComponentData
    {
        public float damage;
        public float damageImpulse;
        public float roundsPerSecond;            
        public int clipSize;
        public float reloadDuration;
        public float hitscanRadius;
	    
	    public float minCOF;
	    public float maxCOF;
	    public float shotCOFIncrease;
	    public float COFDecreaseVel;
    }

	public struct LocalState : IComponentData
	{
		public int lastHitCheckTick;
		public int rayQueryId;
	}
	
    public struct PredictedState : IPredictedData<PredictedState>, IComponentData
    {
        public Phase phase;
        public int phaseStartTick;

	    public int reloadRequested;
	    public int fireRequested;
        public int ammoInClip;
	    public float COF;
        
	    public void SetPhase(Phase phase, int tick)
	    {
		    this.phase = phase;
		    this.phaseStartTick = tick;
	    }
	    
        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("phase", (int)phase);
            writer.WriteInt32("phaseStart", phaseStartTick);
	        writer.WriteBoolean("fireRequested", fireRequested == 1);
	        writer.WriteBoolean("reloadRequested", reloadRequested == 1);
            writer.WriteInt32("ammoInClip", ammoInClip);
	        writer.WriteFloatQ("COF", COF,0);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            phase = (Phase)reader.ReadInt32();
            phaseStartTick = reader.ReadInt32();
	        fireRequested = reader.ReadBoolean() ? 1 : 0;
	        reloadRequested = reader.ReadBoolean() ? 1 : 0;
            ammoInClip = reader.ReadInt32();
	        COF = reader.ReadFloatQ();
        }
        
#if UNITY_EDITOR
        public bool VerifyPrediction(ref PredictedState state)
        {
            return true;
        }
#endif        
    }
    
        
    public struct InterpolatedState : IInterpolatedData<InterpolatedState>, IComponentData
    {
        public int fireTick;
        public float3 fireEndPos;
        public ImpactType impactType;
        public float3 impactNormal;
        
        public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
        {
            writer.WriteInt32("primFireTick", fireTick);
            writer.WriteVector3Q("fireEndPos", fireEndPos,2);
            writer.WriteInt32("impact", (int)impactType);
            writer.WriteVector3Q("impactNormal", impactNormal,1);
        }

        public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
        {
            fireTick = reader.ReadInt32();
            fireEndPos = reader.ReadVector3Q();
            impactType = (ImpactType)reader.ReadInt32();
            impactNormal = reader.ReadVector3Q();
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
            rayQueryId = -1
        };
        var predictedState = new PredictedState
        {
            phase = Phase.Idle,
            ammoInClip = settings.clipSize,
            COF = settings.minCOF,
        };
	    entityManager.AddComponentData(abilityEntity, settings);
	    entityManager.AddComponentData(abilityEntity, localState);
	    entityManager.AddComponentData(abilityEntity, predictedState);
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
class AutoRifle_RequestActive : BaseComponentDataSystem<CharacterAbility,AbilityControl,Ability_AutoRifle.PredictedState,Ability_AutoRifle.Settings>
{
	public AutoRifle_RequestActive(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	
	protected override void Update(Entity entity, CharacterAbility charAbility, AbilityControl abilityCtrl, 
		Ability_AutoRifle.PredictedState predictedState, Ability_AutoRifle.Settings settings)
	{
		Profiler.BeginSample("AutoRifle_RequestActive");
		
		var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
		var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
		
		var isAlive = character.State.locoState != CharacterPredictedState.StateData.LocoState.Dead;
		predictedState.fireRequested = (command.primaryFire && isAlive) ? 1 : 0;
		
		if (command.reload && isAlive && predictedState.ammoInClip < settings.clipSize)
		{
			predictedState.reloadRequested = 1;
		}

		var isIdle = predictedState.phase == Ability_AutoRifle.Phase.Idle;
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
		
		Profiler.EndSample();
	}
}


[DisableAutoCreation]
class AutoRifle_Update : BaseComponentDataSystem<AbilityControl,Ability_AutoRifle.PredictedState,Ability_AutoRifle.Settings>
{
	public AutoRifle_Update(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}
	
	protected override void Update(Entity abilityEntity, AbilityControl abilityCtrl, 
		Ability_AutoRifle.PredictedState predictedState, Ability_AutoRifle.Settings settings)
	{
		Profiler.BeginSample("AutoRifle_Update");
		
		switch (predictedState.phase)
		{
			case Ability_AutoRifle.Phase.Idle:
			{
				if(abilityCtrl.activeAllowed == 0)
					break;
				
				if (predictedState.reloadRequested == 1)
				{
					EnterReloadingPhase(abilityEntity, ref predictedState, m_world.worldTime.tick);
					break;
				}

				if (predictedState.fireRequested == 1)
				{
					EnterFiringPhase(abilityEntity, ref predictedState, m_world.worldTime.tick);
					break;
				}

				break;
			}
			case Ability_AutoRifle.Phase.Firing:
			{
				var fireDuration = 1.0f / settings.roundsPerSecond; 
				var phaseDuration = m_world.worldTime.DurationSinceTick(predictedState.phaseStartTick);
				if (phaseDuration > fireDuration)
				{

					if (predictedState.fireRequested == 1 && predictedState.ammoInClip > 0)
						EnterFiringPhase(abilityEntity, ref predictedState, m_world.worldTime.tick);
					else
						EnterIdlePhase(abilityEntity, ref predictedState, m_world.worldTime.tick);
				}

				break;
			}
			case Ability_AutoRifle.Phase.Reloading:
			{
				var phaseDuration = m_world.worldTime.DurationSinceTick(predictedState.phaseStartTick);
				if (phaseDuration > settings.reloadDuration)
				{
					predictedState.reloadRequested = 0;
					var neededInClip = settings.clipSize - predictedState.ammoInClip;
					predictedState.ammoInClip += neededInClip;

					EnterIdlePhase(abilityEntity, ref predictedState, m_world.worldTime.tick);
					break;
				}

				break;
			}
		}

		// Decrease cone
		if (predictedState.phase != Ability_AutoRifle.Phase.Firing)
		{
			predictedState.COF -= settings.COFDecreaseVel * m_world.worldTime.tickDuration;
			if (predictedState.COF < settings.minCOF)
				predictedState.COF = settings.minCOF;
		}
		
		
		
		EntityManager.SetComponentData(abilityEntity, predictedState);
		
		Profiler.EndSample();
	}



	void EnterReloadingPhase(Entity abilityEntity, ref Ability_AutoRifle.PredictedState predictedState, int tick)
	{
		//GameDebug.Log("EnterReloadingPhase");

		var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
		var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);

		predictedState.SetPhase(Ability_AutoRifle.Phase.Reloading, tick);
		character.State.SetAction(CharacterPredictedState.StateData.Action.Reloading, tick);
	}

	void EnterIdlePhase(Entity abilityEntity, ref Ability_AutoRifle.PredictedState predictedState, int tick)
	{
		//GameDebug.Log("EnterIdlePhase");

		var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
		var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);

		predictedState.SetPhase(Ability_AutoRifle.Phase.Idle, tick);
		character.State.SetAction(CharacterPredictedState.StateData.Action.None, tick);
	}

	void EnterFiringPhase(Entity abilityEntity, ref Ability_AutoRifle.PredictedState predictedState, int tick) 
	{
		//GameDebug.Log("EnterFiringPhase");

		var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
		var settings = EntityManager.GetComponentData<Ability_AutoRifle.Settings>(abilityEntity);
		var character = EntityManager.GetComponentObject<Character>(charAbility.character);
		var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
		
		predictedState.SetPhase(Ability_AutoRifle.Phase.Firing, tick);
		charPredictedState.State.SetAction(CharacterPredictedState.StateData.Action.PrimaryFire, tick);

		predictedState.ammoInClip -= 1;

		// Only fire shot once for each tick (so it does not fire again when re-predicting)
		var localState = EntityManager.GetComponentData<Ability_AutoRifle.LocalState>(abilityEntity);
		if (tick > localState.lastHitCheckTick)
		{
			localState.lastHitCheckTick = tick;

			var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;

			var aimDir = (float3)command.lookDir;

			var cross = math.cross(new float3(0, 1, 0),aimDir);
			var cofAngle = math.radians(predictedState.COF)*0.5f;
			var direction = math.mul(quaternion.axisAngle(cross, cofAngle), aimDir);

			// TODO use tick as random seed so server and client calculates same angle for given tick  
			var rndAngle = UnityEngine.Random.Range(0, (float) math.PI * 2);
			var rndRot = quaternion.axisAngle(aimDir, rndAngle);
			direction = math.mul(rndRot, direction);

			predictedState.COF  += settings.shotCOFIncrease;
			if (predictedState.COF > settings.maxCOF)
				predictedState.COF = settings.maxCOF;

			var eyePos = charPredictedState.State.position + Vector3.up*character.eyeHeight; 
			
//            Debug.DrawRay(rayStart, direction * 1000, Color.green, 1.0f);

			const int distance = 500;
			var collisionMask = ~(1 << charPredictedState.teamId);

			var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
			localState.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
			{
				origin = eyePos,
				direction = direction,
				distance = distance,
				sphereCastExcludeOwner = charAbility.character,
				hitCollisionTestTick = command.renderTick,
				testAgainsEnvironment = 1,
				sphereCastRadius = settings.hitscanRadius,
				sphereCastMask = collisionMask,
			});

			EntityManager.SetComponentData(abilityEntity,localState);
		}
	}
}

[DisableAutoCreation]
class AutoRifle_HandleCollisionQuery : BaseComponentDataSystem<Ability_AutoRifle.LocalState,Ability_AutoRifle.InterpolatedState>
{
	public AutoRifle_HandleCollisionQuery(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}
	
	protected override void Update(Entity abilityEntity, Ability_AutoRifle.LocalState localState, Ability_AutoRifle.InterpolatedState interpolatedState)
	{
		if (localState.rayQueryId == -1)
			return;

		Profiler.BeginSample("-get result");
		var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
		RaySphereQueryReciever.Query query;
		RaySphereQueryReciever.Result result;
		queryReciever.GetResult(localState.rayQueryId, out query, out result);
		localState.rayQueryId = -1;
		Profiler.EndSample();
		
		float3 endPos;

		var impact = result.hit == 1;
		if (impact)
		{
			var hitCollisionHit = result.hitCollisionOwner != Entity.Null;
			interpolatedState.impactType = hitCollisionHit
				? Ability_AutoRifle.ImpactType.Character
				: Ability_AutoRifle.ImpactType.Environment;
			endPos = result.hitPoint;
			
			// Apply damage
			if (hitCollisionHit)
			{
				Profiler.BeginSample("-add damage");
				var charAbility = EntityManager.GetComponentData<CharacterAbility>(abilityEntity);
				var settings = EntityManager.GetComponentData<Ability_AutoRifle.Settings>(abilityEntity);
				var hitCollisionOwner = EntityManager.GetComponentObject<HitCollisionOwner>(result.hitCollisionOwner);
				hitCollisionOwner.damageEvents.Add(new DamageEvent(charAbility.character, settings.damage, query.direction, 
					settings.damageImpulse));
				Profiler.EndSample();
			}

			interpolatedState.impactNormal = result.hitNormal;
		}
		else
		{
			interpolatedState.impactType = Ability_AutoRifle.ImpactType.None;
			endPos = query.origin + query.distance * query.direction;
		}
		interpolatedState.fireTick = m_world.worldTime.tick;
		interpolatedState.fireEndPos = endPos;
		
		EntityManager.SetComponentData(abilityEntity,localState);
		EntityManager.SetComponentData(abilityEntity,interpolatedState);
	}
}