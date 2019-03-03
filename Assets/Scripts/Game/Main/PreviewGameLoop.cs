using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


[DisableAutoCreation]
public class PreviewGameMode : BaseComponentSystem   
{
    public int respawnDelay = 20;
    
    public PreviewGameMode(GameWorld world, PlayerState Player) : base(world)
    {
        m_Player = Player;
        
        // Fallback spawnpos!
        m_SpawnPos = new Vector3(0.0f, 2.0f, 0.0f);
        m_SpawnRot = new Quaternion();
    }

    protected override void OnUpdate()
    {
        var playerEntity = m_Player.gameObject.GetComponent<GameObjectEntity>().Entity;    
        var charControl = m_world.GetEntityManager().GetComponentObject<PlayerCharacterControl>(playerEntity);
        if (charControl.requestedCharacterType != -1 && charControl.characterType != charControl.requestedCharacterType)
        {
            charControl.characterType = charControl.requestedCharacterType;
            charControl.requestedCharacterType = -1;

            GameDebug.Log(string.Format("PreviewGameMode. Respawning as char requested. New chartype:{0}", charControl.characterType));
            
            Spawn(true);
            return;
        }
        
        if (m_Player.controlledEntity == Entity.Null)
        {
            GameDebug.Log(string.Format("PreviewGameMode. Spawning as we have to char. Chartype:{0}", charControl.characterType));
            
            Spawn(false);
            return;
        }

        if (m_world.GetEntityManager().HasComponent<HealthStateData>(m_Player.controlledEntity))
        {
            var healthState = m_world.GetEntityManager().GetComponentData<HealthStateData>(m_Player.controlledEntity);
            if (!m_respawnPending && healthState.health == 0)
            {
                m_respawnPending = true;
                m_respawnTime = Time.time + respawnDelay;
            }
            
            if(m_respawnPending && Time.time > m_respawnTime)
            {
                Spawn(false);
                m_respawnPending = false;
            } 
        }
    }

    void Spawn(bool keepCharPosition)
    {
        var playerEntity = m_Player.gameObject.GetComponent<GameObjectEntity>().Entity;
        var charControl = EntityManager.GetComponentObject<PlayerCharacterControl>(playerEntity);
        
        if (keepCharPosition && m_Player.controlledEntity != Entity.Null && 
            m_world.GetEntityManager().HasComponent<CharacterInterpolatedData>(m_Player.controlledEntity))
        {
            var charPresentationState = m_world.GetEntityManager().GetComponentData<CharacterInterpolatedData>(m_Player.controlledEntity);
            m_SpawnPos = charPresentationState.position;
            m_SpawnRot = Quaternion.Euler(0f, charPresentationState.rotation, 0f);
        } 
        else
            FindSpawnTransform();

        // Despawn old controlled
        if (m_Player.controlledEntity != Entity.Null)
        {
            if (EntityManager.HasComponent<Character>(m_Player.controlledEntity))
            {
                CharacterDespawnRequest.Create(PostUpdateCommands, m_Player.controlledEntity);
            }  
            
            m_Player.controlledEntity = Entity.Null;
        }

        if (charControl.characterType == 1000)
        {
            SpectatorCamSpawnRequest.Create(PostUpdateCommands, m_SpawnPos, m_SpawnRot, playerEntity);
        }
        else
            CharacterSpawnRequest.Create(PostUpdateCommands, charControl.characterType, m_SpawnPos, m_SpawnRot, playerEntity);
    }
    
    void FindSpawnTransform()
    {
        // Find random spawnpoint that matches teamIndex
        var spawnpoints = Object.FindObjectsOfType<SpawnPoint>();
        var offset = UnityEngine.Random.Range(0, spawnpoints.Length);
        for (var i = 0; i < spawnpoints.Length; ++i)
        {
            var sp = spawnpoints[(i + offset) % spawnpoints.Length];
            if (sp.teamIndex != m_Player.teamIndex) continue;
            m_SpawnPos = sp.transform.position;
            m_SpawnRot = sp.transform.rotation;
            return;
        }
    }
    
    PlayerState m_Player;
    Vector3 m_SpawnPos;
    Quaternion m_SpawnRot;

    bool m_respawnPending;
    float m_respawnTime;
}


public class PreviewGameLoop : Game.IGameLoop
{
    public bool Init(string[] args)
    {
        m_StateMachine = new StateMachine<PreviewState>();
        m_StateMachine.Add(PreviewState.Loading, null, UpdateLoadingState, null);
        m_StateMachine.Add(PreviewState.Active, EnterActiveState, UpdateStateActive, LeaveActiveState);

        Console.AddCommand("nextchar", CmdNextHero, "Select next character", GetHashCode());
        Console.AddCommand("nextteam", CmdNextTeam, "Select next character", GetHashCode());
        Console.AddCommand("spectator", CmdSpectatorCam, "Select spectator cam", GetHashCode());
        Console.AddCommand("respawn", CmdRespawn, "Force a respawn. Optional argument defines now many seconds untill respawn", this.GetHashCode());
        
        Console.SetOpen(false);

        m_GameWorld = new GameWorld("World[PreviewGameLoop]");
        
        if (args.Length > 0)
        {
            Game.game.levelManager.LoadLevel(args[0]);
            m_StateMachine.SwitchTo(PreviewState.Loading);
        }
        else
        {
            m_StateMachine.SwitchTo(PreviewState.Active);
        }

        GameDebug.Log("Preview initialized");
        return true;
    }

    public void Shutdown()
    {
        GameDebug.Log("PreviewGameState shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();

        m_PlayerModuleServer.Shutdown();
        
        Game.game.levelManager.UnloadLevel();
        
        m_GameWorld.Shutdown();
    }

    void UpdateLoadingState()
    {
        if (Game.game.levelManager.IsCurrentLevelLoaded())
            m_StateMachine.SwitchTo(PreviewState.Active);
    }

    public void Update()
    {
        m_StateMachine.Update();
    }

    void EnterActiveState()
    {
        m_GameWorld.RegisterSceneEntities();

        m_resourceSystem = new BundledResourceManager(m_GameWorld,"BundledResources/Client");

        // Create serializers so we get errors in preview build
        var dataComponentSerializers = new DataComponentSerializers();

        m_CharacterModule = new CharacterModulePreview(m_GameWorld, m_resourceSystem);
        m_ProjectileModule = new ProjectileModuleClient(m_GameWorld, m_resourceSystem);
        m_HitCollisionModule = new HitCollisionModule(m_GameWorld,1, 2);
        m_PlayerModuleClient = new PlayerModuleClient(m_GameWorld);
        m_PlayerModuleServer = new PlayerModuleServer(m_GameWorld, m_resourceSystem);
        m_SpectatorCamModuleServer = new SpectatorCamModuleServer(m_GameWorld, m_resourceSystem);    
        m_SpectatorCamModuleClient = new SpectatorCamModuleClient(m_GameWorld);
        m_EffectModule = new EffectModuleClient(m_GameWorld, m_resourceSystem);
        m_ItemModule = new ItemModule(m_GameWorld);
        
        m_ragdollModule = new RagdollModule(m_GameWorld);
        
        m_DespawnProjectiles = m_GameWorld.GetECSWorld().CreateManager<DespawnProjectiles>(m_GameWorld);
        m_DamageAreaSystemServer = m_GameWorld.GetECSWorld().CreateManager<DamageAreaSystemServer>(m_GameWorld);
        
        m_TeleporterSystemServer = m_GameWorld.GetECSWorld().CreateManager<TeleporterSystemServer>(m_GameWorld);
        m_TeleporterSystemClient = m_GameWorld.GetECSWorld().CreateManager<TeleporterSystemClient>(m_GameWorld);
            
        m_UpdateDestructableProps = m_GameWorld.GetECSWorld().CreateManager<UpdateDestructableProps>(m_GameWorld);
        m_DestructiblePropSystemClient = m_GameWorld.GetECSWorld().CreateManager<DestructiblePropSystemClient>(m_GameWorld);
        
        m_UpdatePresentationOwners = m_GameWorld.GetECSWorld().CreateManager<UpdatePresentationOwners>(
            m_GameWorld, m_resourceSystem);
        m_HandlePresentationOwnerDespawn = m_GameWorld.GetECSWorld().CreateManager<HandlePresentationOwnerDesawn>(m_GameWorld);
        
        m_HandleGrenadeRequests = m_GameWorld.GetECSWorld().CreateManager<HandleGrenadeRequest>(m_GameWorld,m_resourceSystem);
        m_StartGrenadeMovement = m_GameWorld.GetECSWorld().CreateManager<StartGrenadeMovement>(m_GameWorld);
        m_FinalizeGrenadeMovement = m_GameWorld.GetECSWorld().CreateManager<FinalizeGrenadeMovement>(m_GameWorld);
        m_ApplyGrenadePresentation = m_GameWorld.GetECSWorld().CreateManager<ApplyGrenadePresentation>(m_GameWorld);
        
        m_moverUpdate = m_GameWorld.GetECSWorld().CreateManager<MoverUpdate>(m_GameWorld);
        
        m_SpinSystem = m_GameWorld.GetECSWorld().CreateManager<SpinSystem>(m_GameWorld);
        m_HandleNamePlateOwnerSpawn = m_GameWorld.GetECSWorld().CreateManager<HandleNamePlateSpawn>(m_GameWorld);
        m_HandleNamePlateOwnerDespawn = m_GameWorld.GetECSWorld().CreateManager<HandleNamePlateDespawn>(m_GameWorld);
        m_UpdateNamePlates = m_GameWorld.GetECSWorld().CreateManager<UpdateNamePlates>(m_GameWorld);
        
        m_UpdateReplicatedOwnerFlag = m_GameWorld.GetECSWorld().CreateManager<UpdateReplicatedOwnerFlag>(m_GameWorld);
            

        m_TwistSystem = new TwistSystem(m_GameWorld);
        m_FanSystem = new FanSystem(m_GameWorld);   
        m_TranslateScaleSystem = new TranslateScaleSystem(m_GameWorld);
        
        m_PlayerModuleClient.RegisterLocalPlayer(0, null);


        // Spawn PlayerState, Character and link up LocalPlayer
        m_Player = m_PlayerModuleServer.CreatePlayer(m_GameWorld, 0, "LocalHero", true);
        
        var playerEntity = m_Player.gameObject.GetComponent<GameObjectEntity>().Entity; 
        var charControl = m_GameWorld.GetEntityManager().GetComponentObject<PlayerCharacterControl>(playerEntity);
        charControl.characterType = math.max(Game.characterType.IntValue,0);
        m_Player.teamIndex = 0;

        m_previewGameMode = m_GameWorld.GetECSWorld().CreateManager<PreviewGameMode>(m_GameWorld, m_Player);

        Game.SetMousePointerLock(true);
    }

    void LeaveActiveState()
    {
        m_CharacterModule.Shutdown();
        m_ProjectileModule.Shutdown();
        m_ragdollModule.Shutdown();
        m_HitCollisionModule.Shutdown();
        m_PlayerModuleClient.Shutdown();
        m_PlayerModuleServer.Shutdown();
        m_SpectatorCamModuleServer.Shutdown();
        m_SpectatorCamModuleClient.Shutdown();
        m_EffectModule.Shutdown();
        m_ItemModule.Shutdown();
        
        m_GameWorld.GetECSWorld().DestroyManager(m_DamageAreaSystemServer);
        m_GameWorld.GetECSWorld().DestroyManager(m_DespawnProjectiles);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_TeleporterSystemServer);
        m_GameWorld.GetECSWorld().DestroyManager(m_TeleporterSystemClient);
            
        m_GameWorld.GetECSWorld().DestroyManager(m_UpdateDestructableProps);
        m_GameWorld.GetECSWorld().DestroyManager(m_DestructiblePropSystemClient);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_UpdatePresentationOwners);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandlePresentationOwnerDespawn);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleGrenadeRequests);
        m_GameWorld.GetECSWorld().DestroyManager(m_StartGrenadeMovement);
        m_GameWorld.GetECSWorld().DestroyManager(m_FinalizeGrenadeMovement);
        m_GameWorld.GetECSWorld().DestroyManager(m_ApplyGrenadePresentation);
            
        m_GameWorld.GetECSWorld().DestroyManager(m_moverUpdate);
        m_GameWorld.GetECSWorld().DestroyManager(m_previewGameMode);
        m_GameWorld.GetECSWorld().DestroyManager(m_SpinSystem);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleNamePlateOwnerSpawn);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleNamePlateOwnerDespawn);
        m_GameWorld.GetECSWorld().DestroyManager(m_UpdateNamePlates);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_UpdateReplicatedOwnerFlag);
        
        m_TwistSystem.ShutDown();
        m_FanSystem.ShutDown();
        m_TranslateScaleSystem.ShutDown();

        m_resourceSystem.Shutdown();
    }

    void UpdateStateActive()
    {
        // Sample input
        bool userInputEnabled = Game.GetMousePointerLock();
        m_PlayerModuleClient.SampleInput(userInputEnabled, Time.deltaTime, 0);

        if (gameTime.tickRate != Game.serverTickRate.IntValue)
            gameTime.tickRate = Game.serverTickRate.IntValue;

        if (Game.Input.GetKeyUp(KeyCode.H) && Game.allowCharChange.IntValue == 1)
        {
            CmdNextHero(null);
        }
        if (Game.Input.GetKeyUp(KeyCode.T))
        {
            CmdNextTeam(null);
        }
        
        bool commandWasConsumed = false;
        while (Game.frameTime > m_GameWorld.nextTickTime)
        {
            gameTime.tick++;
            gameTime.tickDuration = gameTime.tickInterval;
            
            commandWasConsumed = true;

            PreviewTickUpdate();
            m_GameWorld.nextTickTime += m_GameWorld.worldTime.tickInterval;
            
        }
        if (commandWasConsumed)
            m_PlayerModuleClient.ResetInput(userInputEnabled);
    }

    
    public void FixedUpdate()
    {
    }

    public void PreviewTickUpdate()
    {
        m_GameWorld.worldTime = gameTime;
        m_GameWorld.frameDuration = gameTime.tickDuration;
            
        m_PlayerModuleClient.ResolveReferenceFromLocalPlayerToPlayer();
        m_PlayerModuleClient.HandleCommandReset();
        m_PlayerModuleClient.StoreCommand(m_GameWorld.worldTime.tick);

        // Game mode update
        m_previewGameMode.Update();

        // Handle spawn requests
        m_CharacterModule.HandleSpawnRequests();
        m_ProjectileModule.HandleProjectileRequests();  
        m_HandleGrenadeRequests.Update();
        
        m_UpdatePresentationOwners.Update();    // Updates game entity presentation. After gameentities are created but before compenent spawn handler
        
        m_UpdateReplicatedOwnerFlag.Update();
        
        // Apply command for frame
        m_PlayerModuleClient.RetrieveCommand(m_GameWorld.worldTime.tick);
        
        // Handle spawn
        m_CharacterModule.HandleSpawns(); ; // TODO (mogensh) creates presentations, so it needs to be done first. Find better solution for ordering
        m_SpectatorCamModuleServer.HandleSpawnRequests();
        m_HitCollisionModule.HandleSpawning();
        m_HandleNamePlateOwnerSpawn.Update();
        m_PlayerModuleClient.HandleSpawn();
        m_ragdollModule.HandleSpawning();
        m_TwistSystem.HandleSpawning();
        m_FanSystem.HandleSpawning();
        m_TranslateScaleSystem.HandleSpawning();
        m_ProjectileModule.HandleProjectileSpawn();
        m_ItemModule.HandleSpawn();

        // Handle controlled entity changed
        m_PlayerModuleClient.HandleControlledEntityChanged();
        m_CharacterModule.HandleControlledEntityChanged();
        
        // Update movement of scene objects. Projectiles and grenades can also start update as they use collision data from last frame
        m_SpinSystem.Update();
        m_moverUpdate.Update();
        m_ProjectileModule.StartPredictedMovement();
        m_StartGrenadeMovement.Update();

        // Update movement of player controlled units (depends on moveable scene objects being done)
        m_SpectatorCamModuleClient.Update();
        m_TeleporterSystemServer.Update();
        m_CharacterModule.AbilityRequestUpdate();
        m_CharacterModule.MovementStart();
        m_CharacterModule.MovementResolve();
        m_CharacterModule.AbilityStart();
        m_CharacterModule.AbilityResolve();


        m_FinalizeGrenadeMovement.Update();
        m_ProjectileModule.FinalizePredictedMovement();
        
        // Handle damage        
        m_HitCollisionModule.HandleSplashDamage();
        m_UpdateDestructableProps.Update();
        m_DamageAreaSystemServer.Update();
        m_CharacterModule.HandleDamage();
        
        // Update presentation
        m_CharacterModule.UpdatePresentation();
        m_DestructiblePropSystemClient.Update();
        m_TeleporterSystemClient.Update(); 
        m_ApplyGrenadePresentation.Update();
        
        // Handle despawns
        m_HandlePresentationOwnerDespawn.Update(); 
        m_CharacterModule.HandleDepawns(); // TODO (mogensh) this destroys presentations and needs to be done first so its picked up. Find better solution  
        m_DespawnProjectiles.Update();
        m_ProjectileModule.HandleProjectileDespawn();
        m_HandleNamePlateOwnerDespawn.Update();
        m_TwistSystem.HandleDespawning();
        m_FanSystem.HandleDespawning();
        m_ragdollModule.HandleDespawning();
        m_HitCollisionModule.HandleDespawn();
        m_TranslateScaleSystem.HandleDepawning();
        m_GameWorld.ProcessDespawns();
    }

    public void LateUpdate()
    {
        // TODO (petera) Should the state machine actually have a lateupdate so we don't have to do this always?
        if (m_StateMachine.CurrentState() == PreviewState.Active)
        {
            m_GameWorld.frameDuration = Time.deltaTime;
            
            
            m_TranslateScaleSystem.Schedule();
            var twistSystemHandle = m_TwistSystem.Schedule();
            m_FanSystem.Schedule(twistSystemHandle);
                
            m_HitCollisionModule.StoreColliderState();
            
            m_CharacterModule.LateUpdate();
            m_ItemModule.LateUpdate();
            m_ragdollModule.LateUpdate();
            
            m_ProjectileModule.UpdateClientProjectilesPredicted();
            m_EffectModule.ClientUpdate();
            
            // Update camera
            m_PlayerModuleClient.CameraUpdate();


            // Update UI
            m_CharacterModule.UpdateUI();
            m_UpdateNamePlates.Update();

            // Finalize jobs that needs to be done before rendering
            m_TranslateScaleSystem.Complete();
            m_FanSystem.Complete();
        }
    }
    
    void CmdNextHero(string[] args)
    {
        if (m_Player == null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;
        
        var charSetupRegistry = m_resourceSystem.GetResourceRegistry<HeroTypeRegistry>();
        var charSetupCount = charSetupRegistry.entries.Count;

        var playerEntity = m_Player.gameObject.GetComponent<GameObjectEntity>().Entity; 
        var charControl = m_GameWorld.GetEntityManager().GetComponentObject<PlayerCharacterControl>(playerEntity);

        charControl.requestedCharacterType = charControl.characterType + 1;
        if (charControl.requestedCharacterType >= charSetupCount)    
            charControl.requestedCharacterType = 0;
        
        GameDebug.Log(string.Format("PreviewGameLoop. Requesting char:{0}", charControl.requestedCharacterType));
    }
    
    void CmdSpectatorCam(string[] args)
    {
        if (m_Player == null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;
 
        var playerEntity = m_Player.gameObject.GetComponent<GameObjectEntity>().Entity; 
        var charControl = m_GameWorld.GetEntityManager().GetComponentObject<PlayerCharacterControl>(playerEntity);

        // Until we have better way of controlling other units than character, the spectator cam gets type 1000         
        charControl.requestedCharacterType = 1000;    
    }

    void CmdRespawn(string[] args)
    {
        if (m_Player == null)
            return;

        m_previewGameMode.respawnDelay = args.Length == 0 ? 3 : int.Parse(args[0]);
        
        var healthState = m_GameWorld.GetEntityManager().GetComponentData<HealthStateData>(m_Player.controlledEntity);
        healthState.health = 0;
        m_GameWorld.GetEntityManager().SetComponentData(m_Player.controlledEntity, healthState);
    }
    

    void CmdNextTeam(string[] args)
    {
        if (m_Player == null)
            return;

        m_Player.teamIndex++;
        if (m_Player.teamIndex > 1)
            m_Player.teamIndex = 0;
    }
    
    enum PreviewState
    {
        Loading,
        Active
    }
    StateMachine<PreviewState> m_StateMachine;

    BundledResourceManager m_resourceSystem;

    GameWorld m_GameWorld;
    CharacterModulePreview m_CharacterModule;
    ProjectileModuleClient m_ProjectileModule;
    HitCollisionModule m_HitCollisionModule;
    PlayerModuleClient m_PlayerModuleClient;
    PlayerModuleServer m_PlayerModuleServer;
    SpectatorCamModuleServer m_SpectatorCamModuleServer;
    SpectatorCamModuleClient m_SpectatorCamModuleClient;
    EffectModuleClient m_EffectModule;
    ItemModule m_ItemModule;
    UpdateReplicatedOwnerFlag m_UpdateReplicatedOwnerFlag;

    RagdollModule m_ragdollModule;
    SpinSystem m_SpinSystem;
    DespawnProjectiles m_DespawnProjectiles;
    
    PreviewGameMode m_previewGameMode;
    DamageAreaSystemServer m_DamageAreaSystemServer;
    
    TeleporterSystemServer m_TeleporterSystemServer;
    TeleporterSystemClient m_TeleporterSystemClient;

    HandlePresentationOwnerDesawn m_HandlePresentationOwnerDespawn;
    UpdatePresentationOwners m_UpdatePresentationOwners;

    HandleGrenadeRequest m_HandleGrenadeRequests;
    StartGrenadeMovement m_StartGrenadeMovement;
    FinalizeGrenadeMovement m_FinalizeGrenadeMovement;
    ApplyGrenadePresentation m_ApplyGrenadePresentation;
    
    HandleNamePlateSpawn m_HandleNamePlateOwnerSpawn;
    HandleNamePlateDespawn m_HandleNamePlateOwnerDespawn;
    UpdateNamePlates m_UpdateNamePlates;

    MoverUpdate m_moverUpdate;
    DestructiblePropSystemClient m_DestructiblePropSystemClient;
    UpdateDestructableProps m_UpdateDestructableProps;
   
    TwistSystem m_TwistSystem;
    FanSystem m_FanSystem;
    TranslateScaleSystem m_TranslateScaleSystem;
    
    PlayerState m_Player;

    GameTime gameTime = new GameTime(60);
}
