using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using static Macrometa.MacrometaAPI;

namespace Macrometa.Lobby {
    
    /// <summary>
    /// Key value methods
    /// client browser mode check Lobby list 
    /// </summary>
    [Serializable]
    public class GdnDocumentLobbyDriver {
        private MonoBehaviour _monoBehaviour;
        private GDNData _gdnData;
        private GDNErrorhandler _gdnErrorHandler;
        
        public ListKVValue listKVValues ;
        public ListCollection listCollection;
        public ListIndexes listIndexes;
        
        public bool collectionListDone = false;
        public bool lobbiesCollectionExists;
        public string lobbiesCollectionName = "FPSGames_Lobbies_Documents";
        public bool indexesListDone = false;
        public List<bool> indexesExist = new List<bool>();
        public List<IndexParams> indexParamsList = new List<IndexParams>();
        public bool indexTTLExist;
        public IndexParams ttlIndexParams;

        public bool maxSerialIsDone;
        public MaxSerialResult maxSerialResult;
        public int errorSerialIncr = 0;
        
        public bool lobbyListIsDone;
        public LobbyListResult lobbyListResult;
        
        
        public bool lobbyIsMade = false;
        public bool postLobbyStuff;
        public string lobbyKey;
        
        public LobbyList lobbyList = new LobbyList();
        
        /// <summary>
        /// passing in a monobehaviour to be able use StartCoroutine
        /// happens because of automatic refactoring
        /// probably can hand cleaned
        /// </summary>
        /// <param name="gdnData"></param>
        /// <param name="gdnErrorhandler"></param>
        /// <param name="monoBehaviour"></param>
        public GdnDocumentLobbyDriver(GDNNetworkDriver gdnNetworkDriver) {
            _gdnData = gdnNetworkDriver.baseGDNData;
            _monoBehaviour = gdnNetworkDriver;
            _gdnErrorHandler = gdnNetworkDriver.gdnErrorHandler;

            indexParamsList.Add(new IndexParams() {
                fields = new List<string>() {"baseName", "serialNumber", "gameMaster"},
                unique = true,
                sparse = true,
                type = "persistent"
            });
            indexesExist.Add(false);
            indexParamsList.Add(new IndexParams() {
                fields = new List<string>() {"baseName", "serialNumber", "activeGame"},
                unique = true,
                sparse = true,
                type = "persistent"
            });
            indexesExist.Add(false);
            indexParamsList.Add(new IndexParams() {
                fields = new List<string>() {"baseName", "serialNumber", "lobby"},
                unique = true,
                sparse = true,
                type = "persistent"
            });
            indexesExist.Add(false);
            indexParamsList.Add(new IndexParams() {
                fields = new List<string>() {"baseName", "serialNumber"},
                unique = false,
                type = "persistent"
            });
            indexesExist.Add(false);

            ttlIndexParams = new IndexParams() {
                expireAfter = 30,
                fields = new List<string>() {"lastUpdate"},
                type = "ttl"
            };

        }

        
        #region Query
        
        public void PostMaxSerialQuery(string baseName) {
            _gdnErrorHandler.isWaiting = true;
            var data = $"{{\"bindVars\" : {{\"baseName\" : \"{baseName}\",\"@collection\" : \"{lobbiesCollectionName}\" }},";
            data +=
                "\"query\": \"FOR c IN  @@collection FILTER c.baseName == @baseName SORT c.serialNumber desc LIMIT 1 RETURN c.serialNumber\" }";
            _monoBehaviour.StartCoroutine(PostQuery(_gdnData, data, PostMaxSerialQueryCallback));
        }

        public void PostMaxSerialQueryCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("Post Max Serial Query : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                maxSerialIsDone = false;
            }
            else {
                maxSerialResult = JsonUtility.FromJson<MaxSerialResult>(www.downloadHandler.text);
                if (maxSerialResult.error == true) {
                    GameDebug.Log("Post Max Serial Query  failed:" + maxSerialResult.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    maxSerialIsDone = false;
                }
                else {
                    GameDebug.Log("Post Max Serial Query  ");
                    maxSerialIsDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void PostLobbyListQuery() {
            _gdnErrorHandler.isWaiting = true;
            var data = $"{{\"bindVars\" : {{\"@collection\" : \"{lobbiesCollectionName}\" }},";

            data +=
                "\"query\": \"FOR doc IN  @@collection FILTER doc.lobby RETURN doc.lobbyValue\" }";
            _monoBehaviour.StartCoroutine(PostQuery(_gdnData, data, PostLobbyListQueryCallback));
        }

        public void PostLobbyListQueryCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("Post Lobby List Query error: " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                lobbyListIsDone = false;
            }
            else {
                lobbyListResult = JsonUtility.FromJson<LobbyListResult>(www.downloadHandler.text);
                if (maxSerialResult.error == true) {
                    GameDebug.Log("Post Lobby List Query failed:" + maxSerialResult.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    lobbyListIsDone = false;
                }
                else {
                   // GameDebug.Log("Post Lobby List Query: "+ lobbyListResult.result.Count);
                    lobbyListIsDone = true;
                    lobbyList.lobbies = lobbyListResult.result;
                    lobbyList.isDirty = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        
        #endregion Query
        
        #region Document
        
        
        //todo can this be post 
        public void PostLobbyDocument(LobbyDocument lobbyDocument) {
            GameDebug.Log("Post Lobby Document: " + lobbyDocument.baseName + " : "+ lobbyDocument.serialNumber);
            _gdnErrorHandler.isWaiting = true;
            string data = JsonUtility.ToJson(lobbyDocument);
            _monoBehaviour.StartCoroutine(PostInsertReplaceDocument(_gdnData, lobbiesCollectionName,
                data,false, lobbyDocument.lobbyValue,PostLobbyDocumentCallback));
        }

        public void PostLobbyDocumentCallback(UnityWebRequest www, LobbyValue lv) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                if (www.responseCode == 409) {
                    errorSerialIncr++;
                }
                GameDebug.Log("Post Lobby Doc network: " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                lobbyIsMade = false;
                maxSerialIsDone = false;
            }
            else {
                
                //409 error means unique index clash  so increase serial number
                var insertResponse = JsonUtility.FromJson<InsertResponse>(www.downloadHandler.text);
                if (insertResponse.error == true) {
                    if (insertResponse.code == 409) {
                       errorSerialIncr++;
                    }
                    GameDebug.Log("Post Lobby Doc insert response:" +insertResponse.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    lobbyIsMade  = false;
                    maxSerialIsDone = false;
                }
                else {
                    GameDebug.Log("Post Lobby Doc key: "+ insertResponse._key);
                    errorSerialIncr = 0;
                    lobbyKey = insertResponse._key;
                    lv.key = insertResponse._key;
                    lobbyIsMade = true;
                    postLobbyStuff = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        
        //need to repeat with same post on error
        public void UpdateLobbyDocument(LobbyDocument lobbyDocument, string key) {
            _gdnErrorHandler.isWaiting = true;
            String data = JsonUtility.ToJson(lobbyDocument);
            _monoBehaviour.StartCoroutine(PutReplaceDocument(_gdnData, lobbiesCollectionName,
                data,key, UpdateLobbyDocumentCallback));
        }

        public void UpdateLobbyDocumentCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("replace Lobby Doc : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
            }
            else {
                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("replace Lobby Doc :" +baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    maxSerialIsDone = false;
                }
                else {
                    GameDebug.Log("replace Lobby Doc   ");
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        #endregion Document
        
        #region Collection
        public void CreateLobbiesCollection() {
            lobbiesCollectionExists = listCollection.result.Any
                (item => item.name == lobbiesCollectionName);
            if (!lobbiesCollectionExists) {
                _gdnErrorHandler.isWaiting = true;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monoBehaviour.StartCoroutine(CreateCollection(_gdnData, lobbiesCollectionName,
                    CreateLobbiesCollectionCallback));
            }
        }

        public void CreateLobbiesCollectionCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("Create document collection : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                collectionListDone = false;
            }
            else {
                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create  Collection failed:" + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    collectionListDone = false;
                }
                else {
                    GameDebug.Log("Create Collection  ");
                    lobbiesCollectionExists = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        
        public void GetListDocumentCollections() {
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(ListDocumentCollections(_gdnData, ListDocumentCollectionsCallback));
        }

        public void ListDocumentCollectionsCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("List Collections: " + www.error);
            }
            else {

                //overwrite does not assign toplevel fields
                //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
                listCollection = JsonUtility.FromJson<ListCollection>(www.downloadHandler.text);
                if (listCollection.error == true) {
                    GameDebug.Log("List Collection failed:" + listCollection.code);
                    //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                    _gdnErrorHandler.currentNetworkErrors++;
                }
                else {
                    collectionListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        
        #endregion Collection
        
        #region Index

        public bool IndexExists(IndexParams indexParams) {
            return listIndexes.indexes.Any
            (item => {
                if (indexParams.fields.Count != item.fields.Length) {
                    return false;
                }
                for (int i = 0; i < item.fields.Length; i++) {
                    if (item.fields[i] != indexParams.fields[i]) {
                        return false;
                    }
                }
                if (indexParams.sparse != !item.sparse) {
                    return false;
                }
                if (indexParams.unique != !item.unique) {
                    return false;
                }
                if (indexParams.type != item.type) {
                    return false;
                }
                return true;
            });
        }
        public void GetListIndexes(string collection) {
            _gdnErrorHandler.isWaiting = true;
            _monoBehaviour.StartCoroutine(ListIndexes(_gdnData, collection, ListIndexesCallback));
        }

        public void ListIndexesCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                _gdnErrorHandler.currentNetworkErrors++;
                GameDebug.Log("List Indexes: " + www.error);
            }
            else {

                //overwrite does not assign toplevel fields
                //JsonUtility.FromJsonOverwrite(www.downloadHandler.text, listStream);
                listIndexes= JsonUtility.FromJson<ListIndexes>(www.downloadHandler.text);
                //GameDebug.Log( "list index result: "+ www.downloadHandler.text);
                if (listIndexes.error == true) {
                    GameDebug.Log("List Indexes failed:" + listIndexes.code);
                    //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                    _gdnErrorHandler.currentNetworkErrors++;
                }
                else {
                    indexesListDone = true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }

        public void CreateIndex( int indexId) {
            IndexParams indexParams = indexParamsList[indexId]; 
            indexesExist[indexId] = IndexExists(indexParams);
            if (!indexesExist[indexId]) {
                _gdnErrorHandler.isWaiting = true;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monoBehaviour.StartCoroutine(PostCreateIndex(_gdnData, lobbiesCollectionName, indexParams, indexId,
                    CreateIndexCallback));
            }
        }

        public void CreateIndexCallback(UnityWebRequest www, int indexId) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("create an index failed:" + indexId + " : " + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                indexesExist[indexId] = false;
            }
            else {
                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create an index failed:" + indexId + " : " + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    indexesExist[indexId] = false;
                }
                else {
                    GameDebug.Log("Create an Index " + indexId);
                    indexesExist[indexId] =  true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        public void CreateTTLIndex() {
            if (!indexTTLExist) {
                _gdnErrorHandler.isWaiting = true;
                //Debug.Log("creating server in stream: " + baseGDNData.CreateStreamURL(serverInStreamName));
                _monoBehaviour.StartCoroutine(PostCreateTTLIndex(_gdnData, lobbiesCollectionName, ttlIndexParams,
                    CreateTTLIndexCallback));
            }
        }

        public void CreateTTLIndexCallback(UnityWebRequest www) {
            _gdnErrorHandler.isWaiting = false;
            if (www.isHttpError || www.isNetworkError) {
                GameDebug.Log("create TTL index failed:"  + www.error);
                _gdnErrorHandler.currentNetworkErrors++;
                indexTTLExist = false;
            }
            else {
                var baseHttpReply = JsonUtility.FromJson<BaseHtttpReply>(www.downloadHandler.text);
                if (baseHttpReply.error == true) {
                    GameDebug.Log("create TTL index failed:" + baseHttpReply.code);
                    _gdnErrorHandler.currentNetworkErrors++;
                    indexTTLExist = false;
                }
                else {
                    GameDebug.Log("Create TTL Index " );
                    indexTTLExist=  true;
                    _gdnErrorHandler.currentNetworkErrors = 0;
                }
            }
        }
        #endregion Index
        
    }

}