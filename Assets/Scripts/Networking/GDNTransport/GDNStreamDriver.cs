using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Pkix;
using BestHTTP.WebSocket;
using JetBrains.Annotations;
using Macrometa.Lobby;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Networking;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace Macrometa {

    [Serializable]
    public class GDNStreamDriver {
        private MonoBehaviour _monobehaviour;
        public bool  isPlayStatsServerOn = true; //Used to SendMessage data start stats sts stream
        
        // used to collect throughput, shots fired , health , maybe position
        public static bool  isPlayStatsClientOn = false; 
        public static bool isSocketPingOn = false; // must be set before webSocket is opened
        public static bool isStatsOn = false; //off by default for compatibility 
        public static bool sendDummyTraffic = false; //off by default for compatibility
        public static int missedPingDisconnect = 3;
        public static float initialPingDelay = 30; // wait period before first ping is sent after connect
        public static bool isClientBrowser = false;
        public static bool isLobbyAdmin = false;
        public static string lobbyTarget;
        public static bool isLobbyPinger;
        public static Stopwatch chatPingStopwatch;
        public static string localId;  // is player name in FPS
        public static string appType;
        public string nodeId = "";
        public ListStream listStream;
        public WebSocket consumer1;
        public WebSocket producer1;
        public WebSocket chatConsumer1;
        public WebSocket chatProducer1;
        public WebSocket producerStats;
        public WebSocket producerGameStats;
        
        
        public StreamStats consumer1Stats;
        public StreamStats producer1Stats;
        public WebSocket lobbyDocumentReader; // lobby collection stream
        public Region region;
        public string consumerName = "Server";
        public string serverName;
        public string chatChannelId;
        public string chatLobbyId;  //used for internal messages not using channel switching in chat yet
        public bool regionIsDone;
        public bool streamListDone = false;
        public bool serverInStreamExists = false;
        public bool serverOutStreamExists = false;
        public bool serverStatsStreamExists = false;
        public bool gameStatsStreamExists = false;

        public bool chatStreamExists = false;
        public bool producerExists = false;
        public bool consumerExists = false;
        public bool chatProducerExists = false;
        public bool chatConsumerExists = false;
        public bool producerStatsExists = false;
        public bool lobbyDocumentReaderExists = false;
        public bool producerGameStatsExists = false ;
        
        public bool lobbyUpdateAvail = false;
        public int lobbyRtt;
        public bool lobbyURttAvail;
        public LobbyValue lobbyUpdate;
        public bool sendConnect = true;
        public bool setupComplete = false;
        public bool pingStarted = false;
        public string serverInStreamName;
        public string serverOutStreamName;
        public string serverStatsStreamName;
        public string consumerStreamName;
        public string producerStreamName;
        public string chatStreamName;
        public string gameStatsStreamName = "FPSGame_GameStats" ;
        
        public float pingFrequency = 1; //hard coded here
        public float dummyFrequency = 0.05f; // FPSSample standard is 20 messages per second
        public int dummySize = 50; // FPSSample standard is under 2000 bytes per second
        
        public PingStatsGroup pingStatsGroup = new PingStatsGroup();
        public int statsGroupSize;
        public int dummyTrafficQuantity;

        public Dictionary<int, GDNNetworkDriver.GDNConnection> gdnConnections =
            new Dictionary<int, GDNNetworkDriver.GDNConnection>();

        public Queue<GDNNetworkDriver.DriverTransportEvent> driverTransportEvents =
            new Queue<GDNNetworkDriver.DriverTransportEvent>();
        
        public Queue<string> chatMessages = new Queue<string>();
        private ConcurrentQueue<GDNStreamDriver.Command> _commandQueue = new ConcurrentQueue<GDNStreamDriver.Command>();
        private ConcurrentQueue<LobbyCommand> _lobbyQueue = new ConcurrentQueue<LobbyCommand>();
        private GDNErrorhandler _gdnErrorHandler;
        private GDNData _gdnData;
        private bool _isServer;

        public bool receivedPongOnly = false;
        public float pongOnlyRtt = 0;

        public class ChatBuffer {
            public static Queue<ReceivedMessage> chatReceivedMessages = new Queue<ReceivedMessage>();
            public static void Add(ReceivedMessage receivedMessage) {
                chatReceivedMessages.Enqueue(receivedMessage);
                if (chatReceivedMessages.Count > 100) {
                    chatReceivedMessages.Dequeue();
                }
            }
            
            public static ReceivedMessage[] Dump() {
                return chatReceivedMessages.ToArray();
            }
        }

        public GDNStreamDriver(GDNNetworkDriver gdnNetworkDriver) {
            _monobehaviour = gdnNetworkDriver;
            _gdnErrorHandler = gdnNetworkDriver.gdnErrorHandler;
            _gdnData = gdnNetworkDriver.baseGDNData;
            // _gdnNetworkDriver = gdnNetworkDriver;
            _isServer = gdnNetworkDriver.isServer;
        }

        public void setRandomClientName() {
            consumerName = "C" + (10000000 + Random.Range(1, 89999999)).ToString();
        }

        public enum QueueCommand {
            ConnectClient, //(ReceivedMessage receivedMessage)
            DisconnectFromClient, //(receivedMessage);
            DisconnectFromServer, //(receivedMessage);
            SendTransportPong, //(receivedMessage);
            ReceiveTransportPong, //(receivedMessage);
            ReceiveInternal, //(receivedMessage);
            ReceiveDummy, //(receivedMessage);
            Connect, //()
            SendTransportPing, //()
        }

        public class Command {
            public QueueCommand command;
            public ReceivedMessage receivedMessage;
        }

        public void GetRegion() {
            //Debug.Log(baseGDNData.ListStreamsURL());
            _gdnErrorHandler.isWaiting = true;
            _monobehaviour.StartCoroutine(MacrometaAPI.GetRegion(_gdnData, GetRegionCallback));
        }

        public void GetRegionCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("Get Region : " + www.error);
            }
            else {
                region = JsonUtility.FromJson<Region>(www.downloadHandler.text);
                    GameDebug.Log("Get Regionsucceed " );
                    regionIsDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                
            }
        }
        
        public void GetListStream() {
            //Debug.Log(baseGDNData.ListStreamsURL());
            _gdnErrorHandler.isWaiting = true;
            _monobehaviour.StartCoroutine(MacrometaAPI.ListStreams(_gdnData, ListStreamCallback));
        }

        public void ListStreamCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("ListStream : " + www.error);
            }
            else {

                //overwrite does not assign toplevel fields
                //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
                listStream = JsonUtility.FromJson<ListStream>(www.downloadHandler.text);
                if (listStream.error == true) {
                    GameDebug.Log("ListStream failed:" + listStream.code);
                    //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                    _gdnErrorHandler.currentNetworkErrors++;
                }
                else {
                    GameDebug.Log("ListStream succeed ");
                    streamListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void CreatServerInStream() {
            var gsListName = _gdnData.StreamListName(serverInStreamName);
            serverInStreamExists = listStream.result.Any(item => item.topic == gsListName);
            if (!serverInStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                ;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, serverInStreamName,
                    CreateServerInStreamCallback));
            }
        }

        public void CreateServerInStreamCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("CreateServerInStream : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                streamListDone = false;
            }
            else {

                var baseHtttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHtttpReply.error == true) {
                    GameDebug.Log("create ServerIn stream failed:" + baseHtttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    streamListDone = false;
                }
                else {
                    GameDebug.Log("create ServerIn stream ");
                    serverInStreamExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void CreateChatStream() {
            var gsListName = _gdnData.StreamListName(chatStreamName);
            chatStreamExists = listStream.result.Any(item => item.topic == gsListName);
            if (!chatStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                ;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, chatStreamName,
                    CreateChatStreamCallback));
            }
        }

        public void CreateChatStreamCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("CreateChatStream : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                streamListDone = false;
            }
            else {

                var baseHtttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHtttpReply.error == true) {
                    GameDebug.Log("create Chat stream failed:" + baseHtttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    streamListDone = false;
                }
                else {
                    GameDebug.Log("create chat stream ");
                    chatStreamExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        
        public void CreatServerOutStream() {
            var gsListName = _gdnData.StreamListName(serverOutStreamName);
            serverOutStreamExists = listStream.result.Any(item => item.topic == gsListName);
            if (!serverOutStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, serverOutStreamName,
                    CreateServerOutStreamCallback));
            }
        }

        public void CreateServerOutStreamCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("CreateServerOutStream error: " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                streamListDone = false;
            }
            else {

                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create Server Out stream failed:" + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    streamListDone = false;
                }
                else {
                    GameDebug.Log("CreateServerOutStream ");
                    serverOutStreamExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void CreatServerStatsStream() {
            var gsListName = _gdnData.StreamListName(serverStatsStreamName);
            serverStatsStreamExists = listStream.result.Any(item => item.topic == gsListName);
            if (!serverStatsStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, serverStatsStreamName,
                    CreateServerStatsStreamCallback));
            }
        }

        public void CreateServerStatsStreamCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("CreateServerStatsStream error: " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                streamListDone = false;
            }
            else {

                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create Server Stats stream failed:" + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    streamListDone = false;
                }
                else {
                    GameDebug.Log("CreateServerStatsStream ");
                    serverStatsStreamExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void CreatGameStatsStream() {
            var gsListName = _gdnData.StreamListName(gameStatsStreamName);
            gameStatsStreamExists = listStream.result.Any(item => item.topic == gsListName);
            if (!gameStatsStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, gameStatsStreamName,
                    CreateGameStatsStreamCallback));
            }
        }

        public void CreateGameStatsStreamCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("Create Game StatsStream error: " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                streamListDone = false;
            }
            else {

                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create Game Stats stream failed:" + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    streamListDone = false;
                }
                else {
                    GameDebug.Log("Create Game StatsStream ");
                    gameStatsStreamExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        
        public void CreateProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            producer1Stats = new StreamStats() {
                dataType = DataType.JSON,
                streamName = streamName,
                streamType = StreamType.Shared
            };
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetProducer, _gdnErrorHandler));
        }

        public void SetProducer(WebSocket ws, string debug = "") {
            producer1 = ws;
            if (isSocketPingOn) {
                ws.StartPingThread = true;
                ws.CloseAfterNoMessage = TimeSpan.FromSeconds(10);
            }

            producer1.OnOpen += (o) => {
                _gdnErrorHandler.isWaiting = false;
                producerExists = true;
                GameDebug.Log("Open " + debug);
            };

            producer1.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);
                if (producer1 != null && producer1.IsOpen) {
                    producer1.Close();
                }
                else {
                    GameDebug.Log("WebSocket " + debug);
                    producerExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            producer1.OnClosed += (socket, code, message) => {
                producerExists = false;
                _gdnErrorHandler.isWaiting = false;
                GameDebug.Log("Produce closed: " + code + " : " + message);
            };
            producer1.Open();
        }

        public void CreateChatProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetChatProducer,_gdnErrorHandler));
        }

        public void SetChatProducer(WebSocket ws, string debug = "") {
            chatProducer1 = ws;
            chatProducer1.StartPingThread = true; // needed for lobby
            chatProducer1.CloseAfterNoMessage = TimeSpan.FromSeconds(10);
            chatProducer1.OnOpen += (o) => {
                _gdnErrorHandler.isWaiting = false;
                chatProducerExists = true;
                GameDebug.Log("Open chatProducer1" + debug);
            };

            chatProducer1.OnError += (sender, e) => {
                GameDebug.Log("WebSocket chatProducer1 Error" + debug + " : " + e);
                if (producer1 != null && producer1.IsOpen) {
                    producer1.Close();
                }
                else {
                    GameDebug.Log("WebSocket chatProducer1 " + debug);
                    chatProducerExists  = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            chatProducer1.OnClosed += (socket, code, message) => {
                chatProducerExists  = false;
                _gdnErrorHandler.isWaiting = false;
                GameDebug.Log("chatProducer1 closed: " + code + " : " + message);
            };
            chatProducer1.Open();
        }

        
        public void CreateStatsProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            GameDebug.Log("CreateStatsProducer: " + streamName);
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetStatsProducer,_gdnErrorHandler));
        }

        public void SetStatsProducer(WebSocket ws, string debug = "") {
            producerStats = ws;
            ws.OnOpen += (o) => {
                _gdnErrorHandler.isWaiting = false;
                producerStatsExists = true;
                GameDebug.Log("Open " + debug);
            };

            ws.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);
                if (ws != null && ws.IsOpen) {
                    ws.Close();
                }
                else {
                    GameDebug.Log("WebSocket " + debug);
                    producerStatsExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            ws.OnClosed += (socket, code, message) => {
                producerStatsExists = false;
                _gdnErrorHandler.isWaiting = false;
                GameDebug.Log("Produce closed: " + code + " : " + message);
            };
            ws.Open();
        }
        
        public void CreateGameStatsProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            GameDebug.Log("Create Game StatsProducer: " + streamName);
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetGameStatsProducer,_gdnErrorHandler));
        }

        public void SetGameStatsProducer(WebSocket ws, string debug = "") {
            producerGameStats = ws;
            ws.OnOpen += (o) => {
                _gdnErrorHandler.isWaiting = false;
                producerGameStatsExists = true;
                GameDebug.Log("Open " + debug);
            };

            ws.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);
                if (ws != null && ws.IsOpen) {
                    ws.Close();
                }
                else {
                    GameDebug.Log("WebSocket " + debug);
                    producerGameStatsExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            ws.OnClosed += (socket, code, message) => {
                producerGameStatsExists = false;
                _gdnErrorHandler.isWaiting = false;
                GameDebug.Log("Produce closed: " + debug + " : " + code + " : " + message);
            };
            ws.Open();
        }
        
        // send message to clients
        // killed
        //destination All
        //message killed
        //playername
        public void ProducerSendKilled(string playerName) {
            var properties = new MessageProperties() {
                t =  VirtualMsgType.Internal,
                p = 9,
                d = "all",
                s = consumerName,
                z =0,
                killedPlayerName =  playerName
            };
            var message = new SendMessage() {
                properties = properties,
                payload = Convert.ToBase64String(new byte[0])
            };
            string msgJSON = JsonUtility.ToJson(message);
            producer1.Send(msgJSON);
            if (isStatsOn || isPlayStatsClientOn) {
                producer1Stats.IncrementCounts(msgJSON.Length);
            }
        }
        

        public void ProducerSend(int id, VirtualMsgType msgType, byte[] payload,
            int pingId = 0, int pingTimeR = 0, int pingTimeO = 0, string localId = "") {
            //GameDebug.Log("ProducerSend A: " + id);
            if (!gdnConnections.ContainsKey(id)) {
                GameDebug.Log("ProducerSend bad id:" + id)  ;
            }

            var gdnConnection = gdnConnections[id];
            
            ProducerSend( gdnConnection,msgType, payload, pingId, pingTimeR, pingTimeO, localId);
        }

        private void ProducerSend(GDNNetworkDriver.GDNConnection gdnConnection, VirtualMsgType msgType,
            byte[] payload, int pingId, int pingTimeR, int pingTimeO,
            string localId ) {
            if (msgType == VirtualMsgType.Connect) {
                GameDebug.Log("ProducerSend connect: "+ GDNStats.playerName);
            }

            var properties = new MessageProperties() {
                t = msgType,
                p = gdnConnection.port,
                d = gdnConnection.destination,
                s = gdnConnection.source,
                z = payload.Length,
                localId = GDNStats.playerName
            };
            if (msgType == VirtualMsgType.Ping ) {
                properties.i = pingId;
                properties.r = pingTimeR;
                properties.o = pingTimeO;
                properties.host = region.host;
                properties.city = region.locationInfo.city;
                properties.countrycode = region.locationInfo.countrycode;
               
            }
            
            if ( msgType == VirtualMsgType.Pong) {
                properties.i = pingId;
                properties.r = pingTimeR;
                properties.o = pingTimeO;
                properties.host = region.host;
                properties.city = region.locationInfo.city;
                properties.countrycode = region.locationInfo.countrycode;
                properties.rifleShots = PlayStats.GetRifle();
                properties.grenadeShots =   PlayStats.GetGrenade();
                properties.fps = PlayStats.FPS;
                properties.health = PlayStats.health;
                properties.posX = PlayStats.position.x;
                properties.posY = PlayStats.position.y;
                properties.posZ = PlayStats.position.z;
                properties.orientation = PlayStats.orientation;
                properties.remotePlayerCity = PlayStats.remotePlayerCity;
                properties.remotePlayerCountrycode = PlayStats.remotePlayerCountry;
                properties.remoteConnectin_Type = PlayStats.remoteConnectin_Type;
                
                //GameDebug.Log("pong to: "+ gdnConnection.destination + " rifle: " +properties.rifleShots );
            }

            var message = new SendMessage() {
                properties = properties,
                payload = Convert.ToBase64String(payload)
            };
            string msgJSON = JsonUtility.ToJson(message);
            //GameDebug.Log("Send msg: " + message.properties.msgType);
            if (msgType == VirtualMsgType.Data) {
                LogFrequency.Incr("ProducerSend Data",
                    message.payload.Length,
                    payload.Length, 0
                );
            }

            producer1.Send(msgJSON);
            if (isStatsOn || isPlayStatsClientOn) {
                producer1Stats.IncrementCounts(msgJSON.Length);
            }
        }

        
        public void ProducerStatsSend(PingStatsGroup.NetworkStatsData data) {
            GameDebug.Log("ProducerStatsSend ");
            var properties = new StatsMessageProperties {
                NetworkStatsData = data
            };
            var json = JsonUtility.ToJson(data);
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            var message = new StatsSendMessage() {
                properties = properties,
                payload = payload
            };
            string msgJSON = JsonUtility.ToJson(message);
            producerStats.Send(msgJSON);

        }

        public void ProducerGameStatsSend(Object data) {
            GameDebug.Log("Producer Game StatsSend ");
            
            var json = JsonUtility.ToJson(data);
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            var message = new StatsSendMessage() {
                payload = payload
            };
            string msgJSON = JsonUtility.ToJson(message);
            producerGameStats.Send(msgJSON);

        }
        
        public void CreateConsumer(string streamName, string consumerName) {
            _gdnErrorHandler.isWaiting = true;
            consumer1Stats = new StreamStats() {
                dataType = DataType.JSON,
                streamName = streamName,
                streamType = StreamType.Shared
            };
            _monobehaviour.StartCoroutine(MacrometaAPI.Consumer(_gdnData, streamName, consumerName, SetConsumer,_gdnErrorHandler));
        }

        public void SetConsumer(WebSocket ws, string debug = "") {

            consumer1 = ws;
            consumer1.StartPingThread = isSocketPingOn;
            consumer1.CloseAfterNoMessage = TimeSpan.FromSeconds(10);
            consumer1.OnOpen += (o) => {
                GameDebug.Log("Stream driver Open " + debug + " isPingOn: " + isSocketPingOn);
                consumerExists = true;
                _gdnErrorHandler.isWaiting = false;
            };

            consumer1.OnMessage += (sender, e) => {
                //GameDebug.Log("Consumer1.OnMessage A " );
                var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);
                //GameDebug.Log("Consumer1.OnMessage type: "+ receivedMessage.properties.t );
                //Debug.Log("low time: " + DateTime.Now.AddMinutes(discardMinutes));
                //Debug.Log("published: " + DateTime.Parse(receivedMessage.publishTime));
                //GameDebug.Log("recieved msg: " + e);
                if (isStatsOn) {
                    consumer1Stats.IncrementCounts(e.Length);
                    //GameDebug.Log("Consumer1.OnMessage");
                }

                if (receivedMessage.properties != null &&
                    receivedMessage.properties.d == "all"
                ) {
                    PlayStats.PlayerKilled(receivedMessage.properties.killedPlayerName);
                }

                if (receivedMessage.properties != null &&
                    receivedMessage.properties.d == consumerName
                    // && DateTime.Now.AddMinutes(discardMinutes) < DateTime.Parse(receivedMessage.publishTime)  
                ) {
                    // GameDebug.Log("Consumer1.OnMessage 2");
                    Command command;
                    switch (receivedMessage.properties.t) {
                        case VirtualMsgType.Data:

                            //GameDebug.Log("Consumer1.OnMessage Data");
                            var connection = new GDNNetworkDriver.GDNConnection() {
                                source = receivedMessage.properties.d,
                                destination = receivedMessage.properties.s,
                                port = receivedMessage.properties.p
                            };

                            var id = GetConnectionId(connection);
                            if (id == -1) {
                                GameDebug.Log("discarded message unknown source "
                                              + receivedMessage.properties.s);
                                break;
                            }

                            var driverTransportEvent = new GDNNetworkDriver.DriverTransportEvent() {
                                connectionId = id,
                                data = Convert.FromBase64String(receivedMessage.payload),
                                dataSize = Convert.FromBase64String(receivedMessage.payload).Length,
                                type = GDNNetworkDriver.DriverTransportEvent.Type.Data
                            };
                            PushEventQueue(driverTransportEvent);
                            LogFrequency.Incr("consumer1.OnMessage", receivedMessage.payload.Length,
                                receivedMessage.properties.z, driverTransportEvent.data.Length);


                            break;
                        case VirtualMsgType.Connect:
                            GameDebug.Log("Consumer1.OnMessage Connect: " + receivedMessage.properties.s);
                            GameDebug.Log("Consumer1.OnMessage Connect: " + receivedMessage.properties.localId);
                            if (_isServer) {
                                command = new GDNStreamDriver.Command() {
                                    command = QueueCommand.ConnectClient,
                                    receivedMessage = receivedMessage
                                };
                                AddCommand(command);
                                //GameDebug.Log("Consumer1.OnMessage Connect debug _isServer true: " );
                                //ConnectClient(receivedMessage);
                            }

                            break;
                        case VirtualMsgType.Disconnect:
                            GameDebug.Log("Consumer1.OnMessage Disonnect: " + receivedMessage.properties.s);
                            if (_isServer) {
                                command = new GDNStreamDriver.Command() {
                                    command = QueueCommand.DisconnectFromClient,
                                    receivedMessage = receivedMessage
                                };
                                AddCommand(command);
                                //DisconnectFromClient(receivedMessage);
                            }
                            else {
                                command = new GDNStreamDriver.Command() {
                                    command = QueueCommand.DisconnectFromServer,
                                    receivedMessage = receivedMessage
                                };
                                AddCommand(command);
                                //DisconnectFromServer(receivedMessage);
                            }

                            break;
                        case VirtualMsgType.Ping:
                            //GameDebug.Log("Consumer1.OnMessage Ping ");
                            command = new GDNStreamDriver.Command() {
                                command = QueueCommand.SendTransportPong,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            //SendTransportPong(receivedMessage);
                            break;

                        case VirtualMsgType.Pong:
                            //GameDebug.Log("Consumer1.OnMessage Pong ");
                            command = new GDNStreamDriver.Command() {
                                command = QueueCommand.ReceiveTransportPong,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            //ReceiveTransportPong(receivedMessage);
                            break;

                        case VirtualMsgType.Internal:
                            //GameDebug.Log("Consumer1.OnMessage internal ");
                            command = new GDNStreamDriver.Command() {
                                command = QueueCommand.ReceiveInternal,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            //ReceiveInternal(receivedMessage);
                            break;

                        case VirtualMsgType.Dummy:
                            //GameDebug.Log("Consumer1.OnMessage dummy ");
                            command = new GDNStreamDriver.Command() {
                                command = QueueCommand.ReceiveDummy,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            // ReceiveDummy(receivedMessage);

                            break;
                    }
                }

                //ttl is set low but not working and So FPSSample does need this 

                var ackMessage = new AckMessage() {
                    messageId = receivedMessage.messageId
                };
                var msgString = JsonUtility.ToJson(ackMessage);
                consumer1.Send(msgString);
                //Debug.Log("ack: " + debug + " : " + msgString);


            };
            consumer1.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);

                //Debug.Log("producer1: " + producer1);
                //Debug.Log("IsOpen: " + producer1?.IsOpen.ToString());
                if (producer1 != null && producer1.IsOpen) {
                    producer1.Close();
                }
                else {
                    consumerExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            consumer1.OnClosed += (socket, code, message) => {
                consumerExists = false;
                _gdnErrorHandler.isWaiting = false;
            };
            consumer1.Open();
        }

        public void CreateConsumerPongOnly(string streamName, string consumerName) {
            _gdnErrorHandler.isWaiting = true;
            consumer1Stats = new StreamStats() {
                dataType = DataType.JSON,
                streamName = streamName,
                streamType = StreamType.Shared
            };
            _monobehaviour.StartCoroutine(
                MacrometaAPI.Consumer(_gdnData, streamName, consumerName, SetConsumerPongOnly,_gdnErrorHandler));
        }

        public void SetConsumerPongOnly(WebSocket ws, string debug = "") {

            consumer1 = ws;
            consumer1.StartPingThread = isSocketPingOn;
            consumer1.CloseAfterNoMessage = TimeSpan.FromSeconds(10);
            consumer1.OnOpen += (o) => {
                GameDebug.Log("Stream driver Open " + debug + " PongOnly isPingOn: " + isSocketPingOn);
                consumerExists = true;
                _gdnErrorHandler.isWaiting = false;
            };

            consumer1.OnMessage += (sender, e) => {
                GameDebug.Log(" consumer1.OnMessage pong only"  );
                var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);

                if (receivedMessage.properties != null &&
                    receivedMessage.properties.d == consumerName
                ) {
                    
                    switch (receivedMessage.properties.t) {
                        case VirtualMsgType.Pong:
                            receivedPongOnly = true;
                            var transportPing = TransportPings.Remove(receivedMessage.properties.i);
                            pongOnlyRtt = transportPing.elapsedTime;
                            GameDebug.Log(" consumer1.OnMessage pong only acted on"  );
                            break;

                        default:
                            break;
                    }
                }

                var ackMessage = new AckMessage() {
                    messageId = receivedMessage.messageId
                };
                var msgString = JsonUtility.ToJson(ackMessage);
                consumer1.Send(msgString);
            };
            consumer1.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);

                //Debug.Log("producer1: " + producer1);
                //Debug.Log("IsOpen: " + producer1?.IsOpen.ToString());
                if (producer1 != null && producer1.IsOpen) {
                    producer1.Close();
                }
                else {
                    consumerExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            consumer1.OnClosed += (socket, code, message) => {
                consumerExists = false;
                _gdnErrorHandler.isWaiting = false;
            };
            consumer1.Open();
        }
        
        
        public void ChatSend(string channelId,  string msg, VirtualMsgType msgType = VirtualMsgType.Data) {
            var properties = new MessageProperties() {
                t = msgType,
                d = channelId,
            };
            var message = new SendMessage() {
                properties = properties,
                payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))
            };
            string msgJSON = JsonUtility.ToJson(message);
            chatProducer1.Send(msgJSON);
        }
        
        public void ChatSendRoomRequest(int roomId) {
            var command = new LobbyCommand() {
                command = LobbyCommandType.RequestRoom,
                source = consumerName,
                playerName = localId,
                roomNumber = roomId,
                teamSlot = GDNClientLobbyNetworkDriver2.MakeSelfTeamSlot(),
                admin = true,
            };
            ChatSendCommand(chatChannelId,command);
        }
        public void ChatSendCloseLobby() {
            var command = new LobbyCommand() {
                command = LobbyCommandType.CloseLobby,
                all = true,
            };
            ChatSendCommand(chatChannelId,command);
        }
        public void ChatSendSetRttTarget(string clientId) {
            var command = new LobbyCommand() {
                command = LobbyCommandType.SetRttTarget,
                target = clientId,
            };
            ChatSendCommand(chatChannelId,command);
        }
        public void ChatSendSetRttTime(string rttClientId,int val) {
            var command = new LobbyCommand() {
                command = LobbyCommandType.SendRttTime,
                source = consumerName,
                target = rttClientId,
                admin = true,
                intVal = val,
            };
            GameDebug.Log("hatSendSetRttTime : "+consumerName);
            ChatSendCommand(chatChannelId,command);
        }
        public void ChatSendAllowServer(string clientId) {
            var command = new LobbyCommand() {
                command = LobbyCommandType.AllowServer,
                target = clientId,
            };
            ChatSendCommand(chatChannelId,command);
        }
        public void ChatSendGameInit() {
            var command = new LobbyCommand() {
                command = LobbyCommandType.GameInit,
                all = true,
            };
            ChatSendCommand(chatChannelId,command);
        }
        public void ChatSendGameReady() {
            var command = new LobbyCommand() {
                command = LobbyCommandType.GameReady,
                all = true,
            };
            ChatSendCommand(chatChannelId,command);
        }
        
        public void ChatSendCommand(string channelId,  LobbyCommand command ) {
            var properties = new MessageProperties() {
                t = VirtualMsgType.Internal,
                d = channelId,
                s = consumerName
            };
            var msg = JsonUtility.ToJson(command);
            var message = new SendMessage() {
                properties = properties,
                payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))
            };
            string msgJSON = JsonUtility.ToJson(message);
            GameDebug.Log("ChatSendCommand D " );
            chatProducer1.Send(msgJSON);
        }
        
        public void CreateChatConsumer(string streamName, string consumerName) {
            _gdnErrorHandler.isWaiting = true;
           
            _monobehaviour.StartCoroutine(
                MacrometaAPI.Consumer(_gdnData, streamName, consumerName, SetChatConsumer,_gdnErrorHandler));
        }

        public void SetChatConsumer(WebSocket ws, string debug = "") {

            chatConsumer1 = ws;
            chatConsumer1.CloseAfterNoMessage = TimeSpan.FromSeconds(10);
            chatConsumer1.OnOpen += (o) => {
                GameDebug.Log("Stream driver Open " + debug + " chatConsumer1: " );
                chatConsumerExists = true;
                _gdnErrorHandler.isWaiting = false;
            };

            chatConsumer1.OnMessage += (sender, e) => {
                //GameDebug.Log(" chatConsumer1.OnMessage"  );
                var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);
                GameDebug.Log(" chatConsumer1.OnMessage channelid: "  +receivedMessage.properties.d +
                              " : " + receivedMessage.properties.t );
                if (receivedMessage.properties != null &&
                    receivedMessage.properties.d == chatChannelId
                ) {
                    switch (receivedMessage.properties.t) {

                        case VirtualMsgType.Data:
                            chatMessages.Enqueue(
                                Encoding.UTF8.GetString(Convert.FromBase64String(receivedMessage.payload)));
                            break;
                        case VirtualMsgType.Internal:
                           // GameDebug.Log(" chatConsumer1.OnMessage internal " );
                            var json  =Encoding.UTF8.GetString(Convert.FromBase64String(receivedMessage.payload));
                            var lobbyCommand = JsonUtility.FromJson<LobbyCommand>(json);
                            if (lobbyCommand.all || (lobbyCommand.admin && isLobbyAdmin) ||
                                (lobbyCommand.target == consumerName)) {
                                //GameDebug.Log(" chatConsumer1.OnMessage lobbycommand: " + lobbyCommand.command);
                                AddLobbyCommand(lobbyCommand);
                            }
                            break;
                        case VirtualMsgType.Ping:
                            //GameDebug.Log(" chatConsumer1.OnMessage ping source: "+ receivedMessage.properties.s +
                             //            " lobbyTarget: "+  lobbyTarget + "consumer: "+consumerName );
                            SendChatTransportPong(receivedMessage.properties.d,receivedMessage);
                            break;
                        case VirtualMsgType.Pong:
                            
                            var jsonPong  =Encoding.UTF8.GetString(Convert.FromBase64String(receivedMessage.payload));
                            var lobbyCommandPong = JsonUtility.FromJson<LobbyCommand>(jsonPong);
                            //GameDebug.Log("ping chatConsumer1.OnMessage pong source/target : local:"+ lobbyCommandPong.source +
                             //             " : " + consumerName + " : "+ lobbyCommandPong.target );
                            if (lobbyCommandPong.target != consumerName) break;
                            // this need to check check target is me
                            //GameDebug.Log("ping chatConsumer1.OnMessage lobbycommand: " + lobbyCommandPong.command );
                            AddLobbyCommand(lobbyCommandPong);
                            break;
                    }
                }

                if (receivedMessage.properties.t == VirtualMsgType.Data) {
                    ChatBuffer.Add(receivedMessage);
                }
                var ackMessage = new AckMessage() {
                    messageId = receivedMessage.messageId
                };
                var msgString = JsonUtility.ToJson(ackMessage);
                chatConsumer1.Send(msgString);
            };
            chatConsumer1.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);

                //Debug.Log("producer1: " + producer1);
                //Debug.Log("IsOpen: " + producer1?.IsOpen.ToString());
                if (producer1 != null && producer1.IsOpen) {
                    producer1.Close();
                }
                else {
                    consumerExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            chatConsumer1.OnClosed += (socket, code, message) => {
                consumerExists = false;
                _gdnErrorHandler.isWaiting = false;
            };

            chatConsumer1.Open();

        }

        
        public void CreateDocuomentReader(string streamName, string aConsumerName) {
            _gdnErrorHandler.isWaiting = true;
            _monobehaviour.StartCoroutine(
                MacrometaAPI.DocumentReader(_gdnData, streamName, aConsumerName, SetDocumentReader,_gdnErrorHandler));
        }

        public void SetDocumentReader(WebSocket ws, string debug = "") {
            lobbyDocumentReader = ws;
            lobbyDocumentReader.OnOpen += (o) => {
                GameDebug.Log("Stream driver Open " + debug + " chatConsumer1: " );
                lobbyDocumentReaderExists = true;
                _gdnErrorHandler.isWaiting = false;
            };

            lobbyDocumentReader.OnMessage += (sender, e) => {
                GameDebug.Log(" LobbyDocumentReader.OnMessage: " );
                var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);
                var json  =Encoding.UTF8.GetString(Convert.FromBase64String(receivedMessage.payload));
                var lobbyBase = JsonUtility.FromJson<LobbyBase>(json);
                GameDebug.Log(" LobbyDocumentReaderExists.OnMessage stream name:" +  lobbyBase.lobbyValue.streamName + ":"+
                              chatLobbyId +":"+lobbyBase.lobby);
                if (lobbyBase.lobby && chatLobbyId ==  lobbyBase.lobbyValue.streamName) {
                    lobbyUpdate = lobbyBase.lobbyValue;
                    lobbyUpdateAvail = true;
                }
                
            };
            lobbyDocumentReader.OnError += (sender, e) => {
                GameDebug.Log("WebSocket Error" + debug + " : " + e);

                if (producer1 != null && producer1.IsOpen) {
                    producer1.Close();
                }
                else {
                    lobbyDocumentReaderExists = false;
                    _gdnErrorHandler.isWaiting = false;
                }
            };

            lobbyDocumentReader.OnClosed += (socket, code, message) => {
                lobbyDocumentReaderExists = false;
                _gdnErrorHandler.isWaiting = false;
            };

            lobbyDocumentReader.Open();

        }

        
        
        public void ConnectClient(ReceivedMessage receivedMessage) {
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = receivedMessage.properties.d,
                destination = receivedMessage.properties.s,
                port = receivedMessage.properties.p,
                playerName =  receivedMessage.properties.localId,
            };

            var id = AddOrGetConnectionId(connection);
            var driverTransportEvent = new GDNNetworkDriver.DriverTransportEvent() {
                connectionId = id,
                data = new byte[0],
                dataSize = 0,
                type = GDNNetworkDriver.DriverTransportEvent.Type.Connect
            };
            PushEventQueue(driverTransportEvent);
            if (sendDummyTraffic) {
                _monobehaviour.StartCoroutine(RepeatDummyMsg());
                GameDebug.Log("dummy size:" + dummySize + " freq: " + dummyFrequency);
            }

            GameDebug.Log("ConnectClient id: " + id + " count: " + gdnConnections.Count);

        }

        public void DisconnectFromClient(ReceivedMessage receivedMessage) {
            GameDebug.Log("DisconnectFromClient id called   count: " + gdnConnections.Count);
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = receivedMessage.properties.d,
                destination = receivedMessage.properties.s,
                port = receivedMessage.properties.p
            };

            var id = GetConnectionId(connection);
            if (id != -1) {
                RemoveConnectionId(id);
                GameDebug.Log("DisconnectFromClient A id: " + id + " : " + GetConnectionId(connection));
            }

            var driverTransportEvent = new GDNNetworkDriver.DriverTransportEvent() {
                connectionId = id,
                data = new byte[0],
                dataSize = 0,
                type = GDNNetworkDriver.DriverTransportEvent.Type.Disconnect
            };

            PushEventQueue(driverTransportEvent);
            GameDebug.Log("DisconnectFromClient B id: " + id + " count: " + gdnConnections.Count);
        }

        /// <summary>
        /// called when server refuses connection due to max client number
        /// not coded
        /// should shutdown driver and transport
        /// and tell game to go back to browse
        /// also maybe called when leave game
        /// </summary>
        /// <param name="receivedMessage"></param>
        public void DisconnectFromServer(ReceivedMessage receivedMessage) {
            GameDebug.Log("DisconnectFromServer ");

            // this works as is 
            /*
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = receivedMessage.properties.d,
                destination = receivedMessage.properties.s,
                port = receivedMessage.properties.p 
            };
    
            var id = GetConnectionId(connection);
            if(id == -1)
                RemoveConnectionId(id);
            var driverTransportEvent = new GDNNetworkDriver.DriverTransportEvent() {
                connectionId = id,
                data = new byte[0],
                dataSize = 0,
                type = GDNNetworkDriver.DriverTransportEvent.Type.Disconnect
            };
             
            PushEventQueue(driverTransportEvent);
            GameDebug.Log("DisconnectFromClient C id: " + id + " Q: " + driverTransportEvents.Count);
            */
        }

        public void Connect() {
            if (_isServer) return;
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = consumerName,
                destination = serverName,
                playerName = GDNStats.playerName,
                port = 443
            };

            var id = AddOrGetConnectionId(connection);
            GameDebug.Log(" Client send Connect");
            ProducerSend(id, VirtualMsgType.Connect, new byte[0]);
            if (sendDummyTraffic) {
                _monobehaviour.StartCoroutine(RepeatDummyMsg());
                GameDebug.Log("dummy size:" + dummySize + " freq: " + dummyFrequency);
            }
        }

        public int AddOrGetConnectionId(GDNNetworkDriver.GDNConnection gdnConnection) {
           
            foreach (var kvp in gdnConnections) {
                if (kvp.Value.destination == gdnConnection.destination &&
                    kvp.Value.port == gdnConnection.port) {
                    return kvp.Value.id;
                }
            }

            IEnumerable<int> query = from id in gdnConnections.Keys
                orderby id
                select id;

            int min = 0;
            foreach (var id in query) {
                if (id != min) {
                    break;
                }
                else {
                    min++;
                }
            }

            gdnConnection.id = min;
            gdnConnections[min] = gdnConnection;
//            GameDebug.Log("modifying connection collection");
            return min;
        }

        public int GetConnectionId(GDNNetworkDriver.GDNConnection gdnConnection) {
            foreach (var kvp in gdnConnections) {
                if (kvp.Value.destination == gdnConnection.destination &&
                    kvp.Value.port == gdnConnection.port) {
                    return kvp.Value.id;
                }
            }

            IEnumerable<int> query = from id in gdnConnections.Keys
                orderby id
                select id;

            int min = 0;
            foreach (var id in query) {
                if (id != min) {
                    break;
                }
                else {
                    min++;
                }
            }
            if (!gdnConnections.ContainsKey(min)) {
                return -1;
            }
            gdnConnection.id = min;
            return min;
        }

        public void RemoveConnectionId(int connectionID) {
            if (gdnConnections.ContainsKey(connectionID)) {
                GameDebug.Log(" Remove player: "+ gdnConnections[connectionID].playerName);
                var aPlayerName = gdnConnections[connectionID].playerName;
                DisconnectPlayer(aPlayerName);
                gdnConnections.Remove(connectionID);
               
            }
        }

        public void DisconnectPlayer(string aPlayerName) {
            var ps = GDNStats.baseGameStats.CopyOf();
            //GameDebug.Log("GenerataPeriodicGameStats2 B");
            ps.timeStamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            ps.playerName = aPlayerName;
            ps.disconnect = true;
            ProducerGameStatsSend(ps);
        }

        public void PushEventQueue(GDNNetworkDriver.DriverTransportEvent driverTransportEvent) {
            driverTransportEvents.Enqueue(driverTransportEvent);
        }

        public GDNNetworkDriver.DriverTransportEvent PopEventQueue() {
            if (driverTransportEvents.Count == 0) {
                return new GDNNetworkDriver.DriverTransportEvent() {
                    type = GDNNetworkDriver.DriverTransportEvent.Type.Empty,
                    connectionId = -1,
                    data = new byte[0],
                    dataSize = 0
                };
            }

            return driverTransportEvents.Dequeue();
        }

        public void InitPingStatsGroup() {
            
            pingStatsGroup.InitStatsFromGDNDate(_gdnData);
            pingStatsGroup.SetStreamStats(consumer1Stats, true);
            pingStatsGroup.SetStreamStats(producer1Stats, false);
            pingStatsGroup.InitStatsFromRegion(region);
            pingStatsGroup.localId = localId;
            pingStatsGroup.appType = appType;
        }

        public IEnumerator RepeatTransportPing() {
            GameDebug.Log("starting RepeatTransportPing");
            for (;;) {
                SendTransportPings();
                yield return new WaitForSeconds(pingFrequency);

            }
        }

        public void ReceiveTransportPong(ReceivedMessage receivedMessage) {
            var transportPing = TransportPings.Remove(receivedMessage.properties.i);
            pongOnlyRtt = transportPing.elapsedTime;


            if (isStatsOn) {
               
                var networkStatsData = pingStatsGroup.AddRtt(transportPing.elapsedTime,
                    producer1.Latency, consumer1.Latency,
                    receivedMessage.properties.o, receivedMessage.properties.r,
                    receivedMessage.properties.localId,
                    receivedMessage.properties.host, receivedMessage.properties.city,
                    receivedMessage.properties.countrycode);
                
                if (networkStatsData != null) {
                   // GameDebug.Log("ReceiveTransportPong C");
                    if (isPlayStatsServerOn) {
                        //GameDebug.Log("ReceiveTransportPong D ");
                        var gameStats2 = PlayStats.GenerataPeriodicGameStats2(networkStatsData,receivedMessage);
                       // GameDebug.Log("ReceiveTransportPong E ");
                        ProducerGameStatsSend(gameStats2);
                       // GameDebug.Log("GameStats: " + gameStats2);
                    }
                    else {
                        ProducerStatsSend(networkStatsData);
                    }
                }
            }
        }



        public void ReceiveChatTransportPong(string rttClientId) {

            var rtt = (int) chatPingStopwatch.ElapsedMilliseconds;
            //GameDebug.Log("lRtt: " + rttClientId + " : " + rtt);
            ChatSendSetRttTime(rttClientId,rtt);
            
        }
        /// <summary>
        /// crashing latency test
        /// so moved
        ///  RemoveConnectionId(id); out of foreach
        /// retest with Games list type usage
        /// is all this first ping stuff just games list?
        /// need to check games list code again
        /// </summary>
        public void SendTransportPings() {
            foreach (var destinationId in gdnConnections.Keys) {
                if (TransportPings.firstPingTimes.ContainsKey(destinationId) &&
                    Time.time > TransportPings.firstPingTimes[destinationId]) {

                    var pingId = TransportPings.Add(destinationId, Time.realtimeSinceStartup, 0);
                    ProducerSend(destinationId, VirtualMsgType.Ping, new byte[0], pingId);
                }
                else if (!TransportPings.firstPingTimes.ContainsKey(destinationId)) {
                    TransportPings.firstPingTimes[destinationId] = Time.time + initialPingDelay;
                }
            }
            
            var disocnnects = TransportPings.HeartbeatCheck(missedPingDisconnect);
            foreach (var id in disocnnects) {
                var driverTransportEvent = new GDNNetworkDriver.DriverTransportEvent() {
                    connectionId = id,
                    data = new byte[0],
                    dataSize = 0,
                    type = GDNNetworkDriver.DriverTransportEvent.Type.Disconnect
                };
                PushEventQueue(driverTransportEvent);
                TransportPings.RemoveDestinationId(id);
                RemoveConnectionId(id);
                GameDebug.Log("lost connection id: " + id);
            }
            
        }
        
        public void SendSimpleTransportPing() {
            GameDebug.Log("SendSimpleTransportPing()");
                var pingId = TransportPings.Add(0, Time.realtimeSinceStartup, 0);
                ProducerSend(0, VirtualMsgType.Ping, new byte[0], pingId);
                GameDebug.Log("SendSimpleTransportPing() ProducerSend called ");
        }
        
        public void SendTransportPong(ReceivedMessage receivedMessage) {
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = receivedMessage.properties.d,
                destination = receivedMessage.properties.s,
                port = receivedMessage.properties.p
            };
            GameDebug.Log("send pong : "+ receivedMessage.properties.s);
            ProducerSend(connection, VirtualMsgType.Pong, new byte[0], receivedMessage.properties.i,
                producer1.Latency, consumer1.Latency,GDNStats.playerName);
        }
        
        public void SendChatTransportPing() {
            //GameDebug.Log("Send chat TransportPing()");
            
            chatPingStopwatch = Stopwatch.StartNew();
            chatPingStopwatch.Reset();
            chatPingStopwatch.Start();
            
            //do I need other properties
            var properties = new MessageProperties() {
                t = VirtualMsgType.Ping,
                d = chatLobbyId,
                s = consumerName,
                i = 0
            };
            var msg = "";
            var message = new SendMessage() {
                properties = properties,
                payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))
            };
            string msgJSON = JsonUtility.ToJson(message);
            chatProducer1.Send(msgJSON);
            GameDebug.Log("SendChatTransportPing() called: "+ consumerName);
        }
        
        public void SendChatTransportPong(string channelId,ReceivedMessage receivedMessage) {
            var properties = new MessageProperties() {
                t = VirtualMsgType.Pong,
                d = channelId,
                s = consumerName,
                i = receivedMessage.properties.i
            };
            var command = new LobbyCommand() {
                command = LobbyCommandType.Pong,
                source = consumerName,
                target = receivedMessage.properties.s,
                intVal =  receivedMessage.properties.i,
            };
            var msg = JsonUtility.ToJson(command);
            var message = new SendMessage() {
                properties = properties,
                payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))
            };
            string msgJSON = JsonUtility.ToJson(message);
            chatProducer1.Send(msgJSON);
            GameDebug.Log("pingSendChatTransportPong() called: consumer"+ consumerName + " i: "+receivedMessage.properties.i);
        }

        
        private IEnumerator RepeatDummyMsg() {
            for (;;) {
                for (int i = 0; i < dummyTrafficQuantity; i++) {
                    SendDummy();
                }
                yield return new WaitForSeconds(dummyFrequency);
            }
        }

        public void SendDummy() {
            if (gdnConnections.Count == 0) {
                return;
            }

            int desitnationId = gdnConnections.Keys.First();
            System.Random rnd = new System.Random();
            byte[] data = new byte[dummySize]; // convert kb to byte
            rnd.NextBytes(data);
            ProducerSend(desitnationId, VirtualMsgType.Dummy, data);

        }

        public void ReceiveInternal(ReceivedMessage receivedMessage) {
            //process internal message
            //for update ping stats for client stream
        }

        public void ReceiveDummy(ReceivedMessage receivedMessage) {
            //store dummy stats somewhere
        }

        private void AddCommand(GDNStreamDriver.Command command) {
            _commandQueue.Enqueue(command);
        }

        public void ExecuteCommands() {
            GDNStreamDriver.Command command;
            while ( _commandQueue.TryDequeue(out command)) {
                Execute(command);
            }
        }

        private void Execute(GDNStreamDriver.Command command) {
            switch (command.command) {
                case QueueCommand.Connect:
                    Connect();
                    break;
                case QueueCommand.ConnectClient:
                    ConnectClient(command.receivedMessage);
                    break;
                case QueueCommand.ReceiveInternal:
                    ReceiveInternal(command.receivedMessage);
                    break;
                case QueueCommand.ReceiveDummy:
                    ReceiveDummy(command.receivedMessage);
                    break;
                case QueueCommand.DisconnectFromClient:
                    DisconnectFromClient(command.receivedMessage);
                    break;
                case QueueCommand.DisconnectFromServer:
                    DisconnectFromServer(command.receivedMessage);
                    break;
                case QueueCommand.ReceiveTransportPong:
                    ReceiveTransportPong(command.receivedMessage);
                    break;
                case QueueCommand.SendTransportPing:
                    SendTransportPings();
                    break;
                case QueueCommand.SendTransportPong:
                    SendTransportPong(command.receivedMessage);
                    break;
                default:
                    break;
            }
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
                    if (isLobbyAdmin) {
                        GameDebug.Log("Request for lobby Room: " + command.roomNumber + " playerNAme: " + command.playerName
                                      +  "clientId " + command.source);
                        GDNClientLobbyNetworkDriver2.MoveToTeam(command.teamSlot, command.roomNumber);
                    }
                    break;
                case LobbyCommandType.AllowServer:
                    GameDebug.Log("unhandled lobby command from source: " 
                                  + command.command +" : "+command.source );
                        break;
                case LobbyCommandType.CloseLobby:
                    if (isLobbyAdmin) {
                        GameDebug.Log("unhandled lobby command from source: " 
                                      + command.command +" : "+command.source );
                    }
                    break;
                case LobbyCommandType.GameInit:
                    GameDebug.Log("unhandled lobby command from source: " 
                                  + command.command +" : "+command.source );
                    break;
                case LobbyCommandType.GameReady:
                    GameDebug.Log("unhandled lobby command from source: " 
                                  + command.command +" : "+command.source );
                    break;
                case LobbyCommandType.SendRttTime:
                    GameDebug.Log("unhandled lobby command from source: " 
                                  + command.command +" : "+command.source );
                    GDNClientLobbyNetworkDriver2.SendRttTime(command.target, command.intVal);
                    
                    break;
                case LobbyCommandType.SetRttTarget:
                    GameDebug.Log("unhandled lobby command from source: " 
                                  + command.command +" : "+command.source );
                    
                    SendChatTransportPing();
                    break;
                case LobbyCommandType.Pong:
                    GameDebug.Log(" lobby command from source: " 
                                  + command.command +" : "+command.source );
                    ReceiveChatTransportPong(command.source);
                    break;
                default:
                    break;

            }
        }
    }

}