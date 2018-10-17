using UnityEngine;
using UnityEngine.UI;

// Bindings to the scoreboard you can bring up with TAB. 

[System.Serializable]
public class ScoreboardUIBinding
{
    public Image[] headers;
    public ScoreboardTeamUIBinding[] teams;

    internal void Clear()
    {
        foreach(var team in teams)
        {
            team.name.text = "";
            team.score.text = "";
            foreach (var player in team.players)
            {
                player.name.text = "";
            }
        }
    }
}

[System.Serializable]
public class ScoreboardTeamUIBinding
{
    public Text name;
    public Text score;
    public ScoreboardPlayerUIBinding[] players;
}

[System.Serializable]
public class ScoreboardPlayerUIBinding
{
    public Text name;
    public Text score;
}

