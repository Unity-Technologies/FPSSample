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
using System.Diagnostics;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

//need to finish Connection distinary at bottom
// need event queue
public class GDNNetworkDriver : MonoBehaviour {
    
    public static bool overrideIsServer = false;
    public static bool overrideIsServerValue = false;
    public static bool isSocketPingOn= false; // must be set before webSocket is opened
    public static bool isStatsOn= false; //off by default for compatibility 
    public static bool sendDummyTraffic = false; //off by default for compatibility
    public static int missedPingDisconnect = 3;
    public static float initialPingDelay = 30; // wait period before first ping is sent after connect

    public static int WebSocketPing = 0;
    
    public GDNData baseGDNData;
    public string gameName = "FPSGameXX";
    public bool isServer = false;

    public ListStream listStream;
    public WebSocket consumer1;
    public WebSocket producer1;
    public StreamStats consumer1Stats;
    public StreamStats producer1Stats;
    public string consumerName = "Server";
    public string serverName;
    public int ttl = 3600;
    public double discardMinutes = -2;// discard messages more than X minutes old should be a negative number 
    public int port;
    
    //Connection handling
    public int increasePauseConnectionError = 10;
    public bool isWaitingDebug = false;
    private bool _isWaiting;
    private const string GDNDriverPing = "GDNDriver";
    
    public bool isWaiting {
        get => _isWaiting;
        set {
            _isWaiting = value;
            if (isWaitingDebug) {
                GameDebug.Log("isWaiting: " + value);
            }
        }
    }

    [SerializeField] 
    private int _currentNetworkErrors;
    
    public int currentNetworkErrors {
        get => _currentNetworkErrors;
        set {
            _currentNetworkErrors = value;
            if (currentNetworkErrors == 0) {
                pauseNetworkError = basePauseNetworkError;
            }
            if (isWaitingDebug) {
                GameDebug.Log("NetworkError: " + value);
                pauseNetworkErrorUntil = Time.time + pauseNetworkError;
            }
        }
    }

    public float pauseNetworkError = 1f;
    public float basePauseNetworkError = 1f;
    public float pauseNetworkErrorMultiplier = 2f;
    public float pauseNetworkErrorUntil = 0f;
    public bool isNetworkErrorPause = false;

    //init progress
    public bool clearAllBacklogsDone = false;
    public bool setTTLDone = false;
    public bool streamListDone = false;
    public bool serverInStreamExists = false;
    public bool serverOutStreamExists = false;
    public bool producerExists = false;
    public bool consumerExists = false;
    public bool sendConnect = true;
    public bool setupComplete = false;
    public bool pingStarted = false;

    protected string serverInStreamName;
    protected string serverOutStreamName;
    protected string consumerStreamName;
    protected string producerStreamName;

    // for latency & data rate testing
    public float pingFrequency = 1; 
                                    
    public float dummyFrequency = 0.05f; // FPSSample standard is 20 messages per second
    public int dummySize = 50; // FPSSample standard is under 2000 bytes per second
    public string nodeId = "";

    public PingStatsGroup pingStatsGroup = new PingStatsGroup();
    public int statsGroupSize;
    public int dummyTrafficQuantity;
    
    public void Awake() {
        
        BestHTTP.HTTPManager.Setup();
        BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
        //var configGDNjson = Resources.Load<TextAsset>("configGDN");
        var defaultConfig = RwConfig.ReadConfig();
        RwConfig.Flush();
        baseGDNData = defaultConfig.gdnData;
        gameName = defaultConfig.gameName; 
        statsGroupSize = defaultConfig.statsGroupSize;
        if (statsGroupSize < 1) {
            statsGroupSize = 900; //15 minutes
        }
        dummyTrafficQuantity = defaultConfig.dummyTrafficQuantity;
        if (dummyTrafficQuantity < 0) {
            dummyTrafficQuantity = 0; 
        }
        nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
        GameDebug.Log("Setup: " + nodeId);
        if (overrideIsServer) {
            isServer = overrideIsServerValue;
        }
        else {
            isServer = defaultConfig.isServer;
        }

        serverInStreamName = gameName + "_InStream";
        serverOutStreamName = gameName + "_OutStream";
        serverName = consumerName;
       
        if (isServer) {
            consumerStreamName = serverInStreamName;
            producerStreamName = serverOutStreamName;
        }
        else {
            consumerStreamName = serverOutStreamName;
            producerStreamName = serverInStreamName;
            setRandomClientName();
        }
        
        LogFrequency.AddLogFreq("consumer1.OnMessage",1,"consumer1.OnMessage data: ",3);
        LogFrequency.AddLogFreq("ProducerSend Data",1,"ProducerSend Data: ",3);

        isStatsOn &= isSocketPingOn && isServer;
    }
    
    
    void Update() {
        SetupLoopBody();
    }
    
    public void setRandomClientName() {
        consumerName = "C" + (10000000 + Random.Range(1, 89999999)).ToString();
    }

    public void SetupLoopBody() {
        
        
       
        if (pauseNetworkErrorUntil > Time.time) return;
        if (currentNetworkErrors >= increasePauseConnectionError) {
            pauseNetworkError *= pauseNetworkErrorMultiplier;
            
            return;
        }

        if (isWaiting) return;
/*
        if (!clearAllBacklogsDone) {
            ClearAllBacklogs();
            return;
        }

        if (!setTTLDone) {
            SetTTL(ttl);
            return;
        }
    */    
        if (!streamListDone) {
            GetListStream();
            return;
        }

        if (!serverInStreamExists) {
            CreatServerInStream();
            return;
        }

        if (!serverOutStreamExists) {
            CreatServerOutStream();
            return;
        }

        if (!producerExists) {
            CreateProducer(producerStreamName);
            return;
        }

        if (!consumerExists) {
            CreateConsumer(consumerStreamName, consumerName);
            return;
        }

        if (!setupComplete) {
            GameDebug.Log("Set up Complete as " + gameName + " : " + consumerName);
            setupComplete = true;
            GDNTransport.setupComplete = true;
        }
        if (!sendConnect && !isServer) {
            GameDebug.Log("Connect after complete " + gameName + " : " + consumerName);
            Connect(); // called on main thread so this is OK
            sendConnect = true;
            
        }
        if (isSocketPingOn && !pingStarted ) {
            pingStarted = true;
            if (isStatsOn) {
                PingStatsGroup.Init(Application.dataPath, "LatencyStats",statsGroupSize);
                InitPingStatsGroup();
                GameDebug.Log("isSocketPingOn: " + PingStatsGroup.latencyGroupSize);
            }
            StartCoroutine(RepeatTransportPing());
        }

        if (isSocketPingOn) {
            WebSocketPing = consumer1.Latency;
        }

        ExecuteCommands();
    }

    public void SetTTL(int ttl = 3) {
        isWaiting = true;
        StartCoroutine(MacrometaAPI.SetTTL(baseGDNData, ttl, SetTTLCallBack));
    }
    
    
    public void SetTTLCallBack(UnityWebRequest www) {
        isWaiting = false;
        if (www.isHttpError || www.isNetworkError) {
            GameDebug.Log("SetTTL : " + www.error);
            currentNetworkErrors++;
        }
        else {

            var baseHtttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
            if (baseHtttpReply.error == true) {
                currentNetworkErrors++;
            }
            else {
                setTTLDone = true;
                currentNetworkErrors = 0;
            }
        }
    }

    public void ClearAllBacklogs() {
        isWaiting = true;
        StartCoroutine(MacrometaAPI.ClearAllBacklogs(baseGDNData, ClearAllBacklogsCallBack));
    }
    
    
    public void ClearAllBacklogsCallBack(UnityWebRequest www) {
        isWaiting = false;
        if (www.isHttpError || www.isNetworkError) {
            GameDebug.Log("ClearAllBacklogs : " + www.error);
            currentNetworkErrors++;
            GameDebug.Log("ClearAllBacklogs URL: " + baseGDNData.ClearAllBacklogs());
        }
        else {

            var baseHtttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
            if (baseHtttpReply.error == true) {
                GameDebug.Log("ClearAllBacklogs failed:" + baseHtttpReply.code);
                currentNetworkErrors++;
            }
            else {
                clearAllBacklogsDone = true;
                currentNetworkErrors = 0;
            }
        }
    }
    
    public void GetListStream() {
        //Debug.Log(baseGDNData.ListStreamsURL());
        isWaiting = true;
        StartCoroutine(MacrometaAPI.ListStreams(baseGDNData, ListStreamCallback));
    }

    public void ListStreamCallback(UnityWebRequest www) {
        isWaiting = false;
        if (www.isHttpError || www.isNetworkError) {
            currentNetworkErrors++;
            GameDebug.Log("ListStream : " + www.error);
        }
        else {

            //overwrite does not assign toplevel fields
            //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
            listStream = JsonUtility.FromJson<ListStream>(www.downloadHandler.text);
            if (listStream.error == true) {
                GameDebug.Log("ListStream failed:" + listStream.code);
                //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                currentNetworkErrors++;
            }
            else {
                GameDebug.Log("ListStream succeed " );
                streamListDone = true;
                currentNetworkErrors = 0;
            }
        }
    }
    
    
    public void CreatServerInStream() {
        var gsListName = baseGDNData.StreamListName(serverInStreamName);
        serverInStreamExists = listStream.result.Any(item => item.topic == gsListName);
        if (!serverInStreamExists) {
            isWaiting = true; ;
            //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
            StartCoroutine(MacrometaAPI.CreateStream(baseGDNData, serverInStreamName, CreateServerInStreamCallback));
        }
    }

    public void CreateServerInStreamCallback(UnityWebRequest www) {
        isWaiting = false;
        if (www.isHttpError || www.isNetworkError) {
            GameDebug.Log("CreateServerInStream : " + www.error);
            currentNetworkErrors++;
            streamListDone= false;
        }
        else {

            var baseHtttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
            if (baseHtttpReply.error == true) {
                GameDebug.Log("create ServerIn stream failed:" + baseHtttpReply.code);
                currentNetworkErrors++;
                streamListDone= false;
            }
            else {
                GameDebug.Log("create ServerIn stream ");
                serverInStreamExists = true;
                currentNetworkErrors = 0;
            }
        }
    }

    
    public void CreatServerOutStream() {
        var gsListName = baseGDNData.StreamListName(serverOutStreamName);
        serverOutStreamExists = listStream.result.Any(item => item.topic == gsListName);
        if (!serverOutStreamExists) {
            isWaiting = true;
            StartCoroutine(MacrometaAPI.CreateStream(baseGDNData, serverOutStreamName, CreateServerOutStreamCallback));
        }
    }

    public void CreateServerOutStreamCallback(UnityWebRequest www) {
        isWaiting = false;
        if (www.isHttpError || www.isNetworkError) {
            GameDebug.Log("CreateServerOutStream error: " + www.error);
            currentNetworkErrors++;
            streamListDone= false;
        }
        else {

            var baseHtttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
            if (baseHtttpReply.error == true) {
                GameDebug.Log("create Server Out stream failed:" + baseHtttpReply.code);
                currentNetworkErrors++;
                streamListDone= false;
            }
            else {
                GameDebug.Log("CreateServerOutStream ");
                serverOutStreamExists = true;
                currentNetworkErrors = 0;
            }
        }
    }

    
    public void CreateProducer(string streamName) {
        isWaiting = true;
        producer1Stats = new StreamStats() {
            dataType = DataType.JSON,
            streamName = streamName,
            streamType = StreamType.Shared
        } ;
        StartCoroutine(MacrometaAPI.Producer(baseGDNData, streamName, SetProducer));
    }

    public void SetProducer(WebSocket ws, string debug = "") {
        producer1 = ws;
        if (isSocketPingOn) {
            ws.StartPingThread = true;
        }
        producer1.OnOpen += (o) => {
            isWaiting = false;
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
                isWaiting = false;
            }
        };

        producer1.OnClosed += (socket, code, message) => {
            producerExists = false;
            isWaiting = false;
            GameDebug.Log("Produce closed: " + code + " : " + message);
        };
        producer1.Open();
    }
    
    public void ProducerSend(int id, VirtualMsgType msgType, byte[] payload,
        int pingId = 0, int pingTimeR = 0, int pingTimeO = 0, string aNodeId = "") {
        if (!gdnConnections.ContainsKey(id)) {
            GameDebug.Log("ProducerSend bad id:" + id);
        }
        var properties = new MessageProperties() {
            t = msgType,
            p = gdnConnections[id].port,
            d = gdnConnections[id].destination,
            s = gdnConnections[id].source,
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
        if(msgType == VirtualMsgType.Data){
            LogFrequency.Incr("ProducerSend Data", 
                message.payload.Length,
            payload.Length,0
                );}
        producer1.Send(msgJSON);
        if (isStatsOn) {
            producer1Stats.IncrementCounts(msgJSON.Length);
        }
    }
    
    public void CreateConsumer(string streamName, string consumerName) {
        isWaiting = true;
        consumer1Stats = new StreamStats() {
            dataType = DataType.JSON,
            streamName = streamName,
            streamType = StreamType.Shared
        } ;
        StartCoroutine(MacrometaAPI.Consumer(baseGDNData, streamName, consumerName, SetConsumer));
    }

     public void SetConsumer(WebSocket ws, string debug = "") {

        consumer1 = ws;
        consumer1.StartPingThread = isSocketPingOn;
        consumer1.OnOpen += (o) => {
            GameDebug.Log("Open " + debug + " isPingOn: "+ isSocketPingOn);
            consumerExists = true;
            isWaiting = false;
        };
        
        consumer1.OnMessage += (sender, e) => {
            var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);
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
                //GameDebug.Log("Consumer1.OnMessage 2");
                Command command;
                switch (receivedMessage.properties.t) {
                    case VirtualMsgType.Data:
                   
                        //GameDebug.Log("Consumer1.OnMessage Data");
                        var connection = new GDNConnection() {
                            source = receivedMessage.properties.d,
                            destination = receivedMessage.properties.s,
                            port = receivedMessage.properties.p
                        };

                        var id = GetConnectionId(connection);
                        if (id == -1) {
                            //GameDebug.Log("discarded message unknown source " 
                            //              + receivedMessage.properties.source);
                            break;
                        }

                        var driverTransportEvent = new DriverTransportEvent() {
                            connectionId = id,
                            data = Convert.FromBase64String(receivedMessage.payload),
                            dataSize = Convert.FromBase64String(receivedMessage.payload).Length,
                            type = DriverTransportEvent.Type.Data
                        };
                        PushEventQueue(driverTransportEvent);
                        LogFrequency.Incr("consumer1.OnMessage", receivedMessage.payload.Length,
                            receivedMessage.properties.z, driverTransportEvent.data.Length);


                        break;
                    case VirtualMsgType.Connect:
                        //GameDebug.Log("Consumer1.OnMessage Connect: " + receivedMessage.properties.s);
                        if (isServer) {
                            command = new Command() {
                                command = QueueCommand.ConnectClient,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            //ConnectClient(receivedMessage);
                        }

                        break;
                    case VirtualMsgType.Disconnect:
                        GameDebug.Log("Consumer1.OnMessage Disonnect: " + receivedMessage.properties.s);
                        if (isServer) { 
                            command = new Command() {
                                command = QueueCommand.DisconnectFromClient,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            //DisconnectFromClient(receivedMessage);
                        }
                        else { 
                            command = new Command() {
                                command = QueueCommand.DisconnectFromServer,
                                receivedMessage = receivedMessage
                            };
                            AddCommand(command);
                            //DisconnectFromServer(receivedMessage);
                        }

                        break;
                    case VirtualMsgType.Ping :
                        //GameDebug.Log("Consumer1.OnMessage Ping ");
                        command = new Command() {
                            command = QueueCommand.SendTransportPong,
                            receivedMessage = receivedMessage
                        };
                        AddCommand(command);
                        //SendTransportPong(receivedMessage);
                        break;
                    
                    case VirtualMsgType.Pong :
                        //GameDebug.Log("Consumer1.OnMessage Pong ");
                        command = new Command() {
                            command = QueueCommand.ReceiveTransportPong,
                            receivedMessage = receivedMessage
                        };
                        AddCommand(command);
                        //ReceiveTransportPong(receivedMessage);
                        break;
                    
                    case VirtualMsgType.Internal:
                        //GameDebug.Log("Consumer1.OnMessage internal ");
                        command = new Command() {
                            command = QueueCommand.ReceiveInternal,
                            receivedMessage = receivedMessage
                        };
                        AddCommand(command);
                        //ReceiveInternal(receivedMessage);
                        break;
                    
                    case VirtualMsgType.Dummy:
                        //GameDebug.Log("Consumer1.OnMessage dummy ");
                        command = new Command() {
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
                isWaiting = false;
            }
        };
        
        consumer1.OnClosed += (socket, code, message) => {
            consumerExists = false;
            isWaiting = false;
        };
        
        consumer1.Open();

    }

    

     public void ConnectClient(ReceivedMessage receivedMessage) {
         var connection = new GDNConnection() {
             source = receivedMessage.properties.d,
             destination = receivedMessage.properties.s,
             port = receivedMessage.properties.p 
         };

         var id = AddOrGetConnectionId(connection);
         var driverTransportEvent = new DriverTransportEvent() {
             connectionId = id,
             data = new byte[0],
             dataSize = 0,
             type = DriverTransportEvent.Type.Connect
         };
         PushEventQueue(driverTransportEvent);
         if (sendDummyTraffic) {
             StartCoroutine(RepeatDummyMsg()); 
             GameDebug.Log("dummy size:"+ dummySize + " freq: " + dummyFrequency);
         }
         GameDebug.Log("ConnectClient id: " + id + " count: " + gdnConnections.Count);
         
     }
     
     public void DisconnectFromClient(ReceivedMessage receivedMessage) {
         GameDebug.Log("DisconnectFromClient id called   count: " + gdnConnections.Count);
         var connection = new GDNConnection() {
             source = receivedMessage.properties.d,
             destination = receivedMessage.properties.s,
             port = receivedMessage.properties.p 
         };

         var id = GetConnectionId(connection);
         if (id != -1) {
             RemoveConnectionId(id);
             GameDebug.Log("DisconnectFromClient A id: " + id + " : " +  GetConnectionId(connection));
         }

         var driverTransportEvent = new DriverTransportEvent() {
             connectionId = id,
             data = new byte[0],
             dataSize = 0,
             type = DriverTransportEvent.Type.Disconnect
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
         return;
         
         var connection = new GDNConnection() {
             source = receivedMessage.properties.d,
             destination = receivedMessage.properties.s,
             port = receivedMessage.properties.p 
         };

         var id = GetConnectionId(connection);
         if(id == -1)
             RemoveConnectionId(id);
         var driverTransportEvent = new DriverTransportEvent() {
             connectionId = id,
             data = new byte[0],
             dataSize = 0,
             type = DriverTransportEvent.Type.Disconnect
         };
         
         PushEventQueue(driverTransportEvent);
         GameDebug.Log("DisconnectFromClient C id: " + id + " Q: " + driverTransportEvents.Count);
     }
     
     public void Connect() {
         if (isServer) return; 
         var connection = new GDNConnection() {
             source = consumerName,
             destination = serverName,
             port = 443 
         };

         var id = AddOrGetConnectionId(connection);
         GameDebug.Log(" Client send Connect");
         ProducerSend(id, VirtualMsgType.Connect, new byte[0]);
         if (sendDummyTraffic) {
             StartCoroutine(RepeatDummyMsg()); 
             GameDebug.Log("dummy size:"+ dummySize + " freq: " + dummyFrequency);
         }
     }

     public Dictionary<int,GDNConnection> gdnConnections = new Dictionary<int,GDNConnection>();

     public struct GDNConnection {
         public string source;
         public string destination;
         public int port;
         public int id;
     }

     public int AddOrGetConnectionId(GDNConnection gdnConnection) {
        foreach(var kvp in gdnConnections) {
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
     
     public int GetConnectionId(GDNConnection gdnConnection) {
         foreach(var kvp in gdnConnections) {
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
     
     public void PushEventQueue(DriverTransportEvent driverTransportEvent) {
         driverTransportEvents.Enqueue(driverTransportEvent);
     }
     public DriverTransportEvent PopEventQueue() {
         if (driverTransportEvents.Count == 0) {
             return new DriverTransportEvent() {
                 type = DriverTransportEvent.Type.Empty,
                 connectionId = -1,
                 data = new byte[0],
                 dataSize = 0
             };
         }
         return driverTransportEvents.Dequeue();
     }
     
     public Queue<DriverTransportEvent> driverTransportEvents = new Queue<DriverTransportEvent>();

     #region Latency & bandwidth testing

     public void InitPingStatsGroup() {
         
         pingStatsGroup.InitStatsFromGDNDate(baseGDNData);
         pingStatsGroup.SetStreamStats(consumer1Stats,true);
         pingStatsGroup.SetStreamStats(producer1Stats,false);
     }
     
     
     private IEnumerator RepeatTransportPing() {
         for(;;){
             SendTransportPing();
             yield return new WaitForSeconds(pingFrequency);
             
         }
     }
     
     public void ReceiveTransportPong(ReceivedMessage receivedMessage) {
         var transportPing = TransportPings.Remove(receivedMessage.properties.i);
          if (isStatsOn) {
             pingStatsGroup.AddRtt( transportPing.elapsedTime,
                 producer1.Latency, consumer1.Latency,
                 receivedMessage.properties.o, receivedMessage.properties.r,
                 receivedMessage.properties.n);
         }
     }

     public void SendTransportPing() {
         foreach (var destinationId in gdnConnections.Keys) {
             if (TransportPings.firstPingTimes.ContainsKey(destinationId) &&
                 Time.time > TransportPings.firstPingTimes[destinationId] ) {
                 
                 var pingId = TransportPings.Add(destinationId, Time.realtimeSinceStartup, 0);
                 ProducerSend(destinationId, VirtualMsgType.Ping, new byte[0], pingId);
                 
                 var disocnnects = TransportPings.HeartbeatCheck(missedPingDisconnect);
                 foreach (var id in disocnnects) {
                     var driverTransportEvent = new DriverTransportEvent() {
                         connectionId = id,
                         data = new byte[0],
                         dataSize = 0,
                         type = DriverTransportEvent.Type.Disconnect
                     };
                     PushEventQueue(driverTransportEvent);
                     TransportPings.RemoveDestinationId(id);
                     RemoveConnectionId(id);
                     GameDebug.Log("lost connection id: " + id);
                 }
                 
             } else if (!TransportPings.firstPingTimes.ContainsKey(destinationId)) {
                TransportPings.firstPingTimes[destinationId] = Time.time + initialPingDelay;
             }
         }
     }
     public void SendTransportPong(ReceivedMessage receivedMessage) {
         var connection = new GDNConnection() {
             source = receivedMessage.properties.d,
             destination = receivedMessage.properties.s,
             port = receivedMessage.properties.p
         };

         var id = GetConnectionId(connection);
         if (id == -1) {
             //GameDebug.Log("discarded message unknown source " 
             //              + receivedMessage.properties.source);
             return;
         }
         ProducerSend(id, VirtualMsgType.Pong, new byte[0], receivedMessage.properties.i,
             producer1.Latency,consumer1.Latency,nodeId);
     }
     
     private IEnumerator RepeatDummyMsg() {
         for(;;){
             for (int i = 0; i < dummyTrafficQuantity; i++) {
                 SendDummy();
             }
             yield return new WaitForSeconds(dummyFrequency);
             
         }
     }
     
     public void SendDummy() {
         if(gdnConnections.Count==0){
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
     #endregion
     
     //these these are methods that were being called asynchronously
     // and access concurrent collections
     // putting a command queue that runs on the mand thread 
     // means only the main thread trys to update the collections.
     private enum QueueCommand {
         ConnectClient, //(ReceivedMessage receivedMessage)
         DisconnectFromClient, //(receivedMessage);
         DisconnectFromServer, //(receivedMessage);
         SendTransportPong, //(receivedMessage);
         ReceiveTransportPong, //(receivedMessage);
         ReceiveInternal, //(receivedMessage);
         ReceiveDummy, //(receivedMessage);
         Connect, //()
         SendTransportPing,//()
     }

     private class Command {
         public QueueCommand command;
         public ReceivedMessage receivedMessage;
     }
     
     private  ConcurrentQueue<Command> queue = new ConcurrentQueue<Command>();

     private  void AddCommand(Command command) {
         queue.Enqueue(command);
     }

     //execute commands in queue
     //no guarantee that all commands will be executed in a single call
     public  void ExecuteCommands() {
         Command command;
         while (queue.TryDequeue(out command)) {
             Execute(command);
         }
     }

     private void Execute(Command command) {
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
