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

	public struct SerializerState : IReplicatedComponent, IComponentData
	{
		public CharacterEmote emote;
		public int emoteCount;

		public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
		{
			return new ReplicatedComponentSerializerFactory<SerializerState>();
		}
		
		public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
		{
			writer.WriteInt16("emote", (short)emote);
			writer.WriteInt16("emoteCount", (short)emoteCount);
		}

		public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
		{
			emote = (CharacterEmote)reader.ReadInt16();
			emoteCount = reader.ReadInt16();
		}
	}
	
	public override Entity Create(EntityManager entityManager, List<Entity> entities)
	{
		var entity = CreateCharBehavior(entityManager);
		entities.Add(entity);
		
		entityManager.AddComponentData(entity, new InternalState());
		entityManager.AddComponentData(entity, new SerializerState());

		return entity;
	}
}


[DisableAutoCreation]
class Emote_RequestActive : BaseComponentDataSystem<CharBehaviour,AbilityControl, Ability_Emote.InternalState>
{
	public Emote_RequestActive(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	protected override void Update(Entity entity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
		Ability_Emote.InternalState internalState)
	{
		if (abilityCtrl.behaviorState == AbilityControl.State.Active || abilityCtrl.behaviorState == AbilityControl.State.Cooldown)
			return;
		
		var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
		abilityCtrl.behaviorState = command.emote != CharacterEmote.None ?  
			AbilityControl.State.RequestActive : AbilityControl.State.Idle;
		EntityManager.SetComponentData(entity, abilityCtrl);			
	}
}


[DisableAutoCreation]
class Emote_Update : BaseComponentDataSystem<CharBehaviour, AbilityControl, Ability_Emote.InternalState, Ability_Emote.SerializerState>
{
	public Emote_Update(GameWorld world) : base(world)
	{
		ExtraComponentRequirements = new ComponentType[] { typeof(ServerEntity) } ;
	}

	protected override void Update(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl, 
		Ability_Emote.InternalState internalState, Ability_Emote.SerializerState serializerState)
	{
		if (abilityCtrl.active == 0)
		{
			// If deactivate from outside we need to clean up
			if (internalState.active == 1)
				Deactivate(abilityEntity, charAbility, abilityCtrl, internalState, serializerState);
			return;
		}

		// Cancel if moving or requested 
		var command = EntityManager.GetComponentData<UserCommandComponentData>(charAbility.character).command;
		var moving = command.moveMagnitude > 0; 
		if( moving)
		{
			Deactivate(abilityEntity, charAbility, abilityCtrl, internalState, serializerState);
			return;
		}

		if (internalState.animDone == 1)
		{
			Deactivate(abilityEntity, charAbility, abilityCtrl, internalState, serializerState);
			return;
		}
		
		
		if (command.emote != CharacterEmote.None)
		{
			var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

			abilityCtrl.behaviorState = AbilityControl.State.Active;
			serializerState.emote = command.emote;
			serializerState.emoteCount = serializerState.emoteCount + 1;
			charPredictedState.cameraProfile = CameraProfile.ThirdPerson;
			internalState.active = 1;

			EntityManager.SetComponentData(abilityEntity,abilityCtrl);
			EntityManager.SetComponentData(abilityEntity,internalState);
			EntityManager.SetComponentData(abilityEntity,serializerState);
			EntityManager.SetComponentData(charAbility.character, charPredictedState);
		}
	}
	
	void Deactivate(Entity abilityEntity, CharBehaviour charAbility, AbilityControl abilityCtrl,
		Ability_Emote.InternalState internalState, Ability_Emote.SerializerState serializerState)
	{
		var charPredictedState = EntityManager.GetComponentData<CharacterPredictedData>(charAbility.character);

		abilityCtrl.behaviorState = AbilityControl.State.Idle;
		serializerState.emote = CharacterEmote.None;
		internalState.active = 0;
		internalState.animDone = 0;
		charPredictedState.cameraProfile = CameraProfile.FirstPerson;
		
		EntityManager.SetComponentData(abilityEntity, abilityCtrl);
		EntityManager.SetComponentData(abilityEntity,internalState);
		EntityManager.SetComponentData(abilityEntity,serializerState);
		EntityManager.SetComponentData(charAbility.character, charPredictedState);
	}
}