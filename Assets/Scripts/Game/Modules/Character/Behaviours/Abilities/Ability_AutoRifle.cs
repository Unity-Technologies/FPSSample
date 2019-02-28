using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

[CreateAssetMenu(fileName = "Ability_AutoRifle", menuName = "FPS Sample/Abilities/Ability_AutoRifle")]
public class Ability_AutoRifle : CharBehaviorFactory
{
	public enum State
	{
		Idle,
		Fire,
		Reload,
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
		public UserCommand.Button fireButton;
		public UserCommand.Button reloadButton;
		
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

	public struct InternalState : IComponentData
	{
		public int lastHitCheckTick;
		public int rayQueryId;
	}

	public struct PredictedState : IPredictedComponent<PredictedState>, IComponentData
    {
        public State action;
        public int phaseStartTick;

        public int ammoInClip;
	    public float COF;

	    public void SetPhase(State action, int tick)
	    {
		    this.action = action;
		    this.phaseStartTick = tick;
	    }
	    
	    public static IPredictedComponentSerializerFactory CreateSerializerFactory()
	    {
		    return new PredictedComponentSerializerFactory<PredictedState>();
	    }
	    
        public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("phase", (int)action);
            writer.WriteInt32("phaseStart", phaseStartTick);
            writer.WriteInt32("ammoInClip", ammoInClip);
	        writer.WriteFloatQ("COF", COF,0);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            action = (State)reader.ReadInt32();
            phaseStartTick = reader.ReadInt32();
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
    
    public struct InterpolatedState : IInterpolatedComponent<InterpolatedState>, IComponentData
    {
        public int fireTick;
        public float3 fireEndPos;
        public ImpactType impactType;
        public float3 impactNormal;

	    public static IInterpolatedComponentSerializerFactory CreateSerializerFactory()
	    {
		    return new InterpolatedComponentSerializerFactory<InterpolatedState>();
	    }
	    
	    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
        {
            writer.WriteInt32("primFireTick", fireTick);
            writer.WriteVector3Q("fireEndPos", fireEndPos,2);
            writer.WriteInt32("impact", (int)impactType);
            writer.WriteVector3Q("impactNormal", impactNormal,1);
        }

        public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
        {
            fireTick = reader.ReadInt32();
            fireEndPos = reader.ReadVector3Q();
            impactType = (ImpactType)reader.ReadInt32();
            impactNormal = reader.ReadVector3Q();
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
		var internalState = new InternalState
		{
			rayQueryId = -1
		};
		var predictedState = new PredictedState
		{
			action = State.Idle,
			ammoInClip = settings.clipSize,
			COF = settings.minCOF,
		};
		entityManager.AddComponentData(entity, settings);
		entityManager.AddComponentData(entity, internalState);
		entityManager.AddComponentData(entity, predictedState);
		entityManager.AddComponentData(entity, new InterpolatedState());
		
		return entity;
	}

	public static State GetPreferredState(ref PredictedState predictedState, ref Settings settings, 
		ref UserCommand command)
	{
		if (command.buttons.IsSet(settings.reloadButton) && predictedState.ammoInClip < settings.clipSize)
		{
			return State.Reload;
		}
		
		var isIdle = predictedState.action == State.Idle;
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
class AutoRifle_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl,Ability_AutoRifle.PredictedState,Ability_AutoRifle.Settings>
{
	public AutoRifle_RequestActive(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
		Ability_AutoRifle.PredictedState predictedState, Ability_AutoRifle.Settings settings)
	{
		if (abilityCtrl.behaviorState == AbilityControl.State.Active || abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
			return;
		
		var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
		var request = Ability_AutoRifle.GetPreferredState(ref predictedState, ref settings, ref command);
		
		abilityCtrl.behaviorState = request != Ability_AutoRifle.State.Idle ?  AbilityControl.State.RequestActive : AbilityControl.State.Idle;
		
		
		EntityManager.SetComponentData(entity, abilityCtrl);			
	}
}

[DisableAutoCreation]
class AutoRifle_Update : BaseComponentDataSystem<CharBehaviour,AbilityControl,Ability_AutoRifle.PredictedState,Ability_AutoRifle.Settings>
{
	public AutoRifle_Update(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}
	
	protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
		Ability_AutoRifle.PredictedState predictedState, Ability_AutoRifle.Settings settings)
	{
		// Decrease cone
		if (predictedState.action != Ability_AutoRifle.State.Fire)
		{
			predictedState.COF -= settings.COFDecreaseVel * m_world.worldTime.tickDuration;
			if (predictedState.COF < settings.minCOF)
				predictedState.COF = settings.minCOF;
		}
		EntityManager.SetComponentData(abilityEntity, predictedState);

		if (abilityCtrl.active == 0)
		{
			return;
		}
		
		var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
		
		switch (predictedState.action)
		{
			case Ability_AutoRifle.State.Idle:
			{
				var request = Ability_AutoRifle.GetPreferredState(ref predictedState, ref settings, ref command);
				if (request == Ability_AutoRifle.State.Reload)
				{
					EnterReloadingPhase(abilityEntity, ref abilityCtrl, ref predictedState, m_world.worldTime.tick);
					break;
				}

				if (request == Ability_AutoRifle.State.Fire)
				{
					EnterFiringPhase(abilityEntity, ref abilityCtrl, ref predictedState, m_world.worldTime.tick);
					break;
				}

				break;
			}
			case Ability_AutoRifle.State.Fire:
			{
				var fireDuration = 1.0f / settings.roundsPerSecond; 
				var phaseDuration = m_world.worldTime.DurationSinceTick(predictedState.phaseStartTick);
				if (phaseDuration > fireDuration)
				{
					var request = Ability_AutoRifle.GetPreferredState(ref predictedState, ref settings, ref command);
					if (request == Ability_AutoRifle.State.Fire && predictedState.ammoInClip > 0)
						EnterFiringPhase(abilityEntity, ref abilityCtrl, ref predictedState, m_world.worldTime.tick);
					else
						EnterIdlePhase(abilityEntity, ref abilityCtrl, ref predictedState, m_world.worldTime.tick);
				}

				break;
			}
			case Ability_AutoRifle.State.Reload:
			{
				var phaseDuration = m_world.worldTime.DurationSinceTick(predictedState.phaseStartTick);
				if (phaseDuration > settings.reloadDuration)
				{
					var neededInClip = settings.clipSize - predictedState.ammoInClip;
					predictedState.ammoInClip += neededInClip;

					EnterIdlePhase(abilityEntity, ref abilityCtrl, ref predictedState, m_world.worldTime.tick);
				}

				break;
			}
		}
	}

	void EnterReloadingPhase(Entity abilityEntity, ref AbilityControl abilityCtrl, 
		ref Ability_AutoRifle.PredictedState predictedState, int tick)
	{
		//GameDebug.Log("EnterReloadingPhase");

		var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
		var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

		abilityCtrl.behaviorState = AbilityControl.State.Active;
		predictedState.SetPhase(Ability_AutoRifle.State.Reload, tick);
		charPredictedState.SetAction(CharacterPredictedData.Action.Reloading, tick);

		EntityManager.SetComponentData(abilityEntity, abilityCtrl);
		EntityManager.SetComponentData(abilityEntity, predictedState);
		EntityManager.SetComponentData(charAbility.character, charPredictedState);
	}

	void EnterIdlePhase(Entity abilityEntity, ref AbilityControl abilityCtrl, 
		ref Ability_AutoRifle.PredictedState predictedState, int tick)
	{
		//GameDebug.Log("EnterIdlePhase");
		var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
		var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

		abilityCtrl.behaviorState = AbilityControl.State.Idle;
		predictedState.SetPhase(Ability_AutoRifle.State.Idle, tick);
		charPredictedState.SetAction(CharacterPredictedData.Action.None, tick);

		EntityManager.SetComponentData(abilityEntity, abilityCtrl);
		EntityManager.SetComponentData(abilityEntity, predictedState);
		EntityManager.SetComponentData(charAbility.character, charPredictedState);
	}

	void EnterFiringPhase(Entity abilityEntity, ref AbilityControl abilityCtrl,
		ref Ability_AutoRifle.PredictedState predictedState, int tick) 
	{
		//GameDebug.Log("EnterFiringPhase");

		var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
		var settings = EntityManager.GetComponentData<Ability_AutoRifle.Settings>(abilityEntity);
		var character = EntityManager.GetComponentObject<Character>(charAbility.character);
		var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);
		
		abilityCtrl.behaviorState = AbilityControl.State.Active;
		predictedState.SetPhase(Ability_AutoRifle.State.Fire, tick);
		predictedState.ammoInClip -= 1;
		charPredictedState.SetAction(CharacterPredictedData.Action.PrimaryFire, tick);
		
		// Only fire shot once for each tick (so it does not fire again when re-predicting)
		var internalState = EntityManager.GetComponentData<Ability_AutoRifle.InternalState>(abilityEntity);
		if (tick > internalState.lastHitCheckTick)
		{
			internalState.lastHitCheckTick = tick;

			var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;

			var aimDir = (float3)command.lookDir;

			var cross = math.cross(new float3(0, 1, 0),aimDir);
			var cofAngle = math.radians(predictedState.COF)*0.5f;
			var direction = math.mul(quaternion.AxisAngle(cross, cofAngle), aimDir);

			// TODO use tick as random seed so server and client calculates same angle for given tick  
			var rndAngle = UnityEngine.Random.Range(0, (float) math.PI * 2);
			var rndRot = quaternion.AxisAngle(aimDir, rndAngle);
			direction = math.mul(rndRot, direction);

			predictedState.COF  += settings.shotCOFIncrease;
			if (predictedState.COF > settings.maxCOF)
				predictedState.COF = settings.maxCOF;

			var eyePos = charPredictedState.position + Vector3.up*character.eyeHeight; 
			
//            Debug.DrawRay(rayStart, direction * 1000, Color.green, 1.0f);

			const int distance = 500;
			var collisionMask = ~(1U << character.teamId);

			var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
			internalState.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
			{
				origin = eyePos,
				direction = direction,
				distance = distance,
				ExcludeOwner = charAbility.character,
				hitCollisionTestTick = command.renderTick,
				radius = settings.hitscanRadius,
				mask = collisionMask,
			});

			EntityManager.SetComponentData(abilityEntity,internalState);
		}
		
		EntityManager.SetComponentData(abilityEntity, abilityCtrl);
		EntityManager.SetComponentData(abilityEntity, predictedState);
		EntityManager.SetComponentData(charAbility.character, charPredictedState);
	}
}

[DisableAutoCreation]
class AutoRifle_HandleCollisionQuery : BaseComponentDataSystem<Ability_AutoRifle.InternalState,Ability_AutoRifle.InterpolatedState>
{
	public AutoRifle_HandleCollisionQuery(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}
	
	protected override void Update(Entity abilityEntity, Ability_AutoRifle.InternalState internalState, Ability_AutoRifle.InterpolatedState interpolatedState)
	{
		if (internalState.rayQueryId == -1)
			return;

		Profiler.BeginSample("-get result");
		var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
		RaySphereQueryReciever.Query query;
		RaySphereQueryReciever.QueryResult queryResult;
		queryReciever.GetResult(internalState.rayQueryId, out query, out queryResult);
		internalState.rayQueryId = -1;
		Profiler.EndSample();
		
		float3 endPos;

		var impact = queryResult.hit == 1;
		if (impact)
		{
			var hitCollisionHit = queryResult.hitCollisionOwner != Entity.Null;
			interpolatedState.impactType = hitCollisionHit
				? Ability_AutoRifle.ImpactType.Character
				: Ability_AutoRifle.ImpactType.Environment;
			endPos = queryResult.hitPoint;
			
			// Apply damage
			if (hitCollisionHit)
			{
				Profiler.BeginSample("-add damage");
				var charAbility = EntityManager.GetComponentData<CharBehaviour>(abilityEntity);
				var settings = EntityManager.GetComponentData<Ability_AutoRifle.Settings>(abilityEntity);
				
				var damageEventBuffer = EntityManager.GetBuffer<DamageEvent>(queryResult.hitCollisionOwner);
				DamageEvent.AddEvent(damageEventBuffer, charAbility.character, settings.damage, query.direction,settings.damageImpulse);
				
				Profiler.EndSample();
			}

			interpolatedState.impactNormal = queryResult.hitNormal;
		}
		else
		{
			interpolatedState.impactType = Ability_AutoRifle.ImpactType.None;
			endPos = query.origin + query.distance * query.direction;
		}
		interpolatedState.fireTick = m_world.worldTime.tick;
		interpolatedState.fireEndPos = endPos;
		
		EntityManager.SetComponentData(abilityEntity,internalState);
		EntityManager.SetComponentData(abilityEntity,interpolatedState);
	}
}