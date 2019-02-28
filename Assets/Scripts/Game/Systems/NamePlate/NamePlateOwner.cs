using System;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class NamePlateOwner : MonoBehaviour
{
	public NamePlate namePlatePrefab;		
	public Transform namePlateTransform;

	[NonSerialized] public bool visible = true;
	[NonSerialized] public string text;
	[NonSerialized] public int team;
	[NonSerialized] public float health;
	[NonSerialized] public NamePlate namePlate;
}

[DisableAutoCreation]
public class HandleNamePlateSpawn : InitializeComponentSystem<NamePlateOwner>
{
	public HandleNamePlateSpawn(GameWorld world) : base(world) {}

	protected override void Initialize(Entity entity, NamePlateOwner component)
	{
		component.namePlate = GameObject.Instantiate(component.namePlatePrefab);
	}
}

[DisableAutoCreation]
public class HandleNamePlateDespawn : DeinitializeComponentSystem<NamePlateOwner>
{
	public HandleNamePlateDespawn(GameWorld world) : base(world) {}

	protected override void Deinitialize(Entity entity, NamePlateOwner component)
	{
		GameObject.Destroy(component.namePlate.gameObject);
		component.namePlate = null;
	}
}


[DisableAutoCreation]
public class UpdateNamePlates : BaseComponentSystem
{
	ComponentGroup Group;
	ComponentGroup LocalPlayerGroup;
	
	public UpdateNamePlates(GameWorld world) : base(world) {}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();

		Group = GetComponentGroup(typeof(NamePlateOwner), typeof(CharacterPresentationSetup));
		LocalPlayerGroup = GetComponentGroup(typeof(LocalPlayer));
	}

	protected override void OnUpdate()
	{
		var localPlayerArray = LocalPlayerGroup.GetComponentArray<LocalPlayer>();
		
		if (localPlayerArray.Length == 0)
			return;

		var localPlayer = localPlayerArray[0];
		if (localPlayer.playerState == null)
			return;

		var nameplateArray = Group.GetComponentArray<NamePlateOwner>();
		var charPresentationArray = Group.GetComponentArray<CharacterPresentationSetup>();
		
		for (int i = 0; i < nameplateArray.Length; i++)
		{
			var plateOwner = nameplateArray[i];
			if (plateOwner.namePlate == null) 
			{
				GameDebug.LogError("namePlateOwner.namePlate == null");
				continue;
			}

			var root = plateOwner.namePlate.namePlateRoot.gameObject;

            if (IngameHUD.showHud.IntValue == 0)
            {
                SetActiveIfNeeded(root, false);
                continue;
            }

			if (!plateOwner.visible)
			{
				SetActiveIfNeeded(root, false);
				continue;
			}

			// Dont show our own
			var character = charPresentationArray[i].character;
			if (character == localPlayer.playerState.controlledEntity)
			{
				SetActiveIfNeeded(root, false);
				continue;
			}
			
            // Dont show nameplate behinds
            var camera = Game.game.TopCamera();// Camera.allCameras[0];
			var platePosWorld = plateOwner.namePlateTransform.position;
			var screenPos = camera.WorldToScreenPoint(platePosWorld);
			if (screenPos.z < 0)	
			{
				SetActiveIfNeeded(root,false);
				continue;
			}
			
			// Test occlusion
			var rayStart = camera.ScreenToWorldPoint(new Vector3(screenPos.x,screenPos.y,0));
			var v = platePosWorld - rayStart;
			var distance = v.magnitude;
			const int defaultLayerMask = 1 << 0;
			var occluded = Physics.Raycast(rayStart, v.normalized, distance, defaultLayerMask);
			
			var friendly = plateOwner.team == localPlayer.playerState.teamIndex;
            var color = friendly ? Game.game.gameColors[(int)Game.GameColor.Friend] : Game.game.gameColors[(int)Game.GameColor.Enemy];

			var showPlate = friendly || !occluded;

			// Update plate
			if (!showPlate)
			{
				SetActiveIfNeeded(root,false);
				continue;
			}
				
			plateOwner.namePlate.namePlateRoot.transform.position = screenPos;

			// Update icon
			var showIcon = friendly;
			SetActiveIfNeeded(plateOwner.namePlate.icon.gameObject,showIcon);
			if (showIcon)
			{
				plateOwner.namePlate.icon.color = color;
			}

			// Update name text
			var inNameTextDist = distance <= plateOwner.namePlate.maxNameDistance;
			var showNameText = !occluded && inNameTextDist;
			SetActiveIfNeeded(plateOwner.namePlate.nameText.gameObject,showNameText);
			if (showNameText)
			{
				plateOwner.namePlate.nameText.text = plateOwner.text;	
				plateOwner.namePlate.nameText.color = color;
			}
			
			SetActiveIfNeeded(root,true);
		}
    }

	// Set settings active on UI Text creates garbage we check for whether active state has changed 
	void SetActiveIfNeeded(GameObject go, bool active)
	{
		if (go.activeSelf != active)
		{
			go.SetActive(active);
		}
	}
}