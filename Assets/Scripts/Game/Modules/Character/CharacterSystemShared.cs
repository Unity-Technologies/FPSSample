using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;


[DisableAutoCreation]
public class HandleCharacterSpawn : InitializeComponentGroupSystem<Character, HandleCharacterSpawn.Initialized>
{
    public struct Initialized : IComponentData {}
    
    List<Character> characters = new List<Character>();
    bool server;
    public HandleCharacterSpawn(GameWorld gameWorld, BundledResourceManager resourceManager, bool server) : base(gameWorld)
    {
        m_resourceManager = resourceManager;
        this.server = server;
    }

    List<Entity> entities = new List<Entity>(16); 
    List<INetSerialized> netSerializables = new List<INetSerialized>(16);
    List<IPredictedDataHandler> netPredicted = new List<IPredictedDataHandler>(16); 
    List<IInterpolatedDataHandler> netInterpolated = new List<IInterpolatedDataHandler>(16);
    
    private List<Entity> entityBuffer = new List<Entity>(8);
    private List<Character> characterBuffer = new List<Character>(8);
    protected override void Initialize(ref ComponentGroup group)
    {
        // We are not allowed to spawn prefabs while iterating ComponentGroup so we copy list of entities and characters.
        entityBuffer.Clear();
        characterBuffer.Clear();
        {
            var entityArray = group.GetEntityArray();
            var characterArray = group.GetComponentArray<Character>();
            for (var i = 0; i < entityArray.Length; i++)
            {
                entityBuffer.Add(entityArray[i]);
                characterBuffer.Add(characterArray[i]);
            }
        }

        for (var i = 0; i < entityBuffer.Count; i++)
        {
            var charEntity = entityBuffer[i];
            var character = characterBuffer[i];
            
            var heroTypeRegistry = m_resourceManager.GetResourceRegistry<HeroTypeRegistry>();
            var heroTypeAsset = heroTypeRegistry.entries[character.heroTypeIndex];
            character.heroTypeData = heroTypeAsset;
        
            var characterTypeAsset = heroTypeAsset.character;
            
            // Create main presentation
            var charPrefabGUID = server ? characterTypeAsset.prefabServer.guid : characterTypeAsset.prefabClient.guid;
            var charPrefab = m_resourceManager.LoadSingleAssetResource(charPrefabGUID) as GameObject;
            var presentationGOE = m_world.Spawn<GameObjectEntity>(charPrefab);
            var charPresentationEntity = presentationGOE.Entity;

            character.presentation = charPresentationEntity;

            var charPresentation = EntityManager.GetComponentObject<CharPresentation>(charPresentationEntity);
            charPresentation.character = charEntity;
            character.presentations.Add(charPresentation);
            
            // Setup health
            var healthState = EntityManager.GetComponentObject<HealthState>(charEntity);
            healthState.SetMaxHealth(heroTypeAsset.health);
        
            // Setup CharacterMoveQuery
            var moveQuery = EntityManager.GetComponentObject<CharacterMoveQuery>(charEntity);
            var hitCollisionOwner = EntityManager.GetComponentObject<HitCollisionOwner>(charEntity);
            moveQuery.Initialize(heroTypeAsset.characterMovementSettings, hitCollisionOwner);

            // Setup HitCollisionHistory
            var hitCollisionSettings = EntityManager.GetComponentObject<HitCollisionHistory>(charPresentationEntity);
            hitCollisionSettings.hitCollisionOwner = charEntity;
    
            character.eyeHeight = heroTypeAsset.eyeHeight; 
            
            // Setup abilities
            GameDebug.Assert(EntityManager.Exists(character.behaviourController),"behavior controller entity does not exist");
            GameDebug.Assert(EntityManager.HasComponent<CharBehaviour>(character.behaviourController),"behavior controller entity does not have CharBehaviour component");
            var charBehaviour = EntityManager.GetComponentData<CharBehaviour>(character.behaviourController);
            charBehaviour.character = charEntity;
            EntityManager.SetComponentData(character.behaviourController, charBehaviour);
            var buffer = EntityManager.GetBuffer<EntityGroupChildren>(character.behaviourController);
            for (int j = 0; j < buffer.Length; j++)
            {
                var childEntity = buffer[j].entity;
                if (EntityManager.HasComponent<CharBehaviour>(childEntity))
                {
                    charBehaviour = EntityManager.GetComponentData<CharBehaviour>(childEntity);
                    charBehaviour.character = charEntity;
                    EntityManager.SetComponentData(childEntity, charBehaviour);
                }
            }            

            
            // Create items
            foreach (var itemEntry in heroTypeAsset.items)
            {
                var itemPrefabGuid = server ? itemEntry.itemType.prefabServer.guid : itemEntry.itemType.prefabClient.guid;

                if (itemPrefabGuid == null || itemPrefabGuid == "")
                    continue;
                
                var itemPrefab = m_resourceManager.LoadSingleAssetResource(itemPrefabGuid) as GameObject;
                var itemGOE = m_world.Spawn<GameObjectEntity>(itemPrefab);

                var itemCharPresentation = EntityManager.GetComponentObject<CharPresentation>(itemGOE.Entity);
                itemCharPresentation.character = charEntity;
                itemCharPresentation.attachToPresentation = charPresentationEntity; 
                character.presentations.Add(itemCharPresentation);
            }
        }
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

        // Remove presentations
        foreach (var charPresentation in character.presentations)
        {
            m_world.RequestDespawn(charPresentation.gameObject, PostUpdateCommands);
        }
    }
}


[DisableAutoCreation]
public class UpdateTeleportation : BaseComponentSystem<Character>
{
    public UpdateTeleportation(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Update(Entity entity, Character character)
    {
        if (character.m_TeleportPending)
        {
            character.m_TeleportPending = false;

            var predictedState = EntityManager.GetComponentData<CharPredictedStateData>(entity);
            predictedState.position = character.m_TeleportToPosition;
            predictedState.velocity = character.m_TeleportToRotation * Vector3.forward * predictedState.velocity.magnitude;
            EntityManager.SetComponentData(entity, predictedState);
            
//            character.transform.position = character.m_TeleportToPosition;
       
            var userCommandComponent = character.GetComponent<UserCommandComponent>();
            userCommandComponent.ResetCommand(m_world.worldTime.tick, character.m_TeleportToRotation.eulerAngles.y, 90);            
        }
    }
}

[DisableAutoCreation]
public class UpdateCharPresentationState : BaseComponentSystem
{
    ComponentGroup Group;
    const float k_StopMovePenalty = 0.075f;
        
    public UpdateCharPresentationState(GameWorld gameWorld) : base(gameWorld)
    {}
    
    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(ServerEntity), typeof(Character), typeof(CharPredictedStateData), typeof(PresentationState),
            typeof(UserCommandComponent));
    }


    protected override void OnUpdate()
    {
        var entityArray = Group.GetEntityArray();
        var characterArray = Group.GetComponentArray<Character>();
        var charPredictedStateArray = Group.GetComponentDataArray<CharPredictedStateData>();
        var charAnimStateArray = Group.GetComponentDataArray<PresentationState>();
        var userCommandArray = Group.GetComponentArray<UserCommandComponent>();

        var deltaTime = m_world.frameDuration;
        for (var i = 0; i < charPredictedStateArray.Length; i++)
        {
            var entity = entityArray[i];
            var character = characterArray[i];
            var charPredictedState = charPredictedStateArray[i];
            var animState = charAnimStateArray[i];
            var userCommand = userCommandArray[i].command;
            
            // TODO: Move this into the network
            animState.position = charPredictedState.position;
            animState.charLocoTick = charPredictedState.locoStartTick;
            animState.sprinting = charPredictedState.sprinting;
            animState.charAction = charPredictedState.action;
            animState.charActionTick = charPredictedState.actionStartTick;
            animState.aimYaw = userCommand.lookYaw;
            animState.aimPitch = userCommand.lookPitch;
            animState.previousCharLocoState = animState.charLocoState;
            
            // Add small buffer between GroundMove and Stand, to reduce animation noise when there are gaps in between
            // input keypresses
            if (charPredictedState.locoState == CharPredictedStateData.LocoState.Stand 
                && animState.charLocoState == CharPredictedStateData.LocoState.GroundMove 
                && m_world.worldTime.DurationSinceTick(animState.lastGroundMoveTick) < k_StopMovePenalty) 
            {
                animState.charLocoState = CharPredictedStateData.LocoState.GroundMove;
            }
            else
            {
                animState.charLocoState = charPredictedState.locoState;
            }
            
            if (charPredictedState.locoState == CharPredictedStateData.LocoState.GroundMove)
            {
                animState.lastGroundMoveTick = m_world.worldTime.tick;
            }
    
            var groundMoveVec = Vector3.ProjectOnPlane(charPredictedState.velocity, Vector3.up);
            animState.moveYaw = Vector3.Angle(Vector3.forward, groundMoveVec);
            var cross = Vector3.Cross(Vector3.forward, groundMoveVec);
            if (cross.y < 0)
                animState.moveYaw = 360 - animState.moveYaw;
            
            animState.damageTick = charPredictedState.damageTick;
            var damageDirOnPlane = Vector3.ProjectOnPlane(charPredictedState.damageDirection, Vector3.up);
            animState.damageDirection = Vector3.SignedAngle(Vector3.forward, damageDirOnPlane, Vector3.up);

            
            // Set anim state before anim state ctrl is running 
            EntityManager.SetComponentData(entity, animState);

            // TODO (mogensh) perhaps we should not call presentation, but make system that updates presentation (and reads anim state) 
            // Update presentationstate animstatecontroller
            var animStateCtrl = EntityManager.GetComponentObject<AnimStateController>(character.presentation);    
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

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(ServerEntity), typeof(Character), typeof(CharPredictedStateData));
    }

    protected override void OnUpdate()
    {        
        var charPredictedStateArray = Group.GetComponentDataArray<CharPredictedStateData>();
        var characterArray = Group.GetComponentArray<Character>();
        
        var startOffset = 1f;
        var distance = 3f;

        var rayCommands = new NativeArray<RaycastCommand>(charPredictedStateArray.Length, Allocator.TempJob);
        var rayResults = new NativeArray<RaycastHit>(charPredictedStateArray.Length, Allocator.TempJob);

        for (var i = 0; i < charPredictedStateArray.Length; i++)
        {
            var charPredictedState = charPredictedStateArray[i];
            var origin = charPredictedState.position + Vector3.up * startOffset;
            rayCommands[i] = new RaycastCommand(origin, Vector3.down, distance, m_mask);
        }

        var handle = RaycastCommand.ScheduleBatch(rayCommands, rayResults, 10);
        handle.Complete();

        for (var i = 0; i < characterArray.Length; i++)
        {
            var character = characterArray[i];
            character.groundCollider = rayResults[i].collider;
            character.altitude = character.groundCollider != null ? rayResults[i].distance - startOffset : distance - startOffset;

            if (character.groundCollider != null)
                character.groundNormal = rayResults[i].normal;
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
public class ApplyPresentationState : BaseComponentSystem    
{
    ComponentGroup CharGroup;

    public ApplyPresentationState(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        CharGroup = GetComponentGroup(typeof(AnimStateController), typeof(CharPresentation), ComponentType.Subtractive<DespawningEntity>() );   
    }

    protected override void OnUpdate()
    {
        var deltaTime = m_world.frameDuration;
        var animStateCtrlArray = CharGroup.GetComponentArray<AnimStateController>();

        for (var i = 0; i < animStateCtrlArray.Length; i++)
        {
            var animStateCtrl = animStateCtrlArray[i];
            animStateCtrl.ApplyPresentationState(m_world.worldTime, deltaTime);
        }
    }

}



