using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;

[DisableAutoCreation]
public class GameModeSystemClient : ComponentSystem
{
    ComponentGroup PlayersGroup;
    ComponentGroup GameModesGroup;

    public GameModeSystemClient(GameWorld world)
    {
        if (Game.game.clientFrontend != null)
        {
            Game.game.clientFrontend.scoreboardPanel.uiBinding.Clear(); ;
            Game.game.clientFrontend.gameScorePanel.Clear();
        }
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        PlayersGroup = GetComponentGroup(typeof(PlayerState));
        GameModesGroup = GetComponentGroup(typeof(GameMode));
    }

    public void Shutdown()
    {
        if (Game.game.clientFrontend != null)
        {
            Game.game.clientFrontend.scoreboardPanel.uiBinding.Clear(); ;
            Game.game.clientFrontend.gameScorePanel.Clear();
        }
    }

    // TODO : We need to fix up these dependencies
    public void SetLocalPlayerId(int playerId)
    {
        m_LocalPlayerId = playerId;
    }

    protected override void OnUpdate()
    {
        if (Game.game.clientFrontend == null)
            return;

        var scoreboardUI = Game.game.clientFrontend.scoreboardPanel.uiBinding;
        var overlayUI = Game.game.clientFrontend.gameScorePanel;

        var playerStateArray = PlayersGroup.GetComponentArray<PlayerState>();
        var gameModeArray = GameModesGroup.GetComponentArray<GameMode>();
        
        // Update individual player stats

        // Use these indexes to fill up each of the team lists
        var scoreBoardPlayerIndexes = new int[scoreboardUI.teams.Length];

        for(int i = 0, c = playerStateArray.Length; i < c; ++i)
        {
            var player = playerStateArray[i];
            var teamIndex = player.teamIndex;

            // TODO (petera) this feels kind of hacky
            if (player.playerId == m_LocalPlayerId)
                m_LocalPlayer = player;

            var teamColor = Color.white;
            int scoreBoardColumn = 0;
            if(m_LocalPlayer)
            {
                bool friendly = teamIndex == m_LocalPlayer.teamIndex;
                teamColor = friendly ? Game.game.gameColors[(int)Game.GameColor.Friend]: Game.game.gameColors[(int)Game.GameColor.Enemy];
                scoreBoardColumn = friendly ? 0 : 1;
            }

            var idx = scoreBoardPlayerIndexes[scoreBoardColumn]++;

            var column = scoreboardUI.teams[scoreBoardColumn];

            // If too few, 
            while(idx >= column.playerScores.Count)
            {
                var entry = GameObject.Instantiate<TMPro.TextMeshProUGUI>(column.playerScoreTemplate, column.playerScoreTemplate.transform.parent);
                entry.gameObject.SetActive(true);
                var trans = (RectTransform)entry.transform;
                var tempTrans = ((RectTransform)column.playerScoreTemplate.transform);
                trans.localPosition = tempTrans.localPosition - new Vector3(0, tempTrans.rect.height * idx, 0);
                column.playerScores.Add(entry);
            }

            if (player.score != -1)
                column.playerScores[idx].Format("{0} : {1}", player.playerName, player.score);
            else
                column.playerScores[idx].Format("{0}", player.playerName);

            column.playerScores[idx].color = teamColor;

            if (player.controlledEntity != Entity.Null)
            {
                if (EntityManager.HasComponent<Character>(player.controlledEntity))
                {
                    var character = EntityManager.GetComponentObject<Character>(player.controlledEntity);
                    character.teamId = player.teamIndex;
                }
            } 
        }

        
        // Clear all member text fields that was not used
        for (var teamIndex = 0; teamIndex < scoreboardUI.teams.Length; teamIndex++)
        {
            var numPlayers = scoreBoardPlayerIndexes[teamIndex];
            var column = scoreboardUI.teams[teamIndex];
            for(var i = column.playerScores.Count - 1; i >= numPlayers; --i)
            {
                GameObject.Destroy(column.playerScores[i].gameObject);
                column.playerScores.RemoveAt(i);
            }
        }

        if (m_LocalPlayer == null)
            return;
        
        // Update gamemode overlay
        GameDebug.Assert(gameModeArray.Length < 2);
        var gameMode = gameModeArray.Length > 0 ? gameModeArray[0] : null;
        if(gameMode != null)
        {
            if (m_LocalPlayer.displayGameResult)
            {
                overlayUI.message.text = m_LocalPlayer.gameResult;
            }
            else
                overlayUI.message.text = "";

            var timeLeft = System.TimeSpan.FromSeconds(gameMode.gameTimerSeconds);

            overlayUI.timer.Format("{0}:{1:00}", timeLeft.Minutes, timeLeft.Seconds);
            overlayUI.timerMessage.text = gameMode.gameTimerMessage;
            overlayUI.objective.text = m_LocalPlayer.goalString;
            overlayUI.SetObjectiveProgress(m_LocalPlayer.goalCompletion, (int)m_LocalPlayer.goalAttackers, (int)m_LocalPlayer.goalDefenders, Game.game.gameColors[m_LocalPlayer.goalDefendersColor], Game.game.gameColors[m_LocalPlayer.goalAttackersColor]);
        }

        overlayUI.action.text = m_LocalPlayer.actionString;

        if(gameMode.teamScore0 >= 0 && gameMode.teamScore1 >= 0)
        {
            var friendColor = Game.game.gameColors[(int)Game.GameColor.Friend];
            var enemyColor = Game.game.gameColors[(int)Game.GameColor.Enemy];
            overlayUI.team1Score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore0 : gameMode.teamScore1);
            overlayUI.team1Score.color = friendColor;
            overlayUI.team2Score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore1 :  gameMode.teamScore0);
            overlayUI.team2Score.color = enemyColor;
        }
        scoreboardUI.teams[0].score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore0 : gameMode.teamScore1);
        scoreboardUI.teams[1].score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore1 : gameMode.teamScore0);
        scoreboardUI.teams[0].name.text = m_LocalPlayer.teamIndex == 0 ? gameMode.teamName0 : gameMode.teamName1;
        scoreboardUI.teams[1].name.text = m_LocalPlayer.teamIndex == 0 ? gameMode.teamName1 : gameMode.teamName0;
    }

    int m_LocalPlayerId;
    PlayerState m_LocalPlayer;
}