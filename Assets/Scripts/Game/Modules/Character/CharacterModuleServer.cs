using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

public struct CharacterSpawnRequest : IComponentData
{
    public int characterType;
    public Vector3 position;
    public Quaternion rotation;
    public Entity playerEntity;

    private CharacterSpawnRequest(int characterType, Vector3 position, Quaternion rotation, Entity playerEntity)
    {
        this.characterType = characterType;
        this.position = position;
        this.rotation = rotation;
        this.playerEntity = playerEntity;
    }
    
    public static void Create(EntityCommandBuffer commandBuffer, int characterType, Vector3 position, Quaternion rotation, Entity playerEntity)
    {
        var data = new CharacterSpawnRequest(characterType, position, rotation, playerEntity);
        commandBuffer.CreateEntity();
        commandBuffer.AddComponent(data);
    }
}

public struct CharacterDespawnRequest : IComponentData
{
    public Entity characterEntity;
    
    public static void Create(GameWorld world, Entity characterEntity)
    {
        var data = new CharacterDespawnRequest()
        {
            characterEntity = characterEntity,
        };
        var entity = world.GetEntityManager().CreateEntity(typeof(CharacterDespawnRequest));
        world.GetEntityManager().SetComponentData(entity, data);
    }
    
    public static void Create(EntityCommandBuffer commandBuffer, Entity characterEntity)
    {
        var data = new CharacterDespawnRequest()
        {
            characterEntity = characterEntity,
        };
        commandBuffer.CreateEntity();
        commandBuffer.AddComponent(data);
    }
}

[DisableAutoCreation]
public class HandleCharacterSpawnRequests : BaseComponentSystem
{
    ComponentGroup SpawnGroup;
    CharacterModuleSettings m_settings;
    
    public HandleCharacterSpawnRequests(GameWorld world, BundledResourceManager resourceManager, bool isServer) : base(world)
    {
        m_ResourceManager = resourceManager;
        m_settings = Resources.Load<CharacterModuleSettings>("CharacterModuleSettings");
    }
    
    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        SpawnGroup = GetComponentGroup(typeof(CharacterSpawnRequest));
    }

    protected override void OnDestroyManager()
    {
        base.OnDestroyManager();
        Resources.UnloadAsset(m_settings);
    }

    protected override void OnUpdate()
    {
        var requestArray = SpawnGroup.GetComponentDataArray<CharacterSpawnRequest>();
        if (requestArray.Length == 0)
            return;

        var requestEntityArray = SpawnGroup.GetEntityArray();
        
        // Copy requests as spawning will invalidate Group
        var spawnRequests = new CharacterSpawnRequest[requestArray.Length];
        for (var i = 0; i < requestArray.Length; i++)
        {
            spawnRequests[i] = requestArray[i];
            PostUpdateCommands.DestroyEntity(requestEntityArray[i]);
        }

        for(var i =0;i<spawnRequests.Length;i++)
        {
            var request = spawnRequests[i];
            var playerState = EntityManager.GetComponentObject<PlayerState>(request.playerEntity);
            var character = SpawnCharacter(m_world, playerState, request.position, request.rotation, request.characterType, m_ResourceManager);
            playerState.controlledEntity = character.gameObject.GetComponent<GameObjectEntity>().Entity; 
        }
    }

    List<Entity> abilityList = new List<Entity>(16);
    public Character SpawnCharacter(GameWorld world, PlayerState owner, Vector3 position, Quaternion rotation, 
        int heroIndex, BundledResourceManager resourceSystem)
    {
        var heroTypeRegistry = resourceSystem.GetResourceRegistry<HeroTypeRegistry>();

        heroIndex = Mathf.Min(heroIndex, heroTypeRegistry.entries.Count);
        var heroTypeAsset = heroTypeRegistry.entries[heroIndex];

        var charPrefabGUID = m_settings.characterPrefab.guid;
        
        var charResource = resourceSystem.LoadSingleAssetResource(charPrefabGUID);
        
        if(charResource == null)
        {
            GameDebug.LogError("BundledResourceManager Cant find resource with guid:" + charPrefabGUID);
            return null;
        }

        var charPrefab = (GameObject)charResource;
        var charGameObjectEntity = world.Spawn<GameObjectEntity>(charPrefab);
        charGameObjectEntity.name = string.Format("{0}_{1}",charPrefab.name,owner.playerName);
        GameDebug.Log("Spawning character:" + charGameObjectEntity.name + " tick:" + m_world.worldTime.tick);
        var charEntity = charGameObjectEntity.Entity;

        // Set as predicted by owner
        var replicatedEntity = EntityManager.GetComponentObject<ReplicatedEntity>(charEntity);
        replicatedEntity.predictingPlayerId = owner.playerId;

        var character = EntityManager.GetComponentObject<Character>(charEntity);
        character.heroTypeIndex = heroIndex;
        character.teamId = 0;
        character.TeleportTo(position, rotation);
        character.behaviourController = heroTypeAsset.behaviorsController.Create(EntityManager, owner.playerId);

        // TODO (mogensh) support multiple weapons (for now we just take first)
        if(heroTypeAsset.items.Length > 0)
        {
            character.item = heroTypeAsset.items[0].itemType.Create(EntityManager, owner.playerId);
            var itemState = EntityManager.GetComponentData<ItemTypeDefinition.State>(character.item);
            itemState.character = charEntity;
            EntityManager.SetComponentData(character.item,itemState);
        }
        
        return character;
    }

    readonly BundledResourceManager m_ResourceManager;
}


[DisableAutoCreation]
public class HandleCharacterDespawnRequests : BaseComponentSystem
{
    ComponentGroup DespawnGroup;
//    ComponentGroup ItemGroup;

    public HandleCharacterDespawnRequests(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        DespawnGroup = GetComponentGroup(typeof(CharacterDespawnRequest));
//        ItemGroup = GetComponentGroup(typeof(CharacterItem));
    }
    
    protected override void OnUpdate()
    {
        var requestArray = DespawnGroup.GetComponentDataArray<CharacterDespawnRequest>();
        if (requestArray.Length == 0)
            return;
        
        Profiler.BeginSample("HandleCharacterDespawnRequests");

        var requestEntityArray = DespawnGroup.GetEntityArray();
        
        for (var i = 0; i < requestArray.Length; i++)
        {
            var request = requestArray[i];
            
            var character = EntityManager
                .GetComponentObject<Character>(request.characterEntity);
            GameDebug.Assert(character != null,"Character despawn requst entity is not a character");

            GameDebug.Log("Despawning character:" + character.name + " tick:" + m_world.worldTime.tick);
            
            m_world.RequestDespawn(character.gameObject, PostUpdateCommands);

            
            
            m_world.RequestDespawn(PostUpdateCommands, character.behaviourController);
            m_world.RequestDespawn(PostUpdateCommands, character.item);
            
            PostUpdateCommands.DestroyEntity(requestEntityArray[i]);
        }
        
        Profiler.EndSample();
    }
}

[DisableAutoCreation]
public class HandleDamage : BaseComponentSystem<HealthState,HitCollisionOwner>
{
    public HandleDamage(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Update(Entity entity, HealthState healthState, HitCollisionOwner hitCollisionOwner)
    {
        if (healthState.health <= 0)
            return;

        var isDamaged = false;
        var impulseVec = Vector3.zero;
        var damage = 0.0f;
        var damageVec = Vector3.zero;


        // Apply hitcollider damage events
        var damageEvents = hitCollisionOwner.damageEvents;
        for (var eventIndex=0;eventIndex < damageEvents.Count; eventIndex++)
        {
            isDamaged = true;

            var damageEvent = damageEvents[eventIndex];

            //GameDebug.Log(string.Format("ApplyDamage. Target:{0} Instigator:{1} Dam:{2}", healthState.name, m_world.GetGameObjectFromEntity(damageEvent.instigator), damageEvent.damage ));
            healthState.ApplyDamage(ref damageEvent, m_world.worldTime.tick);

            impulseVec += damageEvent.direction * damageEvent.impulse;
            damageVec += damageEvent.direction * damageEvent.damage;
            damage += damageEvent.damage;

            //damageHistory.ApplyDamage(ref damageEvent, m_world.worldTime.tick);

            if (damageEvents[eventIndex].instigator != Entity.Null && EntityManager.Exists(damageEvent.instigator) && EntityManager.HasComponent<DamageHistory>(damageEvent.instigator))
            {
                var instigatorDamageHistory = EntityManager.GetComponentObject<DamageHistory>(damageEvent.instigator);
                if (m_world.worldTime.tick > instigatorDamageHistory.inflictedDamage.tick)
                {
                    instigatorDamageHistory.inflictedDamage.tick = m_world.worldTime.tick;
                    instigatorDamageHistory.inflictedDamage.lethal = false;
                }
                instigatorDamageHistory.inflictedDamage.lethal |= healthState.health <= 0;
            }

            hitCollisionOwner.collisionEnabled = healthState.health > 0;
        }
        hitCollisionOwner.damageEvents.Clear();

        if (isDamaged)
        {
            var damageImpulse = impulseVec.magnitude;
            var damageDir = damageImpulse > 0 ? impulseVec.normalized : damageVec.normalized;
            
            var charPredictedState = EntityManager.GetComponentData<CharPredictedStateData>(entity);
            charPredictedState.damageTick = m_world.worldTime.tick;
            charPredictedState.damageDirection = damageDir;
            charPredictedState.damageImpulse = damageImpulse;
            EntityManager.SetComponentData(entity, charPredictedState);

            if (healthState.health <= 0)
            {
                var ragdollState =  EntityManager.GetComponentObject<RagdollState>(entity);
                ragdollState.ragdollActive = true;
                ragdollState.impulse = impulseVec;
            }
        }
     
    }
}



public class CharacterModuleServer : CharacterModuleShared
{
    public CharacterModuleServer(GameWorld world, BundledResourceManager resourceSystem): base(world)
    {
        // Handle spawn requests
        m_HandleCharacterSpawnRequests = m_world.GetECSWorld().CreateManager<HandleCharacterSpawnRequests>(m_world, resourceSystem, true);
        m_HandleCharacterDespawnRequests = m_world.GetECSWorld().CreateManager<HandleCharacterDespawnRequests>(m_world);

        // Handle controlled entity changed
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<PlayerCharacterControlSystem>(m_world));

        // Handle spawn
        CharacterBehaviours.CreateHandleSpawnSystems(m_world, m_HandleSpawnSystems, resourceSystem, true);

        // Handle despawn
        CharacterBehaviours.CreateHandleDespawnSystems(m_world, m_HandleDespawnSystems);
        
        // Movement
        m_MovementStartSystems.Add(m_world.GetECSWorld().CreateManager<UpdateTeleportation>(m_world));
        CharacterBehaviours.CreateMovementStartSystems(m_world,m_MovementStartSystems);
        CharacterBehaviours.CreateMovementResolveSystems(m_world,m_MovementResolveSystems);

        // Ability 
        CharacterBehaviours.CreateAbilityStartSystems(m_world,m_AbilityStartSystems);
        CharacterBehaviours.CreateAbilityResolveSystems(m_world,m_AbilityResolveSystems);
        
        
        
        m_UpdateCharPresentationState = m_world.GetECSWorld().CreateManager<UpdateCharPresentationState>(m_world);
        m_ApplyPresentationState = m_world.GetECSWorld().CreateManager<ApplyPresentationState>(m_world);

        
        m_HandleDamage = m_world.GetECSWorld().CreateManager<HandleDamage>(m_world);
        m_UpdatePresentationRootTransform = m_world.GetECSWorld().CreateManager<UpdatePresentationRootTransform>(m_world);
        m_UpdatePresentationAttachmentTransform = m_world.GetECSWorld().CreateManager<UpdatePresentationAttachmentTransform>(m_world);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterDespawnRequests);
        
        m_world.GetECSWorld().DestroyManager(m_UpdateCharPresentationState);

        m_world.GetECSWorld().DestroyManager(m_HandleDamage);
        m_world.GetECSWorld().DestroyManager(m_UpdatePresentationRootTransform);
        m_world.GetECSWorld().DestroyManager(m_UpdatePresentationAttachmentTransform);
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationState);
    }

    public void HandleSpawnRequests()
    {
        m_HandleCharacterDespawnRequests.Update();
        m_HandleCharacterSpawnRequests.Update();
    }

    public void HandleDamage()
    {
        m_HandleDamage.Update();
    }
    
    public void PresentationUpdate()
    {
        m_UpdateCharPresentationState.Update();
        m_ApplyPresentationState.Update();
    }

    public void AttachmentUpdate()
    {
        m_UpdatePresentationRootTransform.Update();    
        m_UpdatePresentationAttachmentTransform.Update();
    }
    
    public void CleanupPlayer(PlayerState player)
    {
        if (player.controlledEntity != Entity.Null)
        {
            CharacterDespawnRequest.Create(m_world, player.controlledEntity);
        }
    }

   

    readonly HandleCharacterSpawnRequests m_HandleCharacterSpawnRequests;
    readonly HandleCharacterDespawnRequests m_HandleCharacterDespawnRequests;

    readonly UpdateCharPresentationState m_UpdateCharPresentationState;
    readonly ApplyPresentationState m_ApplyPresentationState;
    
    readonly HandleDamage m_HandleDamage;
    
    readonly UpdatePresentationRootTransform m_UpdatePresentationRootTransform;
    readonly UpdatePresentationAttachmentTransform m_UpdatePresentationAttachmentTransform;
}
