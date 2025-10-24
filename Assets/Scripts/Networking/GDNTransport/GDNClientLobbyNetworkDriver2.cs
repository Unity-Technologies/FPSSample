using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BestHTTP.WebSocket;
using Macrometa.Lobby;
using Random = UnityEngine.Random;

namespace Macrometa {
    public class GDNClientLobbyNetworkDriver2 : GDNNetworkDriver {
        
        public bool startDocumentInit =true;

        public string localId;
        public string gameMode;
        public string mapName;
        public int maxPlayers; 
        
        
        public bool tryDocumentInit = false;
        public string clientId;

        public int nextGameSerialnumber = 1;
        public GdnDocumentLobbyDriver gdnDocumentLobbyDriver;
        public LobbyValue lobbyValue;
        //public bool isRttTarget;
        public bool lobbyUpdateAvail = true;
        static public bool isLobbyAdmin;
        public float nextUpdateLobby = 0;
        public float lobbyClosingTime;
        public bool closeLobby;
        public float closeLobbyInactiveTime;
        public bool closeLobbyInactive = true;
        public float closeLobbyInactiveDelay = 30;
        

        public LobbyList lobbyList = new LobbyList();
        public float nextRefreshLobbyList= 0;

        //protected Lobby.PingData transportPingData;
        public bool sendTransportPing = false;
        static public bool pingWaiting = false;
        //public Lobby.PingData debugPingData;
        public int pingCount = 0;
        public int maxPing = 3; //send this many pings for calculating RTT
            
        
        
        public bool waitStreamClearing = false;
        public float streamClearTime = 0;
        
        private ConcurrentQueue<LobbyCommand> _lobbyQueue = new ConcurrentQueue<LobbyCommand>();

        static  GDNClientLobbyNetworkDriver2 _inst;
        
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
            gdnDocumentLobbyDriver = new GdnDocumentLobbyDriver(this);
            gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
            if (gdnStreamDriver.statsGroupSize < 1) {
                gdnStreamDriver.statsGroupSize = 10; //seconds
            }
            gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
            GameDebug.Log("Setup GDNClientBrowserNetworkDriver: " + gdnStreamDriver.nodeId);
            MakeGDNConnection(null);
            clientId = gdnStreamDriver.consumerName;
            gdnStreamDriver.chatStreamName = "FPSChat";
            gdnStreamDriver.chatChannelId = "_Lobby";
            lobbyList = gdnDocumentLobbyDriver.lobbyList;
            lobbyList.isDirty = true;
            //servers are all using default name server.
        }
        public override void Update() {
            Bodyloop();
            if (closeLobbyInactive && Time.time > closeLobbyInactiveTime) {
                closeLobbyInactive = false;
                lobbyClosingTime = Time.time + 5;
                closeLobby = true;
                lobbyValue.closeLobbyNow = true;
            }
            if (closeLobby && Time.time > lobbyClosingTime) {
                closeLobby = false;
                LeaveLobby();
            }
        }

        public void CreateLobby() {
            startDocumentInit = true;
        }
        public void OnDisable() {
            GameDebug.Log("GDNClientBrowserNetworkDriver OnDisable");
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

            var id = gdnStreamDriver.AddOrGetConnectionId(connection);
        }

        public void JoinLobby(LobbyValue aLobbyValue, bool isAdmin) {
            SetIsLobbyAdmin(isAdmin);
            lobbyValue = aLobbyValue;
            GameDebug.Log("JoinLobby:" + aLobbyValue.streamName);
            gdnStreamDriver.chatChannelId = aLobbyValue.streamName;
            gdnStreamDriver.chatLobbyId = aLobbyValue.streamName;
            gdnStreamDriver.ChatSendRoomRequest(2);
            closeLobbyInactiveTime = Time.time + closeLobbyInactiveDelay;
            closeLobbyInactive = true;
        }
        
        public void LeaveLobby() {
            Debug.Log(" LeaveLobby()");
            
            if (isLobbyAdmin) {
                lobbyValue.closeLobbyNow = true;
                UpdateLobby();
            }
            SetIsLobbyAdmin(false);
            lobbyValue = new LobbyValue();
            gdnStreamDriver.chatLobbyId = "_Lobby";
            gdnStreamDriver.chatChannelId = "_Lobby";
        }

        public void SetIsLobbyAdmin(bool val) {
            GDNStreamDriver.isLobbyAdmin = val;
            isLobbyAdmin = val;
        }
        
        public void UpdateLocalLobby(LobbyValue lobbyUpdate) {
            GameDebug.Log("UpdateLocalLobby");
            lobbyValue = lobbyUpdate;
            lobbyValue.clientId = clientId;
            lobbyUpdateAvail = true;
            closeLobbyInactiveTime = Time.time + closeLobbyInactiveDelay;
            if (lobbyValue.closeLobbyNow) {
                lobbyClosingTime = Time.time + 5;
                closeLobby = true;
            }
        }
        
        public void Bodyloop() {
            if (gdnStreamDriver.lobbyUpdateAvail) {
                GameDebug.Log("gdnStreamDriver.lobbyUpdateAvail");
                UpdateLocalLobby( gdnStreamDriver.lobbyUpdate);
                gdnStreamDriver.lobbyUpdateAvail = false;
            }

            if (gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
            if (gdnErrorHandler.currentNetworkErrors >= gdnErrorHandler.increasePauseConnectionError) {
                gdnErrorHandler.pauseNetworkError *= gdnErrorHandler.pauseNetworkErrorMultiplier;
                return;
            }

            if (gdnErrorHandler.isWaiting) return;

            if (!gdnDocumentLobbyDriver.collectionListDone) {
                GameDebug.Log("CollectionListDone not done");
                gdnDocumentLobbyDriver.GetListDocumentCollections();
                return;
            }

            
            if (!gdnDocumentLobbyDriver.lobbiesCollectionExists) {
                GameDebug.Log("Setup  lobbiesCollectionExists  A");
                gdnDocumentLobbyDriver.CreateLobbiesCollection();
                return;
            }

            if (!gdnDocumentLobbyDriver.indexesListDone) {
                GameDebug.Log(" indexesListDone not done");
                gdnDocumentLobbyDriver.GetListIndexes(gdnDocumentLobbyDriver.lobbiesCollectionName);
                return;
            }

            for (int checkIndex = 0; checkIndex < gdnDocumentLobbyDriver.indexesExist.Count; checkIndex++) {
                
                if (!gdnDocumentLobbyDriver.indexesExist[checkIndex]) {
                    gdnDocumentLobbyDriver.CreateIndex( checkIndex);
                    return;
                }
            }

            if (!gdnDocumentLobbyDriver.indexTTLExist) {
                gdnDocumentLobbyDriver.CreateTTLIndex();
                return;
            }
            
            if (!gdnStreamDriver.regionIsDone) {
                gdnStreamDriver.GetRegion();
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
            
            if (!gdnDocumentLobbyDriver.lobbyListIsDone ) {
                gdnDocumentLobbyDriver.PostLobbyListQuery();
                nextRefreshLobbyList = Time.time + 2f;
                return;
            }

            if (nextRefreshLobbyList < Time.time) {
                gdnDocumentLobbyDriver.lobbyListIsDone = false;
            }
            
            if (startDocumentInit) {
                startDocumentInit = false;
                tryDocumentInit = true;
                gdnDocumentLobbyDriver.lobbyIsMade = false;
                gdnDocumentLobbyDriver.maxSerialIsDone = false;
                gdnDocumentLobbyDriver.postLobbyStuff = false;
            }

            if (tryDocumentInit) {
                if (!gdnDocumentLobbyDriver.maxSerialIsDone && !gdnDocumentLobbyDriver.lobbyIsMade) {
                    gdnDocumentLobbyDriver.PostMaxSerialQuery(RwConfig.ReadConfig().gameName);
                    return;
                }

                if (!gdnDocumentLobbyDriver.lobbyIsMade) {
                    if (gdnDocumentLobbyDriver.maxSerialResult.result.Count == 0) {
                        nextGameSerialnumber = 1 + gdnDocumentLobbyDriver.errorSerialIncr;
                    }
                    else {
                        nextGameSerialnumber = gdnDocumentLobbyDriver.maxSerialResult.result[0] + 1
                            + gdnDocumentLobbyDriver.errorSerialIncr;
                    }

                    //unassigned.slots = new List<TeamSlot>();
                    lobbyValue = new LobbyValue() {
                        adminName = localId,
                        gameMode = gameMode,
                        mapName = mapName,
                        clientId = clientId,
                        maxPlayers = maxPlayers,
                        baseName = RwConfig.ReadConfig().gameName,
                        streamName = RwConfig.ReadConfig().gameName + "_" + nextGameSerialnumber,
                        serialNumber = nextGameSerialnumber,
                        region = gdnStreamDriver.region,
                    };

                    lobbyValue.MoveToTeam(SelfTeamSlot(), 2);
                    //AddDummyTeamSlots(0, 1);
                    //AddDummyTeamSlots(1, 3);
                    //AddDummyTeamSlots(2, 1);
                    GameDebug.Log("make lobby: " + lobbyValue.streamName);
                    var lobbyLobby = LobbyLobby.GetFromLobbyValue(lobbyValue);
                    gdnDocumentLobbyDriver.PostLobbyDocument(lobbyLobby);
                    return;
                }

                if (gdnDocumentLobbyDriver.postLobbyStuff) {
                    GameDebug.Log("Lobby joined");
                    JoinLobby(lobbyValue, true);
                    gdnDocumentLobbyDriver.postLobbyStuff = false;
                    tryDocumentInit = false;
                    //initial insert message comes before this joinLobby can happen
                    UpdateLobby();
                    return;
                }
            }

            if (isLobbyAdmin && gdnDocumentLobbyDriver.lobbyIsMade && Time.time > nextUpdateLobby) {
                UpdateLobby();
                return;
            }
            
            if (StreamsBodyLoop()) {
               //debugPingData = transportPingData.Copy();
                return;
            }

        }

        public void UpdateLobby() {
            nextUpdateLobby = Time.time +5;
            var lobbyLobby = LobbyLobby.GetFromLobbyValue(lobbyValue);
            var key = gdnDocumentLobbyDriver.lobbyKey;
            gdnDocumentLobbyDriver.UpdateLobbyDocument(lobbyLobby, key);
        }

        /// <summary>
        /// returns true if containing loop should return
        /// this run when inside a lobby
        /// </summary>
        /// <returns></returns>
        public bool  StreamsBodyLoop() {
            //GameDebug.Log("StreamsBodyLoop A");
            if (!gdnStreamDriver.lobbyDocumentReaderExists) {
               
                gdnStreamDriver.CreateDocuomentReader(gdnDocumentLobbyDriver.lobbiesCollectionName, clientId);
                return false;
            }
            //GameDebug.Log("StreamsBodyLoop B");
            if (waitStreamClearing) {
                if (Time.time > streamClearTime) {
                    FinishClearStreams();
                }
                return false; // none streams action OK
            }
            gdnStreamDriver.ExecuteLobbyCommands();
            //GameDebug.Log("StreamsBodyLoop C");

            //this for pinging rtt clients
            // ping three time

            
                
           
            
            
            
            return false;
        }

        
        
        /// <summary>
        /// pings are being sent to lobby streams (chat)
        /// </summary>

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
            var result = lobbyValue.FindPlayer(clientId);
            if (result != null) {
                return result;
            }
            result = new TeamSlot() {
                playerName = localId,
                clientId = clientId,
                region = gdnStreamDriver.region,
                ping = gdnStreamDriver.chatProducer1.Latency,
            };
            return result;
        }

        public static Lobby.TeamSlot MakeSelfTeamSlot() {
           return  _inst.SelfTeamSlot();
        }

        /// <summary>
        /// This is static to make calling from UI easier
        /// </summary>
        /// <param name="teamIndex"></param>
        static public void MoveToTeam(int teamIndex) {
            _inst.gdnStreamDriver.ChatSendRoomRequest(teamIndex);
            if (teamIndex == -1) {
                _inst.LeaveLobby();
            }
        }

        static public void TeamNameChanged(string teamName, int teamIndex) {
            /// this change goes directo lobby and updates
            _inst.lobbyValue.TeamFromIndex( teamIndex).name = teamName;
            _inst.UpdateLobby();
        }
        
        static public bool MoveToTeam(TeamSlot teamSlot,int teamIndex) {
            var val = _inst.lobbyValue.MoveToTeam(teamSlot, teamIndex);
            _inst.UpdateLobby();
            return val;
        }

        static public void SetServerAllowed(string consumerName) {
            GameDebug.Log("pushed SetServerAllowed");
            _inst.lobbyValue.serverAllowed = consumerName;
            _inst.UpdateLobby();
        }
        
        static public void SetRttTarget(string rttTarget) {
            GameDebug.Log("pushed SetRttTarget");
            _inst.lobbyValue.rttTarget = rttTarget;
            _inst.gdnStreamDriver.ChatSendSetRttTarget(rttTarget);

        }
        static public void SendRttTime(string consumerName, int rttTime) {
            _inst.lobbyValue.SetRttTime(consumerName, rttTime);
            _inst.UpdateLobby();
        }
        
        static public void CloseLobby() {
            // we need to sen this to UI
           //_inst.lobbyValue.MoveToTeam(teamSlot, teamIndex);
            
        }
        
        public void AddLobbyCommand(LobbyCommand command) {
            _lobbyQueue.Enqueue(command);
        }

        public void ExecuteLobbyCommands() {
            //GameDebug.Log("ExecuteLobbyCommands");
            LobbyCommand command;
            while (_lobbyQueue.TryDequeue(out command)) {
                GameDebug.Log("ExecuteLobbyCommand: "+ command.command);
                ExecuteLobby(command);
            }
        }

        public void ExecuteLobby(LobbyCommand command) {

            switch (command.command) {
                case LobbyCommandType.RequestRoom:
                    GameDebug.Log("Request for lobby Room: " + command.roomNumber + " playerName: " +
                                  command.playerName
                                  + "clientId " + command.source);
                    GDNClientLobbyNetworkDriver2.MoveToTeam(command.teamSlot, command.roomNumber);
                    // do MoveTo()
                    //      this move if possible and then send update to lobby even if not done
                    break;
                case LobbyCommandType.AllowServer:
                    GameDebug.Log("unhandled lobby command from source: "
                                  + command.command + " : " + command.source);
                    //update last heartbeat time in heartBeat collection
                    //update last heartbeat time
                    break;
                case LobbyCommandType.CloseLobby:
                    GameDebug.Log("unhandled lobby command from source: "
                                  + command.command + " : " + command.source);
                    //update last heartbeat time in heartBeat collection
                    //update last heartbeat time
                    break;
                case LobbyCommandType.GameInit:
                    GameDebug.Log("unhandled lobby command from source: "
                                  + command.command + " : " + command.source);
                    //update last heartbeat time in heartBeat collection
                    //update last heartbeat time
                    break;
                case LobbyCommandType.GameReady:
                    GameDebug.Log("unhandled lobby command from source: "
                                  + command.command + " : " + command.source);
                    //update last heartbeat time in heartBeat collection
                    //update last heartbeat time
                    break;
                case LobbyCommandType.SendRttTime:
                    GameDebug.Log("unhandled lobby command from source: "
                                  + command.command + " : " + command.source);
                    
                    break;
                case LobbyCommandType.SetRttTarget:
                    GameDebug.Log("unhandled lobby command from source: "
                                  + command.command + " : " + command.source);
                    //update last heartbeat time in heartBeat collection
                    //update last heartbeat time
                    break;
                default:
                    break;

            }
        }


   #endregion LobbyManipulation 
   
   #region LobbyTesting
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
        
        
        
    #endregion LobbyLobbyTesting
        
    }
}