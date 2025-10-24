using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BestHTTP.WebSocket;
using Macrometa.Lobby;
using UnityEngine;

using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Macrometa {

    [Serializable]
    public class GDNStreamStatsDriver {
        private MonoBehaviour _monobehaviour;
        public ListStream listStream;
        public WebSocket consumer1;
        public WebSocket producer1;
        public Region region;
        public string consumerName = "Server";
        public string serverName;
        public bool regionIsDone;
        public bool streamListDone = false;
        public bool serverInStreamExists = false;
        public bool serverOutStreamExists = false;
        public bool producerExists = false;
        public bool consumerExists = false;
       
        public bool setupComplete = false;
        public bool pingStarted = false;
        public InGameStats inGameStats;

        
        private ConcurrentQueue< GDNStreamStatsDriver.Command> _commandQueue = new ConcurrentQueue< GDNStreamStatsDriver.Command>();
       
        private GDNErrorhandler _gdnErrorHandler;
        private GDNData _gdnData;

        

        public GDNStreamStatsDriver(TestPlayStatsDriver gdnNetworkDriver) {
            _monobehaviour = gdnNetworkDriver;
            _gdnErrorHandler = gdnNetworkDriver.gdnErrorHandler;
            _gdnData = gdnNetworkDriver.baseGDNData;
           
            
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

        public void CreateServerInStream(string streamName) {
            var topic = _gdnData.region + "s." + streamName;
            serverInStreamExists = listStream.result.Any(item => item.topic ==  topic);
            if (!serverInStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                ;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, streamName,
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
        
        public void CreateServerOutStream(string streamName) {
            var topic = _gdnData.region + "s." + streamName;
            GameDebug.Log("CreateServerOutStream topic:"+topic);
            serverOutStreamExists = listStream.result.Any(item => item.topic == topic);
            if (!serverOutStreamExists) {
                _gdnErrorHandler.isWaiting = true;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monobehaviour.StartCoroutine(MacrometaAPI.CreateStream(_gdnData, streamName,
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
        

        public void CreateProducer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
            _monobehaviour.StartCoroutine(MacrometaAPI.Producer(_gdnData, streamName, SetProducer, _gdnErrorHandler));
        }

        public void SetProducer(WebSocket ws, string debug = "") {
            producer1 = ws;
           
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

       

        public void ProducerSend( string msg) {
            var message = new SendMessage() {
                
                payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(msg))
            };
            string msgJSON = JsonUtility.ToJson(message);
            
            producer1.Send(msgJSON);
            
        }
        
        public void CreateConsumer(string streamName) {
            _gdnErrorHandler.isWaiting = true;
          
            _monobehaviour.StartCoroutine(MacrometaAPI.Consumer(_gdnData, streamName, consumerName, SetConsumer,_gdnErrorHandler));
        }

        public void SetConsumer(WebSocket ws, string debug = "") {

            consumer1 = ws;
            consumer1.CloseAfterNoMessage = TimeSpan.FromSeconds(10);
            consumer1.OnOpen += (o) => {
                GameDebug.Log("Stream driver Open " + debug );
                consumerExists = true;
                _gdnErrorHandler.isWaiting = false;
            };

            consumer1.OnMessage += (sender, e) => {
                //GameDebug.Log("Consumer1.OnMessage A " );
                var receivedMessage = JsonUtility.FromJson<ReceivedMessage>(e);
                //GameDebug.Log("Consumer1.OnMessage type: "+ receivedMessage.properties.t );
                //Debug.Log("low time: " + DateTime.Now.AddMinutes(discardMinutes));
                //Debug.Log("published: " + DateTime.Parse(receivedMessage.publishTime));
                var json  =Encoding.UTF8.GetString(Convert.FromBase64String(receivedMessage.payload));
                inGameStats = JsonUtility.FromJson<InGameStats>(json);
                Debug.Log("ingameStats name: " + inGameStats.gameName);
                inGameStats.Convert();
                
                GameDebug.Log("recieved msg: " + json);
               

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

        
        public void ReceiveInternal(ReceivedMessage receivedMessage) {
            //process internal message
            //for update ping stats for client stream
        }

        public void ReceiveDummy(ReceivedMessage receivedMessage) {
            //store dummy stats somewhere
        }

        private void AddCommand( GDNStreamStatsDriver.Command command) {
            _commandQueue.Enqueue(command);
        }

        public void ExecuteCommands() {
            GDNStreamStatsDriver.Command command;
            while ( _commandQueue.TryDequeue(out command)) {
                Execute(command);
            }
        }

        private void Execute( GDNStreamStatsDriver.Command command) {
            switch (command.command) {
                case QueueCommand.Connect:
                   
                    break;
                
                default:
                    break;
            }
        }

    }

}