using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;


#if UNITY_EDITOR 
                       

[DisableAutoCreation]
public class StoreStateHistory : ComponentSystem
{
    public struct Characters
    {
        public ComponentDataArray<ServerEntity> serverBehaviors;
        public ComponentArray<CharacterPredictedState> characters;
        public ComponentArray<CharacterPresentation> presentations;
    }

    [Inject]
    public Characters Group;

    public StoreStateHistory(GameWorld gameWorld)
    {
        m_world = gameWorld;
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample("StoreStateHistory.Update");
        for (var i = 0; i < Group.characters.Length; i++)
        {
            // Only store data from full ticks
            if (m_world.worldTime.tickDuration != m_world.worldTime.tickInterval)
                return;

            StateHistory.SetPredictedState(Group.characters[i], m_world.worldTime.tick, ref Group.characters[i].State);
        }
        Profiler.EndSample();
    }
    
    readonly GameWorld m_world;
}
#endif            



[DisableAutoCreation]
public class CharacterLateUpdate : ComponentSystem
{
    public struct CharacterGroupType
    {
        [ReadOnly] public EntityArray entities;
        [ReadOnly] public ComponentArray<Character> characters;
    }
    
    public struct Character1PGroupType
    {
        [ReadOnly] public EntityArray entities;
        [ReadOnly] public ComponentArray<Character1P> char1Ps;
    }

    [Inject]
    public CharacterGroupType CharGroup;

    [Inject]
    public Character1PGroupType Char1PGroup;

    public CharacterLateUpdate(GameWorld gameWorld)
    {}

    protected override void OnUpdate()
    {
        for (var i = 0; i < CharGroup.characters.Length; i++)
        {
            var character = CharGroup.characters[i];
            var entity = CharGroup.entities[i];
            
            // Update visibility
            character.SetVisible(character.isVisible);
            if (EntityManager.HasComponent<CharacterEvents>(entity))
            {
                var footsteps = EntityManager.GetComponentObject<CharacterEvents>(entity);
                footsteps.active = character.isVisible;
            }
            
            // Update nameplate
            if (EntityManager.HasComponent<NamePlateOwner>(entity))
            {
                var predictedState = EntityManager.GetComponentObject<CharacterPredictedState>(entity); 
                
                var namePlateOwner = EntityManager.GetComponentObject<NamePlateOwner>(entity);
                var healthState = EntityManager.GetComponentObject<HealthState>(entity);
                namePlateOwner.text = character.characterName;
                namePlateOwner.team = predictedState.teamId;
                namePlateOwner.health = healthState.health;
                namePlateOwner.visible = healthState.health > 0;
            }
            
#if UNITY_EDITOR
            if (character.weaponBoneDebug != null)
            {
                var animState = EntityManager.GetComponentData<CharAnimState>(entity);
                var lookDir = Quaternion.Euler(new Vector3(-animState.aimPitch , animState.aimYaw, 0)) * Vector3.down;
                var weaponPos = character.weaponBoneDebug.position;
                var weaponRot = character.weaponBoneDebug.rotation;
                Debug.DrawLine(weaponPos, weaponPos + lookDir, Color.magenta);                
                var aimDir = weaponRot * Quaternion.Euler(character.weaponOffsetDebug) * Vector3.down;
                Debug.DrawLine(weaponPos, weaponPos + aimDir, Color.green);                
            }
#endif
        }
        
        for (var i = 0; i < Char1PGroup.char1Ps.Length; i++)
        {
            var char1P = Char1PGroup.char1Ps[i];
            var entity = Char1PGroup.entities[i];
                        
            // Update visibility
            char1P.SetVisible(char1P.isVisible);
            if (EntityManager.HasComponent<CharacterEvents>(entity))
            {
                var footsteps = EntityManager.GetComponentObject<CharacterEvents>(entity);
                footsteps.active = char1P.isVisible;
            }
        }  
    }
}



class CharacterModuleClient : CharacterModuleShared
{
    public CharacterModuleClient(GameWorld world, BundledResourceManager resourceSystem) : base(world)
    {
        // Handle controlled entity change        
        CharacterBehaviours.CreateControlledEntityChangedSystems(m_world, m_ControlledEntityChangedSystems);
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<UpdateCharacter1PSpawn>(m_world, resourceSystem));
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<PlayerCharacterControlSystem>(m_world));

        // Handle spawn
        CharacterBehaviours.CreateHandleSpawnSystems(m_world, m_HandleSpawnSystems, resourceSystem);
       
        // Handle despawn
        CharacterBehaviours.CreateHandleDespawnSystems(m_world, m_HandleSpawnSystems);
        
        // Movement
        CharacterBehaviours.CreateMovementStartSystems(m_world,m_MovementStartSystems);
        CharacterBehaviours.CreateMovementResolveSystems(m_world,m_MovementResolveSystems);
        
        // Abilities
        CharacterBehaviours.CreateAbilityStartSystems(m_world,m_AbilityStartSystems);
        CharacterBehaviours.CreateAbilityResolveSystems(m_world,m_AbilityResolveSystems);

        // Rollback
        CharacterBehaviours.CreateRollbackSystems(m_world,m_RollbackSystems);
        m_RollbackSystems.Add(m_world.GetECSWorld().CreateManager<CharacterRollback>(m_world));

        // Interpolation        
        m_InterpolateSystems.Add(world.GetECSWorld().CreateManager<ReplicatedAbilityInterpolate>(world));
        m_InterpolatePresentationState = m_world.GetECSWorld().CreateManager<InterpolatePresentationState>(m_world);
        
        m_UpdateCharPresentationState = m_world.GetECSWorld().CreateManager<UpdateCharPresentationState>(m_world);
        m_ApplyPresentationStateToCharacters = m_world.GetECSWorld().CreateManager<ApplyPresentationStateToCharacters>(m_world);
        m_ApplyPresentationStateToItems = m_world.GetECSWorld().CreateManager<ApplyPresentationStateToItems>(m_world);
        m_CharacterLateUpdate = m_world.GetECSWorld().CreateManager<CharacterLateUpdate>(m_world);
        m_characterItemLateUpdate = m_world.GetECSWorld().CreateManager<CharacterItemLateUpdate>(m_world);
        m_characterItem1PLateUpdate = m_world.GetECSWorld().CreateManager<CharacterItem1PLateUpdate>(m_world);
        m_updateCharacterUI = m_world.GetECSWorld().CreateManager<UpdateCharacterUI>(m_world);
        characterCameraSystem = m_world.GetECSWorld().CreateManager<UpdateCharacterCamera>(m_world);
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
        
        foreach (var system in m_RollbackSystems)
            m_world.GetECSWorld().DestroyManager(system);
        
        foreach (var system in m_InterpolateSystems)
            m_world.GetECSWorld().DestroyManager(system);
        foreach (var system in m_LateUpdateSystems)
            m_world.GetECSWorld().DestroyManager(system);
        
        m_world.GetECSWorld().DestroyManager(m_InterpolatePresentationState);
        m_world.GetECSWorld().DestroyManager(m_UpdateCharPresentationState);
        
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationStateToCharacters);
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationStateToItems);

        m_world.GetECSWorld().DestroyManager(m_CharacterLateUpdate);
            
        m_world.GetECSWorld().DestroyManager(m_characterItemLateUpdate);
        m_world.GetECSWorld().DestroyManager(m_characterItem1PLateUpdate);
        m_world.GetECSWorld().DestroyManager(m_updateCharacterUI);
        m_world.GetECSWorld().DestroyManager(characterCameraSystem);
        
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterEvents);

        Console.RemoveCommandsWithTag(this.GetHashCode());
    }

    
    public void Interpolate()
    {
        m_InterpolatePresentationState.Update();
        
        foreach (var system in m_InterpolateSystems)
            system.Update();
    }

    public void Rollback()
    {
        foreach (var system in m_RollbackSystems)
            system.Update();
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
        characterCameraSystem.Update();
        
        m_CharacterLateUpdate.Update();
        m_characterItemLateUpdate.Update();
        m_characterItem1PLateUpdate.Update();
    }
    
    void CmdToggleThirdperson(string[] args)
    {
        characterCameraSystem.ToggleFOrceThirdPerson();
    }
   
    readonly List<ScriptBehaviourManager> m_RollbackSystems = new List<ScriptBehaviourManager>();

    readonly List<ScriptBehaviourManager> m_InterpolateSystems = new List<ScriptBehaviourManager>();
    readonly List<ScriptBehaviourManager> m_LateUpdateSystems = new List<ScriptBehaviourManager>();
    

    
    readonly InterpolatePresentationState m_InterpolatePresentationState;
    readonly UpdateCharPresentationState m_UpdateCharPresentationState;
    readonly ApplyPresentationStateToCharacters m_ApplyPresentationStateToCharacters;
    readonly ApplyPresentationStateToItems m_ApplyPresentationStateToItems;

    readonly CharacterLateUpdate m_CharacterLateUpdate;
    
    readonly CharacterItemLateUpdate m_characterItemLateUpdate;
    readonly CharacterItem1PLateUpdate m_characterItem1PLateUpdate;
    readonly UpdateCharacterUI m_updateCharacterUI;
    readonly UpdateCharacterCamera characterCameraSystem;
    
    readonly HandleCharacterEvents m_HandleCharacterEvents;

    
}
