using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;


         



[DisableAutoCreation]
public class CharacterLateUpdate : BaseComponentSystem<CharacterPresentationSetup>
{
    public CharacterLateUpdate(GameWorld gameWorld) : base(gameWorld)
    {}
    
    protected override void Update(Entity entity, CharacterPresentationSetup charPresentation)
    {
        // Update visibility
        if (EntityManager.HasComponent<CharacterEvents>(entity))
        {
            var footsteps = EntityManager.GetComponentObject<CharacterEvents>(entity);
            footsteps.active = charPresentation.IsVisible;
        }
        
        // Update nameplate
        if (EntityManager.HasComponent<NamePlateOwner>(entity))
        {
            var namePlateOwner = EntityManager.GetComponentObject<NamePlateOwner>(entity);
            var character = EntityManager.GetComponentObject<Character>(charPresentation.character);
            var healthState = EntityManager.GetComponentData<HealthStateData>(charPresentation.character);
            namePlateOwner.text = character.characterName;
            namePlateOwner.team = character.teamId;
            namePlateOwner.health = healthState.health;
            namePlateOwner.visible = healthState.health > 0;
        }
        
        if (EntityManager.HasComponent<CharacterEvents>(entity))
        {
            var footsteps = EntityManager.GetComponentObject<CharacterEvents>(entity);
            footsteps.active = charPresentation.IsVisible;
        }

        
#if UNITY_EDITOR    // TODO (mogensh) move this test code ... somewhere
        if (charPresentation.weaponBoneDebug != null)
        {
            var animState = EntityManager.GetComponentData<CharacterInterpolatedData>(entity);
            var lookDir = Quaternion.Euler(new Vector3(-animState.aimPitch , animState.aimYaw, 0)) * Vector3.down;
            var weaponPos = charPresentation.weaponBoneDebug.position;
            var weaponRot = charPresentation.weaponBoneDebug.rotation;
            Debug.DrawLine(weaponPos, weaponPos + lookDir, Color.magenta);                
            var aimDir = weaponRot * Quaternion.Euler(charPresentation.weaponOffsetDebug) * Vector3.down;
            Debug.DrawLine(weaponPos, weaponPos + aimDir, Color.green);                
        }
#endif
                    
    }
}



class CharacterModuleClient : CharacterModuleShared
{
    public CharacterModuleClient(GameWorld world, BundledResourceManager resourceSystem) : base(world)
    {
        // Handle controlled entity change        
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<UpdateCharacter1PSpawn>(m_world, resourceSystem));
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<PlayerCharacterControlSystem>(m_world));

        // Handle spawn
        CharacterBehaviours.CreateHandleSpawnSystems(m_world, m_HandleSpawnSystems, resourceSystem, false);
       
        // Handle despawn
        CharacterBehaviours.CreateHandleDespawnSystems(m_world, m_HandleDespawnSystems);
        
        // Behaviors
        CharacterBehaviours.CreateAbilityRequestSystems(m_world, m_AbilityRequestUpdateSystems);
        CharacterBehaviours.CreateMovementStartSystems(m_world,m_MovementStartSystems);
        CharacterBehaviours.CreateMovementResolveSystems(m_world,m_MovementResolveSystems);
        CharacterBehaviours.CreateAbilityStartSystems(m_world,m_AbilityStartSystems);
        CharacterBehaviours.CreateAbilityResolveSystems(m_world,m_AbilityResolveSystems);

        // Interpolation        
        
        m_UpdateCharPresentationState = m_world.GetECSWorld().CreateManager<UpdateCharPresentationState>(m_world);
        m_ApplyPresentationState = m_world.GetECSWorld().CreateManager<ApplyPresentationState>(m_world);
        m_CharacterLateUpdate = m_world.GetECSWorld().CreateManager<CharacterLateUpdate>(m_world);

        m_UpdatePresentationRootTransform = m_world.GetECSWorld().CreateManager<UpdatePresentationRootTransform>(m_world);
        m_UpdatePresentationAttachmentTransform = m_world.GetECSWorld().CreateManager<UpdatePresentationAttachmentTransform>(m_world);

        m_updateCharacterUI = m_world.GetECSWorld().CreateManager<UpdateCharacterUI>(m_world);
        characterCameraSystem = m_world.GetECSWorld().CreateManager<UpdateCharacterCamera>(m_world);
        m_HandleCharacterEvents = m_world.GetECSWorld().CreateManager<HandleCharacterEvents>();

        
        // Preload all character resources (until we have better streaming solution)
        var charRegistry = resourceSystem.GetResourceRegistry<CharacterTypeRegistry>();
        for (var i = 0; i < charRegistry.entries.Count; i++)
        {
            resourceSystem.GetSingleAssetResource(charRegistry.entries[i].prefab1P);
            resourceSystem.GetSingleAssetResource(charRegistry.entries[i].prefabClient);
        }

        Console.AddCommand("thirdperson", CmdToggleThirdperson, "Toggle third person mode", this.GetHashCode());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        
        foreach (var system in m_InterpolateSystems)
            m_world.GetECSWorld().DestroyManager(system);
        foreach (var system in m_LateUpdateSystems)
            m_world.GetECSWorld().DestroyManager(system);
        
//        m_world.GetECSWorld().DestroyManager(m_InterpolatePresentationState);
        m_world.GetECSWorld().DestroyManager(m_UpdateCharPresentationState);
        
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationState);

        m_world.GetECSWorld().DestroyManager(m_CharacterLateUpdate);
            
        m_world.GetECSWorld().DestroyManager(m_UpdatePresentationRootTransform);
        m_world.GetECSWorld().DestroyManager(m_UpdatePresentationAttachmentTransform);
        
        m_world.GetECSWorld().DestroyManager(m_updateCharacterUI);
        m_world.GetECSWorld().DestroyManager(characterCameraSystem);
        
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterEvents);

        Console.RemoveCommandsWithTag(this.GetHashCode());
    }

    
    public void Interpolate()
    {
//        m_InterpolatePresentationState.Update();
        
        foreach (var system in m_InterpolateSystems)
            system.Update();
    }

    public void UpdatePresentation()
    {
        m_UpdateCharPresentationState.Update();
        m_ApplyPresentationState.Update();
    }

    public void LateUpdate()
    {
        m_updateCharacterUI.Update();
        
        foreach (var system in m_LateUpdateSystems)
            system.Update();
                      
        m_HandleCharacterEvents.Update();
    }
    
    public void CameraUpdate()
    {
        m_CharacterLateUpdate.Update();
        m_UpdatePresentationRootTransform.Update();
        characterCameraSystem.Update();
        m_UpdatePresentationAttachmentTransform.Update();
    }
    
    void CmdToggleThirdperson(string[] args)
    {
        characterCameraSystem.ToggleFOrceThirdPerson();
    }
   
    
    readonly List<ScriptBehaviourManager> m_InterpolateSystems = new List<ScriptBehaviourManager>();
    readonly List<ScriptBehaviourManager> m_LateUpdateSystems = new List<ScriptBehaviourManager>();
    

    
//    readonly InterpolatePresentationState m_InterpolatePresentationState;
    readonly UpdateCharPresentationState m_UpdateCharPresentationState;
    readonly ApplyPresentationState m_ApplyPresentationState;

    readonly CharacterLateUpdate m_CharacterLateUpdate;
    
    readonly UpdatePresentationRootTransform m_UpdatePresentationRootTransform;
    readonly UpdatePresentationAttachmentTransform m_UpdatePresentationAttachmentTransform;
    
    readonly UpdateCharacterUI m_updateCharacterUI;
    readonly UpdateCharacterCamera characterCameraSystem;
    
    readonly HandleCharacterEvents m_HandleCharacterEvents;

    
}
