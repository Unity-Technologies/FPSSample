using System;
using System.Collections;
using System.Collections.Generic;
using Macrometa;
using Macrometa.Lobby;
using UnityEngine;

public class GDNStats {
    public static bool setupComplete = false;
    // must be set before websockets are opened.


    public static GameStats2 baseGameStats;
    public TestPlayStatsDriver testPlayStatsDriver;
    public static GDNStats instance=null;
    public TeamInfo team0;
    public TeamInfo team1;
    public string gameName;
    public string playerName;
    public InGameStats InGameStats;
    

    public static GDNStats Instance {
        get {
            if (instance==null) {
                instance = new GDNStats();
            }
            return instance;
        }
    }

    public static void SetPlayerName(string playerName) {
        instance.playerName = playerName;
        GameDebug.Log("SetPlayerName: " + playerName);
    }
    
    static public void SendKills(string killed, string killedBy) {
        var gameStats2 = baseGameStats.CopyOf();
        gameStats2.timeStamp = (long) (DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        gameStats2.killed = killed;
        gameStats2.killedBy = killedBy;
        TestPlayStatsDriver.SendStats(gameStats2);
    }
    
    /// <summary>
    /// Start stat streams
    /// </summary>
    /// <param name="port"></param>
    /// <param name="maxConnections"></param>
    public void Start(bool isServer )
    {
        testPlayStatsDriver= new GameObject().AddComponent<TestPlayStatsDriver>();
        MonoBehaviour.DontDestroyOnLoad(testPlayStatsDriver.gameObject);
        ResetTeams();
    }
    
    
    /// <summary>
    /// this only need when lobby system is not complete
    /// </summary>
    static public void AddPlayer(int teamIndex, string playerName) {
        if (teamIndex == 0) {
            instance.team0.players.Add(playerName);
        }
        else {
            instance.team1.players.Add(playerName);
        }
    }

    static public void ResetTeams() {
        instance.team0 = new TeamInfo() {
            players =new List<string>(),
            teamName = "A Team"
        };
        instance.team1 = new TeamInfo() {
            players = new List<string>(),
            teamName = "B Team"
        };
        instance.gameName = "QuickKill";
        baseGameStats = new GameStats2() {
            gameName = instance.gameName,
            team0 = instance.team0,
            team1 = instance.team1
        };
    }
    
    public void MakeBaseInfo(string gameName, string team0Name, string team1Name, List<string> team0Players,
        List<String> team1Players) {
        var team0 = new TeamInfo() {
            players = team0Players,
            teamName = team0Name
        };
        var team1 = new TeamInfo() {
            players = team1Players,
            teamName = team1Name
        };

        baseGameStats = new GameStats2() {
            gameName = gameName,
            team0 = team0,
            team1 = team1
        };
    }

    static public void SendStats(GameStats2 gs) {
        TestPlayStatsDriver.SendStats(gs);
    }
    
}
