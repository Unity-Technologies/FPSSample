using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static Macrometa.MacrometaAPI;

namespace Macrometa.Lobby {
    
    /// <summary>
    /// Key value methods
    /// client browser mode check Lobby list 
    /// </summary>
    public class GdnKvLobbyDriver {
        private MonoBehaviour _monoBehaviour;
        private GDNData _gdnData;
        private GDNErrorhandler _gdnErrorHandler;
        public ListKVCollection listKVCollection;
        public ListKVValue listKVValues ;
        public bool kvCollectionListDone = false;
        public bool lobbiesKVCollectionExists;
        public string lobbiesKVCollectionName = "FPSGames_Lobbies";
        public bool kvValueListDone;
        public bool putKVValueDone;
        public LobbyList lobbyList = new LobbyList();
        
        /// <summary>
        /// passing in a monobehaviour to be able use StartCoroutine
        /// happens because of automatic refactoring
        /// probably can hand cleaned
        /// </summary>
        /// <param name="gdnData"></param>
        /// <param name="gdnErrorhandler"></param>
        /// <param name="monoBehaviour"></param>
        public GdnKvLobbyDriver(GDNNetworkDriver gdnNetworkDriver) {

            _gdnData = gdnNetworkDriver.baseGDNData;
            _monoBehaviour = gdnNetworkDriver;
            _gdnErrorHandler = gdnNetworkDriver.gdnErrorHandler;
        }

        public void CreateLobbiesKVCollection() {

            lobbiesKVCollectionExists = listKVCollection.result.Any
                (item => item.name == lobbiesKVCollectionName);
            if (!lobbiesKVCollectionExists) {
                _gdnErrorHandler.isWaiting = true;
                ;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monoBehaviour.StartCoroutine(CreateKVCollection(_gdnData, lobbiesKVCollectionName,
                    CreateKVCollectionCallback));
            }
        }

        public void CreateKVCollectionCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("CreateServerInStream : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                kvCollectionListDone = false;
            }
            else {
                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create KV Collection failed:" + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    kvCollectionListDone = false;
                }
                else {
                    GameDebug.Log("Create KV Collection  ");
                    lobbiesKVCollectionExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void GetListKVColecions() {
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(ListKVCollections(_gdnData, ListKVCollectionsCallback));
        }

        public void ListKVCollectionsCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("List KV Collections: " + www.error);
            }
            else {

                //overwrite does not assign toplevel fields
                //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
                listKVCollection = JsonUtility.FromJson<ListKVCollection>(www.downloadHandler.text);
                if (listKVCollection.error == true) {
                    GameDebug.Log("List KV Collection failed:" + listKVCollection.code);
                    //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                    _gdnErrorHandler.currentNetworkErrors++;
                }
                else {
                    kvCollectionListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void GetListKVValues() {
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(GetKVValues(_gdnData, lobbiesKVCollectionName,
                ListKVValuesCallback));
        }

        public void ListKVValuesCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("List KVvalues: " + www.error);
            }
            else {

                //overwrite does not assign toplevel fields
                //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
                listKVValues = JsonUtility.FromJson<ListKVValue>(www.downloadHandler.text);
                if (listKVValues.error == true) {
                    GameDebug.Log("List KV values failed:" + listKVValues.code);
                    //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                    _gdnErrorHandler.currentNetworkErrors++;
                }
                else {
                    
                    kvValueListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                    var newLobbyList = new List<LobbyValue>();
                    foreach (KVValue kvv in listKVValues.result) {
                        newLobbyList.Add(LobbyValue.FromKVValue(kvv));
                    }
                    LobbyValue.UpdateFrom(lobbyList.lobbies,newLobbyList);
                    lobbyList.isDirty = true;
                    //GameDebug.Log("List KV values succeed" );
                }
            }
        }

        public void PutKVValue(LobbyRecord kvRecord) {
            string data = "[" +JsonUtility.ToJson(kvRecord)+"]"; // JsonUtility can not handle bare values
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(MacrometaAPI.PutKVValue(_gdnData, lobbiesKVCollectionName,
                data, PutKVValueCallback));
        }

        public void PutKVValueCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            putKVValueDone = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("Put KV value: " + www.error);
            }
            else {
                //GameDebug.Log("put KV value succeed ");
                putKVValueDone = true;
                _gdnErrorHandler.currentNetworkErrors = 0;
            }
        }
    }

}