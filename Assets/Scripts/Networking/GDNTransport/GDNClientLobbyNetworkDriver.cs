using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BestHTTP.WebSocket;
using Macrometa.Lobby;
using Random = UnityEngine.Random;

namespace Macrometa {
    public class GDNClientLobbyNetworkDriver : GDNNetworkDriver {
        
        public bool debugKVTryInit =true;

        public string localId;
        public string gameMode;
        public string mapName;
        public int maxPlayers; 
        
        
        public bool tryKVInit = false;
        public bool tryKVInitB = false;
        public bool canNotInit = false;
        public bool initSucceeded = false;
        public bool initFail=false;
        public bool initTrying = false;
        public string clientId;
        public int maxConfirmInit = 3;
        public int currentConfirmInit = 3;
        public GdnKvLobbyDriver gdnKvLobbyDriver;
        public LobbyValue lobbyValue;

        public LobbyList lobbyList = new LobbyList();
        private float nextKVValueGet = 0;
        public float nextKVValueGetIncr = 5;
        
        protected Lobby.PingData transportPingData;
        public bool sendTransportPing = false;
        public Lobby.PingData debugPingData;
        public bool waitStreamClearing = false;
        public float streamClearTime = 0;

        static  GDNClientLobbyNetworkDriver _inst;
        
        public override void Awake() {
            _inst = this;
            GDNStreamDriver.isClientBrowser = true;
            GDNStreamDriver.localId = localId;
            gdnErrorHandler = new GDNErrorhandler();
            var defaultConfig = RwConfig.ReadConfig();
            RwConfig.Flush();
            baseGDNData = defaultConfig.gdnData;
            BestHTTP.HTTPManager.Setup();
            BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
            gdnStreamDriver = new GDNStreamDriver(this);
            gdnKvLobbyDriver = new GdnKvLobbyDriver(this);
            gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
            if (gdnStreamDriver.statsGroupSize < 1) {
                gdnStreamDriver.statsGroupSize = 10; //seconds
            }
            gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
            GameDebug.Log("Setup GDNClientBrowserNetworkDriver: " + gdnStreamDriver.nodeId);
            setRandomClientName();
            gdnStreamDriver.chatStreamName = "FPSChat";
            gdnStreamDriver.chatChannelId = "_Lobby";
            lobbyList = gdnKvLobbyDriver.lobbyList;
            lobbyList.isDirty = true;
            MakeGDNConnection(null); //servers are all using default name server.
        }

        public void MakeGDNConnection(LobbyValue aL) {
            var destination = "Server";
            if (aL != null) {
                destination = aL.clientId;
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

            if (!gdnKvLobbyDriver.kvCollectionListDone) {
                GameDebug.Log("kvCollectionListDone not done");
                gdnKvLobbyDriver.GetListKVColecions();
                return;
            }

            if (!gdnKvLobbyDriver.lobbiesKVCollectionExists) {
                GameDebug.Log("Setup  lobbiesKVCollectionExists  A");
                gdnKvLobbyDriver.CreateLobbiesKVCollection();
                return;
            }

             
            if (!gdnStreamDriver.regionIsDone) {
                gdnStreamDriver.GetRegion();
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
                gdnKvLobbyDriver.kvValueListDone = false;
            }
            
           
            if (!gdnKvLobbyDriver.kvValueListDone  ||  nextKVValueGet < Time.time) {
                //GameDebug.Log("Setup  kvValueListDone ");
                nextKVValueGet = Time.time + nextKVValueGetIncr;
                gdnKvLobbyDriver.GetListKVValues();
                return;
            }
            
            if (tryKVInitB) {
                GameDebug.Log("Setup  tryKVInit C: " + RwConfig.ReadConfig().gameName);
                initTrying = true;
                tryKVInitB = false;
               
               var foundRecord =  gdnKvLobbyDriver.listKVValues.result.
                   SingleOrDefault(l => l._key == RwConfig.ReadConfig().gameName);
               

               if (foundRecord != null) {
                   canNotInit = true;
                   initTrying = false;
                   initFail = true;
                   GameDebug.Log("Setup  tryKVInit C fail");
               }
               else {
                   var unassigned = new TeamInfo();
                   //unassigned.slots = new List<TeamSlot>();
                   lobbyValue = new LobbyValue() {
                       adminName = localId,
                       gameMode = gameMode,
                       mapName = mapName,
                       clientId = clientId,
                       maxPlayers = maxPlayers,
                       streamName = RwConfig.ReadConfig().gameName,
                       region = gdnStreamDriver.region,
                   };
                   lobbyValue.unassigned.slots.Add(SelfTeamSlot());
                   AddDummyTeamSlots(0, 4);
                   AddDummyTeamSlots(1, 3);
                   AddDummyTeamSlots(2, 10);
                   var record = LobbyRecord.GetFromLobbyValue( lobbyValue, 60 );
                   //GameDebug.Log(record.value);
                   gdnKvLobbyDriver.putKVValueDone = false;
                   gdnKvLobbyDriver.PutKVValue(record);
                   currentConfirmInit = 0;
               }
               return;
            }

            if (gdnKvLobbyDriver.putKVValueDone) {
                gdnKvLobbyDriver.putKVValueDone = false;
                gdnKvLobbyDriver.kvValueListDone = false;
                return;
            }
            
            if ( initTrying) {
               
                GameDebug.Log("Setup  initTrying D");
                var foundRecord =  gdnKvLobbyDriver.listKVValues.result.
                    FirstOrDefault(l => l._key == RwConfig.ReadConfig().gameName);
               if (foundRecord == null ) {
                   GameDebug.Log("Setup  initTrying E put record in put not found");
                    initFail = true;
                    initTrying = false;
                    lobbyValue = null;
                    return;
               }
            
               var aLobbyRecord = JsonUtility.FromJson<LobbyValue>(foundRecord.value);
               if (aLobbyRecord.clientId != clientId) {
                   GameDebug.Log("Setup  initTrying F other init same name");
                   initFail = true;
                   lobbyValue = null;
                   initTrying = false;
                   return;
               }

               if (currentConfirmInit < maxConfirmInit) {
                   GameDebug.Log("Setup  initTrying G");
                   gdnKvLobbyDriver.kvValueListDone = false;
                   currentConfirmInit++;
                   return;
               }
               GameDebug.Log("Setup  initTrying H succeed");
               gdnKvLobbyDriver.putKVValueDone = false;
               initTrying = false; 
               initSucceeded = true;
            }

            if (StreamsBodyLoop()) {
               debugPingData = transportPingData.Copy();
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
                lobbyValue.ping = -1;
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
                    GameDebug.Log("pingCount set "+ lobbyValue.streamName + " : "+ gdnStreamDriver.pongOnlyRtt) ;
                    lobbyValue.ping = gdnStreamDriver.pongOnlyRtt;
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
                var unpingedLobby = lobbyList.UnpingedLobby();
                if (unpingedLobby != null) {
                    transportPingData = new Lobby.PingData() {
                        lobbyValue = unpingedLobby
                    };
                    //ClearStreams();
                    gdnStreamDriver.producerStreamName = unpingedLobby.streamName + "_InStream";
                    gdnStreamDriver.consumerStreamName  = unpingedLobby.streamName + "_OutStream";
                    sendTransportPing = true;
                    lobbyValue = unpingedLobby;
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
        
        #region LobbyManipulation
        public Lobby.TeamSlot SelfTeamSlot() {
            var result = new TeamSlot() {
                playerName = localId,
                clientId = clientId,
                region = gdnStreamDriver.region,
                ping = gdnStreamDriver.chatProducer1.Latency,
            };
            return result;
        }
        /// <summary>
        /// This is static to make calling from UI easier
        /// </summary>
        /// <param name="teamIndex"></param>
        static public void MoveToTeam(int teamIndex) {
            _inst.lobbyValue.MoveToTeam(_inst.SelfTeamSlot(), teamIndex);
            
        }

        public void UpdateLobby() {
            var record = LobbyRecord.GetFromLobbyValue( lobbyValue, 60 );
            gdnKvLobbyDriver.PutKVValue(record);
        }
        
        
        //testing

        public void AddDummyTeamSlots(int teamIndex, int count) {
            for (int i = 0; i < count; i++) {
                var dummy = DummyTeamSlot();
                lobbyValue.MoveToTeam(dummy, teamIndex);
            }
        }
        
        public Lobby.TeamSlot DummyTeamSlot() {
            var result = new TeamSlot() {
                playerName = "Dummy "+ Random.Range(1,1000).ToString(),
                clientId = Random.Range(1,1000).ToString(),
                region = gdnStreamDriver.region,
                ping = gdnStreamDriver.chatProducer1.Latency,
            };
            return result;
        }
        
        
        
        #endregion LobbyManipulation
        
    }
}