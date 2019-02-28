using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

public interface IGameMode
{
    void Initialize(GameWorld world, GameModeSystemServer gameModeSystemServer);
    void Shutdown();

    void Restart();
    void Update();

    void OnPlayerJoin(PlayerState player);
    void OnPlayerRespawn(PlayerState player, ref Vector3 position, ref Quaternion rotation);
    void OnPlayerKilled(PlayerState victim, PlayerState killer);
}

public class NullGameMode : IGameMode
{
    public void Initialize(GameWorld world, GameModeSystemServer gameModeSystemServer) { }
    public void OnPlayerJoin(PlayerState teamMember) { }
    public void OnPlayerKilled(PlayerState victim, PlayerState killer) { }
    public void OnPlayerRespawn(PlayerState player, ref Vector3 position, ref Quaternion rotation) { }
    public void Restart() { }
    public void Shutdown() { }
    public void Update() { }
}

public class Team
{
    public string name;
    public int score;
}

[DisableAutoCreation]
public class GameModeSystemServer : ComponentSystem
{
    [ConfigVar(Name = "game.respawndelay", DefaultValue = "10", Description = "Time from death to respawning")]
    public static ConfigVar respawnDelay;
    [ConfigVar(Name = "game.modename", DefaultValue = "assault", Description = "Which gamemode to use")]
    public static ConfigVar modeName;

    public ComponentGroup playersComponentGroup;
    ComponentGroup m_TeamBaseComponentGroup;
    ComponentGroup m_SpawnPointComponentGroup;
    ComponentGroup m_PlayersComponentGroup;

    public readonly GameMode gameModeState;
    public readonly ChatSystemServer chatSystem;
    public List<Team> teams = new List<Team>();
    public List<TeamBase> teamBases = new List<TeamBase>();

    public GameModeSystemServer(GameWorld world, ChatSystemServer chatSystem, BundledResourceManager resourceSystem)
    {
        m_World = world;
        m_ResourceSystem = resourceSystem;
        this.chatSystem = chatSystem;
        m_CurrentGameModeName = "";

        // TODO (petera) Get rid of need for loading these 'settings' and the use of them below.
        // We need a way to spawn a 'naked' replicated entity, i.e. one that is not created from a prefab.
        m_Settings = Resources.Load<GameModeSystemSettings>("GameModeSystemSettings");

        // Create game mode state
        var prefab = (GameObject)resourceSystem.GetSingleAssetResource(m_Settings.gameModePrefab);
        gameModeState = m_World.Spawn<GameMode>(prefab);

    }

    public void Restart()
    {
        GameDebug.Log("Restarting gamdemode");
        var bases = m_TeamBaseComponentGroup.GetComponentArray<TeamBase>();
        teamBases.Clear();
        for (var i = 0; i < bases.Length; i++)
        {
            teamBases.Add(bases[i]);
        }

        for (int i = 0, c = teams.Count; i < c; ++i)
        {
            teams[i].score = -1;
        }

        var players = playersComponentGroup.GetComponentArray<PlayerState>();
        for (int i = 0, c = players.Length; i < c; ++i)
        {
            var player = players[i];
            player.score = 0;
            player.displayGameScore = true;
            player.goalCompletion = -1.0f;
            player.actionString = "";
        }

        m_EnableRespawning = true;

        m_GameMode.Restart();

        chatSystem.ResetChatTime();
    }


    public void Shutdown()
    {
        m_GameMode.Shutdown();

        Resources.UnloadAsset(m_Settings);

        m_World.RequestDespawn(gameModeState.gameObject);
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        playersComponentGroup = GetComponentGroup(typeof(PlayerState));
        m_TeamBaseComponentGroup = GetComponentGroup(typeof(TeamBase));
        m_SpawnPointComponentGroup = GetComponentGroup(typeof(SpawnPoint));
        m_PlayersComponentGroup = GetComponentGroup(typeof(PlayerState), typeof(PlayerCharacterControl));
    }

    new public ComponentGroup GetComponentGroup(params ComponentType[] componentTypes)
    {
        return base.GetComponentGroup(componentTypes);
    }

    float m_TimerStart;
    ConfigVar m_TimerLength;
    public void StartGameTimer(ConfigVar seconds, string message)
    {
        m_TimerStart = Time.time;
        m_TimerLength = seconds;
        gameModeState.gameTimerMessage = message;
    }

    public int GetGameTimer()
    {
        return Mathf.Max(0, Mathf.FloorToInt(m_TimerStart + m_TimerLength.FloatValue - Time.time));
    }

    public void SetRespawnEnabled(bool enable)
    {
        m_EnableRespawning = enable;
    }

    char[] _msgBuf = new char[256];
    protected override void OnUpdate()
    {
        // Handle change of game mode
        if (m_CurrentGameModeName != modeName.Value)
        {
            m_CurrentGameModeName = modeName.Value;

            switch (m_CurrentGameModeName)
            {
                case "deathmatch":
                    m_GameMode = new GameModeDeathmatch();
                    break;
                case "assault":
                    m_GameMode = new GameModeAssault();
                    break;
                default:
                    m_GameMode = new NullGameMode();
                    break;
            }
            m_GameMode.Initialize(m_World, this);
            GameDebug.Log("New gamemode : '" + m_GameMode.GetType().ToString() + "'");
            Restart();
            return;
        }

        // Handle joining players
        var playerStates = m_PlayersComponentGroup.GetComponentArray<PlayerState>();
        for (int i = 0, c = playerStates.Length; i < c; ++i)
        {
            var player = playerStates[i];
            if (!player.gameModeSystemInitialized)
            {
                player.score = 0;
                player.displayGameScore = true;
                player.goalCompletion = -1.0f;
                m_GameMode.OnPlayerJoin(player);
                player.gameModeSystemInitialized = true;
            }
        }

        m_GameMode.Update();

        // General rules
        gameModeState.gameTimerSeconds = GetGameTimer();

        var playerEntities = m_PlayersComponentGroup.GetEntityArray();
        var playerCharacterControls = m_PlayersComponentGroup.GetComponentArray<PlayerCharacterControl>();
        for (int i = 0, c = playerStates.Length; i < c; ++i)
        {
            var player = playerStates[i];
            var controlledEntity = player.controlledEntity;
            var playerEntity = playerEntities[i];

            
            player.actionString = player.enableCharacterSwitch ? "Press H to change character" : "";

            var charControl = playerCharacterControls[i];

            // Spawn contolled entity (character) any missing
            if (controlledEntity == Entity.Null)
            {
                var position = new Vector3(0.0f, 0.2f, 0.0f);
                var rotation = Quaternion.identity;
                GetRandomSpawnTransform(player.teamIndex, ref position, ref rotation);
                
                m_GameMode.OnPlayerRespawn(player, ref position, ref rotation);

                if (charControl.characterType == -1)
                {
                    charControl.characterType = Game.characterType.IntValue;
                    if (Game.allowCharChange.IntValue == 1)
                    {
                        charControl.characterType = player.teamIndex;
                    }
                }

                if (charControl.characterType == 1000)
                    SpectatorCamSpawnRequest.Create(PostUpdateCommands, position, rotation, playerEntity);
                else
                    CharacterSpawnRequest.Create(PostUpdateCommands, charControl.characterType, position, rotation, playerEntity);

                continue;
            }

            // Has new new entity been requested
            if (charControl.requestedCharacterType != -1)
            {
                if (charControl.requestedCharacterType != charControl.characterType)
                {
                    charControl.characterType = charControl.requestedCharacterType;
                    if (player.controlledEntity != Entity.Null)
                    {

                        // Despawn current controlled entity. New entity will be created later
                        if (EntityManager.HasComponent<Character>(controlledEntity))
                        {
                            var predictedState = EntityManager.GetComponentData<CharacterPredictedData>(controlledEntity);
                            var rotation = predictedState.velocity.magnitude > 0.01f ? Quaternion.LookRotation(predictedState.velocity.normalized) : Quaternion.identity;

                            CharacterDespawnRequest.Create(PostUpdateCommands, controlledEntity);
                            CharacterSpawnRequest.Create(PostUpdateCommands, charControl.characterType, predictedState.position, rotation, playerEntity);
                        }
                        player.controlledEntity = Entity.Null;
                    }
                }
                charControl.requestedCharacterType = -1;
                continue;
            }

            if (EntityManager.HasComponent<HealthStateData>(controlledEntity))
            {
                // Is character dead ?
                var healthState = EntityManager.GetComponentData<HealthStateData>(controlledEntity);
                if (healthState.health == 0)
                {
                    // Send kill msg
                    if (healthState.deathTick == m_World.worldTime.tick)
                    {
                        var killerEntity = healthState.killedBy;
                        var killerIndex = FindPlayerControlling(ref playerStates, killerEntity);
                        PlayerState killerPlayer = null;
                        if (killerIndex != -1)
                        {
                            killerPlayer = playerStates[killerIndex];
                            var format = s_KillMessages[Random.Range(0, s_KillMessages.Length)];
                            var l = StringFormatter.Write(ref _msgBuf, 0, format, killerPlayer.playerName, player.playerName, m_TeamColors[killerPlayer.teamIndex], m_TeamColors[player.teamIndex]);
                            chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                        }
                        else
                        {
                            var format = s_SuicideMessages[Random.Range(0, s_SuicideMessages.Length)];
                            var l = StringFormatter.Write(ref _msgBuf, 0, format, player.playerName, m_TeamColors[player.teamIndex]);
                            chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                        }
                        m_GameMode.OnPlayerKilled(player, killerPlayer);
                    }

                    // Respawn dead players except if in ended mode
                    if (m_EnableRespawning && (m_World.worldTime.tick - healthState.deathTick) *
                        m_World.worldTime.tickInterval > respawnDelay.IntValue)
                    {
                        // Despawn current controlled entity. New entity will be created later
                        if (EntityManager.HasComponent<Character>(controlledEntity))
                            CharacterDespawnRequest.Create(PostUpdateCommands, controlledEntity);
                        player.controlledEntity = Entity.Null;
                    }
                }
            }
        }
    }

    internal void RequestNextChar(PlayerState player)
    {
        if (!player.enableCharacterSwitch)
            return;

        var heroTypeRegistry = m_ResourceSystem.GetResourceRegistry<HeroTypeRegistry>();
        var c = player.GetComponent<PlayerCharacterControl>();
        c.requestedCharacterType = (c.characterType + 1) % heroTypeRegistry.entries.Count;

        chatSystem.SendChatMessage(player.playerId, "Switched to: " + heroTypeRegistry.entries[c.requestedCharacterType].name);
    }

    public void CreateTeam(string name)
    {
        var team = new Team();
        team.name = name;
        teams.Add(team);

        // Update clients
        var idx = teams.Count - 1;
        if (idx == 0) gameModeState.teamName0 = name;
        if (idx == 1) gameModeState.teamName1 = name;
    }

    // Assign to team with fewest members
    public void AssignTeam(PlayerState player)
    {
        // Count team sizes
        var players = playersComponentGroup.GetComponentArray<PlayerState>();
        int[] teamCount = new int[teams.Count];
        for (int i = 0, c = players.Length; i < c; ++i)
        {
            var idx = players[i].teamIndex;
            if (idx < teamCount.Length)
                teamCount[idx]++;
        }

        // Pick smallest
        int joinIndex = -1;
        int smallestTeamSize = 1000;
        for (int i = 0, c = teams.Count; i < c; i++)
        {
            if (teamCount[i] < smallestTeamSize)
            {
                smallestTeamSize = teamCount[i];
                joinIndex = i;
            }
        }

        // Join 
        player.teamIndex = joinIndex < 0 ? 0 : joinIndex;
        GameDebug.Log("Assigned team " + joinIndex + " to player " + player);
    }

    int FindPlayerControlling(ref ComponentArray<PlayerState> players, Entity entity)
    {
        if (entity == Entity.Null)
            return -1;

        for (int i = 0, c = players.Length; i < c; ++i)
        {
            var playerState = players[i];
            if (playerState.controlledEntity == entity)
                return i;
        }
        return -1;
    }

    public bool GetRandomSpawnTransform(int teamIndex, ref Vector3 pos, ref Quaternion rot)
    {
        // Make list of spawnpoints for team 
        var teamSpawns = new List<SpawnPoint>();
        var spawnPoints = m_SpawnPointComponentGroup.GetComponentArray<SpawnPoint>();
        for (var i = 0; i < spawnPoints.Length; i++)
        {
            var spawnPoint = spawnPoints[i];
            if (spawnPoint.teamIndex == teamIndex)
                teamSpawns.Add(spawnPoint);
        }

        if (teamSpawns.Count == 0)
            return false;

        var index = (m_prevTeamSpawnPointIndex[teamIndex] + 1) % teamSpawns.Count;
        m_prevTeamSpawnPointIndex[teamIndex] = index;
        pos = teamSpawns[index].transform.position;
        rot = teamSpawns[index].transform.rotation;

        GameDebug.Log("spawning at " + teamSpawns[index].name);

        return true;
    }

    static string[] s_KillMessages = new string[]
    {
        "<color={2}>{0}</color> killed <color={3}>{1}</color>",
        "<color={2}>{0}</color> terminated <color={3}>{1}</color>",
        "<color={2}>{0}</color> ended <color={3}>{1}</color>",
        "<color={2}>{0}</color> owned <color={3}>{1}</color>",
    };

    static string[] s_SuicideMessages = new string[]
    {
        "<color={1}>{0}</color> rebooted",
        "<color={1}>{0}</color> gave up",
        "<color={1}>{0}</color> slipped and accidently killed himself",
        "<color={1}>{0}</color> wanted to give the enemy team an edge",
    };

    static string[] m_TeamColors = new string[]
    {
        "#1EA00000", //"#FF19E3FF",
        "#1EA00001", //"#00FFEAFF",
    };

    readonly GameWorld m_World;
    readonly BundledResourceManager m_ResourceSystem;
    readonly GameModeSystemSettings m_Settings;
    int[] m_prevTeamSpawnPointIndex = new int[2];
    IGameMode m_GameMode;
    bool m_EnableRespawning = true;
    string m_CurrentGameModeName;
}
