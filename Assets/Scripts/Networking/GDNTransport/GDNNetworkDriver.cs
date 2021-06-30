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

//need to finish Connection distinary at bottom
// need event queue
public class GDNNetworkDriver : MonoBehaviour {
    
    public static bool overrideIsServer = false;
    public static bool overrideIsServerValue = false;
    
    public GDNData baseGDNData;
    public string gameName = "FPSGameXX";
    public bool isServer = false;

    public ListStream listStream;
    public WebSocket consumer1;
    public WebSocket producer1;
    public string consumerName = "Server";
    public string serverName;
    public int ttl = 1;
    public double discardMinutes = -2;// discard messages more than X minutes old should be a negative number 
    public int port;
    
    //Connection handling
    public int maxConnectionError = 20;
    public bool isWaitingDebug = false;
    private bool _isWaiting;
    
    public bool isWaiting {
        get => _isWaiting;
        set {
            _isWaiting = value;
            if (isWaitingDebug) {
                Debug.Log("isWaiting: " + value);
            }
        }
    }

    [SerializeField] 
    private int _currentNetworkErrors;
    
    public int currentNetworkErrors {
        get => _currentNetworkErrors;
        set {
            _currentNetworkErrors = value;
            if (isWaitingDebug) {
                Debug.Log("NetworkError: " + value);
                pauseNetworkErrorUntil = Time.time + pauseNetworkError;
            }
        }
    }

    public float pauseNetworkError = 1f;
    public float pauseNetworkErrorUntil = 0f;
    public bool isNetworkErrorPause = false;

    //itnit progross
    public bool clearAllBacklogsDone = false;
    public bool setTTLDone = false;
    public bool streamListDone = false;
    public bool serverInStreamExists = false;
    public bool serverOutStreamExists = false;
    public bool producerExists = false;
    public bool consumerExists = false;
    public bool sendConnect = true;
    public bool setupComplete = false;


    protected string serverInStreamName;
    protected string serverOutStreamName;
    protected string consumerStreamName;
    protected string producerStreamName;

    
    public void Awake() {
        BestHTTP.HTTPManager.Setup();
       
        
        var configGDNjson = Resources.Load<TextAsset>("configGDN");
        var defaultConfig = RWConfig.ReadConfig("ConfigGDN.json", configGDNjson);
        RWConfig.WriteConfig("ConfigGDN.json", defaultConfig);
        baseGDNData = defaultConfig.gdnData;
        gameName = defaultConfig.gameName;
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
    }
    
    
    void Update() {
        SetupLoopBody();
    }
    
    public void setRandomClientName() {
        consumerName = "C" + (10000000 + Random.Range(1, 89999999)).ToString();
    }

    public void SetupLoopBody() {
        
        
        if (isNetworkErrorPause) return;
        if (pauseNetworkErrorUntil > Time.time) return;
        if (currentNetworkErrors >= maxConnectionError) {
            isNetworkErrorPause = true;
            //Debug.LogError("Network problem Game Paused");
            //AddText("Sorry Network there are problems try restarting\n");
            return;
        }

        if (isWaiting) return;
/*
        if (!clearAllBacklogsDone) {
            ClearAllBacklogs();
            return;
        }
*/
        if (!setTTLDone) {
            SetTTL(ttl);
            return;
        }
        
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
            Connect();
            sendConnect = true;
        }

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
                Debug.Log("ListStream succeed " );
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
        StartCoroutine(MacrometaAPI.Producer(baseGDNData, streamName, SetProducer));
    }

    public void SetProducer(WebSocket ws, string debug = "") {
        producer1 = ws;

        producer1.OnOpen += (o) => {
            isWaiting = false;
            producerExists = true;
            GameDebug.Log("Open " + debug);
        };
/*
        producer1.OnMessage += (sender, e) => {
            //AddText( "SendMessage: "+debug+":" + e +"\n");
            //Debug.Log("SendMessage: "+debug+" : " + e );
        };
        */
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
        };
        producer1.Open();
    }
    
    public void ProducerSend(int id, VirtualMsgType msgType, byte[] payload) {
        if (!gdnConnections.ContainsKey(id)) {
            GameDebug.Log("ProducerSend bad id:" + id);
        }
        var properties = new MessageProperties() {
            msgType = msgType,
            port = gdnConnections[id].port,
            desitination = gdnConnections[id].destination,
            source =gdnConnections[id].source,
            payloadByteSize = payload.Length
            
        };
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
    }
    
    public void CreateConsumer(string streamName, string consumerName) {
        isWaiting = true;
        StartCoroutine(MacrometaAPI.Consumer(baseGDNData, streamName, consumerName, SetConsumer));
    }

     public void SetConsumer(WebSocket ws, string debug = "") {

        consumer1 = ws;
        consumer1.OnOpen += (o) => {
            
            GameDebug.Log("Open " + debug);
            consumerExists = true;
            isWaiting = false;
        };

        consumer1.OnMessage += (sender, e) => {
            var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);
            //Debug.Log("low time: " + DateTime.Now.AddMinutes(discardMinutes));
            //Debug.Log("published: " + DateTime.Parse(receivedMessage.publishTime));
            //GameDebug.Log("recieved msg: " + e);
            if (receivedMessage.properties != null && 
                receivedMessage.properties.desitination == consumerName 
                && DateTime.Now.AddMinutes(discardMinutes) < DateTime.Parse(receivedMessage.publishTime)  
                ) {
                
                if(receivedMessage.properties.msgType == VirtualMsgType.Data) {
                    //GameDebug.Log("Consumer1.OnMessage Data");
                    var connection = new GDNConnection() {
                        source = receivedMessage.properties.desitination,
                        destination = receivedMessage.properties.source,
                        port = receivedMessage.properties.port 
                    };
                    
                    var id = AddOrGetConnectionId(connection);
                    //GameDebug.Log("Consumer1.OnMessage ID: "+ id);
                    
                    var driverTransportEvent = new DriverTransportEvent() {
                        connectionId = id,
                        data = Convert.FromBase64String(receivedMessage.payload),
                        dataSize = Convert.FromBase64String(receivedMessage.payload).Length,
                        type = DriverTransportEvent.Type.Data
                    };
                    PushEventQueue(driverTransportEvent);   
                    LogFrequency.Incr("consumer1.OnMessage", receivedMessage.payload.Length,
                        receivedMessage.properties.payloadByteSize,driverTransportEvent.data.Length );
                        
                    
                } else if (receivedMessage.properties.msgType == VirtualMsgType.Connect) {
                    GameDebug.Log("Consumer1.OnMessage Connect: " + receivedMessage.properties.source);
                    if (isServer) {
                        ConnectClient(receivedMessage);
                    }
                } else if (receivedMessage.properties.msgType == VirtualMsgType.Disconnect) {
                    GameDebug.Log("Consumer1.OnMessage Disonnect: " + receivedMessage.properties.source);
                    if (isServer) {
                        ConnectClient(receivedMessage);
                    }
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

     #region ChatServer
     
     public void ConnectClient(ReceivedMessage receivedMessage) {
         var connection = new GDNConnection() {
             source = receivedMessage.properties.desitination,
             destination = receivedMessage.properties.source,
             port = receivedMessage.properties.port 
         };

         var id = AddOrGetConnectionId(connection);
         var driverTransportEvent = new DriverTransportEvent() {
             connectionId = id,
             data = new byte[0],
             dataSize = 0,
             type = DriverTransportEvent.Type.Connect
         };
         PushEventQueue(driverTransportEvent);
         GameDebug.Log("ConnectClient id: " + id + " Q: " + driverTransportEvents.Count);
     }
     
     public void DisconnectClient(ReceivedMessage receivedMessage) {
         var connection = new GDNConnection() {
             source = receivedMessage.properties.desitination,
             destination = receivedMessage.properties.source,
             port = receivedMessage.properties.port 
         };

         var id = AddOrGetConnectionId(connection);
         RemoveConnectionId(id);
         var driverTransportEvent = new DriverTransportEvent() {
             connectionId = id,
             data = new byte[0],
             dataSize = 0,
             type = DriverTransportEvent.Type.Disconnect
         };
         
         PushEventQueue(driverTransportEvent);
         GameDebug.Log("DisconnectClient id: " + id + " Q: " + driverTransportEvents.Count);
     }
     
     
     #endregion

     
     #region ChatClient
     
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
     }
     #endregion
     
     
     //public int AddConnection(GDNConnection)
     
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
    
     public void RemoveConnectionId(int connectionID) {
         if (gdnConnections.ContainsKey(connectionID)) {
             gdnConnections.Remove(connectionID);
         }
     }
     
     
     public struct DriverTransportEvent {
         public enum Type
         {
             Data,
             Connect,
             Disconnect,
             empty
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
                 type = DriverTransportEvent.Type.empty,
                 connectionId = -1,
                 data = new byte[0],
                 dataSize = 0
             };
         }
         return driverTransportEvents.Dequeue();
     }
     
     public Queue<DriverTransportEvent> driverTransportEvents = new Queue<DriverTransportEvent>();
}
