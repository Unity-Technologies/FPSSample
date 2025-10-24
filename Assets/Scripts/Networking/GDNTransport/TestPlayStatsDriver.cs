using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BestHTTP.WebSocket;
using Macrometa.Lobby;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Macrometa {
    public class TestPlayStatsDriver : MonoBehaviour {
        public bool justFormetData;
        public KillStats killStats;
        public LobbyValue lobby;
        public string gameName;
        
        public bool setupComplete;
        
        public int numPlayers = 8;
        
        public InputField prefix;
        public Text postfix;
        
        public TeamInfo team0;
        public TeamInfo team1;
        public List<GameStats> gameStats;
        //public InGameStats inGameStats2;
        public string playStatsStreamName = "FPSGame_PlayStats" ;
        public string InGameStatsStreamName = "FPSGame_InGameStats_New" ;
        //public string InGameStatsStreamName = "FPSGame_InGameStats_New" ;


        public GDNData baseGDNData;

        public GDNStreamStatsDriver gdnStreamStatsDriver;
        public GDNErrorhandler gdnErrorHandler;
        
        public int nextGameSerialnumber = 0;

        static TestPlayStatsDriver _inst;
        
        public  float nextMsgTime = 0;
        public float secondsDelay = 0.5f;

        public int msgIndex = 0;
        
        public void Awake() {
            GameDebug.Log("TestPlayStatsDriver  Awake A");
            _inst = this;
           
            gdnErrorHandler = new GDNErrorhandler();
           
            var defaultConfig = RwConfig.ReadConfig();
            RwConfig.Flush();
            
            baseGDNData = defaultConfig.gdnData;
            BestHTTP.HTTPManager.Setup();
            BestHTTP.HTTPManager.MaxConnectionPerServer = 64;
            GameDebug.Log("TestPlayStatsDriver  Awake C");
            gdnStreamStatsDriver = new GDNStreamStatsDriver(this);
            GameDebug.Log("TestPlayStatsDriver  Awake D");
            // msgIndex = gameStats.Count;
            GameDebug.Log("TestPlayStatsDriver  Awake E");
        }

        public void Update() {
            Bodyloop();
        }

        public void OnDisable() {
            GameDebug.Log("TestPlayStatsDriver OnDisable");
        }

        public void Run() {
            msgIndex = 0;
            
            foreach (var r in gameStats) {
                r.gameName = prefix.text + "Game" + nextGameSerialnumber;
            }
            nextGameSerialnumber++;
            postfix.text = nextGameSerialnumber.ToString();
        }

        public GameStats NewStat(int msgIndex, int p1, int p2) {
            var gs = gameStats[msgIndex].CopyOf();
            if (gs.playerName == "Grant") gs.playerName = "P" + p1;
            if (gs.playerName == "Anurag") gs.playerName = "P" + p2;
            
            if (gs.killed == "Grant") gs.killed = "P" + p1;
            if (gs.killed == "Anurag") gs.killed = "P" + p2;
            
            if (gs.killedBy == "Grant") gs.killedBy = "P" + p1;
            if (gs.killedBy == "Anurag") gs.killedBy = "P" + p2;
            
            

            gs.team0 = team0;
            gs.team1 = team1;
            return gs;
        }


        public GameStats NewStat(int msgIndex, int maxRandom) {
            var r1 = Random.Range(1, maxRandom);
            var r2 =  Random.Range(1, maxRandom);
           

            return NewStat(msgIndex, r1, r2);
        }

        public void Bodyloop() {
            
            if (gdnErrorHandler.pauseNetworkErrorUntil > Time.time) return;
            if (gdnErrorHandler.currentNetworkErrors >= gdnErrorHandler.increasePauseConnectionError) {
                gdnErrorHandler.pauseNetworkError *= gdnErrorHandler.pauseNetworkErrorMultiplier;
                return;
            }

            if (gdnErrorHandler.isWaiting) return;

            if (!gdnStreamStatsDriver.regionIsDone) {
                gdnStreamStatsDriver.GetRegion();
                return;
            }

            if (!gdnStreamStatsDriver.streamListDone) {
                gdnStreamStatsDriver.GetListStream();
                GameDebug.Log("TestPlayStatsDriver GetListStream ");
                return;
            }

            if (!gdnStreamStatsDriver.serverOutStreamExists) {
                gdnStreamStatsDriver.CreateServerOutStream(playStatsStreamName);
                return;
            }
            
            if (!gdnStreamStatsDriver.producerExists) {
                gdnStreamStatsDriver.CreateProducer(playStatsStreamName);
                GameDebug.Log("TestPlayStatsDriver CreateProducer ");
                return;
            }

            if (!gdnStreamStatsDriver.serverInStreamExists) {
                gdnStreamStatsDriver.CreateServerInStream(InGameStatsStreamName);
                return;
            }
            
            if (!gdnStreamStatsDriver.consumerExists) {
                gdnStreamStatsDriver.CreateConsumer(InGameStatsStreamName);
                GameDebug.Log("TestPlayStatsDriver  CreateConsumer  ");
                return;
            }

            setupComplete = true;
            
/*
            if (Time.time > nextMsgTime && msgIndex < gameStats.Count) {
                GameDebug.Log("Send Message: "+ (msgIndex+1) + " / "+ (gameStats.Count));
                nextMsgTime += secondsDelay;
                gdnStreamStatsDriver.ProducerSend(JsonUtility.ToJson(NewStat(msgIndex,numPlayers)));
                msgIndex++;
            }
*/
          
                if ( gdnStreamStatsDriver?.inGameStats != null){
                    //&& gameName == GDNStats.instance.gameName) {
                   // GameDebug.Log("testplay are A ");
                    //inGameStats2 = gdnStreamStatsDriver.inGameStats;
                    //GameDebug.Log("testplay are b ");
                    //killStats = new KillStats(GDNStats.instance.playerName, GDNStats.instance.team0, GDNStats.instance.team1, inGameStats2);
                    //GDNStats.Instance.InGameStats = inGameStats2;
                   //GameDebug.Log("  killStats: "+  killStats.opponents[0]);
                }
            
        }

        public static void SendStats(GameStats2 gameStats) {
            var msg = JsonUtility.ToJson(gameStats);
            _inst.gdnStreamStatsDriver.ProducerSend(msg);
        }
/*
        public static KillStats GetKillStats(string playerName, string gameName) {
            return new KillStats(playerName, _inst.lobby, _inst.inGameStats2);
        }
        */
    }
}
