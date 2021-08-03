using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Macrometa {
    
    public class GameRecord {
        public string _key; //base string name
        public  string value;
        public long expireAt; //unix timestamp

        static public long UnixTSNow(long offset) {
            return (long)(DateTime.UtcNow.
                Subtract(new DateTime(1970, 1, 1))).TotalSeconds+ offset;
        }
        
        public enum Status {
            init,
            waiting,
            playing
        }

        public static GameRecord GetInit(string clientID, string baseStreamName, long ttl) {
            var val = new GameRecordValue() {
                clientId = clientID,
                status = Status.init.ToString()
            };

            return new GameRecord() {
                _key = baseStreamName,
                value = JsonUtility.ToJson(val),
                expireAt = UnixTSNow(ttl)
            };
        }
    }

    [Serializable]
    public class GameRecordValue {
        public string streamName; // only used locally
        public string clientId;
        public string gameMode;
        public string mapName;
        public int maxPlayers;
        public int currPlayers;
        public string status; //init, waiting ( to start), playing
        public long statusChangeTime; // unixTimeStamp --  Gamestart or GameEnd
        public float ping; // only used locally not use in kv db
        
        public static GameRecordValue FromKVValue(KVValue kvValue) {
            GameRecordValue result = JsonUtility.FromJson<GameRecordValue>(kvValue.value);
            result.clientId = kvValue._key;
            return result;
        }

        public static void UpdateFrom(List<GameRecordValue> currRecords,List<GameRecordValue> newRecords) {
           foreach(var grv in newRecords) {
               var oldRecord = currRecords.SingleOrDefault();
               if (oldRecord != null) {
                   grv.ping = oldRecord.ping;
               }
           }
           currRecords.Clear();
           currRecords.AddRange(newRecords);
           
        }
    }
    public class GameList {
        public List<GameRecordValue> games = new List<GameRecordValue>();
        public bool isDirty = false;
    }
    
    
    /// <summary>
    /// Key value methods
    /// client browser mode check game list 
    /// </summary>
    public class GDNKVDriver {
        private MonoBehaviour _monoBehaviour;
        private GDNData _gdnData;
        private GDNErrorhandler _gdnErrorHandler;
        public ListKVCollection listKVCollection;
        public ListKVValue listKVValues ;
        public bool kvCollectionListDone = false;
        public bool gamesKVCollectionExists;
        public string gamesKVCollectionName = "FPSGames_collection";
        public bool kvValueListDone;
        public bool putKVValueDone;
        public GameList gameList = new GameList();
        
       
   
        
        
        /// <summary>
        /// passing in a monobehaviour to be able use StartCoroutine
        /// happens because of automatic refactoring
        /// probably can hand cleaned
        /// </summary>
        /// <param name="gdnData"></param>
        /// <param name="gdnErrorhandler"></param>
        /// <param name="monoBehaviour"></param>
        public GDNKVDriver(GDNNetworkDriver gdnNetworkDriver) {

            _gdnData = gdnNetworkDriver.baseGDNData;
            _monoBehaviour = gdnNetworkDriver;
            _gdnErrorHandler = gdnNetworkDriver.gdnErrorHandler;
        }

        public void CreateGamesKVCollection() {

            gamesKVCollectionExists = listKVCollection.result.Any
                (item => item.name == gamesKVCollectionName);
            if (!gamesKVCollectionExists) {
                _gdnErrorHandler.isWaiting = true;
                ;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monoBehaviour.StartCoroutine(MacrometaAPI.CreateKVCollection(_gdnData, gamesKVCollectionName,
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
                    gamesKVCollectionExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void GetListKVColecions() {
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(MacrometaAPI.ListKVCollections(_gdnData, ListKVCollectionsCallback));
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
                    GameDebug.Log("List KV Collection succeed ");
                    kvCollectionListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void GetListKVValues() {
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(MacrometaAPI.GetKVValues(_gdnData, gamesKVCollectionName,
                ListKVValuesCallback));
        }

        public void ListKVValuesCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("List KV Collections: " + www.error);
            }
            else {

                //overwrite does not assign toplevel fields
                //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
                listKVValues = JsonUtility.FromJson<ListKVValue>(www.downloadHandler.text);
                
                   
                
                if (listKVValues.error == true) {
                    GameDebug.Log("List KV Collection failed:" + listKVValues.code);
                    //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                    _gdnErrorHandler.currentNetworkErrors++;
                }
                else {
                    GameDebug.Log("List KV Collection succeed ");
                    kvValueListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                    var newGamesList = new List<GameRecordValue>();
                    foreach (KVValue kvv in listKVValues.result) {
                        newGamesList.Add(GameRecordValue.FromKVValue(kvv));
                    }
                    GameRecordValue.UpdateFrom(gameList.games,newGamesList);
                    gameList.isDirty = true;
                }
            }
        }

        public void PutKVValue(GameRecord kvRecord) {
            string data = "[" +JsonUtility.ToJson(kvRecord)+"]"; // JsonUtility can not handle bare values
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(MacrometaAPI.PutKVValue(_gdnData, gamesKVCollectionName,
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
                GameDebug.Log("List KV Collection succeed ");
                putKVValueDone = true;
                _gdnErrorHandler.currentNetworkErrors = 0;
            }
        }
    }

}

