using UnityEngine;
using Macrometa.Lobby;

namespace Macrometa {
    //

    public class GDNCServerLobbyNetworkDriver2 : GDNNetworkDriver {
        public GdnDocumentLobbyDriver gdnDocumentLobbyDriver;
        public float nextRefreshLobbyList= 0;
        public LobbyValue lobbyValue;
        public bool lobbyUpdated = false;

        public override void Awake() {
            GameDebug.Log("  GDNNetworkDriver Awake");
            PlayStats.remotePlayerCity = RwConfig.ReadConfig().userCity;
            PlayStats.remotePlayerCountry = RwConfig.ReadConfig().userCountry;
            PlayStats.remoteConnectin_Type = RwConfig.ReadConfig().connectionType;

            gdnErrorHandler = new GDNErrorhandler();

            BestHTTP.HTTPManager.Setup();
            BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
            //var configGDNjson = Resources.Load<TextAsset>("configGDN");
            var defaultConfig = RwConfig.ReadConfig();
            RwConfig.Flush();
            GameDebug.Log("  GDNNetworkDriver Awake gamename: " + defaultConfig.gameName);
            baseGDNData = defaultConfig.gdnData;
            //gameName = defaultConfig.gameName;
            if (overrideIsServer) {
                isServer = overrideIsServerValue;
            }
            else {
                isServer = defaultConfig.isServer;
            }

            // error handler and baseGDNData need to assigned before creating other handlers
            gdnDocumentLobbyDriver = new GdnDocumentLobbyDriver(this);
            GDNStreamDriver.isPlayStatsClientOn = isPlayStatsClientOn;
            gdnStreamDriver = new GDNStreamDriver(this);
            gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
            if (gdnStreamDriver.statsGroupSize < 1) {
                gdnStreamDriver.statsGroupSize = 10; //seconds
            }

            gdnStreamDriver.statsGroupSize = 1; // hard coded for rifleShots
            gdnStreamDriver.dummyTrafficQuantity = defaultConfig.dummyTrafficQuantity;
            if (gdnStreamDriver.dummyTrafficQuantity < 0) {
                gdnStreamDriver.dummyTrafficQuantity = 0;
            }

            gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
            GameDebug.Log("Setup: " + gdnStreamDriver.nodeId);

            gdnStreamDriver.serverInStreamName = RwConfig.ReadConfig().gameName + "_InStream";
            gdnStreamDriver.serverOutStreamName = RwConfig.ReadConfig().gameName + "_OutStream";
            gdnStreamDriver.serverStatsStreamName = "Unity" + "_StatsStream";
            gdnStreamDriver.serverName = gdnStreamDriver.consumerName;
            GDNStats.gameName = RwConfig.ReadConfig().gameName;

            if (isServer) {
                gdnStreamDriver.consumerStreamName = gdnStreamDriver.serverInStreamName;
                gdnStreamDriver.producerStreamName = gdnStreamDriver.serverOutStreamName;
            }
            else {
                gdnStreamDriver.consumerStreamName = gdnStreamDriver.serverOutStreamName;
                gdnStreamDriver.producerStreamName = gdnStreamDriver.serverInStreamName;
                gdnStreamDriver.setRandomClientName();
            }

            //ToDo switch document collection 
            // how do i get lobby what function()

            // need to get Lobby name from RWConfig
            // read LobbyDocument
            // make LobbyGameActive & LobbyGameMaster
            if (!isMonitor && isServer) {
                gameRecordValue = new GameRecordValue() {
                    gameMode = "",
                    mapName = "",
                    maxPlayers = 0,
                    currPlayers = 0,
                    status = GameRecord.Status.waiting.ToString(),
                    statusChangeTime = 0,
                    streamName = RwConfig.ReadConfig().gameName,
                    clientId = gdnStreamDriver.consumerName
                };
            }

            LogFrequency.AddLogFreq("consumer1.OnMessage", 1, "consumer1.OnMessage data: ", 3);
            LogFrequency.AddLogFreq("ProducerSend Data", 1, "ProducerSend Data: ", 3);

            GDNStreamDriver.isStatsOn &= GDNStreamDriver.isSocketPingOn && isServer;
            GameDebug.Log("  GDNNetworkDriver Awake end");
        }


        public override void Update() {
            SetupLoopBodyDocument();
        }

        public void SetupLoopBodyDocument() {
            gdnStreamDriver.ExecuteCommands();
            if (gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
            if (gdnErrorHandler.currentNetworkErrors >= gdnErrorHandler.increasePauseConnectionError) {
                gdnErrorHandler.pauseNetworkError *= gdnErrorHandler.pauseNetworkErrorMultiplier;

                return;
            }

            if (gdnErrorHandler.isWaiting) return;

            if (!gdnStreamDriver.regionIsDone) {
                gdnStreamDriver.GetRegion();
            }

            if (!gdnStreamDriver.streamListDone) {
                gdnStreamDriver.GetListStream();
                return;
            }

            if (!gdnStreamDriver.serverInStreamExists) {
                gdnStreamDriver.CreatServerInStream();
                return;
            }

            if (!gdnStreamDriver.serverOutStreamExists) {
                gdnStreamDriver.CreatServerOutStream();
                return;
            }

            if (!gdnStreamDriver.serverStatsStreamExists) {
                gdnStreamDriver.CreatServerStatsStream();
                return;
            }

            if (!gdnStreamDriver.gameStatsStreamExists) {
                gdnStreamDriver.CreatGameStatsStream();
                // GameDebug.Log("try.gameStatsStreamExists");
                return;
            }

            if (!gdnStreamDriver.producerExists) {
                gdnStreamDriver.CreateProducer(gdnStreamDriver.producerStreamName);
                return;
            }

            if (!gdnStreamDriver.consumerExists) {
                gdnStreamDriver.CreateConsumer(gdnStreamDriver.consumerStreamName, gdnStreamDriver.consumerName);
                return;
            }

            if (!gdnStreamDriver.producerStatsExists) {
                gdnStreamDriver.CreateStatsProducer(gdnStreamDriver.serverStatsStreamName);
                GameDebug.Log("try producerStatsExists");
                return;
            }

            if (!gdnStreamDriver.producerGameStatsExists) {
                gdnStreamDriver.CreateGameStatsProducer(gdnStreamDriver.gameStatsStreamName);
                GameDebug.Log("try producerGameStatsExists");
                return;
            }

            if (!gdnDocumentLobbyDriver.lobbyListIsDone ) {
                gdnDocumentLobbyDriver.PostLobbyListQuery();
                nextRefreshLobbyList = Time.time + 2f;
                return;
            }
            
            lobbyValue = gdnDocumentLobbyDriver.lobbyList.lobbies.
                Find(lv => lv.GameName() == RwConfig.ReadConfig().gameName);
            if (lobbyValue == null) {
                GameDebug.LogError("lobby not found: "+ RwConfig.ReadConfig().gameName);
                gdnDocumentLobbyDriver.lobbyListIsDone = false;
                return;
            } 
            
            
            //todo get key from??
            // try to get from lobbylist
            // if not put in lobbyValue
            if (!lobbyUpdated) {
                lobbyValue.showGameInitNow = true;
                var lobbyLobby = LobbyLobby.GetFromLobbyValue(lobbyValue);
                var key = gdnDocumentLobbyDriver.lobbyKey;
                gdnDocumentLobbyDriver.UpdateLobbyDocument(lobbyLobby, lobbyValue.key);
                lobbyUpdated = true;
            }

            //ToDo make PostLobbyGameMaster
            if (!gdnDocumentLobbyDriver.postLobbyStuff) {
                lobbyValue.key = "";
                GameDebug.Log("make game Master: " + lobbyValue.streamName);
                var lobbyGameMaster = LobbyGameMaster.GetFromLobbyValue(lobbyValue);
                gdnDocumentLobbyDriver.PostLobbyDocument(lobbyGameMaster);
                return;
            }

            if (!gdnStreamDriver.setupComplete) {
                if (GDNStreamDriver.isPlayStatsClientOn) {
                    PingStatsGroup.Init(Application.dataPath, "LatencyStats", gdnStreamDriver.statsGroupSize);
                }
                GameDebug.Log("Set up Complete as " + RwConfig.ReadConfig().gameName + " : " +
                              gdnStreamDriver.consumerName);
                gdnStreamDriver.setupComplete = true;
                GDNTransport.setupComplete = true;
            }

            if (!gdnStreamDriver.sendConnect && !isServer) {
                GameDebug.Log("Connect after complete " + RwConfig.ReadConfig().gameName + " : " +
                              gdnStreamDriver.consumerName);
                gdnStreamDriver.Connect(); // called on main thread so this is OK
                gdnStreamDriver.sendConnect = true;
            }

            if (GDNStreamDriver.isSocketPingOn && !gdnStreamDriver.pingStarted) {
                gdnStreamDriver.pingStarted = true;
                if (GDNStreamDriver.isStatsOn) {
                    PingStatsGroup.Init(Application.dataPath, "LatencyStats", gdnStreamDriver.statsGroupSize);
                    gdnStreamDriver.InitPingStatsGroup();
                    GameDebug.Log("isSocketPingOn: " + PingStatsGroup.latencyGroupSize);
                }
                StartCoroutine(gdnStreamDriver.RepeatTransportPing());
            }

            
        }
    }
}