using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[DisableAutoCreation]
public class GameModeSystemClient : ComponentSystem
{
    ComponentGroup PlayersGroup;
    ComponentGroup GameModesGroup;   

    
    public GameModeSystemClient(GameWorld world, ScoreboardUIBinding scoreboard, GameScore overlay)
    {
        m_ScoreboardUI = scoreboard;
        m_ScoreboardUI.Clear();

        m_OverlayUI = overlay;
        m_OverlayUI.Clear();
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        PlayersGroup = GetComponentGroup(typeof(PlayerState));
        GameModesGroup = GetComponentGroup(typeof(GameMode));
    }

    public void Shutdown()
    {
        m_ScoreboardUI.Clear();
        m_OverlayUI.Clear();
    }

    // TODO : We need to fix up these dependencies
    public void SetLocalPlayerId(int playerId)
    {
        m_LocalPlayerId = playerId;
    }

    protected override void OnUpdate()
    {
        var playerStateArray = PlayersGroup.GetComponentArray<PlayerState>();
        var gameModeArray = GameModesGroup.GetComponentArray<GameMode>();
        
        // Update individual player stats

        // Use these indexes to fill up each of the team lists
        var scoreBoardPlayerIndexes = new int[m_ScoreboardUI.teams.Length];

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

            if(idx < m_ScoreboardUI.teams[scoreBoardColumn].players.Length)  
            {
                if (player.score != -1)
                    m_ScoreboardUI.teams[scoreBoardColumn].players[idx].name.Format("{0} : {1}", player.playerName, player.score);
                else
                    m_ScoreboardUI.teams[scoreBoardColumn].players[idx].name.Format("{0}", player.playerName);

                m_ScoreboardUI.teams[scoreBoardColumn].players[idx].name.color = teamColor;
            }

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
        for (var teamIndex = 0; teamIndex < m_ScoreboardUI.teams.Length; teamIndex++)
        {
            for (var idx = scoreBoardPlayerIndexes[teamIndex]; idx < m_ScoreboardUI.teams[teamIndex].players.Length; idx++)
            {
                m_ScoreboardUI.teams[teamIndex].players[idx].name.text = "";
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
                m_OverlayUI.message.text = m_LocalPlayer.gameResult;
            }
            else
                m_OverlayUI.message.text = "";

            var timeLeft = System.TimeSpan.FromSeconds(gameMode.gameTimerSeconds);

            m_OverlayUI.timer.Format("{0}:{1:00}", timeLeft.Minutes, timeLeft.Seconds);
            m_OverlayUI.timerMessage.text = gameMode.gameTimerMessage;
            m_OverlayUI.objective.text = m_LocalPlayer.goalString;
            m_OverlayUI.SetObjectiveProgress(m_LocalPlayer.goalCompletion, (int)m_LocalPlayer.goalAttackers, (int)m_LocalPlayer.goalDefenders, Game.game.gameColors[m_LocalPlayer.goalDefendersColor], Game.game.gameColors[m_LocalPlayer.goalAttackersColor]);
        }

        m_OverlayUI.action.text = m_LocalPlayer.actionString;

        if(gameMode.teamScore0 >= 0 && gameMode.teamScore1 >= 0)
        {
            var friendColor = Game.game.gameColors[(int)Game.GameColor.Friend];
            var enemyColor = Game.game.gameColors[(int)Game.GameColor.Enemy];
            m_OverlayUI.team1Score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore0 : gameMode.teamScore1);
            m_OverlayUI.team1Score.color = friendColor;
            m_OverlayUI.team2Score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore1 :  gameMode.teamScore0);
            m_OverlayUI.team2Score.color = enemyColor;
        }
        m_ScoreboardUI.teams[0].score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore0 : gameMode.teamScore1);
        m_ScoreboardUI.teams[1].score.Format("{0}", m_LocalPlayer.teamIndex == 0 ? gameMode.teamScore1 : gameMode.teamScore0);
        m_ScoreboardUI.teams[0].name.text = m_LocalPlayer.teamIndex == 0 ? gameMode.teamName0 : gameMode.teamName1;
        m_ScoreboardUI.teams[1].name.text = m_LocalPlayer.teamIndex == 0 ? gameMode.teamName1 : gameMode.teamName0;
    }

    int m_LocalPlayerId;
    PlayerState m_LocalPlayer;

    ScoreboardUIBinding m_ScoreboardUI;
    GameScore m_OverlayUI;
}