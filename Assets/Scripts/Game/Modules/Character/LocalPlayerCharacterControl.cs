using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Analytics;
using UnityEngine.Assertions.Must;

public enum CameraMode
{
    Undefined,
    FirstPerson,
    ThirdPerson,
    FreeCam,
}

[RequireComponent(typeof(LocalPlayer))]
[RequireComponent(typeof(PlayerCameraSettings))]
public class LocalPlayerCharacterControl : MonoBehaviour     
{
    [ConfigVar(Name = "char.showhistory", DefaultValue = "0", Description = "Show last char loco states")]
    public static ConfigVar ShowHistory;
    
    public Entity lastRegisteredControlledEntity;      

    public CharacterHealthUI healthUI;
    public IngameHUD hud;

    public CameraMode cameraMode;

    public int lastDamageInflictedTick;
    public int lastDamageReceivedTick;

    public List<AbilityUI> registeredCharUIs = new List<AbilityUI>();

    public class FirstPersonData
    {
        public Entity char3P;
        public Entity char1P;
        public List<Entity> items = new List<Entity>();
    }

    public FirstPersonData firstPerson = new FirstPersonData(); 
}


[DisableAutoCreation]
public class UpdateCharacter1PSpawn : BaseComponentSystem  
{   
    ComponentGroup Group;
    ComponentGroup ItemGroup;    
    
    public UpdateCharacter1PSpawn(GameWorld world, BundledResourceManager resourceManager) : base(world)
    {
        m_ResourceManager = resourceManager;
    }

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(LocalPlayer), typeof(LocalPlayerCharacterControl));
        ItemGroup = GetComponentGroup(typeof(CharacterItem));
    }


    private List<LocalPlayerCharacterControl> charControlBuffer = new List<LocalPlayerCharacterControl>(2);
    private List<Entity> entityBuffer = new List<Entity>(2);
    private List<Entity> itemEntityBuffer = new List<Entity>(8);
    protected override void OnUpdate()
    {
        charControlBuffer.Clear();
        entityBuffer.Clear();
        var charControlArray = Group.GetComponentArray<LocalPlayerCharacterControl>();
        var localPlayerArray = Group.GetComponentArray<LocalPlayer>();
        for (var i = 0; i < charControlArray.Length; i++)
        {
            var localPlayer = localPlayerArray[i];
            var characterControl = charControlArray[i];
            
            var controlledChar3PEntity = EntityManager.Exists(localPlayer.controlledEntity) && EntityManager.HasComponent<CharacterPredictedState>(localPlayer.controlledEntity)
                ? localPlayer.controlledEntity
                : Entity.Null;

            if (characterControl.firstPerson.char3P != controlledChar3PEntity)
            {
                charControlBuffer.Add(characterControl);
                entityBuffer.Add(controlledChar3PEntity);
            }
        }

        if (charControlBuffer.Count > 0)
        {
            var itemArray = ItemGroup.GetComponentArray<CharacterItem>();
            var itemEntityArray = ItemGroup.GetEntityArray();

            for (var i = 0; i < charControlBuffer.Count; i++)
            {
                var charCtrl = charControlBuffer[i];
                var charClientEntity = entityBuffer[i];
                
                // Get items owned by character
                itemEntityBuffer.Clear();
                for (var j = 0; j < itemArray.Length; j++)
                {
                    var item = itemArray[j];
                    if (item.character == charClientEntity)
                        itemEntityBuffer.Add(itemEntityArray[j]);
                }
                
                // Despawn current firstperson
                if (charCtrl.firstPerson.char1P != Entity.Null)
                {
                    GameDebug.Log("Despawning 1P char and items");
                    
                    var go = EntityManager.GetComponentObject<Transform>(charCtrl.firstPerson.char1P).gameObject;
                    m_world.RequestDespawn(go, PostUpdateCommands);
        
                    
                    foreach (var entity in charCtrl.firstPerson.items)
                    {
                        var item = EntityManager.GetComponentObject<Transform>(entity).gameObject;
                
                        // In preview mode the server code will despawn all items so we need to check it isnt already requested
                        if(!EntityManager.HasComponent<DespawningEntity>(entity)) 
                            m_world.RequestDespawn(item, PostUpdateCommands);
                    }
                    
                    charCtrl.firstPerson.items.Clear();
                    charCtrl.firstPerson.char1P = Entity.Null;
                }
        
                charCtrl.firstPerson.char3P = charClientEntity;
                    
                


                // Spawn new
                if (charClientEntity != Entity.Null)
                {
                    GameDebug.Log("Spawning 1P char and items");

        
                    // Create 1P character
                    {
                        var replicatedEntity = EntityManager.GetComponentObject<ReplicatedEntity>(charClientEntity);
                        var registry = m_ResourceManager.GetResourceRegistry<CharacterTypeRegistry>();
                        var registryIndex = registry.GetIndexByClientGUID(replicatedEntity.guid);
                        GameDebug.Assert(registryIndex != -1);
                        var char1PGUID = registry.entries[registryIndex].prefab1P.guid;
                        if (char1PGUID != "")
                        {
                            var prefab1P = m_ResourceManager.LoadSingleAssetResource(char1PGUID) as GameObject;
                            var gameObjectEntity = m_world.Spawn<GameObjectEntity>(prefab1P);
                            var char1P = EntityManager.GetComponentObject<Character1P>(gameObjectEntity.Entity);
                            char1P.character = charClientEntity;
                            charCtrl.firstPerson.char1P = gameObjectEntity.Entity;
                        }
                    }
                    
                    // Create 1P items
                    {
                        for (var j = 0; j < itemEntityBuffer.Count; j++)
                        {
                            var itemEntity = itemEntityBuffer[j];
                            
                            var replicatedEntity = EntityManager.GetComponentObject<ReplicatedEntity>(itemEntity);
                            var itemGUID = replicatedEntity.guid;
                            var itemRegistry = m_ResourceManager.GetResourceRegistry<ItemRegistry>();
                            var itemDefIndex = itemRegistry.GetIndexByClientGUID(itemGUID);
                            if (itemDefIndex != -1)
                            {
                                var item1PGUID = itemRegistry.entries[itemDefIndex].prefab1P.guid;
                                if (item1PGUID != "")
                                {
                                    var itemPrefab1P = m_ResourceManager.LoadSingleAssetResource(item1PGUID) as GameObject;
                                    var gameObejctEntity = m_world.Spawn<GameObjectEntity>(itemPrefab1P);
                                    
                                    var item = EntityManager.GetComponentObject<CharacterItem>(gameObejctEntity.Entity);
                                    item.character = charClientEntity;
                                        
                                    var item1P = EntityManager.GetComponentObject<CharacterItem1P>(gameObejctEntity.Entity);
                                    item1P.item = itemEntity;
                                    item1P.character1P = charCtrl.firstPerson.char1P;
                                    
                                    
                                    
                                    charCtrl.firstPerson.items.Add(gameObejctEntity.Entity);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    BundledResourceManager m_ResourceManager;
}




[DisableAutoCreation]
public class UpdateCharacterCamera : BaseComponentSystem<LocalPlayer,LocalPlayerCharacterControl,PlayerCameraSettings> 
{   
    public UpdateCharacterCamera(GameWorld world) : base(world) {}

    public void ToggleFOrceThirdPerson()   
    {
        forceThirdPerson = !forceThirdPerson;
    }

    protected override void Update(Entity entity, LocalPlayer localPlayer, LocalPlayerCharacterControl characterControl, PlayerCameraSettings cameraSettings)
    {
        if (localPlayer.controlledEntity == Entity.Null || !EntityManager.HasComponent<CharacterPredictedState>(localPlayer.controlledEntity))
        {
            controlledEntity = Entity.Null;
            return;
        }
            
        if (characterControl.firstPerson.char1P == Entity.Null)
        {
            controlledEntity = Entity.Null;
            return;
        }

        GameDebug.Assert(EntityManager.HasComponent<CharAnimState>(localPlayer.controlledEntity),"Controlled entity has no animstate");

        var character = EntityManager.GetComponentObject<Character>(localPlayer.controlledEntity);
        var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(localPlayer.controlledEntity);
        
        var animState = EntityManager.GetComponentData<CharAnimState>(localPlayer.controlledEntity);
        var character1P = EntityManager.GetComponentObject<Character1P>(characterControl.firstPerson.char1P);

        // Check if this is first time update is called with this controlled entity
        var characterChanged = localPlayer.controlledEntity != controlledEntity;
        if (characterChanged)
        {
            controlledEntity = localPlayer.controlledEntity;
            
        }            

        // Update camera mode
        var newCamMode = CameraMode.FreeCam;
        if (charPredictedState != null)
        {
            var characterEntity = charPredictedState.gameObject.GetComponent<GameObjectEntity>().Entity;
            var healthState = EntityManager.GetComponentObject<HealthState>(characterEntity);
            var isAlive = healthState.health > 0;
            newCamMode = isAlive ? CameraMode.FirstPerson : CameraMode.ThirdPerson;
        }
        if (forceThirdPerson)
        {
            newCamMode = CameraMode.ThirdPerson;
        }
        characterControl.cameraMode = newCamMode;

        
        // Update character visibility
        var thirdPerson = characterControl.cameraMode == CameraMode.ThirdPerson;
        
        character.isVisible = thirdPerson;
        character1P.isVisible = !thirdPerson;
      
        // Update camera settings
        var userCommand = EntityManager.GetComponentObject<UserCommandComponent>(localPlayer.controlledEntity);
        var lookRotation = userCommand.command.lookRotation;
        
        cameraSettings.isEnabled = true;

        // Update FOV
        if(characterChanged)
            cameraSettings.fieldOfView = Game.configFov.FloatValue;
        var settings = character.heroTypeData.sprintCameraSettings;
        var targetFOV = animState.sprinting == 1 ? settings.FOVFactor* Game.configFov.FloatValue : Game.configFov.FloatValue;
        var speed = targetFOV > cameraSettings.fieldOfView ? settings.FOVInceraetSpeed : settings.FOVDecreaseSpeed;
        cameraSettings.fieldOfView = Mathf.MoveTowards(cameraSettings.fieldOfView, targetFOV, speed);
        
        switch (characterControl.cameraMode)
        {
            case CameraMode.FirstPerson:

                // Set camera position and adjust 1P char. As 1P char is scaled we need to "up-scale" camera
                // animation to world space and adjust char1P position accordingly
                var camLocalOffset = character1P.cameraTransform.position - character1P.transform.position;
                var camWorldOffset = camLocalOffset/character1P.transform.localScale.x;  
                var camWorldPos = character1P.transform.position + camWorldOffset;
                var charWorldPos = camWorldPos - camLocalOffset;

                cameraSettings.position = camWorldPos;
                cameraSettings.rotation = character1P.cameraTransform.rotation; //lookRotation * cameraRotationOffset;

                character1P.transform.position = charWorldPos;


                break;
            case CameraMode.ThirdPerson:
                var eyePos = charPredictedState.State.position + Vector3.up*character.eyeHeight;
                cameraSettings.position = eyePos; 
                cameraSettings.rotation = lookRotation;

                // Simpe offset of camera for better 3rd person view. This is only for animation debug atm
                var viewDir = cameraSettings.rotation * Vector3.forward;
                cameraSettings.position += -2.5f * viewDir;
                cameraSettings.position += lookRotation*Vector3.right*0.5f + lookRotation*Vector3.up*0.5f;
                break;
        }
        
        
#if UNITY_EDITOR            
        if (LocalPlayerCharacterControl.ShowHistory.IntValue > 0)
        {
            charPredictedState.ShowHistory(m_world);
        }
#endif
    }

    bool forceThirdPerson;
    Entity controlledEntity;
}
