using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using Macrometa;
using BestHTTP.WebSocket;
using UnityEngine.Networking;
using System.Linq;
using Random = UnityEngine.Random;


public class GDNNetworkDriver : MonoBehaviour {
    
    public static bool overrideIsServer = false;
    public static bool overrideIsServerValue = false;
    public static bool isPlayStatsClientOn = false;

    public GDNData baseGDNData;
    
    public bool isServer = false;
    public static bool isMonitor= false; //is running in network monitor not game

    public GDNStreamDriver gdnStreamDriver;
    public GDNErrorhandler gdnErrorHandler;
    public GDNKVDriver gdnKVDriver;
    public static GameRecordValue gameRecordValue;
    private static float nextKVValuePut = 0;
    public float nextKVValuePutIncr = 5;
    public long kvValueTTL = 15;

    public virtual void Awake() {
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
        if (!isMonitor && isServer) {
            gdnKVDriver = new GDNKVDriver(this);
        }

        GDNStreamDriver.isPlayStatsClientOn = isPlayStatsClientOn;
        gdnStreamDriver = new GDNStreamDriver(this);
        gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
        if (gdnStreamDriver.statsGroupSize < 1) {
            gdnStreamDriver.statsGroupSize = 10; //seconds
        }

        gdnStreamDriver.statsGroupSize = 1;// hard coded for rifleShots
        gdnStreamDriver.dummyTrafficQuantity = defaultConfig.dummyTrafficQuantity;
        if (gdnStreamDriver.dummyTrafficQuantity < 0) {
            gdnStreamDriver.dummyTrafficQuantity = 0; 
        }

        gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
        GameDebug.Log("Setup: " + gdnStreamDriver.nodeId);
        
        gdnStreamDriver.serverInStreamName = RwConfig.ReadConfig().gameName + "_InStream";
        gdnStreamDriver.serverOutStreamName = RwConfig.ReadConfig().gameName + "_OutStream";
        gdnStreamDriver.serverStatsStreamName =  "Unity" + "_StatsStream";
        gdnStreamDriver.serverName = gdnStreamDriver.consumerName;
        GDNStats.gameName =  RwConfig.ReadConfig().gameName;
       
        if (isServer) {
            gdnStreamDriver.consumerStreamName = gdnStreamDriver.serverInStreamName;
            gdnStreamDriver.producerStreamName = gdnStreamDriver.serverOutStreamName;
        }
        else {
            gdnStreamDriver.consumerStreamName = gdnStreamDriver.serverOutStreamName;
            gdnStreamDriver.producerStreamName = gdnStreamDriver.serverInStreamName;
            gdnStreamDriver.setRandomClientName();
        }

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

        LogFrequency.AddLogFreq("consumer1.OnMessage",1,"consumer1.OnMessage data: ",3);
        LogFrequency.AddLogFreq("ProducerSend Data",1,"ProducerSend Data: ",3);

        GDNStreamDriver.isStatsOn &= GDNStreamDriver.isSocketPingOn && isServer;
        GameDebug.Log("  GDNNetworkDriver Awake end");
    }
    
    
    public virtual void Update() {
        SetupLoopBody();
    }

    public void SetupLoopBody() {
        
        
        gdnStreamDriver.ExecuteCommands();
        if (gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
        if (gdnErrorHandler.currentNetworkErrors >= gdnErrorHandler.increasePauseConnectionError) {
            gdnErrorHandler.pauseNetworkError *= gdnErrorHandler.pauseNetworkErrorMultiplier;
            
            return;
        }

        if (gdnErrorHandler.isWaiting) return;
        if (!isMonitor && isServer) {
            if (!gdnKVDriver.kvCollectionListDone) {
                GameDebug.Log("kvCollectionListDone not done");
                gdnKVDriver.GetListKVColecions();
                return;
            }

            if (!gdnKVDriver.gamesKVCollectionExists) {
                GameDebug.Log("Setup  gamesKVCollectionExists  A");
                gdnKVDriver.CreateGamesKVCollection();
                return;
            }
        }

        
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
        
        if (!gdnStreamDriver.setupComplete) {
            if (GDNStreamDriver.isPlayStatsClientOn) {
                PingStatsGroup.Init(Application.dataPath, "LatencyStats", gdnStreamDriver.statsGroupSize); 
            }
            GameDebug.Log("Set up Complete as " + RwConfig.ReadConfig().gameName + " : " + gdnStreamDriver.consumerName);
            gdnStreamDriver.setupComplete = true;
            GDNTransport.setupComplete = true;
        }
        if (!gdnStreamDriver.sendConnect && !isServer) {
            GameDebug.Log("Connect after complete " + RwConfig.ReadConfig().gameName + " : " + gdnStreamDriver.consumerName);
            gdnStreamDriver.Connect(); // called on main thread so this is OK
            gdnStreamDriver.sendConnect = true;
            
        }
        
        if (GDNStreamDriver.isSocketPingOn && !gdnStreamDriver.pingStarted ) {
            gdnStreamDriver.pingStarted = true;
            if (GDNStreamDriver.isStatsOn) {
                PingStatsGroup.Init(Application.dataPath, "LatencyStats", gdnStreamDriver.statsGroupSize);
                gdnStreamDriver.InitPingStatsGroup();
                GameDebug.Log("isSocketPingOn: " + PingStatsGroup.latencyGroupSize);
            }
            StartCoroutine(gdnStreamDriver.RepeatTransportPing());
        }
        
        if (!isMonitor && isServer) {
            if (nextKVValuePut < Time.time) {
               //GameDebug.Log("loop kvValuePut");
                nextKVValuePut = Time.time + nextKVValuePutIncr;
                gdnKVDriver.putKVValueDone = false;
                var gameRecord = new GameRecord() {
                    _key = RwConfig.ReadConfig().gameName,
                    value = JsonUtility.ToJson(gameRecordValue),
                    expireAt = GameRecord.UnixTSNow(kvValueTTL)
                };
                gdnKVDriver.PutKVValue(gameRecord);
                return;
            }
        }
    }

    public  void UpdateGameRecord( string gameMode,  string mapName,  int maxPlayers, 
        int currPlayers, string status, long statusChangeTime ) {
        if ( gameRecordValue.gameMode != gameMode ||
             gameRecordValue.mapName != mapName ||
             gameRecordValue.maxPlayers != maxPlayers ||
             gameRecordValue.currPlayers != currPlayers ||
             gameRecordValue.status != status ||
             gameRecordValue.statusChangeTime != statusChangeTime ||
             gameRecordValue.streamName != RwConfig.ReadConfig().gameName
        ) {
           
            gameRecordValue = new GameRecordValue() {
                gameMode = gameMode,
                mapName = mapName,
                maxPlayers = maxPlayers,
                currPlayers = currPlayers,
                status = status,
                statusChangeTime = statusChangeTime,
                streamName = RwConfig.ReadConfig().gameName,
            };
            nextKVValuePut = 0;
            GameDebug.Log("UpdateGameRecord: " +gameRecordValue.ToString());
        }

    }
    
    public struct GDNConnection {
        public string source;
        public string destination;
        public int port;
        public int id;
        public string playerName;
    }
    
    public struct DriverTransportEvent {
        public enum Type {
            Data,
            Connect,
            Disconnect,
            Empty,
        }
        public Type type;
        public int connectionId;
        public byte[] data;
        public int dataSize;
    }





}
