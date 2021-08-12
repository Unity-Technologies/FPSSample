using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BestHTTP.WebSocket;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Macrometa {

    [Serializable]
    public class GDNStreamDriver {
        private MonoBehaviour _monobehaviour;
        public static bool isSocketPingOn = false; // must be set before webSocket is opened
        public static bool isStatsOn = false; //off by default for compatibility 
        public static bool sendDummyTraffic = false; //off by default for compatibility
        public static int missedPingDisconnect = 3;
        public static float initialPingDelay = 30; // wait period before first ping is sent after connect
        public static bool isClientBrowser = false;
        public ListStream listStream;
        public WebSocket consumer1;
        public WebSocket producer1;
        public WebSocket producerStats;
        public StreamStats consumer1Stats;
        public StreamStats producer1Stats;
        public string consumerName = "Server";
        public string serverName;
        public bool streamListDone = false;
        public bool serverInStreamExists = false;
        public bool serverOutStreamExists = false;
        public bool serverStatsStreamExists = false;
        public bool producerExists = false;
        public bool consumerExists = false;
        public bool producerStatsExists = false;
        public bool sendConnect = true;
        public bool setupComplete = false;
        public bool pingStarted = false;
        public string serverInStreamName;
        public string serverOutStreamName;
        public string serverStatsStreamName;
        public string consumerStreamName;
        public string producerStreamName;
        public float pingFrequency = 1;
        public float dummyFrequency = 0.05f; // FPSSample standard is 20 messages per second
        public int dummySize = 50; // FPSSample standard is under 2000 bytes per second
        public string nodeId = "";
        public PingStatsGroup pingStatsGroup = new PingStatsGroup();
        public int statsGroupSize;
        public int dummyTrafficQuantity;

        public Dictionary<int, GDNNetworkDriver.GDNConnection> gdnConnections =
            new Dictionary<int, GDNNetworkDriver.GDNConnection>();

        public Queue<GDNNetworkDriver.DriverTransportEvent> driverTransportEvents =
            new Queue<GDNNetworkDriver.DriverTransportEvent>();

        private ConcurrentQueue<GDNStreamDriver.Command> queue = new ConcurrentQueue<GDNStreamDriver.Command>();
        private GDNErrorhandler _gdnErrorHandler;
        private GDNData _gdnData;
        private bool _isServer;

        public bool receivedPongOnly = false;
        public float pongOnlyRtt = 0;


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

        public void CreateProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            producer1Stats = new StreamStats() {
                dataType = DataType.JSON,
                streamName = streamName,
                streamType = StreamType.Shared
            };
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetProducer));
        }

        public void SetProducer(WebSocket ws, string debug = "") {
            producer1 = ws;
            if (isSocketPingOn) {
                ws.StartPingThread = true;
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

        public void CreateStatsProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            GameDebug.Log("CreateStatsProducer: " + streamName);
            producer1Stats = new StreamStats() {
                dataType = DataType.JSON,
                streamName = streamName,
                streamType = StreamType.Shared
            };
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetStatsProducer));
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

        public void ProducerSend(int id, VirtualMsgType msgType, byte[] payload,
            int pingId = 0, int pingTimeR = 0, int pingTimeO = 0, string aNodeId = "") {
            //GameDebug.Log("ProducerSend A: " + id);
            if (!gdnConnections.ContainsKey(id)) {
                GameDebug.Log("ProducerSend bad id:" + id)  ;
            }

            var gdnConnection = gdnConnections[id];
            
            ProducerSend( gdnConnection,msgType, payload, pingId, pingTimeR, pingTimeO, aNodeId);
        }

        private void ProducerSend(GDNNetworkDriver.GDNConnection gdnConnection, VirtualMsgType msgType,
            byte[] payload, int pingId, int pingTimeR, int pingTimeO,
            string aNodeId ) {
            var properties = new MessageProperties() {
                t = msgType,
                p = gdnConnection.port,
                d = gdnConnection.destination,
                s = gdnConnection.source,
                z = payload.Length
            };
            if (msgType == VirtualMsgType.Ping || msgType == VirtualMsgType.Pong) {
                properties.i = pingId;
                properties.r = pingTimeR;
                properties.o = pingTimeO;
                properties.n = aNodeId;
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
            if (isStatsOn) {
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

        public void CreateConsumer(string streamName, string consumerName) {
            _gdnErrorHandler.isWaiting = true;
            consumer1Stats = new StreamStats() {
                dataType = DataType.JSON,
                streamName = streamName,
                streamType = StreamType.Shared
            };
            _monobehaviour.StartCoroutine(MacrometaAPI.Consumer(_gdnData, streamName, consumerName, SetConsumer));
        }

        public void SetConsumer(WebSocket ws, string debug = "") {

            consumer1 = ws;
            consumer1.StartPingThread = isSocketPingOn;
            consumer1.OnOpen += (o) => {
                GameDebug.Log("Stream driver Open " + debug + " isPingOn: " + isSocketPingOn);
                consumerExists = true;
                _gdnErrorHandler.isWaiting = false;
            };

            consumer1.OnMessage += (sender, e) => {
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
                    receivedMessage.properties.d == consumerName
                    // && DateTime.Now.AddMinutes(discardMinutes) < DateTime.Parse(receivedMessage.publishTime)  
                ) {
                    // GameDebug.Log("Consumer1.OnMessage 2");
                    GDNStreamDriver.Command command;
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
                            //GameDebug.Log("Consumer1.OnMessage Connect: " + receivedMessage.properties.s);
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
                MacrometaAPI.Consumer(_gdnData, streamName, consumerName, SetConsumerPongOnly));
        }

        public void SetConsumerPongOnly(WebSocket ws, string debug = "") {

            consumer1 = ws;
            consumer1.StartPingThread = isSocketPingOn;
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

        public void ConnectClient(ReceivedMessage receivedMessage) {
            var connection = new GDNNetworkDriver.GDNConnection() {
                source = receivedMessage.properties.d,
                destination = receivedMessage.properties.s,
                port = receivedMessage.properties.p
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
                gdnConnections.Remove(connectionID);
            }
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
        }

        public IEnumerator RepeatTransportPing() {
            for (;;) {
                SendTransportPing();
                yield return new WaitForSeconds(pingFrequency);

            }
        }

        public void ReceiveTransportPong(ReceivedMessage receivedMessage) {
            var transportPing = TransportPings.Remove(receivedMessage.properties.i);
            if (isStatsOn) {
                var networkStatsData = pingStatsGroup.AddRtt(transportPing.elapsedTime,
                    producer1.Latency, consumer1.Latency,
                    receivedMessage.properties.o, receivedMessage.properties.r,
                    receivedMessage.properties.n);
                if (networkStatsData != null) {
                    ProducerStatsSend(networkStatsData);
                }
            }
        }

        public void SendTransportPing() {
            foreach (var destinationId in gdnConnections.Keys) {
                if (TransportPings.firstPingTimes.ContainsKey(destinationId) &&
                    Time.time > TransportPings.firstPingTimes[destinationId]) {

                    var pingId = TransportPings.Add(destinationId, Time.realtimeSinceStartup, 0);
                    ProducerSend(destinationId, VirtualMsgType.Ping, new byte[0], pingId);

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
                else if (!TransportPings.firstPingTimes.ContainsKey(destinationId)) {
                    TransportPings.firstPingTimes[destinationId] = Time.time + initialPingDelay;
                }
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
            //GameDebug.Log("send pong : "+ receivedMessage.properties.s);
            ProducerSend(connection, VirtualMsgType.Pong, new byte[0], receivedMessage.properties.i,
                producer1.Latency, consumer1.Latency, nodeId);
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
            queue.Enqueue(command);
        }

        public void ExecuteCommands() {
            GDNStreamDriver.Command command;
            while (queue.TryDequeue(out command)) {
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
                    SendTransportPing();
                    break;
                case QueueCommand.SendTransportPong:
                    SendTransportPong(command.receivedMessage);
                    break;
                default:
                    break;

            }

        }
    }

}