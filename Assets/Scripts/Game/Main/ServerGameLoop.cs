using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using NetworkCompression;
using UnityEngine.Profiling;
using SQP;

public class ServerGameWorld : ISnapshotGenerator, IClientCommandProcessor
{
    public int WorldTick { get { return m_GameWorld.worldTime.tick; } }
    public int TickRate
    {
        get
        {
            return m_GameWorld.worldTime.tickRate;
        }
        set
        {
            m_GameWorld.worldTime.tickRate = value;
        }
    }
    public float TickInterval { get { return m_GameWorld.worldTime.tickInterval; } }

    public ServerGameWorld(GameWorld world, NetworkServer networkServer, Dictionary<int, ServerGameLoop.ClientInfo> clients, ChatSystemServer m_ChatSystem, BundledResourceManager resourceSystem)
    {
        m_NetworkServer = networkServer;
        m_Clients = clients;
        this.m_ChatSystem = m_ChatSystem;

        m_GameWorld = world;

        m_CharacterModule = new CharacterModuleServer(m_GameWorld, resourceSystem);
        m_ProjectileModule = new ProjectileModuleServer(m_GameWorld, resourceSystem);
        m_HitCollisionModule = new HitCollisionModule(m_GameWorld, 128, 1);
        m_PlayerModule = new PlayerModuleServer(m_GameWorld, resourceSystem);
        m_SpectatorCamModule = new SpectatorCamModuleServer(m_GameWorld, resourceSystem);
        m_ReplicatedEntityModule = new ReplicatedEntityModuleServer(m_GameWorld, resourceSystem, m_NetworkServer);
        m_ReplicatedEntityModule.ReserveSceneEntities(networkServer);
        m_ItemModule = new ItemModule(m_GameWorld);

        m_GameModeSystem = m_GameWorld.GetECSWorld().CreateManager<GameModeSystemServer>(m_GameWorld, m_ChatSystem, resourceSystem);

        m_DestructablePropSystem = m_GameWorld.GetECSWorld().CreateManager<UpdateDestructableProps>(m_GameWorld);

        m_DamageAreaSystem = m_GameWorld.GetECSWorld().CreateManager<DamageAreaSystemServer>(m_GameWorld);

        m_TeleporterSystem = m_GameWorld.GetECSWorld().CreateManager<TeleporterSystemServer>(m_GameWorld);

        m_HandleGrenadeRequests = m_GameWorld.GetECSWorld().CreateManager<HandleGrenadeRequest>(m_GameWorld, resourceSystem);
        m_StartGrenadeMovement = m_GameWorld.GetECSWorld().CreateManager<StartGrenadeMovement>(m_GameWorld);
        m_FinalizeGrenadeMovement = m_GameWorld.GetECSWorld().CreateManager<FinalizeGrenadeMovement>(m_GameWorld);

        m_platformSystem = m_GameWorld.GetECSWorld().CreateManager<MoverUpdate>(m_GameWorld);

        m_MoveableSystem = new MovableSystemServer(m_GameWorld, resourceSystem);
        m_CameraSystem = new ServerCameraSystem(m_GameWorld);
    }

    public void Shutdown()
    {
        m_CharacterModule.Shutdown();
        m_ProjectileModule.Shutdown();
        m_HitCollisionModule.Shutdown();
        m_PlayerModule.Shutdown();
        m_SpectatorCamModule.Shutdown();

        m_GameWorld.GetECSWorld().DestroyManager(m_DestructablePropSystem);
        m_GameWorld.GetECSWorld().DestroyManager(m_DamageAreaSystem);
        m_GameWorld.GetECSWorld().DestroyManager(m_TeleporterSystem);

        m_GameWorld.GetECSWorld().DestroyManager(m_HandleGrenadeRequests);
        m_GameWorld.GetECSWorld().DestroyManager(m_StartGrenadeMovement);
        m_GameWorld.GetECSWorld().DestroyManager(m_FinalizeGrenadeMovement);

        m_GameWorld.GetECSWorld().DestroyManager(m_platformSystem);

        m_ReplicatedEntityModule.Shutdown();
        m_ItemModule.Shutdown();

        m_CameraSystem.Shutdown();
        m_MoveableSystem.Shutdown();

        m_GameWorld = null;
    }

    public void RespawnPlayer(PlayerState player)
    {
        if (player.controlledEntity == Entity.Null)
            return;

        if (m_GameWorld.GetEntityManager().HasComponent<Character>(player.controlledEntity))
            CharacterDespawnRequest.Create(m_GameWorld, player.controlledEntity);

        player.controlledEntity = Entity.Null;
    }

    char[] _msgBuf = new char[256];
    public void HandlePlayerSetupEvent(PlayerState player, PlayerSettings settings)
    {
        if (player.playerName != settings.playerName)
        {
            int l = 0;
            if (player.playerName == "")
                l = StringFormatter.Write(ref _msgBuf, 0, "{0} joined", settings.playerName);
            else
                l = StringFormatter.Write(ref _msgBuf, 0, "{0} is now known as {1}", player.playerName, settings.playerName);
            m_ChatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
            player.playerName = settings.playerName;
        }

        var playerEntity = player.gameObject.GetComponent<GameObjectEntity>().Entity;
        var charControl = m_GameWorld.GetEntityManager().GetComponentObject<PlayerCharacterControl>(playerEntity);

        charControl.requestedCharacterType = settings.characterType;
    }

    public void ProcessCommand(int connectionId, int tick, ref NetworkReader data)
    {
        ServerGameLoop.ClientInfo client;
        if (!m_Clients.TryGetValue(connectionId, out client))
            return;

        if (client.player)
        {
            var serializeContext = new SerializeContext
            {
                entityManager = m_GameWorld.GetEntityManager(),
                entity = Entity.Null,
                refSerializer = null,
                tick = tick
            };
                
            if (tick == m_GameWorld.worldTime.tick)
                client.latestCommand.Deserialize(ref serializeContext, ref data);

            // Pass on command to controlled entity
            if (client.player.controlledEntity != Entity.Null)
            {
                var userCommand = m_GameWorld.GetEntityManager().GetComponentData<UserCommandComponentData>(
                    client.player.controlledEntity);

                userCommand.command = client.latestCommand;

                m_GameWorld.GetEntityManager().SetComponentData<UserCommandComponentData>(
                    client.player.controlledEntity,userCommand);
            }
        }
    }

    public bool HandleClientCommand(ServerGameLoop.ClientInfo client, string v)
    {
        if (v == "nextchar")
        {
            GameDebug.Log("nextchar for client " + client.id);
            m_GameModeSystem.RequestNextChar(client.player);
        }
        else
        {
            return false;
        }
        return true;
    }

    public void ServerTickUpdate()
    {
        Profiler.BeginSample("ServerGameWorld.ServerTickUpdate()");

        m_GameWorld.worldTime.tick++;
        m_GameWorld.worldTime.tickDuration = m_GameWorld.worldTime.tickInterval;
        m_GameWorld.frameDuration = m_GameWorld.worldTime.tickInterval;

        Profiler.BeginSample("HandleClientCommands");

        // This call backs into ProcessCommand
        m_NetworkServer.HandleClientCommands(m_GameWorld.worldTime.tick, this);

        Profiler.EndSample();

        GameTime gameTime = new GameTime(m_GameWorld.worldTime.tickRate);
        gameTime.SetTime(m_GameWorld.worldTime.tick, m_GameWorld.worldTime.tickInterval);

        // Handle spawn requests. All creation of game entities should happen in this phase        
        m_CharacterModule.HandleSpawnRequests();
        m_SpectatorCamModule.HandleSpawnRequests();
        m_ProjectileModule.HandleRequests();         
        m_HandleGrenadeRequests.Update();

        // Handle newly spawned entities          
        m_CharacterModule.HandleSpawns();
        m_HitCollisionModule.HandleSpawning();
        m_ReplicatedEntityModule.HandleSpawning();
        m_ItemModule.HandleSpawn();

        // Handle controlled entity changed
        m_CharacterModule.HandleControlledEntityChanged();

        // Start movement of scene objects. Scene objects that player movement
        // depends on should finish movement in this phase
        m_MoveableSystem.Update();
        m_platformSystem.Update();
        m_ProjectileModule.MovementStart();
        m_StartGrenadeMovement.Update();
        m_CameraSystem.Update();

        // Update movement of player controlled units 
        m_TeleporterSystem.Update();
        m_CharacterModule.AbilityRequestUpdate();
        m_CharacterModule.MovementStart();
        m_CharacterModule.MovementResolve();
        m_CharacterModule.AbilityStart();
        m_CharacterModule.AbilityResolve();

        // Finalize movement of modules that only depend on data from previous frames
        // We want to wait as long as possible so queries potentially can be handled in jobs  
        m_ProjectileModule.MovementResolve();
        m_FinalizeGrenadeMovement.Update();

        // Handle damage
        m_DestructablePropSystem.Update();
        m_DamageAreaSystem.Update();
        m_HitCollisionModule.HandleSplashDamage();
        m_CharacterModule.HandleDamage();

        // 
        m_CharacterModule.PresentationUpdate();

        // Update gamemode. Run last to allow picking up deaths etc.
        m_GameModeSystem.Update();


        // Handle despawns
        m_CharacterModule.HandleDepawns(); // TODO (mogensh) this destroys presentations and needs to be done first so its picked up. We need better way of handling destruction ordering
        m_HitCollisionModule.HandleDespawn();
        m_ReplicatedEntityModule.HandleDespawning();
        m_GameWorld.ProcessDespawns();

        Profiler.EndSample();
    }

    // This is called every render frame where an tick update has been performed
    public void LateUpdate()
    {
        m_CharacterModule.AttachmentUpdate();

        m_HitCollisionModule.StoreColliderState();
    }

    public void HandleClientConnect(ServerGameLoop.ClientInfo client)
    {
        client.player = m_PlayerModule.CreatePlayer(m_GameWorld, client.id, "", client.isReady);
    }

    public void HandleClientDisconnect(ServerGameLoop.ClientInfo client)
    {
        m_PlayerModule.CleanupPlayer(client.player);
        m_CharacterModule.CleanupPlayer(client.player);
    }

    public void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer)
    {
        Profiler.BeginSample("ServerGameLoop.GenerateEntitySnapshot()");

        m_ReplicatedEntityModule.GenerateEntitySnapshot(entityId, ref writer);

        Profiler.EndSample();
    }

    public string GenerateEntityName(int entityId)
    {
        return m_ReplicatedEntityModule.GenerateName(entityId);
    }

    // External systems
    NetworkServer m_NetworkServer;
    Dictionary<int, ServerGameLoop.ClientInfo> m_Clients;
    readonly ChatSystemServer m_ChatSystem;

    // Internal systems
    GameWorld m_GameWorld;
    readonly CharacterModuleServer m_CharacterModule;
    readonly ProjectileModuleServer m_ProjectileModule;
    readonly HitCollisionModule m_HitCollisionModule;
    readonly PlayerModuleServer m_PlayerModule;
    readonly SpectatorCamModuleServer m_SpectatorCamModule;
    readonly ReplicatedEntityModuleServer m_ReplicatedEntityModule;
    readonly ItemModule m_ItemModule;

    readonly ServerCameraSystem m_CameraSystem;
    readonly GameModeSystemServer m_GameModeSystem;

    readonly DamageAreaSystemServer m_DamageAreaSystem;
    readonly TeleporterSystemServer m_TeleporterSystem;


    readonly HandleGrenadeRequest m_HandleGrenadeRequests;
    readonly StartGrenadeMovement m_StartGrenadeMovement;
    readonly FinalizeGrenadeMovement m_FinalizeGrenadeMovement;

    readonly MoverUpdate m_platformSystem;
    readonly UpdateDestructableProps m_DestructablePropSystem;
    readonly MovableSystemServer m_MoveableSystem;

}



public class ServerGameLoop : Game.IGameLoop, INetworkCallbacks
{
    public bool Init(string[] args)
    {
        // Set up statemachine for ServerGame
        m_StateMachine = new StateMachine<ServerState>();
        m_StateMachine.Add(ServerState.Idle, null, UpdateIdleState, null);
        m_StateMachine.Add(ServerState.Loading, null, UpdateLoadingState, null);
        m_StateMachine.Add(ServerState.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);

        m_StateMachine.SwitchTo(ServerState.Idle);

        m_NetworkTransport = new SocketTransport(NetworkConfig.serverPort.IntValue, serverMaxClients.IntValue);
        var listenAddresses = NetworkUtils.GetLocalInterfaceAddresses();
        if (listenAddresses.Count > 0)
            Console.SetPrompt(listenAddresses[0] + ":" + NetworkConfig.serverPort.Value + "> ");
        GameDebug.Log("Listening on " + string.Join(", ", NetworkUtils.GetLocalInterfaceAddresses()) + " on port " + NetworkConfig.serverPort.IntValue);
        m_NetworkServer = new NetworkServer(m_NetworkTransport);

        if (Game.game.clientFrontend != null)
        {
            var serverPanel = Game.game.clientFrontend.serverPanel;
            serverPanel.SetPanelActive(true);
            serverPanel.serverInfo.text += "Listening on:\n";
            foreach (var a in NetworkUtils.GetLocalInterfaceAddresses())
            {
                serverPanel.serverInfo.text += a + ":" + NetworkConfig.serverPort.IntValue + "\n";
            }
        }

        m_NetworkServer.UpdateClientInfo();
        m_NetworkServer.serverInfo.compressionModel = m_Model;

        if (serverServerName.Value == "")
            serverServerName.Value = MakeServername();

        m_ServerQueryProtocolServer = new SQP.SQPServer(NetworkConfig.serverSQPPort.IntValue > 0? NetworkConfig.serverSQPPort.IntValue : NetworkConfig.serverPort.IntValue + NetworkConfig.sqpPortOffset);


#if UNITY_EDITOR        
        Game.game.levelManager.UnloadLevel();
#endif        
        m_GameWorld = new GameWorld("ServerWorld");

        m_NetworkStatistics = new NetworkStatisticsServer(m_NetworkServer);

        m_ChatSystem = new ChatSystemServer(m_Clients, m_NetworkServer);

        GameDebug.Log("Network server initialized");

        Console.AddCommand("load", CmdLoad, "Load a named scene", this.GetHashCode());
        Console.AddCommand("unload", CmdUnload, "Unload current scene", this.GetHashCode());
        Console.AddCommand("respawn", CmdRespawn, "Respawn character (usage : respawn playername|playerId)", this.GetHashCode());
        Console.AddCommand("servername", CmdSetServerName, "Set name of the server", this.GetHashCode());
        Console.AddCommand("beginnetworkprofile", CmdBeginNetworkProfile, "begins a network profile", this.GetHashCode());
        Console.AddCommand("endnetworkprofile", CmdEndNetworkProfile, "Ends a network profile and analyzes. [optional] filepath for model data", this.GetHashCode());
        Console.AddCommand("loadcompressionmodel", CmdLoadNetworkCompressionModel, "Loads a network compression model from a filepath", this.GetHashCode());
        Console.AddCommand("list", CmdList, "List clients", this.GetHashCode());

        CmdLoad(args);
        Game.SetMousePointerLock(false);

        m_ServerStartTime = Time.time;

        GameDebug.Log("Server initialized");
        Console.SetOpen(false);
        return true;
    }

    public void Shutdown()
    {
        GameDebug.Log("ServerGameState shutdown");
        Console.RemoveCommandsWithTag(this.GetHashCode());

        m_StateMachine.Shutdown();
        m_NetworkServer.Shutdown();

        m_NetworkTransport.Shutdown();
        Game.game.levelManager.UnloadLevel();

        m_GameWorld.Shutdown();
        m_GameWorld = null;
    }

    public void Update()
    {
        if (serverRecycleInterval.FloatValue > 0.0f)
        {
            // Recycle server if time is up and no clients connected
            if (m_Clients.Count == 0 && Time.time > m_ServerStartTime + serverRecycleInterval.FloatValue)
            {
                GameDebug.Log("Server exiting because recycle timeout was hit.");
                Console.EnqueueCommandNoHistory("quit");
            }
        }

        if (m_Clients.Count > m_MaxClients)
            m_MaxClients = m_Clients.Count;

        if (serverQuitWhenEmpty.IntValue > 0 && m_MaxClients > 0 && m_Clients.Count == 0)
        {
            GameDebug.Log("Server exiting because last client disconnected");
            Console.EnqueueCommandNoHistory("quit");
        }

        m_SimStartTime = Game.Clock.ElapsedTicks;
        m_SimStartTimeTick = m_serverGameWorld != null ? m_serverGameWorld.WorldTick : 0;

        UpdateNetwork();
        m_StateMachine.Update();

        m_NetworkServer.SendData();

        m_NetworkStatistics.Update();

        if (showGameLoopInfo.IntValue > 0)
            OnDebugDrawGameloopInfo();
    }

    public NetworkServer GetNetworkServer()
    {
        return m_NetworkServer;
    }

    public void OnConnect(int id)
    {
        var client = new ClientInfo();
        client.id = id;
        m_Clients.Add(id, client);

        if (m_serverGameWorld != null)
            m_serverGameWorld.HandleClientConnect(client);
    }

    public void OnDisconnect(int id)
    {
        ClientInfo client;
        if (m_Clients.TryGetValue(id, out client))
        {
            if (m_serverGameWorld != null)
                m_serverGameWorld.HandleClientDisconnect(client);

            m_Clients.Remove(id);
        }
    }

    unsafe public void OnEvent(int clientId, NetworkEvent info)
    {
        var client = m_Clients[clientId];
        var type = info.type.typeId;
        fixed (uint* data = info.data)
        {
            var reader = new NetworkReader(data, info.type.schema);

            switch ((GameNetworkEvents.EventType)type)
            {
                case GameNetworkEvents.EventType.PlayerReady:
                    m_NetworkServer.MapReady(clientId); // TODO (petera) hacky
                    client.isReady = true;
                    break;

                case GameNetworkEvents.EventType.PlayerSetup:
                    client.playerSettings.Deserialize(ref reader);
                    if (client.player != null)
                        m_serverGameWorld.HandlePlayerSetupEvent(client.player, client.playerSettings);
                    break;

                case GameNetworkEvents.EventType.RemoteConsoleCmd:
                    HandleClientCommand(client, reader.ReadString());
                    break;

                case GameNetworkEvents.EventType.Chat:
                    m_ChatSystem.ReceiveMessage(client, reader.ReadString(256));
                    break;
            }
        }
    }

    private void HandleClientCommand(ClientInfo client, string v)
    {
        if (m_serverGameWorld != null && m_serverGameWorld.HandleClientCommand(client, v))
            return;

        // Fall back is just to become a server console command
        // TODO (petera) Add some sort of security system here
        Console.EnqueueCommandNoHistory(v);
    }

    void UpdateNetwork()
    {
        Profiler.BeginSample("ServerGameLoop.UpdateNetwork");

        // If serverTickrate was changed, update both game world and 
        if ((ConfigVar.DirtyFlags & ConfigVar.Flags.ServerInfo) == ConfigVar.Flags.ServerInfo)
        {
            GameDebug.Log("WARNING: UpdateClientInfo deprecated");
            m_NetworkServer.UpdateClientInfo();
            ConfigVar.DirtyFlags &= ~ConfigVar.Flags.ServerInfo;
        }

        if (m_serverGameWorld != null && m_serverGameWorld.TickRate != Game.serverTickRate.IntValue)
            m_serverGameWorld.TickRate = Game.serverTickRate.IntValue;

        // Update SQP data with current values
        var sid = m_ServerQueryProtocolServer.ServerInfoData;
        sid.BuildId = Game.game.buildId;
        sid.Port = (ushort)NetworkConfig.serverPort.IntValue;
        sid.CurrentPlayers = (ushort)m_Clients.Count;
        sid.GameType = GameModeSystemServer.modeName.Value;
        sid.Map = Game.game.levelManager.currentLevel.name;
        sid.MaxPlayers = (ushort)serverMaxClients.IntValue;
        sid.ServerName = serverServerName.Value;

        m_ServerQueryProtocolServer.Update();

        m_NetworkServer.Update(this);

        Profiler.EndSample();
    }

    /// <summary>
    /// Idle state, no level is loaded
    /// </summary>
    void UpdateIdleState()
    {

    }

    /// <summary>
    /// Loading state, load in progress
    /// </summary>
    void UpdateLoadingState()
    {
        if (Game.game.levelManager.IsCurrentLevelLoaded())
            m_StateMachine.SwitchTo(ServerState.Active);
    }

    /// <summary>
    /// Active state, level loaded
    /// </summary>
    void EnterActiveState()
    {
        GameDebug.Assert(m_serverGameWorld == null);

        m_GameWorld.RegisterSceneEntities();

        m_resourceSystem = new BundledResourceManager(m_GameWorld,"BundledResources/Server");

        m_NetworkServer.InitializeMap((ref NetworkWriter data) =>
        {
            data.WriteString("name", Game.game.levelManager.currentLevel.name);
        });

        m_serverGameWorld = new ServerGameWorld(m_GameWorld, m_NetworkServer, m_Clients, m_ChatSystem, m_resourceSystem);
        foreach (var pair in m_Clients)
        {
            m_serverGameWorld.HandleClientConnect(pair.Value);
        }
    }

    Dictionary<int, int> m_TickStats = new Dictionary<int, int>();
    void UpdateActiveState()
    {
        GameDebug.Assert(m_serverGameWorld != null);

        int tickCount = 0;
        while (Game.frameTime > m_nextTickTime)
        {
            tickCount++;
            m_serverGameWorld.ServerTickUpdate();

            Profiler.BeginSample("GenerateSnapshots");
            m_NetworkServer.GenerateSnapshot(m_serverGameWorld, m_LastSimTime);
            Profiler.EndSample();

            m_nextTickTime += m_serverGameWorld.TickInterval;

            m_performLateUpdate = true;
        }

        //
        // If running as headless we nudge the Application.targetFramerate back and forth
        // around the actual framerate -- always trying to have a remaining time of half a frame
        // The goal is to have the while loop above tick exactly 1 time
        //
        // The reason for using targetFramerate is to allow Unity to sleep between frames
        // reducing cpu usage on server.
        //
        if (Game.IsHeadless())
        {
            float remainTime = (float)(m_nextTickTime - Game.frameTime);

            int rate = m_serverGameWorld.TickRate;
            if (remainTime > 0.75f * m_serverGameWorld.TickInterval)
                rate -= 2;
            else if (remainTime < 0.25f * m_serverGameWorld.TickInterval)
                rate += 2;

            Application.targetFrameRate = rate;

            //
            // Show some stats about how many world ticks per unity update we have been running
            //
            if (debugServerTickStats.IntValue > 0)
            {
                if (Time.frameCount % 10 == 0)
                    GameDebug.Log(remainTime + ":" + rate);

                if (!m_TickStats.ContainsKey(tickCount))
                    m_TickStats[tickCount] = 0;
                m_TickStats[tickCount] = m_TickStats[tickCount] + 1;
                if (Time.frameCount % 100 == 0)
                {
                    foreach (var p in m_TickStats)
                    {
                        GameDebug.Log(p.Key + ":" + p.Value);
                    }
                }
            }
        }
    }

    void LeaveActiveState()
    {
        m_serverGameWorld.Shutdown();
        m_serverGameWorld = null;

        m_resourceSystem.Shutdown();
    }

    public void FixedUpdate()
    {
    }

    public void LateUpdate()
    {
        if (m_serverGameWorld != null && m_SimStartTimeTick != m_serverGameWorld.WorldTick)
        {
            // Only update sim time if we actually simulatated
            // TODO : remove this when targetFrameRate works the way we want it.
            m_LastSimTime = Game.Clock.GetTicksDeltaAsMilliseconds(m_SimStartTime);
        }

        if (m_performLateUpdate)
        {
            m_serverGameWorld.LateUpdate();
            m_performLateUpdate = false;
        }
    }

    void LoadLevel(string levelname, string gamemode = "deathmatch")
    {
        if (!Game.game.levelManager.CanLoadLevel(levelname))
        {
            GameDebug.Log("ERROR : Cannot load level : " + levelname);
            return;
        }

        m_RequestedGameMode = gamemode;
        Game.game.levelManager.LoadLevel(levelname);

        m_StateMachine.SwitchTo(ServerState.Loading);
    }

    void UnloadLevel()
    {
        // TODO
    }

    void CmdSetServerName(string[] args)
    {
        if (args.Length > 0)
        {
            // TODO (petera) fix or remove this?
        }
        else
            Console.Write("Invalid argument to servername (usage : servername name)");
    }

    void CmdLoad(string[] args)
    {
        if (args.Length == 1)
            LoadLevel(args[0]);
        else if (args.Length == 2)
            LoadLevel(args[0], args[1]);
    }

    void CmdUnload(string[] args)
    {
        UnloadLevel();
    }

    void CmdRespawn(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Write("Invalid argument for respawn command (usage : respawn playername|playerId)");
            return;
        }

        var playerId = -1;
        var playerName = args[0];
        var usePlayerId = int.TryParse(args[0], out playerId);

        foreach (var pair in m_Clients)
        {
            var client = pair.Value;
            if (client.player == null)
                continue;

            if (usePlayerId && client.id != playerId)
                continue;

            if (!usePlayerId && client.player.playerName != playerName)
                continue;

            m_serverGameWorld.RespawnPlayer(client.player);
        }

        Console.Write("Could not find character. Unknown player, invalid character id or player doesn't have a character" + args[0]);
    }

    void CmdBeginNetworkProfile(string[] args)
    {
        var networkServer = GetNetworkServer();
        if (networkServer != null)
        {
            networkServer.StartNetworkProfile();
            Console.Write("Profiling started");
        }
        else
        {
            Console.Write("No server running");
        }
    }

    void CmdEndNetworkProfile(string[] args)
    {
        var networkServer = GetNetworkServer();
        if (networkServer != null)
            networkServer.EndNetworkProfile(args.Length >= 1 ? args[0] : null);
        else
            Console.Write("No server running");
    }

    void CmdLoadNetworkCompressionModel(string[] args)
    {
        var networkServer = GetNetworkServer();
        if (networkServer != null && networkServer.GetConnections().Count > 0)
        {
            Console.Write("Can only load compression model when server when no clients are connected");
            return;
        }

        if (args.Length != 1)
        {
            Console.Write("Syntax: loadcompressionmodel filepath");
            return;
        }

        byte[] modelData = null;
        try
        {
            modelData = System.IO.File.ReadAllBytes(args[0]);
        }
        catch (System.Exception e)
        {
            Console.Write("Failed to read file: " + args[0] + " (" + e.ToString() + ")");
            return;
        }

        m_Model = new NetworkCompressionModel(modelData);

        if (networkServer != null)
            networkServer.serverInfo.compressionModel = m_Model;
        Console.Write("Model Loaded");
    }

    void CmdList(string[] args)
    {
        Console.Write("Players on server:");
        Console.Write("-------------------");
        Console.Write(string.Format("   {0,2} {1,-15}", "ID", "PlayerName"));
        Console.Write("-------------------");
        foreach (var c in m_Clients)
        {
            var client = c.Value;
            Console.Write(string.Format("   {0:00} {1,-15}", client.id, client.playerSettings.playerName));
        }
        Console.Write("-------------------");
        Console.Write(string.Format("Total: {0}/{0} players connected", m_Clients.Count, serverMaxClients.IntValue));
    }

    string MakeServername()
    {

        var f = new string[] { "Ultimate", "Furry", "Quick", "Laggy", "Hot", "Curious", "Flappy", "Sneaky", "Nested", "Deep", "Blue", "Hipster", "Artificial" };
        var l = new string[] { "Speedrun", "Fragfest", "Win", "Exception", "Prefab", "Scene", "Garbage", "System", "Souls", "Whitespace", "Dolphin" };
        return f[Random.Range(0, f.Length)] + " " + l[Random.Range(0, l.Length)];
    }

    void OnDebugDrawGameloopInfo()
    {
        //DebugOverlay.Write(2,2,"Server Gameloop Info:");

        //var y = 3;
        //DebugOverlay.Write(2, y++, "  Simulation time average : {0}", m_NetworkServer.simStats.simTime);
        //DebugOverlay.Write(2, y++, "  Simulation time stdev : {0}", m_NetworkServer.simStats.simTimeStdDev);
        //DebugOverlay.Write(2, y++, "  Simulation time peek : {0}", m_NetworkServer.simStats.simTimeMax);

        //y++;
        //DebugOverlay.Write(2, y++, "  Delta time average : {0}", m_NetworkServer.simStats.deltaTime);
        //DebugOverlay.Write(2, y++, "  Delta time stdev : {0}", m_NetworkServer.simStats.deltaTimeStdDev);
        //DebugOverlay.Write(2, y++, "  Delta time peek : {0}", m_NetworkServer.simStats.deltaTimeMax);

        //y += 2;
        //foreach (var clientId in m_NetworkServer.clients)
        //{
        //    var info = m_NetworkServer.GetClientConnectionInfo(clientId);
        //    DebugOverlay.Write(2, y++, "  addr: {0}  port: {1}  rtt: {2} ms", info.address, info.port, info.rtt);
        //}
    }

    // Statemachine
    enum ServerState
    {
        Idle,
        Loading,
        Active,
    }
    StateMachine<ServerState> m_StateMachine;

    public class ClientInfo
    {
        public int id;
        public PlayerSettings playerSettings = new PlayerSettings();
        public bool isReady;
        public PlayerState player;
        public UserCommand latestCommand = UserCommand.defaultCommand;
    }

    NetworkServer m_NetworkServer;
    GameWorld m_GameWorld;
    NetworkStatisticsServer m_NetworkStatistics;
    NetworkCompressionModel m_Model = NetworkCompressionModel.DefaultModel;

    SocketTransport m_NetworkTransport;

    BundledResourceManager m_resourceSystem;
    ChatSystemServer m_ChatSystem;
    Dictionary<int, ClientInfo> m_Clients = new Dictionary<int, ClientInfo>();

    ServerGameWorld m_serverGameWorld;
    public double m_nextTickTime = 0;
    string m_RequestedGameMode = "deathmatch";

    long m_SimStartTime;
    int m_SimStartTimeTick;
    float m_LastSimTime;
    bool m_performLateUpdate;

    SQPServer m_ServerQueryProtocolServer;

    [ConfigVar(Name = "show.gameloopinfo", DefaultValue = "0", Description = "Show gameloop info")]
    static ConfigVar showGameLoopInfo;

    [ConfigVar(Name = "server.quitwhenempty", DefaultValue = "0", Description = "If enabled, quit when last client disconnects.")]
    static ConfigVar serverQuitWhenEmpty;

    [ConfigVar(Name = "server.recycleinterval", DefaultValue = "0", Description = "Exit when N seconds old AND when 0 players. 0 means never.")]
    static ConfigVar serverRecycleInterval;

    [ConfigVar(Name = "debug.servertickstats", DefaultValue = "0", Description = "Show stats about how many ticks we run per Unity update (headless only)")]
    static ConfigVar debugServerTickStats;

    [ConfigVar(Name = "server.maxclients", DefaultValue = "8", Description = "Maximum allowed clients")]
    public static ConfigVar serverMaxClients;

    [ConfigVar(Name = "server.disconnecttimeout", DefaultValue = "30000", Description = "Timeout in ms. Server will kick clients after this interval if nothing has been heard.")]
    public static ConfigVar serverDisconnectTimeout;

    [ConfigVar(Name = "server.servername", DefaultValue = "", Description = "Servername")]
    static ConfigVar serverServerName;

    float m_ServerStartTime;
    int m_MaxClients;
}
