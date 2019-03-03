using System.Collections.Generic;
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
            team.playerScores.Clear();
            team.playerScoreTemplate.gameObject.SetActive(false);
        }
    }
}

[System.Serializable]
public class ScoreboardTeamUIBinding
{
    public TMPro.TextMeshProUGUI name;
    public TMPro.TextMeshProUGUI score;
    public TMPro.TextMeshProUGUI playerScoreTemplate;
    public List<TMPro.TextMeshProUGUI> playerScores = new List<TMPro.TextMeshProUGUI>();
}

