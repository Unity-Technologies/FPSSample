using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BestHTTP.WebSocket;
using Random = UnityEngine.Random;

namespace Macrometa {
    public class GDNClientBrowserNetworkDriver : GDNNetworkDriver {
        
        public bool debugKVTryInit =true;

        public string localId;
        public bool tryKVInit = false;
        public bool tryKVInitB = false;
        public bool canNotInit = false;
        public bool initSucceeded = false;
        public bool initFail=false;
        public bool initTrying = false;
        public string clientId;
        public int maxConfirmInit = 3;
        public int currentConfirmInit = 3;
        

        public GameList gameList = new GameList();
        private float nextKVValueGet = 0;
        public float nextKVValueGetIncr = 5;
        
        protected PingData transportPingData;
        public bool sendTransportPing = false;
        public PingData debugPingData;
        public bool waitStreamClearing = false;
        public float streamClearTime = 0;
        
        public override void Awake() {
            GDNStreamDriver.isClientBrowser = true;
            GDNStreamDriver.localId = localId;
            gdnErrorHandler = new GDNErrorhandler();
            var defaultConfig = RwConfig.ReadConfig();
            RwConfig.Flush();
            baseGDNData = defaultConfig.gdnData;
            //gameName = defaultConfig.gameName;
            BestHTTP.HTTPManager.Setup();
            BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
            gdnStreamDriver = new GDNStreamDriver(this);
            gdnKVDriver = new GDNKVDriver(this);
            gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
            if (gdnStreamDriver.statsGroupSize < 1) {
                gdnStreamDriver.statsGroupSize = 10; //seconds
            }
            gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
            GameDebug.Log("Setup GDNClientBrowserNetworkDriver: " + gdnStreamDriver.nodeId);
            setRandomClientName();
            gdnStreamDriver.chatStreamName = "FPSChat";
            gdnStreamDriver.chatChannelId = "_Lobby";
            gameList = gdnKVDriver.gameList;
            gameList.isDirty = true;
            MakeGDNConnection(null); //servers are all using default name server.
        }

        public void MakeGDNConnection(GameRecordValue grv) {
            var destination = "Server";
            if (grv != null) {
                destination = grv.clientId;
            }
                
            gdnStreamDriver.setRandomClientName();
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = gdnStreamDriver.consumerName,
                destination = destination,
                port = 443
            };

            var id =gdnStreamDriver.AddOrGetConnectionId(connection);
            
        }
        
        public void OnDisable() {
            GameDebug.Log("GDNClientBrowserNetworkDriver OnDisable");
        }

        public void setRandomClientName() {
            clientId = "Cl" + (10000000 + Random.Range(1, 89999999)).ToString();
        }

        public override void Update() {
            Bodyloop();
        }
        
        public void Bodyloop() {

            if (gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
            if (gdnErrorHandler.currentNetworkErrors >= gdnErrorHandler.increasePauseConnectionError) {
                gdnErrorHandler.pauseNetworkError *= gdnErrorHandler.pauseNetworkErrorMultiplier;
                return;
            }

            if (gdnErrorHandler.isWaiting) return;

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

            if (!gdnStreamDriver.streamListDone) {
                gdnStreamDriver.GetListStream();
                return;
            }
            
            if (!gdnStreamDriver.chatStreamExists) {
                gdnStreamDriver.CreateChatStream();
                return;
            }
            
            if (!gdnStreamDriver.chatProducerExists) {
                gdnStreamDriver.CreateChatProducer(gdnStreamDriver.chatStreamName);
                return;
            }

            if (!gdnStreamDriver.chatConsumerExists) {
                gdnStreamDriver.CreateChatConsumer(gdnStreamDriver.chatStreamName, gdnStreamDriver.consumerName);
                return;
            }
            
            
            if (debugKVTryInit) {
                debugKVTryInit = false;
                tryKVInit = true;
            }
            
            if (tryKVInit) {
                tryKVInit = false;
                tryKVInitB = true;
                gdnKVDriver.kvValueListDone = false;
            }
            
           
            if (!gdnKVDriver.kvValueListDone  ||  nextKVValueGet < Time.time) {
                //GameDebug.Log("Setup  kvValueListDone ");
                nextKVValueGet = Time.time + nextKVValueGetIncr;
                gdnKVDriver.GetListKVValues();
                return;
            }
            
            if (tryKVInitB) {
                GameDebug.Log("Setup  tryKVInit C: " + RwConfig.ReadConfig().gameName);
                initTrying = true;
                tryKVInitB = false;
               
               var foundRecord =  gdnKVDriver.listKVValues.result.
                   SingleOrDefault(l => l._key == RwConfig.ReadConfig().gameName);
               

               if (foundRecord != null) {
                   //GameDebug.Log("gameName: " + RwConfig.ReadConfig().gameName);
                   canNotInit = true;
                   initTrying = false;
                   initFail = true;
                   GameDebug.Log("Setup  tryKVInit C fail");
               }
               else {
                   //var record = GameRecord.GetInit(clientId, RwConfig.ReadConfig().gameName, 60);
                   var record = GameRecord.GetInit(clientId, RwConfig.ReadConfig().gameName, 60);
                   //GameDebug.Log(record.value);
                   gdnKVDriver.putKVValueDone = false;
                   gdnKVDriver.PutKVValue(record);
                   currentConfirmInit = 0;
               }
               return;
            }

            if (gdnKVDriver.putKVValueDone) {
                gdnKVDriver.putKVValueDone = false;
                gdnKVDriver.kvValueListDone = false;
                return;
            }
            
            if ( initTrying) {
               
                GameDebug.Log("Setup  initTrying D");
                var foundRecord =  gdnKVDriver.listKVValues.result.
                    FirstOrDefault(l => l._key == RwConfig.ReadConfig().gameName);
               if (foundRecord == null ) {
                   GameDebug.Log("Setup  initTrying E put record in put not found");
                    initFail = true;
                    initTrying = false;
                    return;
               }
            
               var gameRecord = JsonUtility.FromJson<GameRecordValue>(foundRecord.value);
               if (gameRecord.clientId != clientId) {
                   GameDebug.Log("Setup  initTrying F other init same name");
                   initFail = true;
                   initTrying = false;
                   return;
               }

               if (currentConfirmInit < maxConfirmInit) {
                   GameDebug.Log("Setup  initTrying G");
                   gdnKVDriver.kvValueListDone = false;
                   currentConfirmInit++;
                   return;
               }
               GameDebug.Log("Setup  initTrying H succeed");
               gdnKVDriver.putKVValueDone = false;
               initTrying = false; 
               initSucceeded = true;
            }

            if (StreamsBodyLoop()) {
               // debugPingData = transportPingData.Copy();
                return;
            }

        }

        /// <summary>
        /// returns true if containing loop should return
        /// </summary>
        /// <returns></returns>
        public bool  StreamsBodyLoop() {
            if (waitStreamClearing) {
                if (Time.time > streamClearTime) {
                    FinishClearStreams();
                }
                return false; // none streams action OK
            }

            SetPings();
            if (transportPingData == null) {
                return false;
            }
            
            if (!gdnStreamDriver.producerExists) {
                gdnStreamDriver.CreateProducer(gdnStreamDriver.producerStreamName);
                return true;
            }

            if (!gdnStreamDriver.consumerExists) {
                gdnStreamDriver.CreateConsumerPongOnly(gdnStreamDriver.consumerStreamName, gdnStreamDriver.consumerName);
                return false;
            }

            if (sendTransportPing) {
                GameDebug.Log(" PingBodyLoop() sendTransportPing");
                gdnStreamDriver.SendSimpleTransportPing();
                sendTransportPing = false;
            }

            if (TransportPings.PingTime() > 15000) {

                // what shoud gdnStreamDriver.receivedPongOnly
                // be set to?
                gameRecordValue.ping = -1;
                StartClearStreams();
                transportPingData = null;
                TransportPings.Clear();
                sendTransportPing = false;
                gdnStreamDriver.receivedPongOnly = false;
            }

            if (gdnStreamDriver.receivedPongOnly) {
                gdnStreamDriver.receivedPongOnly = false;
                transportPingData.pingCount++;
                if (transportPingData.pingCount > 3) {
                    GameDebug.Log("pingCount set "+ gameRecordValue.streamName + " : "+ gdnStreamDriver.pongOnlyRtt) ;
                    gameRecordValue.ping = gdnStreamDriver.pongOnlyRtt;
                    StartClearStreams();
                    transportPingData = null;
                }
                else {
                    sendTransportPing = true;
                }
            }
            return false;
        }

        public void SetPings() {
            if (transportPingData == null) {
                var grv = gameList.UnpingedGame();
                if (grv != null) {
                    transportPingData = new PingData() {
                        grv = grv
                    };
                    //ClearStreams();
                    gdnStreamDriver.producerStreamName = grv.streamName + "_InStream";
                    gdnStreamDriver.consumerStreamName  = grv.streamName + "_OutStream";
                    sendTransportPing = true;
                    gameRecordValue = grv;
                }
            }
            
        }

        public void StartClearStreams() {
            GameDebug.Log("ClearStreams");
            if (gdnStreamDriver.consumer1 != null) {
                gdnStreamDriver.consumer1.Close();
            }
            if (gdnStreamDriver.producer1 != null) {
                gdnStreamDriver.producer1.Close();
            }

            waitStreamClearing = true;
            streamClearTime = Time.time + 0.5f;
        }

        public void FinishClearStreams() {
            gdnStreamDriver.consumer1 = null;
            gdnStreamDriver.producer1  = null;
            
            gdnStreamDriver.producerExists = false;
            gdnStreamDriver.consumerExists = false;
            
            waitStreamClearing = false;
        }
        
    }
}