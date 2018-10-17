using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ClientFrontend : MonoBehaviour
{
    public ScoreBoard scoreboardPanel;
    public GameScore gameScorePanel;
    public CountDown countDownPanel;
    public MainMenu mainMenu;
    public ChatPanel chatPanel;
    public ServerPanel serverPanel;
    public bool m_ShowScorePanel;

    public SoundDef uiHighlightSound;
    public SoundDef uiSelectSound;
    public SoundDef uiSelectLightSound;
    public SoundDef uiCloseSound;

    Canvas m_ScoreboardPanelCanvas;
    Canvas m_GameScorePanelCanvas;
    Canvas m_CountDownPanelCanvas;
    Canvas m_ChatPanelCanvas;

    public enum MenuShowing
    {
        None,
        Main,
        Ingame
    }

    Interpolator m_MenuFader = new Interpolator(0.0f, Interpolator.CurveType.SmoothStep);

    public MenuShowing menuShowing { get; private set; } = MenuShowing.None;

    // Audio for menus. Called from events on the ui elements
    public void OnHighlight() { Game.SoundSystem.Play(uiHighlightSound); }
    public void OnSelect() { Game.SoundSystem.Play(uiSelectSound); }
    public void OnClose() { Game.SoundSystem.Play(uiCloseSound); }

    void Awake()
    {
        m_ScoreboardPanelCanvas = scoreboardPanel.GetComponent<Canvas>();
        m_GameScorePanelCanvas = gameScorePanel.GetComponent<Canvas>();
        m_CountDownPanelCanvas = countDownPanel.GetComponent<Canvas>();
        m_ChatPanelCanvas = chatPanel.GetComponent<Canvas>();
        Clear();
    }

    public void Clear()
    {
        scoreboardPanel.SetPanelActive(false);
        gameScorePanel.SetPanelActive(false);
        countDownPanel.SetPanelActive(false);
        mainMenu.SetPanelActive(MenuShowing.None);
        chatPanel.SetPanelActive(true); // active always as it has its own display/hide logic
        serverPanel.SetPanelActive(false);
    }

    public void ShowMenu(MenuShowing show, float fadeTime = 0.0f)
    {
        if (menuShowing == show)
            return;
        menuShowing = show;
        m_MenuFader.MoveTo(show != MenuShowing.None ? 1.0f : 0.0f, fadeTime);
        if (menuShowing != MenuShowing.None)
            Game.SoundSystem.Play(uiSelectLightSound);
        else
            Game.SoundSystem.Play(uiCloseSound);
    }

    public void UpdateGame()
    {
        // Show/Hide fully for debug purposes
        var show = IngameHUD.showHud.IntValue > 0;
        if (m_ChatPanelCanvas.enabled != show)
        {
            m_ScoreboardPanelCanvas.enabled = show;
            m_GameScorePanelCanvas.enabled = show;
            m_CountDownPanelCanvas.enabled = show;
            m_ChatPanelCanvas.enabled = show;
        }

        // Toggle menu if not in editor
        if(!Application.isEditor && Input.GetKeyUp(KeyCode.Escape))
        {
            if (menuShowing == MenuShowing.None)
            {
                // What menu should we show?
                // Show main menu if no level loaded or menu level loaded
                if (Game.game.levelManager.currentLevel == null || Game.game.levelManager.currentLevel.name == "level_menu")
                    Console.EnqueueCommandNoHistory("menu 1 0.2");
                else
                    Console.EnqueueCommandNoHistory("menu 2 0.2");
                GameDebug.Log("lvl: " + Game.game.levelManager.currentLevel.name);
            }
            else
            {
                Console.EnqueueCommandNoHistory("menu 0 0.2");
                Game.RequestMousePointerLock();
            }
        }

        // Fade main menu
        var fade = m_MenuFader.GetValue();
        var active = fade > 0.0f;
        if (mainMenu.GetPanelActive() != active)
            mainMenu.SetPanelActive(menuShowing);
        if (active)
            mainMenu.SetAlpha(fade);
    }

    public void UpdateMenu(string playerName, IList<ServerInfo> serverInfos, string gameMessage)
    {
        if (mainMenu.GetPanelActive())
            mainMenu.UpdateInfo(playerName, serverInfos, gameMessage);
    }

    public void UpdateChat(ChatSystemClient chatSystem)
    {
        chatPanel.Tick(chatSystem);
    }

    // Force showing of score board e.g. when dead
    public void SetShowScorePanel(bool showScorePanel)
    {
        m_ShowScorePanel = showScorePanel;
    }

    public void UpdateIngame(GameMode gameMode, LocalPlayer localPlayer)
    {
        var playerState = localPlayer.playerState;

        // Countdown
        countDownPanel.SetPanelActive(playerState.displayCountDown);
        if (playerState.displayCountDown)
            countDownPanel.levelInfoCounter.Format("{0}", playerState.countDown);

        // Scoreboard
        scoreboardPanel.SetPanelActive(!playerState.displayCountDown && (playerState.displayScoreBoard || Game.Input.GetKey(KeyCode.Tab) || m_ShowScorePanel));

        // Game score panel
        gameScorePanel.SetPanelActive(playerState.displayGameScore);
    }
}

class ClientFrontendUpdate : BaseComponentSystem
{
    ComponentGroup m_gameModeGroup;
    ComponentGroup m_localPlayerGroup;

    public ClientFrontendUpdate(GameWorld world) : base(world)
    {
    }

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        m_gameModeGroup = GetComponentGroup(typeof(GameMode));
        m_localPlayerGroup = GetComponentGroup(typeof(LocalPlayer));
    }

    protected override void OnUpdate()
    {
        var gameModeArray = m_gameModeGroup.GetComponentArray<GameMode>();
        if (gameModeArray.Length == 0)
            return;

        GameDebug.Assert(gameModeArray.Length == 1, "There should only be one gamemode. Found:{0}",
            gameModeArray.Length);

        var localPlayerArray = m_localPlayerGroup.GetComponentArray<LocalPlayer>();
        GameDebug.Assert(localPlayerArray.Length == 1, "There should only be one localplayer. Found:{0}",
            localPlayerArray.Length);

        var gameMode = gameModeArray[0];
        var localPlayer = localPlayerArray[0];

        Game.game.clientFrontend.UpdateIngame(gameMode, localPlayer);
    }

}



