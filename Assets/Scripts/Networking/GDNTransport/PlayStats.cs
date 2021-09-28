using System;
using System.Collections.Generic;
using Macrometa;
using Macrometa.Lobby;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.Experimental.UIElements;
using Random = UnityEngine.Random;

namespace Macrometa {
    public class PlayStats {
        private static LobbyValue lobbyValue;
        
        public enum MatchResult {
            Lose,
            Tie,
            Win
        }

        [Serializable]
        public class killCount {
            public string killedBy;
            public int count;
        }
        
        [Serializable]
        public class deathCount {
            public string killed;
            public int count;
        }
        
        static public ShotsFired rifleShotsFired =new ShotsFired();
        
        static public int FPS;
        static public int health;
        static public bool prevCooldown = false;
        static public int grenadeShots;
        static public Vector3 position;
        static public float orientation;
        static public string gameTimerMessage = "unset";
        
        static public List<killCount> killCounts = new List<killCount>();
        static public List<deathCount> deathCounts = new List<deathCount>();
        static public bool disconnected= false;
        
        // These are on local player data  is sent through Pong back to server
        //should be somewhere else but where?
        static public string remotePlayerCity ;
        static public string remotePlayerCountry;
        static public string remoteConnectin_Type;

        static public void AddKills(string killed, string killedBy) {
           var dc = deathCounts.Find(rec => rec.killed == killed);
           if (dc == null) {
               deathCounts.Add(new deathCount(){killed = killed,count =1});
           }
           else {
               dc.count++;
           }
           var kc = killCounts.Find(rec => rec.killedBy == killedBy);
           if (kc == null) {
               killCounts.Add(new killCount(){killedBy = killedBy,count =1});
           }
           else {
               kc.count++;
           }
           GameDebug.Log(" killCounts: " +  killCounts.Count);
           updateGDNStatsKills();
        }

        static public void updateGDNStatsKills() {
            GDNStats.baseGameStats.killCounts = killCounts;
            GDNStats.baseGameStats.deathCounts = deathCounts;
        }
        
        static public void PlayerKilled(string playerName) {
            if(GDNStats.playerName == playerName) {
                ReloadRifleShots();
            }
        }
        
        static public void CheckCameTimerMessage( string message) {
            if (message == gameTimerMessage) return;
            gameTimerMessage = message;
            if ("PreMatch" == gameTimerMessage) {
                PrematchStarted();
            }
            else {
                MatchStarted();
            }
        }

        public static void MatchStarted() {
            ResetStats();
        }

        public static void PrematchStarted() {
            ResetStats();
        } 
        
        static public void SetFPS(int val) {
            PlayStats.FPS = val;
        }
        static public void SetOrientation(Quaternion quaternion) {
            PlayStats.orientation = quaternion.eulerAngles.y;
        }
        
        static public void SetHealth(int val) {
            PlayStats.health= val;
            Debug.Log("SetHealth :" + val);
            if (health == 0) {
                ReloadRifleShots();
            }
        }
        //use when avatar dies.
        static public void ReloadRifleShots() {
            Debug.Log( "ReloadRifleShots()  :" + rifleShotsFired.shotsFired);
            rifleShotsFired.Reload();
        }
        static public void UpdateRifleShots(int aClipSize, int anAmmoInClip) {
            rifleShotsFired.Update(aClipSize,anAmmoInClip);
        }
        
        static public int GetAndClearRifle() {
            //Debug.Log("GetAndClearRifle :" + rifleShotsFired.shotsFired);
            return rifleShotsFired.GetAndClear();
        }

        static public int GetRifle() {
            //Debug.Log("GetAndClearRifle :" + rifleShotsFired.shotsFired);
            return rifleShotsFired.Get();
        }
        
        static public void ClearRifle() {
            Debug.Log("ClearRifle :" + rifleShotsFired.shotsFired);
            rifleShotsFired.Clear();
        }
        
        static public void UpdateGrenade(bool cooldown) {
            if (!prevCooldown && cooldown) {
                Debug.Log("update incr :" + grenadeShots);
                grenadeShots++;}
            prevCooldown = cooldown;
        }
        static public int GetAndClearGrenade() {
           // Debug.Log("GetAndClearGrenade :" + grenadeShots);
            var result = grenadeShots;
            grenadeShots = 0;
            return result; ;
        }
       
        static public int GetGrenade() {
            // Debug.Log("GetAndClearGrenade :" + grenadeShots);
            return grenadeShots;
        }
        
        static public void ClearGrenade() {
            // Debug.Log("GetAndClearGrenade :" + grenadeShots);
            grenadeShots = 0;
        }

        // usually because a game started or ended
        static public void ResetStats() {
            GameDebug.Log("ResetStats()");
            ClearRifle();
            ClearGrenade();
            killCounts.Clear();
            deathCounts.Clear();
            updateGDNStatsKills();
        }
        
        public static void AddPlayerStat(string killed, string killedBy) {
           
            var ps = GDNStats.baseGameStats.CopyOf();
            //ps.timeStamp = (long) (DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            ps.killed = killed;
            ps.killedBy = killedBy;
            AddKills(killed, killedBy);
            SendPlayerStats(ps);
            
        }

        public static void AddPlayerMatchResultStat(string playerName, string gameType,
            MatchResult matchResult, int score) {
            var ps = GDNStats.baseGameStats.CopyOf();
            //ps.timeStamp = (long) (DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            ps.playerName = playerName;
            ps.matchType = gameType;
            ps.matchResult = matchResult.ToString();
            //score = score

            SendPlayerStats(ps);
        }

        public static void SendPlayerStats(GameStats2 ps) {
            GDNStats.SendStats(ps);
        }

        public static void UpdateGameType(string gameType) {
            // gameStats.gameType = gameType;
        }

        /// <summary>
        /// do this from lobby info
        /// </summary>
        /// <param name="numPlayers"></param>
        public static void UpdateTeams(int numPlayers) {
        }

        public static void SendGameStats() {
            // GameDebug.Log(gameStats.ToString());
        }
        
        public static GameStats2 GenerataPeriodicGameStats2(PingStatsGroup.NetworkStatsData networkStatsData,
            ReceivedMessage receivedMessage) {
            //GameDebug.Log("GenerataPeriodicGameStats2 A");
            var ps = GDNStats.baseGameStats.CopyOf();
            //GameDebug.Log("GenerataPeriodicGameStats2 B");
            ps.timeStamp = (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
            ps.playerName = networkStatsData.remoteId;
            ps.gdnCity = networkStatsData.remoteCity;
            ps.gdnCountry = networkStatsData.remoteCountrycode;
            ps.rtt = networkStatsData.rttAverage; 
            ps.throughput = networkStatsData.streamOutBytes; // ??? needs to change but useful as dummy data
            ps.health = receivedMessage.properties.health;
            ps.fps = receivedMessage.properties.fps;
            ps.rifleShots =receivedMessage.properties.rifleShots;
            //GameDebug.Log("GenerataPeriodicGameStats2 rifle: " + ps.rifleShotsShots);
            ps.grenadeShots = receivedMessage.properties.grenadeShots;
            ps.posX = receivedMessage.properties.posX;
            ps.posY = receivedMessage.properties.posY;
            ps.posZ = receivedMessage.properties.posZ;
            ps.orientation = receivedMessage.properties.orientation;
            ps.playerCity = receivedMessage.properties.remotePlayerCity;
            ps.playerCountry = receivedMessage.properties.remotePlayerCountrycode;
            ps.connectionType = receivedMessage.properties.remoteConnectin_Type;
            return ps;
        }
        

    }
}


[Serializable]
public class GameStats2 {
    public string gameName;
    public string gameId = (long)(DateTime.Now.
        Subtract(new DateTime(1970, 1, 1))).TotalSeconds+ "R"+Random.Range(1,1000000);
    public long   timeStamp;
    public string playerName;
    public int health;
    public string matchType; //DeathMatch, Assault
    public string matchResult; //Lose , Tie ,  Win

    public string teamName;
    
    public TeamInfo team0;
    public TeamInfo team1;
    public int rtt;
    public string gdnCity;
    public string gdnCountry;
    public int rifleShots;
    public int grenadeShots;
    public string killed;
    public string killedBy;
    public float posX;
    public float posY;
    public float posZ;
    public float orientation; //degrees XZ plane?
    public float throughput; //bytes/second?
    public float fps;// frames per second
    public List<PlayStats.killCount> killCounts;
    public List<PlayStats.deathCount> deathCounts;
    public string playerCity;
    public string playerCountry;
    public string connectionType;
    public bool disconnect;


    public string LongToString() {
        return gameName + ":"+gameId + ":"+ timeStamp + ":"+ playerName  + ":"+ health + ":"+ matchResult + " rtt:"+ rtt +":" +
            fps +":" + throughput
            +":" + gdnCity + ":" + gdnCountry + ":" + rifleShots + ":" + grenadeShots + " posX:" + posX + ":" +
            orientation;
    }

    public override string ToString() {
        return LongToString();
    }

    public GameStats2 CopyOf() {
        var result = new GameStats2() {
            gameName = gameName,
            gameId = gameId,
            timeStamp = timeStamp,
            playerName = playerName,
            health = health,
            matchType = matchType,
            matchResult = matchResult,
            teamName = teamName,
            team0 = team0.CopyOf(),
            team1 = team1.CopyOf(),
            rtt = rtt,
            gdnCity = gdnCity,
            gdnCountry = gdnCountry,
            rifleShots = rifleShots,
            grenadeShots = grenadeShots,
            killed = killed,
            killedBy = killedBy,
            posX = posX,
            posY = posY,
            posZ = posZ,
            orientation = orientation,
            throughput = throughput,
            fps = fps,
            deathCounts = deathCounts,
            killCounts = killCounts,
            playerCity = playerCity,
            playerCountry = playerCountry,
            connectionType = connectionType,
            disconnect = disconnect,
        };
        
        return result;
    }
}



 


[Serializable]
public class GameStats {
    public string gameName;
    public string playerName;
    public string matchType; //DeathMatch, Assault
    public string matchResult; //Lose , Tie ,  Win

    public string teamName;

    //public List<global::TeamInfo> teams;
    public TeamInfo team0;
    public TeamInfo team1;
    public int rtt;
    public string city;
    public string country;
    public int mainRobotShots;
    public int secondaryRobotShots;
    public int mainPioneerShots;
    public int secondaryPioneerShots;
    public string avatar; //Robot, Pioneer
    public string killed;
    public string killedBy;

    public override string ToString() {
        return gameName + " t0: " + team0 + " t1: " + team1;
    }

    public GameStats CopyOf() {
        var result = new GameStats() {
            gameName = gameName,
            playerName = playerName,
            matchType = matchType,
            matchResult = matchResult,
            teamName = teamName,
            team0 = team0.CopyOf(),
            team1 = team1.CopyOf(),
            rtt = rtt,
            city = city,
            country = country,
            mainPioneerShots = mainPioneerShots,
            mainRobotShots = mainRobotShots,
            secondaryPioneerShots = secondaryPioneerShots,
            secondaryRobotShots = secondaryRobotShots,
            avatar = avatar,
            killed = killed,
            killedBy = killedBy
        };
        
        return result;
    }
}

[Serializable]
public class TeamInfo {
    public string teamName;
    public List<string> players;

    public override string ToString() {
        var result = teamName + " : ";
        foreach (var name in players) {
            result += name + ", ";
        }
        return result;
    }

    public TeamInfo CopyOf() {
        var result = new TeamInfo() {
            teamName = teamName,
        };
        result.players = new List<string>();
        result.players.AddRange(players);
        return result;
    }
}


/*
  {
  "gameName": "t2bGame3",
  "playerName": "P2",
  "matchType": "DeathMatch",
  "killRecords": "[{\"count\":1,\"killedBy\":\"P5\",\"killed\":\"P1\"}]",
  "totalKillRecords": "[{\"count\":1,\"killedBy\":\"P5\"}]"

 */
[Serializable]
public class InGameStats {
    public string gameName;
    public string killRecords;
    public string totalKillRecords;

    public KillRecordList killRecordsList;
    public TotalKillRecordList totalKillRecordList;
    
    static  public string test = $"[{{\"count\":1,\"killedBy\":\"P5\",\"killed\":\"P1\"}}]";
    public string test2 = "{\"krl\":" + test + "}";

    public void Convert() {
        GameDebug.Log("opponents[0] A");
        GameDebug.Log("player: "+ GDNStats.playerName);
        killRecordsList = JsonUtility.FromJson<KillRecordList>( "{\"krl\":" + killRecords + "}");
        
        GameDebug.Log("opponents[0] B");
        //totalKillRecordList = JsonUtility.FromJson<TotalKillRecordList>( "{\"krl\":" + totalKillRecords + "}");
        
       
        GDNStats.Instance.testPlayStatsDriver.killStats =
            new KillStats(GDNStats.playerName, GDNStats.team0, GDNStats.team1, this);
        GameDebug.Log("opponents[0] Z" + GDNStats.Instance.testPlayStatsDriver.killStats.opponents[0]);
        
        if (GDNStats.Instance?.testPlayStatsDriver?.killStats?.opponents != null) {
            for (int i = 0; i < GDNStats.Instance.testPlayStatsDriver.killStats.opponents.Count; i++) {
                var name = GDNStats.Instance.testPlayStatsDriver.killStats.opponents[i];
                var kills = GDNStats.Instance.testPlayStatsDriver.killStats.killOpponents[i];
                var killedBy = GDNStats.Instance.testPlayStatsDriver.killStats.killedByOpponents[i];
               // var totalKills = GDNStats.Instance.testPlayStatsDriver.killStats.totalKillsOpponents[i];
                GameDebug.Log(" " +i +" : "+name +" : "+ kills +" : "+ killedBy +" : "+0);
            }
        }
        GameDebug.Log( "ZZ* " + killRecordsList.ToString());
        GameDebug.Log("GDNStats.instance.team0[0] C: "+ GDNStats.team0.players[0]);
    }
}
public class ShotsFired {
    public int clipSize;
    public int ammoInClip;
    public int shotsFired;
    public bool paused;

    public void Update(int aClipSize, int anAmmoInClip) {
        clipSize = aClipSize;
        if (paused) {
            if (ammoInClip == anAmmoInClip) {
                paused = false;
                GameDebug.Log("Reload() unpause: "+ammoInClip+ " : "+  shotsFired);
            }
            else {
                return;
            }
        }
        if (anAmmoInClip != ammoInClip) {
            if (anAmmoInClip > ammoInClip) {
                shotsFired += ammoInClip +clipSize - anAmmoInClip;
                   
            }
            else {
                shotsFired += ammoInClip - anAmmoInClip;
            }
            ammoInClip = anAmmoInClip;
        }
    }

    public void Reload() {
        GameDebug.Log("Reload() start: "+ammoInClip + " : "+  shotsFired);
                        ammoInClip = clipSize;
        paused = true;
    }
    
    public int GetAndClear() {
        var result =  shotsFired;
        shotsFired = 0;
        return result;
    }
    
    public int Get() {
        return  shotsFired; ;
    }
    
    public void Clear() {
        shotsFired = 0;
    }
    
}
[Serializable]
public class KillRecordList {
    public List<global::KillRecord> krl;
    public override string ToString() {
        string result = "";
        foreach (var k in krl) {
            result += k.ToString();
        }

        return result;
    }
}

[Serializable]
public class TotalKillRecordList {
    public List<global::TotalKillRecord> krl;
    public override string ToString() {
        string result = "";
        foreach (var k in krl) {
            result += k.ToString();
        }
        return result;
    }
}


[Serializable]
public class KillRecord {
    public string killed;
    public string killedBy;
    public int count;

    public override string ToString() {
        return killed + " : " + killedBy + " : " + count;
    }
}

[Serializable]
public class TotalKillRecord {
    public string killedBy;
    public int count;
}


[Serializable]
public class KillStats {
    public List<string> opponents;
    public List<string> comrades;
    public int[] killOpponents;
    public int[] killComrades;
    public int[] killedByOpponents;
    public int[] killedByComrades;
    public int[] totalKillsOpponents;
    public int[] totalKillsComrades;
    
    public KillStats (string playerName, LobbyValue lobbyValue,InGameStats inGameStats ) {
        SetHeader(playerName, lobbyValue);
        KillList(playerName, inGameStats);
        //TotalKills(playerName, inGameStats);
    }

    public KillStats (string playerName, TeamInfo team0, TeamInfo team1,InGameStats inGameStats ) {
        SetHeader(playerName, team0, team1);
        KillList(playerName, inGameStats);
        //TotalKills(playerName, inGameStats);
    }
    
    public void TotalKills(string player, InGameStats inGameStats) {
        totalKillsComrades = new int[opponents.Count];
        totalKillsOpponents = new int[comrades.Count];
        foreach (var kr in inGameStats.totalKillRecordList.krl) {
            var index = opponents.FindIndex(kr.killedBy.Equals);
            if (index > -1) {
                totalKillsOpponents[index] += kr.count;
                continue;
            }
            index = comrades.FindIndex(kr.killedBy.Equals);
            if (index > -1) {
                totalKillsComrades[index] += kr.count;
            }
            continue;

        }
    }
    
    public void KillList(string player,InGameStats inGameStats) {
        killOpponents = new int[opponents.Count] ;
        killComrades = new int[comrades.Count] ;
        killedByOpponents = new int[opponents.Count] ;
        killedByComrades = new int[comrades.Count] ;
        
        
        foreach (var kr in inGameStats.killRecordsList.krl) {
            if (kr.killed == player) {
                var index = opponents.FindIndex(kr.killedBy.Equals);
                if (index > -1) {
                    killedByOpponents[index] += kr.count;
                    continue;
                }
                index = comrades.FindIndex(kr.killedBy.Equals);
                if (index > -1) {
                    killedByComrades[index] += kr.count;
                }
                continue;
            }
            if (kr.killedBy == player) {
                var index = opponents.FindIndex(kr.killed.Equals);
                if (index > -1) {
                    killOpponents[index] += kr.count;
                    continue;
                }
                index = comrades.FindIndex(kr.killed.Equals);
                if (index > -1) {
                    killComrades[index] += kr.count;
                }
                continue;
            }
        }
        
        
    }
    

    public void SetHeader(string playerName, LobbyValue lobbyValue) {
        List<string> t0 = new List<string>();
        foreach (var slot in lobbyValue.team0.slots) {
            t0.Add(slot.playerName);
        }
        List<string> t1 = new List<string>();
        foreach (var slot in lobbyValue.team1.slots) {
            t1.Add(slot.playerName);
        }

        if (t0.Contains(playerName)) {
            opponents = t1;
            comrades = t0;
        }
        else {
            opponents = t0;
            comrades = t1;
        }
        comrades.Remove(playerName);
        comrades.Add(playerName);
    }
    public void SetHeader(string playerName, TeamInfo team0, TeamInfo team1) {
        List<string> t0 = new List<string>();
        foreach (var name in team0.players) {
            t0.Add(name);
        }
        List<string> t1 = new List<string>();
        foreach (var name in team1.players) {
            t1.Add(name);
        }

        if (t0.Contains(playerName)) {
            opponents = t1;
            comrades = t0;
        }
        else {
            opponents = t0;
            comrades = t1;
        }
        comrades.Remove(playerName);
        comrades.Add(playerName);
    }

    
}

/*
"gameName": "GameX5",
      "killRecords": [
       {
      ],
      "totalKillRecords": [
        
      ],
      "totalShotsFired":[
          {
              "playerName":"Anurag",
              "totalShotsFired":{// career level

                "mainRobotShots": 0,
                "secondaryRobotShots": 0,
                "mainPioneerShots": 0,
                "secondaryPioneerShots": 0
              },
              "city": "Pune",
              "country": "In"
          },
          {
            "playerName":"Grant",
            "totalShotsFired":{

              "mainRobotShots": 0,
              "secondaryRobotShots": 0,
              "mainPioneerShots": 0,
              "secondaryPioneerShots": 0
            },
            "city": "Tokyo",
            "country": "Jp"
        }
      ]
  }
*/