using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;

using BestHTTP.WebSocket;
using Object = System.Object;

namespace Macrometa {


    [Serializable]
    public struct GDNData {
        public string apiKey;
        public string federationURL;
        public string tenant;
        public string fabric;
        public bool isGlobal;

        public string region => isGlobal ? "c8global" : "c8local";

        public string requestURL => "api-" + federationURL;

        
        ///_fabric/{fabric}/_api/streams/clearbacklog
        public string ClearAllBacklogs() {
            return "https://" + requestURL + "/_fabric/"+fabric+ "/_api/streams/clearbacklog";
        }
        
        public string CreateStreamURL(string streamName) {
            return "https://" + requestURL + "/_fabric/"+fabric+ "/_api/streams/" + streamName + "?global=" + isGlobal;
        }

        public string ListStreamsURL() {
            return "https://" + requestURL + "/_fabric/"+fabric+ "/_api/streams?global=" + isGlobal;
        }
        
        public string GetTTLURL() {
            return "https://" + requestURL + "/_fabric/"+fabric+ "/_api/streams/ttl";
        }
        
        public string SetTTLURL(int ttl) {
            return "https://" + requestURL + "/_fabric/"+fabric+ "/_api/streams/ttl/"+ttl;
        }
        
        //const consumerUrl = `wss://${requestURL}/_ws/ws/v2/consumer/persistent/${tenant}/${region}._system/${region}s.${STREAM_NAME}/${CONSUMER_NAME}`;
        public string ConsumerURL(string streamName, string consumerName ) {
            return "wss://" + requestURL + "/_ws/ws/v2/consumer/persistent/" + tenant + "/" + region+"."+
                fabric+ "/" + region+"s."+streamName+"/"+consumerName ;
        }
        
        //const producerUrl = `wss://${requestURL}/_ws/ws/v2/producer/persistent/${tenant}/${region}._system/${region}s.${STREAM_NAME}`;
        public string ProducerURL(string streamName) {
            return "wss://" + requestURL + "/_ws/ws/v2/producer/persistent/" + tenant + "/" + region+"."+
                   fabric+ "/" + region+"s."+streamName ;
        }
        
        public string GetOTPURL() {
            return "https://" + requestURL + "/apid/otp";
        }

        public void Authorize(UnityWebRequest www) {
            www.SetRequestHeader("Authorization", "apikey " + apiKey);
        }

        public string StreamListName(string streamName) {
            return region + "s." + streamName;
        }
        
        public string CreateKVURL(string name, bool isExpire) {
            return "https://" + requestURL + "/_fabric/" + fabric + "/_api/kv/" + name + "?expiration=" + isExpire;
        }
        
        
        public string ListKVCollectionsURL() {
            return "https://" + requestURL + "/_fabric/"+fabric+ "/kv";
        }
        
      
        public string GetKVValuesURL(string name, int offset = 0, int limit = 100) {
            return "https://" + requestURL + "/_fabric/" + fabric + "/_api/kv/"
                   + name + "/values?offset=" + offset + "&limit="+ limit ;
        }
        
        public string PutKVValueURL(string name) {
            return "https://" + requestURL + "/_fabric/" + fabric + "/kv/"
                   + name + "/value";
        }
        
    }

    [Serializable]
    public struct OTPResult {
        public string otp;
    }

    [Serializable]
    public struct ListStream{
        public bool error;
        public int code;
        public List<Stream> result;
    }
    
    // a single stream are returned by ListStreamURL
    [Serializable]
    public struct Stream {
        public string topic; //moved topic to first field for better default inspector
        public string _key;
        public string _id;
        public string _rev;
        public string db;
        public bool local;
        public string tenant;
        public int type;
    }

    [Serializable]
    public struct ListKVCollection{
        public bool error;
        public int code;
        public List<KVCollection> result;
    }

    [Serializable]
    public struct KVCollection {
        public string name;
        public bool expiration;
    }
    
    [Serializable]
    public struct ListKVValue{
        public bool error;
        public int code;
        public List<KVValue> result;
    }

    [Serializable]
    public class KVValue {
        public string _key;
        public string value;
        public int expireAt; // unix timestamp
    }
    
    
    
    [Serializable]
    public struct BaseHtttpReply {
        public bool error;
        public int code;
        public Object result;
    }
    
    [Serializable]
    public enum VirtualMsgType {
        Connect,
        Data,
        Disconnect,
        Ping,
        Pong,
        Internal, //used to send stats or other internal driver information
        Dummy     //used to simulate traffic
    }
    
    [Serializable]
    public struct SendMessage {
        public MessageProperties properties;
        public string payload;
    }
    
    [Serializable]
    public struct StatsSendMessage {
        public StatsMessageProperties properties;
        public string payload;
    }
    
    [Serializable]
    public struct ReceivedMessage {
        public string messageId;
        public string payload; 
        public MessageProperties properties; 
        public string publishTime;
        public int redeliveryCount;
    }

    //to reduce json size these fields are 1 character
    [Serializable]
    public class MessageProperties {
        public string s; //source
        public string d;//desitination
        public int p;//port  used for connection ID
        public VirtualMsgType t;//msgType
        public int i;//pingId
        public int z;//payloadByteSize
        // these are only used in TranportPing/Pong
        public int r;// last ping time consumer i.e. remote ping time
        public int o;// last ping time producer i.e. remote ping time
        public string n;// node i.e. remote ping time
    }
    
    [Serializable]
    public class StatsMessageProperties {
        public string type = "NetworkStats";
        public string version = "0.1";
        public string app = "NetworkTester";
        public PingStatsGroup.NetworkStatsData NetworkStatsData;
    }
    
    [Serializable]
    public struct AckMessage {
        public string messageId;
    }
    
    public class MacrometaAPI {
        
        public static string Base64Encode(string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        
        public static string Base64Decode(string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        public static IEnumerator CreateStream(GDNData gdnData, string streamName,
            Action<UnityWebRequest> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            GameDebug.Log("stream url: "+ gdnData.CreateStreamURL(streamName));
            UnityWebRequest www = UnityWebRequest.Post(gdnData.CreateStreamURL(streamName), formData);
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            if (callback != null)
                callback(www);
        }

        public static IEnumerator ClearAllBacklogs(GDNData gdnData, Action<UnityWebRequest> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            UnityWebRequest www = UnityWebRequest.Post(gdnData.ClearAllBacklogs(), formData);
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            if (callback != null)
                callback(www);
        }

        public static IEnumerator ListStreams(GDNData gdnData, Action<UnityWebRequest> callback) {
            UnityWebRequest www = UnityWebRequest.Get(gdnData.ListStreamsURL());
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            if (callback != null)
                callback(www);
        }

        // Testing Callback to act on our response data
        public static void UnityWebRequestCallback(UnityWebRequest www) {
            if (www.isHttpError || www.isNetworkError) {
                Debug.Log(www.error);
            }
            else {
                Debug.Log("text : " + www.downloadHandler.text + "\n");
                Debug.Log("size: " + www.downloadedBytes);
            }
        }

        // /_fabric/{fabric}/_api/streams/ttl
        public static IEnumerator GetTTL(GDNData gdnData, Action<UnityWebRequest> callback) {
            UnityWebRequest www = UnityWebRequest.Get(gdnData.GetTTLURL());
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            if (callback != null)
                callback(www);
        }

        // /_fabric/{fabric}/_api/streams/ttl/{ttl}
        public static IEnumerator SetTTL(GDNData gdnData, int ttl, Action<UnityWebRequest> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            UnityWebRequest www = UnityWebRequest.Post(gdnData.SetTTLURL(ttl), formData);
            Debug.Log("SetTTLURL: "+gdnData.SetTTLURL(ttl));
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
                
            if (callback != null)
                callback(www);
        }
        
        public static IEnumerator Consumer(GDNData gdnData, string streamName, string consumerName,
            Action<WebSocket, string> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            OTPResult otp = new OTPResult();
            using (UnityWebRequest www = UnityWebRequest.Post(gdnData.GetOTPURL(), formData)) {
                www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
                yield return www.SendWebRequest();
                
                otp = JsonUtility.FromJson<OTPResult>(www.downloadHandler.text);
            }

            callback(new WebSocket(new Uri(gdnData.ConsumerURL(streamName, consumerName) + "?otp=" + otp.otp)),
                "LIVE consumer "+ streamName);
            //callback(new WebSocket(gdnData.ConsumerURLDebug(streamName, consumerName) ), "LIVE");
        }
        
        public static IEnumerator Producer(GDNData gdnData, string streamName, Action<WebSocket, string> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            OTPResult otp = new OTPResult();
            using (UnityWebRequest www = UnityWebRequest.Post(gdnData.GetOTPURL(), formData)) {
                www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
                yield return www.SendWebRequest();
                otp = JsonUtility.FromJson<OTPResult>(www.downloadHandler.text);
            }
            callback(new WebSocket(new Uri(gdnData.ProducerURL(streamName) + "?otp=" + otp.otp)),
                "LIVE producer "+ streamName);
        }
        
        public static IEnumerator ProducerMulti(GDNData gdnData, string streamName, int id, Action<WebSocket, string, int> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            OTPResult otp = new OTPResult();
            using (UnityWebRequest www = UnityWebRequest.Post(gdnData.GetOTPURL(), formData)) {
                www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
                yield return www.SendWebRequest();
                otp = JsonUtility.FromJson<OTPResult>(www.downloadHandler.text);
            }
            callback(new WebSocket(new Uri(gdnData.ProducerURL(streamName) + "?otp=" + otp.otp)),
                "LIVE producer "+ streamName, id);
        }

        #region KVCollection

        public static IEnumerator ListKVCollections(GDNData gdnData, Action<UnityWebRequest> callback) {
            
            UnityWebRequest www = UnityWebRequest.Get(gdnData.ListKVCollectionsURL());
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            if (callback != null)
                callback(www);
        }

      
        public static IEnumerator CreateKVCollection(GDNData gdnData, string collectionName,
            Action<UnityWebRequest> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            GameDebug.Log("KV url: "+ gdnData.CreateStreamURL(collectionName));
            UnityWebRequest www = UnityWebRequest.Post(gdnData.CreateKVURL(collectionName,true), formData);
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            
            if (callback != null)
                callback(www);
        }
        
        public static IEnumerator GetKVValues(GDNData gdnData, string collectionName,
            Action<UnityWebRequest> callback) {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            var url = gdnData.GetKVValuesURL(collectionName);
            //GameDebug.Log("KV url: "+ url);
            UnityWebRequest www = UnityWebRequest.Post(url, formData);
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            
            if (callback != null)
                callback(www);
        }
        public static IEnumerator PutKVValue(GDNData gdnData, string collectionName, string kvRecord,
            Action<UnityWebRequest> callback) {
            byte[] putData = System.Text.Encoding.UTF8.GetBytes(kvRecord);
            var url = gdnData.PutKVValueURL(collectionName);
           // GameDebug.Log("put URL: " + url);
            UnityWebRequest www = UnityWebRequest.Put(url, putData);
            www.SetRequestHeader("Authorization", "apikey " + gdnData.apiKey);
            yield return www.SendWebRequest();
            
            if (callback != null)
                callback(www);
        }
        
       
        
        
        #endregion
    }


}


