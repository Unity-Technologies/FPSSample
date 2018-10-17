
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

public class CharacterModulePreview : CharacterModuleShared
{

    public CharacterModulePreview(GameWorld world, BundledResourceManager resourceSystem): base(world)
    {
        // Handle spawn requests
        m_HandleCharacterSpawnRequests = m_world.GetECSWorld().CreateManager<HandleCharacterSpawnRequests>(m_world, resourceSystem, false);
        m_HandleCharacterDepawnRequests = m_world.GetECSWorld().CreateManager<HandleCharacterDespawnRequests>(m_world);

        // Handle control change        
        CharacterBehaviours.CreateControlledEntityChangedSystems(m_world, m_ControlledEntityChangedSystems);
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<PlayerCharacterControlSystem>(m_world));
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<UpdateCharacter1PSpawn>(m_world, resourceSystem));

        // Handle spawning
        CharacterBehaviours.CreateHandleSpawnSystems(m_world,m_HandleSpawnSystems, resourceSystem);

        // Handle despawn
        CharacterBehaviours.CreateHandleDespawnSystems(m_world, m_HandleSpawnSystems);
        
        // Movement 
        m_MovementStartSystems.Add(m_world.GetECSWorld().CreateManager<UpdateTeleportation>(m_world));
        CharacterBehaviours.CreateMovementStartSystems(m_world,m_MovementStartSystems);
        CharacterBehaviours.CreateMovementResolveSystems(m_world,m_MovementResolveSystems);
        
        // Ability
        CharacterBehaviours.CreateAbilityStartSystems(m_world,m_AbilityStartSystems);
        CharacterBehaviours.CreateAbilityResolveSystems(m_world,m_AbilityResolveSystems);

        m_UpdateCharPresentationState = m_world.GetECSWorld().CreateManager<UpdateCharPresentationState>(m_world);
        m_ApplyPresentationStateToCharacters = m_world.GetECSWorld().CreateManager<ApplyPresentationStateToCharacters>(m_world);
        m_ApplyPresentationStateToItems = m_world.GetECSWorld().CreateManager<ApplyPresentationStateToItems>(m_world);

        
        
        m_CharacterLateUpdate = m_world.GetECSWorld().CreateManager<CharacterLateUpdate>(m_world);
        
        m_HandleDamage = m_world.GetECSWorld().CreateManager<HandleDamage>(m_world);
        
        m_updateCharacterUI = m_world.GetECSWorld().CreateManager<UpdateCharacterUI>(m_world);
        m_characterCameraSystem = m_world.GetECSWorld().CreateManager<UpdateCharacterCamera>(m_world);
        
        m_characterItemLateUpdate = m_world.GetECSWorld().CreateManager<CharacterItemLateUpdate>(m_world);
        m_characterItem1PLateUpdate = m_world.GetECSWorld().CreateManager<CharacterItem1PLateUpdate>(m_world);
        
            
        m_HandleCharacterEvents = m_world.GetECSWorld().CreateManager<HandleCharacterEvents>();
            
        // Preload all character resources (until we have better streaming solution)
        var charRegistry = resourceSystem.GetResourceRegistry<CharacterTypeRegistry>();
        for (var i = 0; i < charRegistry.entries.Length; i++)
        {
            resourceSystem.LoadSingleAssetResource(charRegistry.entries[i].prefab1P.guid);
            resourceSystem.LoadSingleAssetResource(charRegistry.entries[i].prefabClient.guid);
        }
        var itemRegistry = resourceSystem.GetResourceRegistry<ItemRegistry>();
        for (var i = 0; i < itemRegistry.entries.Length; i++)
        {
            resourceSystem.LoadSingleAssetResource(itemRegistry.entries[i].prefab1P.guid);
            resourceSystem.LoadSingleAssetResource(itemRegistry.entries[i].prefabClient.guid);
        }
        var heroTypeRegistry = resourceSystem.GetResourceRegistry<HeroTypeRegistry>();
        for (var i = 0; i < heroTypeRegistry.entries.Length; i++)
        {
            for(var j=0;j<heroTypeRegistry.entries[i].abilities.Length;j++)
                resourceSystem.LoadSingleAssetResource(heroTypeRegistry.entries[i].abilities[j].guid);
        }
        

        Console.AddCommand("thirdperson", CmdToggleThirdperson, "Toggle third person mode", this.GetHashCode());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        
        foreach (var system in m_LateUpdateSystems)
            m_world.GetECSWorld().DestroyManager(system);
        
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterSpawnRequests);
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterDepawnRequests);

        m_world.GetECSWorld().DestroyManager(m_UpdateCharPresentationState);
        m_world.GetECSWorld().DestroyManager(m_CharacterLateUpdate);

        m_world.GetECSWorld().DestroyManager(m_HandleDamage);
        m_world.GetECSWorld().DestroyManager(m_updateCharacterUI);
        m_world.GetECSWorld().DestroyManager(m_characterCameraSystem);

        m_world.GetECSWorld().DestroyManager(m_characterItemLateUpdate);

        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationStateToCharacters);
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationStateToItems);
        
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterEvents);
        
        Console.RemoveCommandsWithTag(GetHashCode());
    }

    public void HandleSpawnRequests()
    {
        m_HandleCharacterDepawnRequests.Update();
        m_HandleCharacterSpawnRequests.Update();
    }
    
    public void HandleDamage() 
    {
        m_HandleDamage.Update();
    }
    
    
    public void UpdatePresentation()
    {
        m_UpdateCharPresentationState.Update();
        m_ApplyPresentationStateToCharacters.Update();      
        m_ApplyPresentationStateToItems.Update();
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
        m_characterCameraSystem.Update();

        m_CharacterLateUpdate.Update();
        m_characterItemLateUpdate.Update();
        m_characterItem1PLateUpdate.Update();
    }
    
    void CmdToggleThirdperson(string[] args)
    {
        m_characterCameraSystem.ToggleFOrceThirdPerson();
    }
    

    
    readonly HandleCharacterSpawnRequests m_HandleCharacterSpawnRequests;
    readonly HandleCharacterDespawnRequests m_HandleCharacterDepawnRequests;

    

    readonly List<ScriptBehaviourManager> m_LateUpdateSystems = new List<ScriptBehaviourManager>();
    
   
    readonly UpdateCharPresentationState m_UpdateCharPresentationState;

    readonly ApplyPresentationStateToCharacters m_ApplyPresentationStateToCharacters;
    readonly ApplyPresentationStateToItems m_ApplyPresentationStateToItems;

    
    readonly HandleDamage m_HandleDamage;
    readonly UpdateCharacterUI m_updateCharacterUI;
    readonly UpdateCharacterCamera m_characterCameraSystem;

    readonly CharacterItemLateUpdate m_characterItemLateUpdate;
    readonly CharacterItem1PLateUpdate m_characterItem1PLateUpdate;

    readonly CharacterLateUpdate m_CharacterLateUpdate;
        

    readonly HandleCharacterEvents m_HandleCharacterEvents;
}
