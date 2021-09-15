using System;
using System.Collections.Generic;
using Macrometa.Lobby;
using UnityEngine;

namespace Macrometa {
    public class PlayStats {
        private static LobbyValue lobbyValue;
        
        public enum MatchResult {
            Lose,
            Tie,
            Win
        }

        static public GameStats baseGameStats;
        
        static public GameStats BaseGameStats() {
            return GDNStats.baseGameStats.CopyOf();
        }
/*
        static public GameStats BaseGameStats() {
            var result = new GameStats() {
                gameName = lobbyValue.GameName(),
                team0 = lobbyValue.team0.ToTeamInfo(),
                team1 = lobbyValue.team1.ToTeamInfo()
            };
            return result;
        }
        */


        public static void AddPlayerStat(string killed, string killedBy) {
            var ps = BaseGameStats();
            ps.playerName = killed;
            ps.killed = killed;
            ps.killedBy = killedBy;
            SendPlayerStats(ps);
        }

        public static void AddPlayerMatchResultStat(string playerName, string gameType,
            MatchResult matchResult, int score) {
            var ps = BaseGameStats();

            ps.playerName = playerName;
            ps.matchType = gameType;
            ps.matchResult = matchResult.ToString();
            //score = score

            SendPlayerStats(ps);
        }

        public static void SendPlayerStats(GameStats ps) {
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
        GameDebug.Log("player: "+ GDNStats.instance.playerName);
        killRecordsList = JsonUtility.FromJson<KillRecordList>( "{\"krl\":" + killRecords + "}");
        
        GameDebug.Log("opponents[0] B");
        //totalKillRecordList = JsonUtility.FromJson<TotalKillRecordList>( "{\"krl\":" + totalKillRecords + "}");
        
       
        GDNStats.Instance.testPlayStatsDriver.killStats =
            new KillStats(GDNStats.instance.playerName, GDNStats.instance.team0, GDNStats.instance.team1, this);
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
        GameDebug.Log("GDNStats.instance.team0[0] C: "+ GDNStats.instance.team0.players[0]);
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