using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class CharacterEvents : MonoBehaviour
{
	public float minFootstepInterval = 0.2f;
	public SoundDef footstepLeft;
	public SoundDef footstepRight;
    [FormerlySerializedAs("landSound")]
    public SoundDef land;
	[FormerlySerializedAs("doubleJumpSound")]
	public SoundDef doubleJump;
	[FormerlySerializedAs("jumpStartSound")]
	public SoundDef jumpStart;
	
	public CharPredictedStateData.LocoState previousLocoState;
	
	[NonSerialized] public bool active = false;
	[NonSerialized] public bool nextFootLeft;
	[NonSerialized] public float lastFootstepTime;
	
	[NonSerialized] public bool onFootDown;
	[NonSerialized] public bool onLand;
	[NonSerialized] public bool onDoubleJump;
	[NonSerialized] public bool onJumpStart;	

    void OnCharEvent(AnimationEvent e)
    {
        switch (e.stringParameter)
        {
            case "FootDown":
                onFootDown = true;
                break;
            case "LeftFootDown":
                onFootDown = true;
                break;
            case "RightFootDown":
                onFootDown = true;
                break;
            case "Land":
                onLand = true;
                break;
        }  
    }

	public void GenerateStateChangeEvents(ref PresentationState animState)
	{
		if (animState.charLocoState != previousLocoState)

		{
			if (animState.charLocoState == CharPredictedStateData.LocoState.DoubleJump)
			{
				onDoubleJump = true;
			}

			if (animState.charLocoState == CharPredictedStateData.LocoState.Stand ||
				animState.charLocoState == CharPredictedStateData.LocoState.GroundMove)
			{
				if (previousLocoState == CharPredictedStateData.LocoState.InAir ||
					previousLocoState == CharPredictedStateData.LocoState.DoubleJump)
				{
					onLand = true;
				}
			}

			if (animState.charLocoState == CharPredictedStateData.LocoState.Jump)
			{
				onJumpStart = true;
			}

			previousLocoState = animState.charLocoState;
		}
	}
}


[DisableAutoCreation]
public class HandleCharacterEvents : ComponentSystem
{
	ComponentGroup Group;

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		Group = GetComponentGroup(typeof(CharacterEvents), typeof(CharPresentation));
	}

	protected override void OnUpdate()
	{
//		var entityArray = Group.GetEntityArray();
		var eventArray = Group.GetComponentArray<CharacterEvents>();
		var charPresentArray = Group.GetComponentArray<CharPresentation>();
		for (var i = 0; i < eventArray.Length; i++)
		{
			var charEvents = eventArray[i];
			var charPresentation = charPresentArray[i];
			
			var presentationState = EntityManager.GetComponentData<PresentationState>(charPresentation.character);  
			charEvents.GenerateStateChangeEvents(ref presentationState);
			
			//Fire sounds based on active events
			if (!charEvents.active)
				continue;

			// TODO clear events first frame active, so we dont fire all events accumulated while inactive
			
			GameDebug.Assert(charEvents.footstepLeft && charEvents.footstepRight, "Footstep on {0} not set up", charEvents);

			if (charEvents.onFootDown)
			{
				if (Time.time > charEvents.lastFootstepTime + charEvents.minFootstepInterval)
				{
					var sound = charEvents.nextFootLeft ? charEvents.footstepLeft : charEvents.footstepRight;
					Game.SoundSystem.Play(sound, charEvents.transform);
					charEvents.nextFootLeft = !charEvents.nextFootLeft;
					charEvents.lastFootstepTime = Time.time;
				}

				charEvents.onFootDown = false;
			}

			if (charEvents.onLand)
			{
				if (charEvents.land != null)
				{
					Game.SoundSystem.Play(charEvents.land, charEvents.transform);
					charEvents.onLand = false;
					charEvents.onLand = false;
				}
			}

			if (charEvents.onDoubleJump)
			{
				if (charEvents.doubleJump != null)
				{
					Game.SoundSystem.Play(charEvents.doubleJump, charEvents.transform);
					charEvents.onDoubleJump = false;
				}
			}

			if (charEvents.onJumpStart)
			{
				if (charEvents.jumpStart != null)
				{
					Game.SoundSystem.Play(charEvents.jumpStart, charEvents.transform);
					charEvents.onJumpStart = false;
				}
			}
		}
	}		
}
