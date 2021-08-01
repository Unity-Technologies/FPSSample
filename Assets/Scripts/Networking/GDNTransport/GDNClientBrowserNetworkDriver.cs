using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;

namespace Macrometa {
    public class GDNClientBrowserNetworkDriver : GDNNetworkDriver {
        
        public bool debugKVTryInit =true;

        public bool tryKVInit = false;
        public bool tryKVInitB = false;
        public bool canNotInit = false;
        public bool initSucceeded = false;
        public bool initFail=false;
        public bool initTrying = false;
        public string clientId;
        public int maxConfirmInit = 3;
        public int currentConfirmInit = 3;

        public List<GameRecordValue> kvvs = new List<GameRecordValue>();
        public ListKVValue debugListKvValue;
        
        public override void Awake() {
            gdnErrorHandler = new GDNErrorhandler();
            var defaultConfig = RwConfig.ReadConfig();
            RwConfig.Flush();
            baseGDNData = defaultConfig.gdnData;
            gameName = defaultConfig.gameName;
            BestHTTP.HTTPManager.Setup();
            BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
            gdnStreamDriver = new GDNStreamDriver(this);
            gdnKVDriver = new GDNKVDriver(this);
            gdnStreamDriver.statsGroupSize = defaultConfig.statsGroupSize;
            if (gdnStreamDriver.statsGroupSize < 1) {
                gdnStreamDriver.statsGroupSize = 10; //seconds
            }
            gdnStreamDriver.nodeId = PingStatsGroup.NodeFromGDNData(baseGDNData);
            GameDebug.Log("Setup  GDNClientBrowserNetworkDriver: " + gdnStreamDriver.nodeId);
            setRandomClientName();
        }

        public void OnDisable() {
            GameDebug.Log("GDNClientBrowserNetworkDriver OnDisable");
        }

        public void setRandomClientName() {
            clientId = "Cl" + (10000000 + Random.Range(1, 89999999)).ToString();
        }

        public override void Update() {
            Bodyloop();
        }
        
        public void Bodyloop() {

            if (gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
            if (gdnErrorHandler.currentNetworkErrors >= gdnErrorHandler.increasePauseConnectionError) {
                gdnErrorHandler.pauseNetworkError *= gdnErrorHandler.pauseNetworkErrorMultiplier;
                return;
            }

            if (gdnErrorHandler.isWaiting) return;

            if (!gdnKVDriver.kvCollectionListDone) {
                GameDebug.Log("kvCollectionListDone not done");
                gdnKVDriver.GetListKVColecions();
                return;
            }

            if (!gdnKVDriver.gamesKVCollectionExists) {
                GameDebug.Log("Setup  gamesKVCollectionExists  A");
                gdnKVDriver.CreateGamesKVCollection();
                return;
            }

            if (debugKVTryInit) {
                debugKVTryInit = false;
                tryKVInit = true;
            }
            
            if (tryKVInit) {
                tryKVInit = false;
                tryKVInitB = true;
                gdnKVDriver.kvValueListDone = false;
            }
            
            // may have to set= false every 10 seconds to force list update
            if (!gdnKVDriver.kvValueListDone) {
                GameDebug.Log("Setup  kvValueListDone B");
                gdnKVDriver.GetListKVValues();
                return;
            }

            if (gdnKVDriver.listKVValues.result != null) {
                //GameDebug.Log("Setup  debug  B 2");
                kvvs.Clear();
                foreach (var kvValue in gdnKVDriver.listKVValues.result) {
                    kvvs.Add( JsonUtility.FromJson<GameRecordValue>(kvValue.value));
                }
            }

            if (tryKVInitB) {
                GameDebug.Log("Setup  tryKVInit C: " + gameName);
                initTrying = true;
                tryKVInitB = false;
               
               var foundRecord =  gdnKVDriver.listKVValues.result.SingleOrDefault(l => l._key == gameName);

               if (foundRecord != null) {
                   canNotInit = true;
                   initTrying = false;
                   initFail = true;
                   GameDebug.Log("Setup  tryKVInit C fail");
               }
               else {
                   var record = GameRecord.GetInit(clientId, gameName, 60);
                   gdnKVDriver.putKVValueDone = false;
                   gdnKVDriver.PutKVValue(record);
                   currentConfirmInit = 0;
               }
               return;
            }

            if (gdnKVDriver.putKVValueDone) {
                gdnKVDriver.putKVValueDone = false;
                gdnKVDriver.kvValueListDone = false;
                return;
            }
            
            if ( initTrying) {
                debugListKvValue = gdnKVDriver.listKVValues;
                GameDebug.Log("Setup  initTrying D");
                var foundRecord =  gdnKVDriver.listKVValues.result.SingleOrDefault(l => l._key == gameName);
               if (foundRecord == null ) {
                   GameDebug.Log("Setup  initTrying E put record in put not found");
                    initFail = true;
                    initTrying = false;
                    return;
               }
            
               var gameRecord = JsonUtility.FromJson<GameRecordValue>(foundRecord.value);
               if (gameRecord.clientId != clientId) {
                   GameDebug.Log("Setup  initTrying F other init same name");
                   initFail = true;
                   initTrying = false;
                   return;
               }

               if (currentConfirmInit < maxConfirmInit) {
                   GameDebug.Log("Setup  initTrying G");
                   gdnKVDriver.kvValueListDone = false;
                   currentConfirmInit++;
                   return;
               }
               GameDebug.Log("Setup  initTrying H");
               gdnKVDriver.putKVValueDone = false;
               initTrying = false; 
               initSucceeded = true;
            }
            
        }
    }
}