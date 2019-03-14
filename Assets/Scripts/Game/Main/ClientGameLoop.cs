using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using UnityEngine.Ucg.Matchmaking;

public class ClientGameWorld
{
    
    public bool PredictionEnabled = true;

    public float frameTimeScale = 1.0f;


    public GameTime PredictedTime
    {
        get { return m_PredictedTime; }
    }

    public GameTime RenderTime
    {
        get { return m_RenderTime; }
    }

    public ReplicatedEntityModuleClient ReplicatedEntityModule
    {
        get { return m_ReplicatedEntityModule;  }
    }
    

    public ClientGameWorld(GameWorld world, NetworkClient networkClient, NetworkStatisticsClient networkStatistics, BundledResourceManager resourceSystem)
    {
        m_NetworkClient = networkClient;          
        m_NetworkStatistics = networkStatistics;

        m_GameWorld = world;
        
        m_CharacterModule = new CharacterModuleClient(m_GameWorld, resourceSystem);
        m_ProjectileModule = new ProjectileModuleClient(m_GameWorld, resourceSystem);
        m_HitCollisionModule = new HitCollisionModule(m_GameWorld,1, 1);
        m_PlayerModule = new PlayerModuleClient(m_GameWorld);
        m_SpectatorCamModule = new SpectatorCamModuleClient(m_GameWorld);
        m_EffectModule = new EffectModuleClient(m_GameWorld, resourceSystem);
        m_ReplicatedEntityModule = new ReplicatedEntityModuleClient(m_GameWorld, resourceSystem);
        m_ItemModule = new ItemModule(m_GameWorld);
        m_ragdollSystem = new RagdollModule(m_GameWorld);

        m_GameModeSystem = m_GameWorld.GetECSWorld().CreateManager<GameModeSystemClient>(m_GameWorld);

        m_ClientFrontendUpdate = m_GameWorld.GetECSWorld().CreateManager<ClientFrontendUpdate>(m_GameWorld);
        
        m_DestructiblePropSystemClient = m_GameWorld.GetECSWorld().CreateManager<DestructiblePropSystemClient>(m_GameWorld);
        
        m_ApplyGrenadePresentation = m_GameWorld.GetECSWorld().CreateManager<ApplyGrenadePresentation>(m_GameWorld);
        
        m_UpdatePresentationOwners = m_GameWorld.GetECSWorld().CreateManager<UpdatePresentationOwners>(
            m_GameWorld, resourceSystem);
        m_HandlePresentationOwnerDespawn = m_GameWorld.GetECSWorld().CreateManager<HandlePresentationOwnerDesawn>(m_GameWorld);
        
        m_moverUpdate = m_GameWorld.GetECSWorld().CreateManager<MoverUpdate>(m_GameWorld);
        
        m_TeleporterSystemClient = m_GameWorld.GetECSWorld().CreateManager<TeleporterSystemClient>(m_GameWorld);
            
        m_SpinSystem = m_GameWorld.GetECSWorld().CreateManager<SpinSystem>(m_GameWorld);
        
        m_HandleNamePlateOwnerSpawn = m_GameWorld.GetECSWorld().CreateManager<HandleNamePlateSpawn>(m_GameWorld);
        m_HandleNamePlateOwnerDespawn = m_GameWorld.GetECSWorld().CreateManager<HandleNamePlateDespawn>(m_GameWorld);
        m_UpdateNamePlates = m_GameWorld.GetECSWorld().CreateManager<UpdateNamePlates>(m_GameWorld);
        
        m_GameModeSystem.SetLocalPlayerId(m_NetworkClient.clientId);
    
        m_TwistSystem = new TwistSystem(m_GameWorld);
        m_FanSystem = new FanSystem(m_GameWorld);   
        m_TranslateScaleSystem = new TranslateScaleSystem(m_GameWorld);
    }

    public void Shutdown()
    {
        m_CharacterModule.Shutdown();
        m_ProjectileModule.Shutdown();
        m_HitCollisionModule.Shutdown();
        m_PlayerModule.Shutdown();
        m_SpectatorCamModule.Shutdown();
        m_EffectModule.Shutdown();
        m_ReplicatedEntityModule.Shutdown();
        m_ItemModule.Shutdown();

        m_GameWorld.GetECSWorld().DestroyManager(m_GameModeSystem);
        m_GameWorld.GetECSWorld().DestroyManager(m_DestructiblePropSystemClient);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_ApplyGrenadePresentation);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_UpdatePresentationOwners);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandlePresentationOwnerDespawn);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_moverUpdate);
        
        m_GameWorld.GetECSWorld().DestroyManager(m_TeleporterSystemClient);
        m_GameWorld.GetECSWorld().DestroyManager(m_SpinSystem);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleNamePlateOwnerSpawn);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleNamePlateOwnerDespawn);
        m_GameWorld.GetECSWorld().DestroyManager(m_UpdateNamePlates);

        m_ragdollSystem.Shutdown();
        
        m_TwistSystem.ShutDown();
        m_FanSystem.ShutDown();
        m_TranslateScaleSystem.ShutDown();
    }

   
    // This is called at the actual client frame rate, so may be faster or slower than tickrate.
    public void Update(float frameDuration)
    {
        // Advances time and accumulate input into the UserCommand being generated
        HandleTime(frameDuration);
        m_GameWorld.worldTime = m_RenderTime;
        m_GameWorld.frameDuration = frameDuration;
        m_GameWorld.lastServerTick = m_NetworkClient.serverTime;

        m_PlayerModule.ResolveReferenceFromLocalPlayerToPlayer();
        m_PlayerModule.HandleCommandReset();
        m_ReplicatedEntityModule.UpdateControlledEntityFlags();
        
        
        // Handle spawn requests
        m_ProjectileModule.HandleProjectileRequests();   
        m_UpdatePresentationOwners.Update(); 

        // Handle spawning  
        m_CharacterModule.HandleSpawns();
        m_ProjectileModule.HandleProjectileSpawn();    
        m_HitCollisionModule.HandleSpawning();
        m_HandleNamePlateOwnerSpawn.Update();
        m_ragdollSystem.HandleSpawning();
        m_TwistSystem.HandleSpawning();
        m_FanSystem.HandleSpawning();
        m_TranslateScaleSystem.HandleSpawning();
        m_PlayerModule.HandleSpawn();
        m_ItemModule.HandleSpawn();
        
        // Handle controlled entity changed
        m_PlayerModule.HandleControlledEntityChanged();
        m_CharacterModule.HandleControlledEntityChanged();
        
        // Update movement of scene objects. Projectiles and grenades can also start update as they use collision data from last frame
        m_SpinSystem.Update();
        m_moverUpdate.Update();
        m_CharacterModule.Interpolate();
        m_ReplicatedEntityModule.Interpolate(m_RenderTime);

        // Prediction
        m_GameWorld.worldTime = m_PredictedTime;
        m_ProjectileModule.StartPredictedMovement();       
        
        if (IsPredictionAllowed())
        {
            // ROLLBACK. All predicted entities (with the ServerEntity component) are rolled back to last server state 
            m_GameWorld.worldTime.SetTime(m_NetworkClient.serverTime, m_PredictedTime.tickInterval);
            PredictionRollback();

            
            // PREDICT PREVIOUS TICKS. Replay every tick *after* the last tick we have from server up to the last stored command we have
            for (var tick = m_NetworkClient.serverTime + 1; tick < m_PredictedTime.tick; tick++)
            {
                m_GameWorld.worldTime.SetTime(tick, m_PredictedTime.tickInterval);
                m_PlayerModule.RetrieveCommand(m_GameWorld.worldTime.tick);
                PredictionUpdate();
#if UNITY_EDITOR                 
                // We only want to store "full" tick to we use m_PredictedTime.tick-1 (as current can be fraction of tick)
                m_ReplicatedEntityModule.StorePredictedState(tick, m_PredictedTime.tick-1);
#endif                
            }

            // PREDICT CURRENT TICK. Update current tick using duration of current tick
            m_GameWorld.worldTime = m_PredictedTime;
            m_PlayerModule.RetrieveCommand(m_GameWorld.worldTime.tick);
            // Dont update systems with close to zero time. 
            if (m_GameWorld.worldTime.tickDuration > 0.008f) 
            {
                PredictionUpdate();
            }
//#if UNITY_EDITOR                 
//            m_ReplicatedEntityModule.StorePredictedState(m_PredictedTime.tick, m_PredictedTime.tick);
//#endif                
        }
        
        m_ProjectileModule.FinalizePredictedMovement();

        m_GameModeSystem.Update();    
                         
        // Update Presentation
        m_GameWorld.worldTime = m_PredictedTime;
        m_CharacterModule.UpdatePresentation();
        m_DestructiblePropSystemClient.Update();
        m_TeleporterSystemClient.Update();

       
        m_GameWorld.worldTime = m_RenderTime;

        // Handle despawns
        m_HandlePresentationOwnerDespawn.Update(); 
        m_CharacterModule.HandleDepawns(); // TODO (mogensh) this destroys presentations and needs to be done first so its picked up. We need better way of handling destruction ordering
        m_ProjectileModule.HandleProjectileDespawn();
        m_HandleNamePlateOwnerDespawn.Update();
        m_TwistSystem.HandleDespawning();
        m_FanSystem.HandleDespawning();
        m_ragdollSystem.HandleDespawning();
        m_HitCollisionModule.HandleDespawn();
        m_TranslateScaleSystem.HandleDepawning();
        m_GameWorld.ProcessDespawns();  
        
#if UNITY_EDITOR

        if (m_GameWorld.GetEntityManager().Exists(m_localPlayer.controlledEntity) &&
            m_GameWorld.GetEntityManager().HasComponent<UserCommandComponentData>(m_localPlayer.controlledEntity))
        {
            var userCommand = m_GameWorld.GetEntityManager().GetComponentData<UserCommandComponentData>(m_localPlayer.controlledEntity);
            m_ReplicatedEntityModule.FinalizedStateHistory(m_PredictedTime.tick-1, m_NetworkClient.serverTime, ref userCommand.command);
        }
#endif                
        
    }
    
    public void LateUpdate(ChatSystemClient chatSystem, float frameDuration)
    {
        m_GameWorld.worldTime = m_RenderTime;
        m_HitCollisionModule.StoreColliderState();

        m_ragdollSystem.LateUpdate();

        m_TranslateScaleSystem.Schedule();
        var twistSystemHandle = m_TwistSystem.Schedule();
        m_FanSystem.Schedule(twistSystemHandle);

        var teamId = -1;   
        bool showScorePanel = false;
        if (m_localPlayer != null && m_localPlayer.playerState != null && m_localPlayer.playerState.controlledEntity != Entity.Null)
        {
            teamId = m_localPlayer.playerState.teamIndex;

            if (m_GameWorld.GetEntityManager().HasComponent<HealthStateData>(m_localPlayer.playerState.controlledEntity))
            {
                var healthState = m_GameWorld.GetEntityManager()
                    .GetComponentData<HealthStateData>(m_localPlayer.playerState.controlledEntity);
            
                // Only show score board when alive
                showScorePanel = healthState.health <= 0;
            }
        }
        // TODO (petera) fix this hack
        chatSystem.UpdateLocalTeamIndex(teamId);

        
        m_ItemModule.LateUpdate();


        m_CharacterModule.CameraUpdate();
        m_PlayerModule.CameraUpdate();
        
        m_CharacterModule.LateUpdate();
        
        m_GameWorld.worldTime = m_RenderTime;
        m_ProjectileModule.UpdateClientProjectilesNonPredicted();
        
        m_GameWorld.worldTime = m_PredictedTime;
        m_ProjectileModule.UpdateClientProjectilesPredicted();
        
        m_ApplyGrenadePresentation.Update();
        
        m_EffectModule.ClientUpdate();
        
        m_UpdateNamePlates.Update();
        
        if(Game.game.clientFrontend != null)
        {
            m_ClientFrontendUpdate.Update();
            Game.game.clientFrontend.SetShowScorePanel(showScorePanel);
        }

        m_TranslateScaleSystem.Complete();
        m_FanSystem.Complete();
        
    }

    bool IsPredictionAllowed()
    {
        if (!m_PlayerModule.PlayerStateReady)
        {
            GameDebug.Log("No predict! No player state.");
            return false;
        }
        
        if(!m_PlayerModule.IsControllingEntity)
        {
            GameDebug.Log("No predict! No controlled entity.");
            return false;
        }

        if (m_PredictedTime.tick <= m_NetworkClient.serverTime)
        {
            GameDebug.Log("No predict! Predict time not ahead of server tick! " + GetFramePredictInfo());
            return false;
        }

        if (!m_PlayerModule.HasCommands(m_NetworkClient.serverTime + 1, m_PredictedTime.tick))
        {
            GameDebug.Log("No predict! No commands available. " + GetFramePredictInfo());
            return false;
        }

        return true;
    }

    string GetFramePredictInfo()
    {
        int firstCommandTick;
        int lastCommandTick;
        m_PlayerModule.GetBufferedCommandsTick(out firstCommandTick, out lastCommandTick);
        
        return string.Format("Last server:{0} predicted:{1} buffer:{2}->{3} time since snap:{4}  rtt avr:{5}",
            m_NetworkClient.serverTime, m_PredictedTime.tick,
            firstCommandTick, lastCommandTick,
            m_NetworkClient.timeSinceSnapshot,m_NetworkStatistics.rtt.average); 
    }



    public LocalPlayer RegisterLocalPlayer(int playerId)
    {
        m_ReplicatedEntityModule.SetLocalPlayerId(playerId);
        m_localPlayer = m_PlayerModule.RegisterLocalPlayer(playerId, m_NetworkClient);
        return m_localPlayer;
    }
    
    public ISnapshotConsumer GetSnapshotConsumer()
    {
        return m_ReplicatedEntityModule;
    }

    void PredictionRollback()
    {
        m_ReplicatedEntityModule.Rollback();
    }

    void PredictionUpdate()
    {
        m_SpectatorCamModule.Update();

        m_CharacterModule.AbilityRequestUpdate();
        
        m_CharacterModule.MovementStart();
        m_CharacterModule.MovementResolve();
        
        m_CharacterModule.AbilityStart();
        m_CharacterModule.AbilityResolve();
        
    }

    void HandleTime(float frameDuration)
    {
        // Update tick rate (this will only change runtime in test scenarios)
        // TODO (petera) consider use ConfigVars with Server flag for this
        if (m_NetworkClient.serverTickRate != m_PredictedTime.tickRate)
        {
            m_PredictedTime.tickRate = m_NetworkClient.serverTickRate;
            m_RenderTime.tickRate = m_NetworkClient.serverTickRate;
        }

        // Sample input into current command
        //  The time passed in here is used to calculate the amount of rotation from stick position
        //  The command stores final view direction
        bool chatOpen = Game.game.clientFrontend != null && Game.game.clientFrontend.chatPanel.isOpen;
        bool userInputEnabled = Game.GetMousePointerLock() && !chatOpen;
        m_PlayerModule.SampleInput(userInputEnabled, Time.deltaTime, m_RenderTime.tick);


        int prevTick = m_PredictedTime.tick;

        // Increment time
        var deltaPredictedTime = frameDuration * frameTimeScale;
        m_PredictedTime.AddDuration(deltaPredictedTime);

        // Adjust time to be synchronized with server
        int preferredBufferedCommandCount = 2;      
        int preferredTick = m_NetworkClient.serverTime + (int)(((m_NetworkClient.timeSinceSnapshot + m_NetworkStatistics.rtt.average) / 1000.0f) * m_GameWorld.worldTime.tickRate) + preferredBufferedCommandCount;

        bool resetTime = false;
        if (!resetTime && m_PredictedTime.tick < preferredTick - 3)
        {
            GameDebug.Log(string.Format("Client hard catchup ... "));
            resetTime = true;
        }

        if (!resetTime && m_PredictedTime.tick > preferredTick + 6)
        {
            GameDebug.Log(string.Format("Client hard slowdown ... "));
            resetTime = true;
        }

        frameTimeScale = 1.0f;
        if (resetTime)
        {
            GameDebug.Log(string.Format("CATCHUP ({0} -> {1})", m_PredictedTime.tick, preferredTick));

            m_NetworkStatistics.notifyHardCatchup = true;
            m_GameWorld.nextTickTime = Game.frameTime;
            m_PredictedTime.tick = preferredTick;
            m_PredictedTime.SetTime(preferredTick, 0);

        }
        else
        {
            int bufferedCommands = m_NetworkClient.lastAcknowlegdedCommandTime - m_NetworkClient.serverTime;
            if (bufferedCommands < preferredBufferedCommandCount)
                frameTimeScale = 1.01f;

            if (bufferedCommands > preferredBufferedCommandCount)
                frameTimeScale = 0.99f;
        }

        // Increment interpolation time
        m_RenderTime.AddDuration(frameDuration * frameTimeScale);

        // Force interp time to not exeede server time
        if (m_RenderTime.tick >= m_NetworkClient.serverTime)
        {
            m_RenderTime.SetTime(m_NetworkClient.serverTime, 0);
        }

        // hard catchup
        if (m_RenderTime.tick < m_NetworkClient.serverTime - 10)
        {
            m_RenderTime.SetTime(m_NetworkClient.serverTime - 8, 0);
        }

        // Throttle up to catch up
        if (m_RenderTime.tick < m_NetworkClient.serverTime - 1)
        {
            m_RenderTime.AddDuration(frameDuration * 0.01f);
        }

        // If predicted time has entered a new tick the stored commands should be sent to server 
        if (m_PredictedTime.tick > prevTick)
        {
            var oldestCommandToSend = Mathf.Max(prevTick, m_PredictedTime.tick - NetworkConfig.commandClientBufferSize);
            for (int tick = oldestCommandToSend; tick < m_PredictedTime.tick; tick++)
            {
                m_PlayerModule.StoreCommand(tick);
                m_PlayerModule.SendCommand(tick);
            }

            m_PlayerModule.ResetInput(userInputEnabled);
            m_PlayerModule.StoreCommand(m_PredictedTime.tick);
        }

        // Store command
        m_PlayerModule.StoreCommand(m_PredictedTime.tick);
    }

    GameWorld m_GameWorld;
    GameTime m_PredictedTime = new GameTime(60);
    GameTime m_RenderTime = new GameTime(60);
    
    // External systems
    NetworkClient m_NetworkClient;
    NetworkStatisticsClient m_NetworkStatistics;
    ClientFrontendUpdate m_ClientFrontendUpdate;
    
    // Internal systems
    readonly CharacterModuleClient m_CharacterModule;
    readonly ProjectileModuleClient m_ProjectileModule;
    readonly HitCollisionModule m_HitCollisionModule;
    readonly PlayerModuleClient m_PlayerModule;
    readonly SpectatorCamModuleClient m_SpectatorCamModule;
    readonly EffectModuleClient m_EffectModule;
    readonly ReplicatedEntityModuleClient m_ReplicatedEntityModule;
    readonly ItemModule m_ItemModule;
    
    readonly RagdollModule m_ragdollSystem;
    readonly GameModeSystemClient m_GameModeSystem;
    
    readonly ApplyGrenadePresentation m_ApplyGrenadePresentation;
    
    readonly HandlePresentationOwnerDesawn m_HandlePresentationOwnerDespawn;
    readonly UpdatePresentationOwners m_UpdatePresentationOwners;
    
    readonly TwistSystem m_TwistSystem;
    readonly FanSystem m_FanSystem;
    readonly TranslateScaleSystem m_TranslateScaleSystem;

    readonly MoverUpdate m_moverUpdate;
    readonly DestructiblePropSystemClient m_DestructiblePropSystemClient;
    readonly HandleNamePlateSpawn m_HandleNamePlateOwnerSpawn;
    readonly HandleNamePlateDespawn m_HandleNamePlateOwnerDespawn;
    
    readonly UpdateNamePlates m_UpdateNamePlates;
    readonly SpinSystem m_SpinSystem;
    readonly TeleporterSystemClient m_TeleporterSystemClient;
     
    LocalPlayer m_localPlayer;
}


public class ClientGameLoop : Game.IGameLoop, INetworkCallbacks, INetworkClientCallbacks
{

    // Client vars
    [ConfigVar(Name ="client.updaterate", DefaultValue = "30000", Description = "Max bytes/sec client wants to receive", Flags = ConfigVar.Flags.ClientInfo)]
    public static ConfigVar clientUpdateRate;
    [ConfigVar(Name ="client.updateinterval", DefaultValue = "3", Description = "Snapshot sendrate requested by client", Flags = ConfigVar.Flags.ClientInfo)]
    public static ConfigVar clientUpdateInterval;

    [ConfigVar(Name ="client.playername", DefaultValue = "Noname", Description = "Name of player", Flags = ConfigVar.Flags.ClientInfo | ConfigVar.Flags.Save)]
    public static ConfigVar clientPlayerName;

    [ConfigVar(Name = "client.matchmaker", DefaultValue = "0.0.0.0:80", Description = "Address of matchmaker", Flags = ConfigVar.Flags.None)]
    public static ConfigVar clientMatchmaker;

    public bool Init(string[] args)
    {
        m_StateMachine = new StateMachine<ClientState>();
        m_StateMachine.Add(ClientState.Browsing,    EnterBrowsingState,     UpdateBrowsingState,    LeaveBrowsingState);
        m_StateMachine.Add(ClientState.Connecting,  EnterConnectingState,   UpdateConnectingState,  null);
        m_StateMachine.Add(ClientState.Loading,     EnterLoadingState,      UpdateLoadingState,     null);
        m_StateMachine.Add(ClientState.Playing,     EnterPlayingState,      UpdatePlayingState,     LeavePlayingState);

#if UNITY_EDITOR        
        Game.game.levelManager.UnloadLevel();
#endif
        m_GameWorld = new GameWorld("ClientWorld");
        
        m_NetworkTransport = new SocketTransport();
        m_NetworkClient = new NetworkClient(m_NetworkTransport);

        if (Application.isEditor || Game.game.buildId == "AutoBuild")
            NetworkClient.clientVerifyProtocol.Value = "0";

        m_NetworkClient.UpdateClientConfig();
        m_NetworkStatistics = new NetworkStatisticsClient(m_NetworkClient);
        m_ChatSystem = new ChatSystemClient(m_NetworkClient);

        GameDebug.Log("Network client initialized");

        m_requestedPlayerSettings.playerName = clientPlayerName.Value;
        m_requestedPlayerSettings.teamId = -1;
        
        Console.AddCommand("disconnect", CmdDisconnect, "Disconnect from server if connected", this.GetHashCode());
        Console.AddCommand("prediction", CmdTogglePrediction, "Toggle prediction", this.GetHashCode());
        Console.AddCommand("runatserver", CmdRunAtServer, "Run command at server", this.GetHashCode());
        Console.AddCommand("respawn", CmdRespawn, "Force a respawn", this.GetHashCode());
        Console.AddCommand("nextchar", CmdNextChar, "Select next character", this.GetHashCode());
        Console.AddCommand("nextteam", CmdNextTeam, "Select next character", this.GetHashCode());
        Console.AddCommand("spectator", CmdSpectator, "Select spectator cam", this.GetHashCode());
        Console.AddCommand("matchmake", CmdMatchmake, "matchmake <hostname[:port]/{projectid}>: Find and join a server", this.GetHashCode());
        
        if (args.Length > 0)
        {
            targetServer = args[0];
            m_StateMachine.SwitchTo(ClientState.Connecting);
        }
        else
            m_StateMachine.SwitchTo(ClientState.Browsing);

        GameDebug.Log("Client initialized");

        return true;
    }

    public void Shutdown()
    {
        GameDebug.Log("ClientGameLoop shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();

        m_NetworkClient.Shutdown();
        m_NetworkTransport.Shutdown();
        
        m_GameWorld.Shutdown();
    }

    public ClientGameWorld GetClientGameWorld()
    {
        return m_clientWorld;
    }
    
    public void OnConnect(int clientId) { }
    public void OnDisconnect(int clientId) { }

    unsafe public void OnEvent(int clientId, NetworkEvent info)
    {
        Profiler.BeginSample("-ProcessEvent");
        switch ((GameNetworkEvents.EventType)info.type.typeId)
        {
            case GameNetworkEvents.EventType.Chat:
                fixed(uint* data = info.data)
                {
                    var reader = new NetworkReader(data, info.type.schema);
                    m_ChatSystem.ReceiveMessage(reader.ReadString(256));
                }
                break;
        }
        Profiler.EndSample();
    }

    public void OnMapUpdate(ref NetworkReader data)
    {
        m_LevelName = data.ReadString();
        if(m_StateMachine.CurrentState() != ClientState.Loading)
            m_StateMachine.SwitchTo(ClientState.Loading);
    }

    public void Update()
    {
        Profiler.BeginSample("ClientGameLoop.Update");

        Profiler.BeginSample("-NetworkClientUpdate");
        m_NetworkClient.Update(this, m_clientWorld?.GetSnapshotConsumer());
        Profiler.EndSample();

        Profiler.BeginSample("-StateMachine update");
        m_StateMachine.Update();
        Profiler.EndSample();

        // TODO (petera) change if we have a lobby like setup one day
        if(m_StateMachine.CurrentState() == ClientState.Playing && Game.game.clientFrontend != null)
            Game.game.clientFrontend.UpdateChat(m_ChatSystem);

        m_NetworkClient.SendData();

        // TODO (petera) merge with clientinfo 
        if (m_requestedPlayerSettings.playerName != clientPlayerName.Value)
        {
            // Cap name length
            clientPlayerName.Value = clientPlayerName.Value.Substring(0, Mathf.Min(clientPlayerName.Value.Length, 16));
            m_requestedPlayerSettings.playerName = clientPlayerName.Value;
            m_playerSettingsUpdated = true;
        }

        if(m_NetworkClient.isConnected && m_playerSettingsUpdated)
        {
            m_playerSettingsUpdated = false;
            SendPlayerSettings();
        }

        if(m_clientWorld != null)
            m_NetworkStatistics.Update(m_clientWorld.frameTimeScale, GameTime.GetDuration(m_clientWorld.RenderTime, m_clientWorld.PredictedTime));

        Profiler.EndSample();
    }

    void EnterBrowsingState()
    {
        GameDebug.Assert(m_clientWorld == null);
        m_ClientState = ClientState.Browsing;
    }

    void UpdateBrowsingState()
    {
         if (m_useMatchmaking)
         {
             m_matchmaker?.Update();
         }
    }

    void LeaveBrowsingState()
    {
    }

    string targetServer = "";
    int connectRetryCount;
    void EnterConnectingState()
    {
        GameDebug.Assert(m_ClientState == ClientState.Browsing, "Expected ClientState to be browsing");
        GameDebug.Assert(m_clientWorld == null, "Expected ClientWorld to be null");
        GameDebug.Assert(m_NetworkClient.connectionState == NetworkClient.ConnectionState.Disconnected, "Expected network connectionState to be disconnected");

        m_ClientState = ClientState.Connecting;
        connectRetryCount = 0;
    }

    void UpdateConnectingState()
    {
        switch (m_NetworkClient.connectionState)
        {
            case NetworkClient.ConnectionState.Connected:
                m_GameMessage = "Waiting for map info";
                break;
            case NetworkClient.ConnectionState.Connecting:
                // Do nothing; just wait for either success or failure
                break;
            case NetworkClient.ConnectionState.Disconnected:
                if(connectRetryCount < 2)
                {
                    connectRetryCount++;
                    m_GameMessage = string.Format("Trying to connect to {0} (attempt #{1})...", targetServer, connectRetryCount);
                    GameDebug.Log(m_GameMessage);
                    m_NetworkClient.Connect(targetServer);
                }
                else
                {
                    m_GameMessage = "Failed to connect to server";
                    GameDebug.Log(m_GameMessage);
                    m_NetworkClient.Disconnect();
                    m_StateMachine.SwitchTo(ClientState.Browsing);
                }
                break;
        }
    }

    void EnterLoadingState()
    {
        if(Game.game.clientFrontend != null)
            Game.game.clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);

        Console.SetOpen(false);

        GameDebug.Assert(m_clientWorld == null);
        GameDebug.Assert(m_NetworkClient.isConnected);

        m_requestedPlayerSettings.playerName = clientPlayerName.Value;
        m_requestedPlayerSettings.characterType = (short)Game.characterType.IntValue;
        m_playerSettingsUpdated = true;

        m_ClientState = ClientState.Loading;
    }

    void UpdateLoadingState()
    {
        // Handle disconnects
        if (!m_NetworkClient.isConnected)
        {
            m_GameMessage = m_DisconnectReason != null ? string.Format("Disconnected from server ({0})", m_DisconnectReason) : "Disconnected from server (lost connection)";
            m_DisconnectReason = null;
            m_StateMachine.SwitchTo(ClientState.Browsing);
        }

        // Wait until we got level info
        if (m_LevelName == null)
            return;

        // Load if we are not already loading
        var level = Game.game.levelManager.currentLevel;
        if (level == null || level.name != m_LevelName)
        {
            if (!Game.game.levelManager.LoadLevel(m_LevelName))
            {
                m_DisconnectReason = string.Format("could not load requested level '{0}'", m_LevelName);
                m_NetworkClient.Disconnect();
                return;
            }
            level = Game.game.levelManager.currentLevel;
        }

        // Wait for level to be loaded
        if (level.state == LevelState.Loaded)
            m_StateMachine.SwitchTo(ClientState.Playing);
    }

    void EnterPlayingState()
    {
        GameDebug.Assert(m_clientWorld == null && Game.game.levelManager.IsCurrentLevelLoaded());

        m_GameWorld.RegisterSceneEntities();
        
        m_resourceSystem = new BundledResourceManager(m_GameWorld,"BundledResources/Client");

        m_clientWorld = new ClientGameWorld(m_GameWorld, m_NetworkClient, m_NetworkStatistics, m_resourceSystem);
        m_clientWorld.PredictionEnabled = m_predictionEnabled;

        m_LocalPlayer = m_clientWorld.RegisterLocalPlayer(m_NetworkClient.clientId);
        
        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.PlayerReady, true, (ref NetworkWriter data) => {});

        m_ClientState = ClientState.Playing;
    }

    void LeavePlayingState()
    {
        m_resourceSystem.Shutdown();

        m_LocalPlayer = null;
        
        m_clientWorld.Shutdown();
        m_clientWorld = null;

        // TODO (petera) replace this with a stack of levels or similar thing. For now we just load the menu no matter what
        //Game.game.levelManager.UnloadLevel();
        //Game.game.levelManager.LoadLevel("level_menu");
        
        m_resourceSystem.Shutdown();

        m_GameWorld.Shutdown();
        m_GameWorld = new GameWorld("ClientWorld");

        if(Game.game.clientFrontend != null)
        {
            Game.game.clientFrontend.Clear();
            Game.game.clientFrontend.ShowMenu(ClientFrontend.MenuShowing.None);
        }

        Game.game.levelManager.LoadLevel("level_menu");

        GameDebug.Log("Left playingstate");
    }

    void UpdatePlayingState()
    {
        // Handle disconnects
        if (!m_NetworkClient.isConnected)
        {
            m_GameMessage = m_DisconnectReason != null ? string.Format("Disconnected from server ({0})", m_DisconnectReason) : "Disconnected from server (lost connection)";
            m_StateMachine.SwitchTo(ClientState.Browsing);
            return;
        }

        // (re)send client info if any of the configvars that contain clientinfo has changed
        if ((ConfigVar.DirtyFlags & ConfigVar.Flags.ClientInfo) == ConfigVar.Flags.ClientInfo)
        {
            m_NetworkClient.UpdateClientConfig();
            ConfigVar.DirtyFlags &= ~ConfigVar.Flags.ClientInfo;
        }

        if (Game.Input.GetKeyUp(KeyCode.H))
        {
            RemoteConsoleCommand("nextchar");
        }

        if (Game.Input.GetKeyUp(KeyCode.T))
            CmdNextTeam(null);

        float frameDuration = m_lastFrameTime != 0 ? (float)(Game.frameTime - m_lastFrameTime) : 0;
        m_lastFrameTime = Game.frameTime;

        m_clientWorld.Update(frameDuration);
        m_performGameWorldLateUpdate = true;
    }

    public void FixedUpdate()
    {
    }

    public void LateUpdate()
    {
        if (m_clientWorld != null && m_performGameWorldLateUpdate)
        {
            m_performGameWorldLateUpdate = false;
            m_clientWorld.LateUpdate(m_ChatSystem, Time.deltaTime);
        }

        ShowInfoOverlay(0, 1);
    }

    public void RemoteConsoleCommand(string command)
    {
        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.RemoteConsoleCmd, true, (ref NetworkWriter writer) =>
        {
            writer.WriteString("args", command);
        });
    }

    public void CmdConnect(string[] args)
    {
        if(m_StateMachine.CurrentState() == ClientState.Browsing)
        {
            targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
            m_StateMachine.SwitchTo(ClientState.Connecting);
        }
        else if (m_StateMachine.CurrentState() == ClientState.Connecting)
        {
            m_NetworkClient.Disconnect();
            targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
            connectRetryCount = 0;
        }
        else
        {
            GameDebug.Log("Unable to connect from this state: " + m_StateMachine.CurrentState().ToString());
        }
    }

    void CmdDisconnect(string[] args)
    {
        m_DisconnectReason = "user manually disconnected";
        m_NetworkClient.Disconnect();
        m_StateMachine.SwitchTo(ClientState.Browsing);
    }

    void CmdTogglePrediction(string[] args)
    {
        m_predictionEnabled = !m_predictionEnabled;
        Console.Write("Prediction:" + m_predictionEnabled);

        if (m_clientWorld != null)
            m_clientWorld.PredictionEnabled = m_predictionEnabled;
    }

    void CmdRunAtServer(string[] args)
    {
        RemoteConsoleCommand(string.Join(" ", args));
    }

    void CmdRespawn(string[] args)
    {
        if (m_LocalPlayer == null || m_LocalPlayer.playerState == null || m_LocalPlayer.playerState.controlledEntity == Entity.Null)
            return;

        // Request new char type
        if (args.Length == 1)
        {
            m_requestedPlayerSettings.characterType = short.Parse(args[0]);
            m_playerSettingsUpdated = true;
        }
        
        // Tell server who to respawn
        RemoteConsoleCommand(string.Format("respawn {0}",m_LocalPlayer.playerState.playerId));
    }
    

    
    void CmdNextChar(string[] args)
    {
        if (m_LocalPlayer == null || m_LocalPlayer.playerState == null || m_LocalPlayer.playerState.controlledEntity == Entity.Null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;

        if (!m_GameWorld.GetEntityManager()
            .HasComponent<Character>(m_LocalPlayer.playerState.controlledEntity))
            return;
        
        var charSetupRegistry = m_resourceSystem.GetResourceRegistry<HeroTypeRegistry>();
        var charSetupCount = charSetupRegistry.entries.Count;
        
        m_requestedPlayerSettings.characterType = m_requestedPlayerSettings.characterType + 1;
        if (m_requestedPlayerSettings.characterType >= charSetupCount)   
            m_requestedPlayerSettings.characterType = 0;
        m_playerSettingsUpdated = true;
    }

    void CmdSpectator(string[] args)
    {
        if (m_LocalPlayer == null || m_LocalPlayer.playerState == null || m_LocalPlayer.playerState.controlledEntity == Entity.Null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;
        
        var isControllingSpectatorCam = m_GameWorld.GetEntityManager()
            .HasComponent<SpectatorCamData>(m_LocalPlayer.playerState.controlledEntity);
        
        // TODO find better way to identity spectatorcam
        m_requestedPlayerSettings.characterType = isControllingSpectatorCam ? 0 : 1000;   
        m_playerSettingsUpdated = true;
    }

    void CmdNextTeam(string[] args)
    {
        if (m_LocalPlayer == null || m_LocalPlayer.playerState == null)
            return;

        if (Game.allowCharChange.IntValue != 1)
            return;

        m_requestedPlayerSettings.teamId = (short)(m_LocalPlayer.playerState.teamIndex + 1);
        if (m_requestedPlayerSettings.teamId > 1)
            m_requestedPlayerSettings.teamId = 0;
        m_playerSettingsUpdated = true;
    }

    /// <summary>
    /// Start matchmaking by issuing a request to the provided endpoint. Use client.matchmaker value 
    /// as endpoint if none given.
    /// </summary>
    void CmdMatchmake(string[] args)
    {
        if (m_matchmaker != null)
        {
            GameDebug.Log("matchmake: Already in a matchmaking session. Wait for completion before matchmaking again.");
            return;
        }

        string endpoint = clientMatchmaker.Value;
        if (args.Length > 0)
            endpoint = args[0];

        if (string.IsNullOrEmpty(endpoint))
        {
            GameDebug.LogError("matchmake: command requires an endpoint <ex: cloud.connected.unity3d.com/{projectid}>");
            return;
        }

        if (string.IsNullOrEmpty(clientPlayerName.Value))
        {
            GameDebug.LogError("matchmake: Player name must be set before matchmaking can be started");
            return;
        }

        if (m_StateMachine.CurrentState() != ClientState.Browsing)
        {
            GameDebug.LogError("matchmake: matchmaking can only be started in Browsing state.  Current state is " + m_StateMachine.CurrentState().ToString());
            return;
        }

        GameDebug.Log($"matchmake: Starting the matchmaker. Requesting match from {endpoint} for request ID {clientPlayerName.Value}.");
        m_useMatchmaking = true;
        m_matchmaker = new Matchmaker(endpoint, OnMatchmakingSuccess, OnMatchmakingError);

        MatchmakingPlayerProperties playerProps = new MatchmakingPlayerProperties() {hats = 5};
        MatchmakingGroupProperties groupProps = new MatchmakingGroupProperties() {mode = 0};

        m_matchmaker.RequestMatch(clientPlayerName.Value, playerProps, groupProps);
    }

    void OnMatchmakingSuccess(Assignment assignment)
    {
        if (string.IsNullOrEmpty(assignment.ConnectionString))
        {
            GameDebug.Log("Matchmaking finished, but did not return a game server.  Ensure your server has been allocated and is running then try again.");
            GameDebug.Log($"MM Error: {assignment.AssignmentError ?? "None"}");
        }
        else
        {
            GameDebug.Log($"Matchmaking has found a game! The server is at {assignment.ConnectionString}.  Attempting to connect...");
            Console.EnqueueCommand($"connect {assignment.ConnectionString}");
        }
        m_useMatchmaking = false;
        m_matchmaker = null;
    }

    void OnMatchmakingError(string errorInfo)
    {
        GameDebug.LogError($"Matchmaking failed! Error is: {errorInfo}");
        m_useMatchmaking = false;
        m_matchmaker = null;
    }

    void ShowInfoOverlay(float x, float y)
    {
        if(m_showTickInfo.IntValue == 1)
            DebugOverlay.Write(x, y++, "Tick:{0} Last server:{1} Predicted:{2}", m_clientWorld.PredictedTime.tick, m_NetworkClient.serverTime, m_clientWorld.PredictedTime.tick - m_NetworkClient.serverTime - 1);

        if(m_showCommandInfo.IntValue == 1)
        {
            UserCommand command = UserCommand.defaultCommand;
            bool valid = m_LocalPlayer.commandBuffer.TryGetValue(m_clientWorld.PredictedTime.tick + 1, ref command);
            if(valid)
                DebugOverlay.Write(x, y++, "Next cmd: PrimaryFire:{0}", command.buttons.IsSet(UserCommand.Button.PrimaryFire));
            valid = m_LocalPlayer.commandBuffer.TryGetValue(m_clientWorld.PredictedTime.tick, ref command);
            if (valid)
                DebugOverlay.Write(x, y++, "Tick cmd: PrimaryFire:{0}", command.buttons.IsSet(UserCommand.Button.PrimaryFire));
        }
    }

    void SendPlayerSettings()
    {
        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.PlayerSetup, true, (ref NetworkWriter writer) =>
        {
            m_requestedPlayerSettings.Serialize(ref writer);
        });
    }

    enum ClientState
    {
        Browsing,
        Connecting,
        Loading,
        Playing,
    }
    StateMachine<ClientState> m_StateMachine;

    ClientState m_ClientState;

    GameWorld m_GameWorld;

    SocketTransport m_NetworkTransport;

    NetworkClient m_NetworkClient;
    
    LocalPlayer m_LocalPlayer;
    PlayerSettings m_requestedPlayerSettings = new PlayerSettings();
    bool m_playerSettingsUpdated;

    NetworkStatisticsClient m_NetworkStatistics;
    ChatSystemClient m_ChatSystem;

    ClientGameWorld m_clientWorld;
    BundledResourceManager m_resourceSystem;

    string m_LevelName;

    string m_DisconnectReason = null;
    string m_GameMessage = "Welcome to the sample game!";

    double m_lastFrameTime;
    bool m_predictionEnabled = true;
    bool m_performGameWorldLateUpdate;

    bool m_useMatchmaking = false;
    Matchmaker m_matchmaker;

    [ConfigVar(Name ="client.showtickinfo", DefaultValue = "0", Description = "Show tick info")]
    static ConfigVar m_showTickInfo;
    [ConfigVar(Name ="client.showcommandinfo", DefaultValue = "0", Description = "Show command info")]
    static ConfigVar m_showCommandInfo;
}
