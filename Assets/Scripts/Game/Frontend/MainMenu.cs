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

        // Create menu
        public TMPro.TMP_InputField servername;
        public TMPro.TMP_Dropdown gamemode;
        public TMPro.TMP_Dropdown levelname;
        public TMPro.TMP_Dropdown maxplayers;
        public Toggle decidatedServer;
    }

    public UIBinding uiBinding;
    public JoinMenu joinMenu;
    public OptionsMenu optionMenu;

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
                var enabled = (a.ingameOption && menuShowing == ClientFrontend.MenuShowing.Ingame) || (a.mainmenuOption && menuShowing == ClientFrontend.MenuShowing.Main);
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

        uiBinding.gamemode.options.Clear();
        uiBinding.gamemode.options.Add(new TMPro.TMP_Dropdown.OptionData("Assault"));
        uiBinding.gamemode.options.Add(new TMPro.TMP_Dropdown.OptionData("Deathmatch"));
        uiBinding.gamemode.RefreshShownValue();

        uiBinding.levelname.options.Clear();
        uiBinding.levelname.options.Add(new TMPro.TMP_Dropdown.OptionData("Level_01"));
        uiBinding.levelname.options.Add(new TMPro.TMP_Dropdown.OptionData("Level_00"));
        uiBinding.levelname.RefreshShownValue();

        uiBinding.maxplayers.options.Clear();
        uiBinding.maxplayers.options.Add(new TMPro.TMP_Dropdown.OptionData("2"));
        uiBinding.maxplayers.options.Add(new TMPro.TMP_Dropdown.OptionData("4"));
        uiBinding.maxplayers.options.Add(new TMPro.TMP_Dropdown.OptionData("8"));
        uiBinding.maxplayers.options.Add(new TMPro.TMP_Dropdown.OptionData("16"));
        uiBinding.maxplayers.RefreshShownValue();

        uiBinding.buildId.text = Game.game.buildId;
    }

    public void UpdateMenus()
    {
        if(joinMenu.gameObject.activeInHierarchy)
            joinMenu.UpdateMenu();

        if(optionMenu.gameObject.activeInHierarchy)
            optionMenu.UpdateMenu();
    }

    internal void SetAlpha(float v)
    {
        m_CanvasGroup.alpha = v;
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

    public void OnQuitGame()
    {
        Console.EnqueueCommand("quit");
    }

    public void OnLeaveGame()
    {
        Console.EnqueueCommand("disconnect");
    }

    public void OnCreateGame()
    {
        var servername = uiBinding.servername.text;

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
            process.StartInfo.Arguments = " -batchmode -nographics -noboot -consolerestorefocus" +
                                          " +serve " + levelname + " +game.modename " + gamemode.ToLower() +
                                          " +servername \"" + servername + "\"";
            if (process.Start())
            {
                Console.EnqueueCommand("connect localhost");
            }
        }
        else
        {
            Console.EnqueueCommand("serve " + levelname);
            Console.EnqueueCommand("servername \"" + servername + "\"");
        }
    }

    static readonly string k_AutoBuildPath = "AutoBuild";
    static readonly string k_AutoBuildExe = "AutoBuild.exe";

}
