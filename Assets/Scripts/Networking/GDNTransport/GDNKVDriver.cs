using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Macrometa {
    /// <summary>
    /// Key value methods
    /// client browser mode check game list 
    /// </summary>
   public class GDNKVHandler {
    private MonoBehaviour _monoBehaviour;
    private GDNData _gdnData;
    private GDNErrorhandler _gdnErrorHandler;
    public ListKVCollection listKVCollection;
    public ListKVCollection listKVValues;
    public bool kvCollectionListDone = false;
    public bool gamesKVCollectionExists;
    public  string gamesKVCollectionName = "test_Games_collection";
    public bool kvValueListDone;

    
    /// <summary>
    /// passing in a monobehaviour to be able use StartCoroutine
    /// happens because of automatic refactoring
    /// probably can hand cleaned
    /// </summary>
    /// <param name="gdnData"></param>
    /// <param name="gdnErrorhandler"></param>
    /// <param name="monoBehaviour"></param>
    public GDNKVHandler(GDNNetworkDriver gdnNetworkDriver) {
        
        _gdnData = gdnNetworkDriver.baseGDNData;
        _monoBehaviour =gdnNetworkDriver;
        _gdnErrorHandler = gdnNetworkDriver._gdnErrorHandler;
    }

    public void CreateGamesKVCollection() {
         
        gamesKVCollectionExists = listKVCollection.result.Any
            (item => item.name == gamesKVCollectionName);
        if (!gamesKVCollectionExists ) {
            _gdnErrorHandler.isWaiting = true; ;
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
        _monoBehaviour.StartCoroutine(MacrometaAPI. GetKVValues(_gdnData,gamesKVCollectionName,
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
            listKVValues = JsonUtility.FromJson<ListKVCollection>(www.downloadHandler.text);
            if (listKVValues.error == true) {
                GameDebug.Log("List KV Collection failed:" + listKVValues.code);
                //Debug.LogWarning("ListStream failed reply:" + www.downloadHandler.text);
                _gdnErrorHandler.currentNetworkErrors++;
            }
            else {
                GameDebug.Log("List KV Collection succeed ");
                kvValueListDone = true;
                _gdnErrorHandler.currentNetworkErrors = 0;
            }
        }
    }
}

}