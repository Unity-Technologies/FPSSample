using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour 
{
    [System.Serializable]
    public struct UIBinding
    {
        public GameObject[] menus;

        public TMPro.TextMeshProUGUI buildId;

        // Player menu
        public InputField playername;

        // Join menu
        public ScrollRect servers;
        public Text selectedServer;
        public Button connectButton;
        public InputField serverAddress;

        // Create menu
        public InputField servername;
        public Dropdown gamemode;
        public Dropdown levelname;
        public Dropdown maxplayers;
        public Toggle decidatedServer;
    }

    public UIBinding uiBinding;

    // Currently active submenu, used by menu backdrop to track what is going on
    public int activeSubmenuNumber;

    CanvasGroup m_CanvasGroup;

    public void SetPanelActive(ClientFrontend.MenuShowing menuShowing)
    {
        var active = menuShowing != ClientFrontend.MenuShowing.None;
        gameObject.SetActive(active);
        if(active)
        {
            foreach(var a in GetComponentsInChildren<MenuButton>(true))
            {
                var enabled = a.ingameOption || menuShowing == ClientFrontend.MenuShowing.Main;
                a.gameObject.SetActive(enabled);
            }
            // Close any open menu
            ShowSubMenu(null);
        }
    }

    public bool GetPanelActive()
    {
        return gameObject.activeSelf;
    }

    public void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();

        m_ListItemTemplate = uiBinding.servers.content.GetChild(0).gameObject;

        uiBinding.gamemode.options.Clear();
        uiBinding.gamemode.options.Add(new Dropdown.OptionData("Assault"));
        uiBinding.gamemode.options.Add(new Dropdown.OptionData("Deathmatch"));
        uiBinding.gamemode.RefreshShownValue();

        uiBinding.levelname.options.Clear();
        uiBinding.levelname.options.Add(new Dropdown.OptionData("Level_01"));
        uiBinding.levelname.options.Add(new Dropdown.OptionData("Level_00"));
        uiBinding.levelname.RefreshShownValue();

        uiBinding.maxplayers.options.Clear();
        uiBinding.maxplayers.options.Add(new Dropdown.OptionData("2"));
        uiBinding.maxplayers.options.Add(new Dropdown.OptionData("4"));
        uiBinding.maxplayers.options.Add(new Dropdown.OptionData("8"));
        uiBinding.maxplayers.options.Add(new Dropdown.OptionData("16"));
        uiBinding.maxplayers.RefreshShownValue();

        uiBinding.buildId.text = Game.game.buildId;
    }

    internal void SetAlpha(float v)
    {
        m_CanvasGroup.alpha = v;
    }

    public void UpdateInfo(string playerName, IList<ServerInfo> serverInfos, string gameMessage)
    {
        if(!uiBinding.playername.isFocused)
            uiBinding.playername.text = ClientGameLoop.clientPlayerName.Value;
    }

    // Called from the Menu/Button_* UI.Buttons
    public void ShowSubMenu(GameObject ShowMenu)
    {
        activeSubmenuNumber = 0;
        for(int i = 0; i < uiBinding.menus.Length; i++)
        {
            var menu = uiBinding.menus[i];
            if (menu == ShowMenu)
            {
                menu.SetActive(true);
                activeSubmenuNumber = i;
            }
            else if (menu.activeSelf)
                menu.SetActive(false);
        }
    }

    public void OnNameChanged(UnityEngine.UI.InputField field)
    {
        Console.EnqueueCommand("client.playername \"" + field.text + '"');
    }

    public void OnQuitGame()
    {
        Console.EnqueueCommand("quit");
    }

    public void OnJoinGame()
    {
        Console.EnqueueCommand("connect " + uiBinding.serverAddress.text);
    }

    public void OnFindMatch()
    {
        Console.EnqueueCommand("matchmake");
    }

    public void OnCreateGame()
    {
        var servername = uiBinding.servername.text;

        // TODO : Fix console handling of whitespaces
        servername = servername.Replace(" ", "");

        var levelname = uiBinding.levelname.options[uiBinding.levelname.value].text;

        // TODO : Add commands to set these
        var gamemode = uiBinding.gamemode.options[uiBinding.gamemode.value].text.ToLower();
        var maxplayers = uiBinding.maxplayers.options[uiBinding.maxplayers.value].text;

        var dedicated = uiBinding.decidatedServer.isOn;
        if(dedicated)
        {
            var process = new System.Diagnostics.Process();
            if (Application.isEditor)
            {
                process.StartInfo.FileName = k_AutoBuildPath + "/" + k_AutoBuildExe;
                process.StartInfo.WorkingDirectory = k_AutoBuildPath;
            }
            else
            {
                // TODO : We should look to make this more robust but for now we just
                // kill other processes to avoid running multiple servers locally
                var thisProcess = System.Diagnostics.Process.GetCurrentProcess();
                var processes = System.Diagnostics.Process.GetProcesses();
                foreach (var p in processes)
                {
                    if (p.Id != thisProcess.Id && p.ProcessName == thisProcess.ProcessName)
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch (System.Exception)
                        {
                        }
                    }
                }

                process.StartInfo.FileName = thisProcess.MainModule.FileName;
                process.StartInfo.WorkingDirectory = thisProcess.StartInfo.WorkingDirectory;
                GameDebug.Log(string.Format("filename='{0}', dir='{1}'", process.StartInfo.FileName, process.StartInfo.WorkingDirectory));
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.Arguments = " -batchmode -nographics -consolerestorefocus" +
                                          " +serve " + levelname + " +game.modename " + gamemode.ToLower() +
                                          " +servername " + servername;
            if (process.Start())
            {
                Console.EnqueueCommand("connect localhost");
            }
        }
        else
        {
            Console.EnqueueCommand("serve " + levelname);
            Console.EnqueueCommand("servername " + servername);
        }
    }

    public void OnServerItemPointerClick(BaseEventData e)
    {
        var item = ((PointerEventData)e).pointerPress;
        for (int i = 0; i < m_Options.Count; ++i)
        {
            if (m_Options[i].listItem == item)
            {
                SetSelectedOption(m_Options[i]);
                return;
            }
        }
    }

    OptionData FindOption(ServerInfo info)
    {
        for(int i = 0; i < m_Options.Count; ++i)
        {
            if (m_Options[i].info == info)
                return m_Options[i];
        }
        return null;
    }

    void SetSelectedOption(OptionData option)
    {
        if (m_SelectedOption == null || m_SelectedOption != option)
        {
            if(m_SelectedOption != null)
                SetListItemColor(m_SelectedOption, Color.white);

            m_SelectedOption = option;
            if (m_SelectedOption != null)
            {
                uiBinding.selectedServer.text = m_SelectedOption.info.Address;
                SetListItemColor(m_SelectedOption, new Color(0, 255, 202));
            }
            else
            {
                uiBinding.selectedServer.text = "";
            }
        }
    }

    void SetListItemColor(OptionData data, Color color)
    {
        data.serverNameField.color = color;
        data.serverModeField.color = color;
        data.serverPlayersField.color = color;
        data.serverPingField.color = color;
    }

    void UpdateData(OptionData data)
    {
        if (data.info.LastSeenTime > data.lastUpdated)
        {
            data.serverNameField.text = data.info.Name;
            data.serverModeField.text = data.info.GameMode;
            data.serverPlayersField.text = data.info.Players + "/" + data.info.MaxPlayers;
            data.serverPingField.text = "-";
            data.lastUpdated = data.info.LastSeenTime;
        }
    }

    void PositionItem(GameObject item, int index)
    {
        var trans = (RectTransform)item.transform;
        trans.localPosition = new Vector3(0.0f, -128.0f * index, 0.0f);

        var image = item.GetComponent<Image>();
        var color = image.color;
        color.a = index % 2 == 0 ? 1/16 : 1/8;
        image.color = color;
    }

    class OptionData
    {
        public ServerInfo info;
        public GameObject listItem;

        public float lastUpdated;

        public Text serverNameField;
        public Text serverModeField;
        public Text serverPlayersField;
        public Text serverPingField;
    }

    static readonly string k_AutoBuildPath = "AutoBuild";
    static readonly string k_AutoBuildExe = "AutoBuild.exe";

    GameObject m_ListItemTemplate;

    OptionData m_SelectedOption;
    List<OptionData> m_Options = new List<OptionData>();
}
