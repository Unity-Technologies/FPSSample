using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum CharacterEmote
{
	None = 0,
	Victory,
	Defeat,
	DanceA,
}

[CreateAssetMenu(fileName = "Ability_Emote",menuName = "FPS Sample/Abilities/Emote")]
public class Ability_Emote : CharBehaviorFactory
{
	public struct InternalState : IComponentData
	{
		public int active;
		public int animDone;
	}

	public struct SerializedState : INetSerialized, IComponentData
	{
		public CharacterEmote emote;
		public int emoteCount;
        
		public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
		{
			writer.WriteInt16("emote", (short)emote);
			writer.WriteInt16("emoteCount", (short)emoteCount);
		}

		public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
		{
			emote = (CharacterEmote)reader.ReadInt16();
			emoteCount = reader.ReadInt16();
		}
	}
	
	public override Entity Create(EntityManager entityManager, List<Entity> entities)
	{
		var entity = CreateCharBehavior(entityManager);
		entities.Add(entity);
		
		// Ability components
		entityManager.AddComponentData(entity, new InternalState());
		entityManager.AddComponentData(entity, new SerializedState());

		return entity;
	}
}

[DisableAutoCreation]
class Emote_Update : BaseComponentDataSystem<CharBehaviour, AbilityControl, Ability_Emote.InternalState, Ability_Emote.SerializedState>
{
	public Emote_Update(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
		Ability_Emote.InternalState internalState, Ability_Emote.SerializedState serializedState)
	{
		if (abilityCtrl.active == 0)
		{
			// If deactivate from outside we need to clean up
			if (internalState.active == 1)
				Deactivate(abilityEntity, charAbility, abilityCtrl, internalState, serializedState);
			return;
		}

		// Cancel if moving or requested 
		var command = EntityManager.GetComponentObject<UserCommandComponent>(charAbility.character).command;
		var moving = command.moveMagnitude > 0; 
		if( moving)
		{
			Deactivate(abilityEntity, charAbility, abilityCtrl, internalState, serializedState);
			return;
		}

		if (internalState.animDone == 1)
		{
			Deactivate(abilityEntity, charAbility, abilityCtrl, internalState, serializedState);
			return;
		}
		
		
		if (command.emote != CharacterEmote.None)
		{
			var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(charAbility.character);

			abilityCtrl.behaviorState = AbilityControl.State.Active;
			serializedState.emote = command.emote;
			serializedState.emoteCount = serializedState.emoteCount + 1;
			charPredictedState.cameraProfile = CameraProfile.ThirdPerson;
			internalState.active = 1;

			EntityManager.SetComponentData(abilityEntity,abilityCtrl);
			EntityManager.SetComponentData(abilityEntity,internalState);
			EntityManager.SetComponentData(abilityEntity,serializedState);
			EntityManager.SetComponentData(charAbility.character, charPredictedState);
		}
	}
	
	void Deactivate(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl,
		Ability_Emote.InternalState internalState, Ability_Emote.SerializedState serializedState)
	{
		var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(charAbility.character);

		abilityCtrl.behaviorState = AbilityControl.State.Idle;
		serializedState.emote = CharacterEmote.None;
		internalState.active = 0;
		internalState.animDone = 0;
		charPredictedState.cameraProfile = CameraProfile.FirstPerson;
		
		EntityManager.SetComponentData(abilityEntity, abilityCtrl);
		EntityManager.SetComponentData(abilityEntity,internalState);
		EntityManager.SetComponentData(abilityEntity,serializedState);
		EntityManager.SetComponentData(charAbility.character, charPredictedState);
	}
}