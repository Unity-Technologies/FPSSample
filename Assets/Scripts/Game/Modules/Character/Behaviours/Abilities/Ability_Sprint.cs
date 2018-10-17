using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


[RequireComponent(typeof(ReplicatedAbility))]
public class Ability_Sprint : MonoBehaviour
{
	[Serializable]
	public struct Settings : IComponentData
	{
		public float stopDelay;
	}
	
	public struct PredictedState : IPredictedData<PredictedState>, IComponentData
	{
		public int active;
		public int terminating;
		public int terminateStartTick;
			
		public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
		{
			writer.WriteBoolean("active", active == 1);
			writer.WriteBoolean("terminating", terminating == 1);
			writer.WriteInt32("terminateStartTick", terminateStartTick);
		}

		public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
		{
			active = reader.ReadBoolean() ? 1 : 0;
			terminating = reader.ReadBoolean() ? 1 : 0;
			terminateStartTick = reader.ReadInt32();
		}
		
	
#if UNITY_EDITOR
		public bool VerifyPrediction(ref PredictedState state)
		{
			return true;
		}

		public override string ToString()
		{
			return "Sprint.State active:" + active + " terminating:" + terminating;
		}
#endif  
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
		entityManager.AddComponentData(abilityEntity, new PredictedState());
		entityManager.AddComponentData(abilityEntity, settings);


		// Setup replicated ability
		var replicatedAbility = entityManager.GetComponentObject<ReplicatedAbility>(abilityEntity);
		replicatedAbility.predictedHandlers = new IPredictedDataHandler[2];
		replicatedAbility.predictedHandlers[0] = new PredictedEntityHandler<PredictedState>(entityManager, abilityEntity);
		replicatedAbility.predictedHandlers[1] = new PredictedEntityHandler<AbilityControl>(entityManager, abilityEntity);
	}
}


[DisableAutoCreation]
class Sprint_RequestActive : BaseComponentDataSystem<CharacterAbility,AbilityControl,Ability_Sprint.PredictedState,Ability_Sprint.Settings>
{
	public Sprint_RequestActive(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	protected override void Update(Entity abilityEntity, CharacterAbility charAbility, AbilityControl abilityCtrl, Ability_Sprint.PredictedState predictedState, Ability_Sprint.Settings settings)
	{
		var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;

		var sprintAllowed = command.moveMagnitude > 0 && (command.moveYaw < 90.0f || command.moveYaw > 270);

		var interruptCommandActive = command.reload || command.primaryFire || command.secondaryFire;
		sprintAllowed = sprintAllowed && !interruptCommandActive;

		var sprintRequested = sprintAllowed && command.sprint;

		if (predictedState.active == 1 && !sprintAllowed && predictedState.terminating == 0)
		{
//			GameDebug.Log("Sprint. Terminating as sprint not allowed");
			predictedState.terminating = 1;
			predictedState.terminateStartTick = m_world.worldTime.tick;
		}

		if (predictedState.terminating == 1)
			abilityCtrl.requestsActive =
				m_world.worldTime.DurationSinceTick(predictedState.terminateStartTick) < settings.stopDelay ? 1 : 0;
		else
			abilityCtrl.requestsActive = (predictedState.active == 1 ? sprintAllowed : sprintRequested) ? 1 : 0;

//		GameDebug.Log("Sprint. requesting active:" + abilityCtrl.requestsActive + " active:" + state.active + " allowed:" + sprintAllowed + " req:" + sprintRequested + " term:" + state.terminating);		

		EntityManager.SetComponentData(abilityEntity, abilityCtrl);
		EntityManager.SetComponentData(abilityEntity, predictedState);
	}
}

[DisableAutoCreation]
class Sprint_Update : BaseComponentDataSystem<CharacterAbility, AbilityControl, Ability_Sprint.PredictedState>
{
	public Sprint_Update(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	protected override void Update(Entity abilityEntity, CharacterAbility charAbility, AbilityControl abilityCtrl, Ability_Sprint.PredictedState predictedState)
	{
		if (abilityCtrl.activeAllowed != predictedState.active)
		{
			predictedState.active = abilityCtrl.activeAllowed;
			predictedState.terminating = 0;
			EntityManager.SetComponentData(abilityEntity, predictedState);
		}

		var character = EntityManager.GetComponentObject<CharacterPredictedState>(charAbility.character);
		character.State.sprinting = predictedState.active == 1 && predictedState.terminating == 0;
	}
}