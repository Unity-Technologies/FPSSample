using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStats3 : MonoBehaviour
{
    [Serializable]
    public struct GameStats {
        public string gameId;
        public string playerName;
        public string matchType; //DeathMatch, Assault
        public string matchResult; //Lose , Tie ,  Win
        public string teamName;
        public int rtt;
        public string city;
        public string country;
        public int mainRobotShots;
        public int secondaryRobotShots;
        public int mainPioneerShots;
        public int secondaryPioneerShots;
        public string avatar; //Robot, Pioneer
        public string killedPlayerName;
        public string killedTeamName;
        public string killedByPlayerName;
        public string killedByTeamName;
    }

    public bool onDebugLog;
    public GameStats debugGS = new GameStats();


    private void Awake() {
        GameDebug.Init(Application.dataPath, "gameStatsDebug.Log");
    }

    public void Update() {
        if (onDebugLog) {
            onDebugLog = false;
            
            GameDebug.Log(JsonUtility.ToJson(debugGS));
        }
    }
}
