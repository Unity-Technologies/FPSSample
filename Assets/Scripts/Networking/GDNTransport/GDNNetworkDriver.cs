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
   
    
    public GDNData baseGDNData;
    public string gameName = "FPSGameX8";
    public bool isServer = false;

    public GDNStreamDriver gdnStreamDriver;


    public void Awake() {
        _gdnErrorHandler = new GDNErrorhandler();
       
        BestHTTP.HTTPManager.Setup();
        BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
        //var configGDNjson = Resources.Load<TextAsset>("configGDN");
        var defaultConfig = RwConfig.ReadConfig();
        RwConfig.Flush();
        baseGDNData = defaultConfig.gdnData;
        gameName = defaultConfig.gameName;
        if (overrideIsServer) {
            isServer = overrideIsServerValue;
        }
        else {
            isServer = defaultConfig.isServer;
        }
        // error handler and baseGDNData need to assigned before creating other handlers
       // _gdnkvHandler = new GDNKVHandler(this);
       gdnStreamDriver = new GDNStreamDriver(this);
        gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
        if (gdnStreamDriver.statsGroupSize < 1) {
            gdnStreamDriver.statsGroupSize = 10; //seconds
        }

        gdnStreamDriver.dummyTrafficQuantity = defaultConfig.dummyTrafficQuantity;
        if (gdnStreamDriver.dummyTrafficQuantity < 0) {
            gdnStreamDriver.dummyTrafficQuantity = 0; 
        }

        gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
        GameDebug.Log("Setup: " + gdnStreamDriver.nodeId);
       
       
        
        gdnStreamDriver.serverInStreamName = gameName + "_InStream";
        gdnStreamDriver.serverOutStreamName = gameName + "_OutStream";
        gdnStreamDriver.serverStatsStreamName =  gameName + "_StatsStream";
        gdnStreamDriver.serverName = gdnStreamDriver.consumerName;
       
        if (isServer) {
            gdnStreamDriver.consumerStreamName = gdnStreamDriver.serverInStreamName;
            gdnStreamDriver.producerStreamName = gdnStreamDriver.serverOutStreamName;
        }
        else {
            gdnStreamDriver.consumerStreamName = gdnStreamDriver.serverOutStreamName;
            gdnStreamDriver.producerStreamName = gdnStreamDriver.serverInStreamName;
            gdnStreamDriver.setRandomClientName();
        }
        
        LogFrequency.AddLogFreq("consumer1.OnMessage",1,"consumer1.OnMessage data: ",3);
        LogFrequency.AddLogFreq("ProducerSend Data",1,"ProducerSend Data: ",3);

        GDNStreamDriver.isStatsOn &= GDNStreamDriver.isSocketPingOn && isServer;
    }
    
    
    void Update() {
        SetupLoopBody();
    }

    public void SetupLoopBody() {
        
        if (_gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
        if (_gdnErrorHandler.currentNetworkErrors >= _gdnErrorHandler.increasePauseConnectionError) {
            _gdnErrorHandler.pauseNetworkError *= _gdnErrorHandler.pauseNetworkErrorMultiplier;
            
            return;
        }

        if (_gdnErrorHandler.isWaiting) return;
        /*
        if (!_gdnkvHandler.kvCollectionListDone) {
            GameDebug.Log("kvCollectionListDone not done");
            _gdnkvHandler.GetListKVColecions();
            return;
        }

        if (!_gdnkvHandler.gamesKVCollectionExists) {
            _gdnkvHandler.CreateGamesKVCollection();
            return;
        }
        
        if (!kvValueListDone) {
            GetListKVValues();
        }
        */
        // more complex loop with delays
        //  check for matching values in list
        // 
        
        
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
            return;
        }

        if (!gdnStreamDriver.setupComplete) {
            GameDebug.Log("Set up Complete as " + gameName + " : " + gdnStreamDriver.consumerName);
            gdnStreamDriver.setupComplete = true;
            GDNTransport.setupComplete = true;
        }
        if (!gdnStreamDriver.sendConnect && !isServer) {
            GameDebug.Log("Connect after complete " + gameName + " : " + gdnStreamDriver.consumerName);
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

        gdnStreamDriver.ExecuteCommands();
    }
    
    public struct GDNConnection {
        public string source;
        public string destination;
        public int port;
        public int id;
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

    public  GDNErrorhandler _gdnErrorHandler;
    
#region Streamdriver

//init progress


// for latency & data rate testing

#endregion   
     
     #region Latency & bandwidth testing

     #endregion
     
     //these these are methods that were being called asynchronously
     // and access concurrent collections
     // putting a command queue that runs on the mand thread 
     // means only the main thread trys to update the collections.

     #region CommandQueue

     //execute commands in queue
     //no guarantee that all commands will be executed in a single call

     #endregion
   
     #region Collections

     private GDNKVHandler _gdnkvHandler;
     

     public GDNNetworkDriver() {
         
     }

     #endregion




}
