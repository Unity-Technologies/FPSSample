using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
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
    bool m_isServer;

    public HandleCharacterSpawnRequests(GameWorld world, BundledResourceManager resourceManager, bool isServer) : base(world)
    {
        m_ResourceManager = resourceManager;
        m_isServer = isServer;
    }
    
    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        SpawnGroup = GetComponentGroup(typeof(CharacterSpawnRequest));
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample("HandleCharacterSpawnRequests");
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
        Profiler.EndSample();
    }

    public CharacterPredictedState SpawnCharacter(GameWorld world, PlayerState owner, Vector3 position, Quaternion rotation, int heroIndex, BundledResourceManager resourceSystem)
    {
        var heroTypeRegistry = resourceSystem.GetResourceRegistry<HeroTypeRegistry>();

        heroIndex = Mathf.Min(heroIndex, heroTypeRegistry.entries.Length);
        var heroTypeAsset = heroTypeRegistry.entries[heroIndex];

        var registry = resourceSystem.GetResourceRegistry<CharacterTypeRegistry>();
        var charTypeIndex = registry.FindIndexByRigistryId(heroTypeAsset.character.registryId);
        var characterType = registry.entries[charTypeIndex];

        var charPrefabGUID = m_isServer ? characterType.prefabServer.guid : characterType.prefabClient.guid;
        var charResource = resourceSystem.LoadSingleAssetResource(charPrefabGUID);
        
        if(charResource == null)
        {
            GameDebug.LogError("BundledResourceManager Cant find resource with guid:" + registry.entries[charTypeIndex].prefabServer.guid);
            return null;
        }

        var charPrefab = (GameObject)charResource;
        var charGameObjectEntity = world.Spawn<GameObjectEntity>(charPrefab, position, rotation);
        charGameObjectEntity.name = string.Format("{0}_{1}",charPrefab,owner.name);
        GameDebug.Log("Spawned character:" + charGameObjectEntity.name);
        var charEntity = charGameObjectEntity.Entity;
        
        var character = EntityManager.GetComponentObject<Character>(charEntity);
        character.heroTypeIndex = heroIndex;

        var abilityController = EntityManager.GetComponentObject<AbilityController>(charEntity);
        for (var i = 0; i < heroTypeAsset.abilities.Length; i++)
        {
            GameDebug.Assert(heroTypeAsset.abilities[i] != null,"HeroTypeAsset:{0} has null ability in index:{1}", heroTypeAsset,i);
            var abilityPrefab = resourceSystem.LoadSingleAssetResource(heroTypeAsset.abilities[i].guid) as GameObject;
            abilityController.abilityEntities[i] = world.Spawn<GameObjectEntity>(abilityPrefab).Entity;
            
            var replicated =  EntityManager.GetComponentObject<ReplicatedEntity>(abilityController.abilityEntities[i]);
            replicated.predictingPlayerId = owner.playerId;
        }
        
        var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(charEntity);
        EntityManager.AddComponentData(charGameObjectEntity.Entity, new ServerEntity());

        var replicatedEntity = EntityManager.GetComponentObject<ReplicatedEntity>(charGameObjectEntity.Entity);
        replicatedEntity.predictingPlayerId = owner.playerId;
        
        // Create items
        var itemCount = heroTypeAsset.items.Length;
        for (var i=0;i< itemCount; i++)
        {
            GameDebug.Assert(heroTypeAsset.items[i] != null,"HeroTypeAsset:{0} has null item in index:{1}",heroTypeAsset,i);
            
            var itemRegistry = resourceSystem.GetResourceRegistry<ItemRegistry>();
            var index = itemRegistry.GetIndexByRegistryId(heroTypeAsset.items[i].itemType.registryId);
            
            var itemPrefabGUID = m_isServer ? itemRegistry.entries[index].prefabServer.guid : itemRegistry.entries[index].prefabClient.guid;
            var resource = resourceSystem.LoadSingleAssetResource(itemPrefabGUID);
            GameDebug.Assert(resource != null);

            var prefab = resource as GameObject;
            GameDebug.Assert(prefab != null);

            var itemGameObjectEntity = world.Spawn<GameObjectEntity>(prefab);
            var item = EntityManager.GetComponentObject<CharacterItem>(itemGameObjectEntity.Entity);
            
            world.GetEntityManager().AddComponentData(itemGameObjectEntity.Entity, new ServerEntity());

            var r = EntityManager.GetComponentObject<ReplicatedEntity>(itemGameObjectEntity.Entity);
            r.predictingPlayerId = owner.playerId;
            
            GameDebug.Assert(item != null);
            item.character = charGameObjectEntity.Entity;

//            var puppetOwner = EntityManager.GetComponentObject<PuppetOwner>(itemEntity);
//            puppetOwner.puppetId = characterSetup.items[i].itemType.puppet.registryId;
        }


        charPredictedState.State.position = position;
        charPredictedState.teamId = 0;

        var userCommandComponent = charPredictedState.GetComponent<UserCommandComponent>();
        userCommandComponent.ResetCommand(world.worldTime.tick, rotation.eulerAngles.y, 90);            
            
        return charPredictedState;
    }

    readonly BundledResourceManager m_ResourceManager;
}


[DisableAutoCreation]
public class HandleCharacterDespawnRequests : BaseComponentSystem
{
    ComponentGroup DespawnGroup;
    ComponentGroup ItemGroup;

    public HandleCharacterDespawnRequests(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        DespawnGroup = GetComponentGroup(typeof(CharacterDespawnRequest));
        ItemGroup = GetComponentGroup(typeof(CharacterItem));
    }
    
    protected override void OnUpdate()
    {
        var requestArray = DespawnGroup.GetComponentDataArray<CharacterDespawnRequest>();
        if (requestArray.Length == 0)
            return;
        
        Profiler.BeginSample("HandleCharacterDespawnRequests");

        var itemArray = ItemGroup.GetComponentArray<CharacterItem>();
        var requestEntityArray = DespawnGroup.GetEntityArray();
        
        for (var i = 0; i < requestArray.Length; i++)
        {
            var request = requestArray[i];
            
            var character = EntityManager
                .GetComponentObject<CharacterPredictedState>(request.characterEntity);
            GameDebug.Assert(character != null,"Character despawn requst entity is not a character");

            //GameDebug.Log(string.Format("HandleCharacterDespawnRequests. Requst despawn:{0}", character));

            // Delete abilities
            var abilityController = EntityManager.GetComponentObject<AbilityController>(request.characterEntity);
            foreach (var ability in abilityController.abilityEntities)
            {
                if (ability == Entity.Null)
                    continue;

                var gameObjectEntity = EntityManager.GetComponentObject<ReplicatedAbility>(ability);
                m_world.RequestDespawn(gameObjectEntity.gameObject, PostUpdateCommands);
            }
            
            m_world.RequestDespawn(character.gameObject, PostUpdateCommands);
            
            for(var j=0;j<itemArray.Length;j++)
            {
                var item = itemArray[j];
                if (item.character != request.characterEntity)
                    continue;
                       
                m_world.RequestDespawn(item.gameObject, PostUpdateCommands);
            }
            
            PostUpdateCommands.DestroyEntity(requestEntityArray[i]);
        }
        
        Profiler.EndSample();
    }
}

[DisableAutoCreation]
public class HandleCharacterSpawn : InitializeComponentSystem<Character>
{
    List<Character> characters = new List<Character>();

    public HandleCharacterSpawn(GameWorld gameWorld, BundledResourceManager resourceManager) : base(gameWorld)
    {
        m_resourceManager = resourceManager;
    }

    protected override void Initialize(Entity entity, Character character)
    {
        var charEntity = character.GetComponent<GameObjectEntity>().Entity;
            
        var heroTypeRegistry = m_resourceManager.GetResourceRegistry<HeroTypeRegistry>();
        var heroTypeAsset = heroTypeRegistry.entries[character.heroTypeIndex];
        character.heroTypeData = heroTypeAsset;
        
        var characterTypeRegistry = m_resourceManager.GetResourceRegistry<CharacterTypeRegistry>();
        var characterTypeAsset = characterTypeRegistry.GetEntryById(heroTypeAsset.character.registryId);

        // Setup health
        var healthState = EntityManager.GetComponentObject<HealthState>(charEntity);
        healthState.SetMaxHealth(heroTypeAsset.health);
            
        // Setup CharacterMoveQuery
        var moveQuery = EntityManager.GetComponentObject<CharacterMoveQuery>(charEntity);
        var hitCollisionOwner = EntityManager.GetComponentObject<HitCollisionOwner>(charEntity);
        moveQuery.Initialize(characterTypeAsset.characterMovementSettings, hitCollisionOwner);

        // Setup HitCollisionHistory
        var hitCollisionSettings = EntityManager.GetComponentObject<HitCollisionHistory>(charEntity);
        hitCollisionSettings.settings = characterTypeAsset.hitCollisionSettings;

        
        
        character.eyeHeight = characterTypeAsset.eyeHeight; 
    }

    BundledResourceManager m_resourceManager;
}

[DisableAutoCreation]
public class HandleCharacterDespawn : DeinitializeComponentSystem<Character>
{
    List<Character> characters = new List<Character>();

    public HandleCharacterDespawn(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Deinitialize(Entity entity, Character character)
    {
        var charEntity = character.GetComponent<GameObjectEntity>().Entity;
            
        var moveQuery = EntityManager.GetComponentObject<CharacterMoveQuery>(charEntity);
        moveQuery.Shutdown();
    }
}


[DisableAutoCreation]
public class UpdateTeleportation : BaseComponentSystem<CharacterPredictedState>
{
    public UpdateTeleportation(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Update(Entity entity, CharacterPredictedState charPredictedState)
    {
        if (charPredictedState.m_TeleportPending)
        {
            charPredictedState.m_TeleportPending = false;
            charPredictedState.State.position = charPredictedState.m_TeleportToPosition;
            charPredictedState.State.velocity = charPredictedState.m_TeleportToRotation * Vector3.forward * charPredictedState.State.velocity.magnitude;
            charPredictedState.transform.position = charPredictedState.m_TeleportToPosition;
       
            var userCommandComponent = charPredictedState.GetComponent<UserCommandComponent>();
            userCommandComponent.ResetCommand(m_world.worldTime.tick, charPredictedState.m_TeleportToRotation.eulerAngles.y, 90);            
        }
    }
}

[DisableAutoCreation]
public class UpdateCharPresentationState : BaseComponentSystem
{
    public struct Characters
    {
        public EntityArray entities;
        [ReadOnly] public ComponentDataArray<ServerEntity> serverBehaviors;
        [ReadOnly] public ComponentArray<CharacterPredictedState> characters;
        public ComponentDataArray<CharAnimState> animStates;
        [ReadOnly] public ComponentArray<UserCommandComponent> commands;
        [ReadOnly] public ComponentArray<AnimStateController> animStateCtrls;
    }

    ComponentGroup Group;

    public UpdateCharPresentationState(GameWorld gameWorld) : base(gameWorld)
    {}
    
    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(ServerEntity), typeof(CharacterPredictedState), typeof(CharAnimState),
            typeof(UserCommandComponent), typeof(AnimStateController));
    }


    protected override void OnUpdate()
    {
        
        var entityArray = Group.GetEntityArray();
        var charPredictedStateArray = Group.GetComponentArray<CharacterPredictedState>();
        var charAnimStateArray = Group.GetComponentDataArray<CharAnimState>();
        var userCommandArray = Group.GetComponentArray<UserCommandComponent>();
        var animStateCtrlArray = Group.GetComponentArray<AnimStateController>();

        var deltaTime = m_world.frameDuration;
        for (var i = 0; i < charPredictedStateArray.Length; i++)
        {
            var entity = entityArray[i];
            var charPredictedState = charPredictedStateArray[i];
            var animState = charAnimStateArray[i];
            var userCommand = userCommandArray[i].command;
            var animStateCtrl = animStateCtrlArray[i];
            
            animState.position = charPredictedState.State.position;
            animState.previousCharLocoState = animState.charLocoState;
            animState.charLocoState = charPredictedState.State.locoState;
            animState.charLocoTick = charPredictedState.State.locoStartTick;
            animState.sprinting = charPredictedState.State.sprinting ? 1 : 0;
            animState.charAction = charPredictedState.State.action;
            animState.charActionTick = charPredictedState.State.actionStartTick;
            animState.aimYaw = userCommand.lookYaw;
            animState.aimPitch = userCommand.lookPitch;
    
            var groundMoveVec = Vector3.ProjectOnPlane(charPredictedState.State.velocity, Vector3.up);
            animState.moveYaw = Vector3.Angle(Vector3.forward, groundMoveVec);
            var cross = Vector3.Cross(Vector3.forward, groundMoveVec);
            if (cross.y < 0)
                animState.moveYaw = 360 - animState.moveYaw;
            
            animState.damageTick = charPredictedState.State.damageTick;
            var damageDirOnPlane = Vector3.ProjectOnPlane(charPredictedState.State.damageDirection, Vector3.up);
            animState.damageDirection = Vector3.SignedAngle(Vector3.forward, damageDirOnPlane, Vector3.up);

            
            // Set anim state before anim state ctrl is running 
            EntityManager.SetComponentData(entity, animState);

            // Update presentationstate animstatecontroller
            animStateCtrl.UpdatePresentationState(m_world.worldTime, deltaTime);
        }
    }
}


[DisableAutoCreation]
public class GroundTest : BaseComponentSystem
{
    ComponentGroup Group;

    public GroundTest(GameWorld gameWorld) : base(gameWorld)
    {
        m_defaultLayer = LayerMask.NameToLayer("Default");
        m_playerLayer = LayerMask.NameToLayer("collision_player");
        m_platformLayer = LayerMask.NameToLayer("Platform");

        m_mask = 1 << m_defaultLayer | 1 << m_playerLayer | 1 << m_platformLayer;
    }

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(ServerEntity), typeof(CharacterPredictedState));
    }

    protected override void OnUpdate()
    {        
        var charPredictedStateArray = Group.GetComponentArray<CharacterPredictedState>();
        
        var startOffset = 1f;
        var distance = 3f;

        var rayCommands = new NativeArray<RaycastCommand>(charPredictedStateArray.Length, Allocator.TempJob);
        var rayResults = new NativeArray<RaycastHit>(charPredictedStateArray.Length, Allocator.TempJob);

        for (var i = 0; i < charPredictedStateArray.Length; i++)
        {
            var charPredictedState = charPredictedStateArray[i];
            var origin = charPredictedState.State.position + Vector3.up * startOffset;
            rayCommands[i] = new RaycastCommand(origin, Vector3.down, distance, m_mask);
        }

        var handle = RaycastCommand.ScheduleBatch(rayCommands, rayResults, 10);
        handle.Complete();

        for (var i = 0; i < charPredictedStateArray.Length; i++)
        {
            var charPredictedState = charPredictedStateArray[i];
            charPredictedState.groundCollider = rayResults[i].collider;
            charPredictedState.altitude = charPredictedState.groundCollider != null ? rayResults[i].distance - startOffset : distance - startOffset;

            if (charPredictedState.groundCollider != null)
                charPredictedState.groundNormal = rayResults[i].normal;
        }
        
        rayCommands.Dispose();
        rayResults.Dispose();
    }
    
    readonly int m_defaultLayer;
    readonly int m_playerLayer;
    readonly int m_platformLayer;
    readonly int m_mask;
}


[DisableAutoCreation]
public class ApplyPresentationStateToCharacters : BaseComponentSystem    
{
    struct CharGroupType
    {
        public EntityArray entities;
        public ComponentArray<AnimStateController> animStateControllers;
        public ComponentDataArray<CharAnimState> charAnimState;
    }
    
    struct Char1PGroupType
    {
        public EntityArray entities;
        public ComponentArray<Character1P> character1Ps;
        public ComponentArray<AnimStateController> animStateControllers;
//        public ComponentDataArray<CharacterAnimState> charAnimState;
    }

    ComponentGroup CharGroup;
    ComponentGroup Char1PGroup;

    public ApplyPresentationStateToCharacters(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        CharGroup = GetComponentGroup(typeof(AnimStateController), typeof(CharAnimState), typeof(Character));
        Char1PGroup = GetComponentGroup(typeof(AnimStateController), typeof(Character1P));
    }

    protected override void OnUpdate()
    {
        var deltaTime = m_world.frameDuration;
        // 3P character
        {
            var entityArray = CharGroup.GetEntityArray();
            var animStateCtrlArray = CharGroup.GetComponentArray<AnimStateController>();
            var charAnimStateArray = CharGroup.GetComponentDataArray<CharAnimState>();
            for (var i = 0; i < entityArray.Length; i++)
            {
                var entity = entityArray[i];
                var animStateCtrl = animStateCtrlArray[i];
                var animState = charAnimStateArray[i];
            
                animStateCtrl.ApplyPresentationState(m_world.worldTime, deltaTime);

                // Update transformation
                animStateCtrl.transform.position = animState.position;
                animStateCtrl.transform.rotation =  Quaternion.Euler(0f, animState.rotation, 0f);
            }
        }
        
        // 1P character
        {
            var entityArray = Char1PGroup.GetEntityArray();
            var animStateCtrlArray = Char1PGroup.GetComponentArray<AnimStateController>();
            var char1PArray = Char1PGroup.GetComponentArray<Character1P>();
            for (var i = 0; i < entityArray.Length; i++)
            {
                var character1P = char1PArray[i];

                if (!EntityManager.Exists(character1P.character))    
                    continue;

                var entity = entityArray[i];
                var animStateCtrl = animStateCtrlArray[i];
            
                var animState = EntityManager.GetComponentData<CharAnimState>(character1P.character);
            
                var character = EntityManager.GetComponentObject<Character>(character1P.character);
                var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(character1P.character);
                var userCmd = EntityManager.GetComponentObject<UserCommandComponent>(character1P.character);
            
                EntityManager.SetComponentData(entity, animState);

                animStateCtrl.ApplyPresentationState(m_world.worldTime, deltaTime);

                // 1P character position is not update here as it is dependent on animation result (camera location)
                var eyePos = charPredictedState.State.position + Vector3.up*character.eyeHeight;
                character1P.transform.position = eyePos;
                character1P.transform.rotation = userCmd.command.lookRotation;
            }  
        }
    }

}



