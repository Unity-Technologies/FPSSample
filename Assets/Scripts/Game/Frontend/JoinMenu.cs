using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoinMenu : MonoBehaviour
{
    [ConfigVar(Name = "serverlist", Description = "Comma seperated list of commonly used servers", DefaultValue = "localhost", Flags = ConfigVar.Flags.Save)]
    public static ConfigVar serverlist;

    public ScrollRect servers;
    public TMPro.TextMeshProUGUI connectButtonText;
    public Button connectButton;
    public TMPro.TMP_InputField serverAddress;
    public ServerListEntry serverListEntryTemplate;
    public TMPro.TMP_InputField playername;
    public RectTransform serverListContentRect;

    public void Awake()
    {
        serverListEntryTemplateHeight = ((RectTransform)serverListEntryTemplate.transform).rect.height + 10.0f;
        foreach (var s in serverlist.Value.Split(','))
        {
            if(s != "")
                AddServer(s);
        }
        RepositionItems();
        SetSelectedServer(-1);
    }

    public void UpdateMenu()
    {
        // Unless typing, fill in field from configvar
        if (!playername.isFocused)
            playername.text = ClientGameLoop.clientPlayerName.Value;

        // Update all servers in our list
        foreach (var server in m_Servers)
        {
            // Update server list with any new results
            UpdateItem(server);
            if (Time.time > server.nextUpdate && server.sqpQuery.m_State == SQP.SQPClient.SQPClientState.Idle)
            {
                Game.game.sqpClient.StartInfoQuery(server.sqpQuery);
                server.nextUpdate = Time.time + 5.0f + UnityEngine.Random.Range(0.0f, 1.0f);
            }
        }
    }

    public void OnServerItemPointerClick(BaseEventData e)
    {
        var ped = (PointerEventData)e;
        for (int i = 0; i < m_Servers.Count; ++i)
        {
            if (m_Servers[i].listItem.gameObject == ped.pointerPress)
            {
                SetSelectedServer(i);
                if (ped.clickCount == 2)
                    OnJoinGame();
                return;
            }
        }
    }

    public void OnJoinGame()
    {
        Console.EnqueueCommandNoHistory("connect " + connectButtonText.text);
    }

    public void OnAddServer()
    {
        AddServer(serverAddress.text);
        RepositionItems();
        SaveServerlist();
    }

    List<string> m_HostnameList = new List<string>();
    void SaveServerlist()
    {
        m_HostnameList.Clear();
        foreach (var s in m_Servers)
            m_HostnameList.Add(s.hostname);
        serverlist.Value = string.Join(",", m_HostnameList);
        Console.EnqueueCommandNoHistory("saveconfig");
    }

    public void OnRemoveServer()
    {
        if (m_SelectedServer == -1)
            return;

        RemoveServer(m_SelectedServer);
        SetSelectedServer(m_SelectedServer - 1);
        RepositionItems();
        SaveServerlist();
    }

    public void OnNameChanged()
    {
        Console.EnqueueCommandNoHistory("client.playername \"" + playername.text + '"');
    }

    public void OnFindMatch()
    {
        Console.EnqueueCommandNoHistory("matchmake");
    }

    void RemoveServer(int index)
    {
        var o = m_Servers[index];
        Destroy(o.listItem.gameObject);
        m_Servers.RemoveAt(index);
        RepositionItems();
    }

    void RepositionItems()
    {
        for (int i = 0; i < m_Servers.Count; i++)
            PositionItem(m_Servers[i].listItem.gameObject, i);
        var sd = serverListContentRect.sizeDelta;
        sd.y = m_Servers.Count * serverListEntryTemplateHeight;
        serverListContentRect.sizeDelta = sd;
    }

    void AddServer(string hostname)
    {
        for (int i = 0; i < m_Servers.Count; ++i)
        {
            var s = m_Servers[i];
            if (s.hostname == hostname)
            {
                // If already here, just select it
                SetSelectedServer(i);
                return;
            }
        }

        var server = new ServerListItemData();
        server.listItem = GameObject.Instantiate<ServerListEntry>(serverListEntryTemplate, servers.content);
        server.hostname = hostname;
        server.listItem.gameObject.SetActive(true);

        // Create a SQP query
        System.Net.IPAddress addr;
        int port;
        NetworkUtils.EndpointParse(server.hostname, out addr, out port, 0);
        // SQP Port is sqpPortOffset after whatever port we are using for the game itself
        port = (port == 0 ? NetworkConfig.serverPort.IntValue : port) + NetworkConfig.sqpPortOffset;
        server.sqpQuery = Game.game.sqpClient.GetSQPQuery(new System.Net.IPEndPoint(addr, port));

        UpdateItem(server);
        m_Servers.Add(server);
        SetSelectedServer(m_Servers.Count - 1);
    }

    void UpdateItem(ServerListItemData option)
    {
        option.listItem.hostName.text = option.hostname;
        if (option.sqpQuery.validResult)
        {
            var sid = option.sqpQuery.m_ServerInfo.ServerInfoData;
            option.listItem.serverName.text = sid.ServerName ?? "";
            option.listItem.gameMode.text = sid.GameType ?? "";
            option.listItem.numPlayers.Format("{0}/{1}", (int)sid.CurrentPlayers, (int)sid.MaxPlayers);
            option.listItem.pingTime.Format("{0} ms", (int)option.sqpQuery.RTT);
            option.listItem.mapName.text = sid.Map ?? "";
        }
        else
        {
            option.listItem.serverName.text = "--";
            option.listItem.gameMode.text = "--";
            option.listItem.numPlayers.text = "-/-";
            option.listItem.pingTime.text = "--";
            option.listItem.mapName.text = "--";
        }
    }

    void SetSelectedServer(int index)
    {
        if (m_SelectedServer == index)
            return;

        m_SelectedServer = index;

        for (int i = 0; i < m_Servers.Count; i++)
        {
            SetListItemSelected(m_Servers[i], index == i);
        }
        if (index >= 0)
        {
            connectButton.interactable = true;
            connectButtonText.text = m_Servers[index].hostname;
        }
        else
        {
            connectButton.interactable = false;
            connectButtonText.text = "--";
        }
    }

    void SetListItemSelected(ServerListItemData data, bool selected)
    {
        //data.listItem.GetComponent<Image>().color = new Color(1.0f, 1.0f, 1.0f, selected ? 0.3f : 0.1f);
        data.listItem.GetComponent<UIFrame>().enabled = selected;
    }

    void PositionItem(GameObject item, int index)
    {
        var trans = (RectTransform)item.transform;
        trans.localPosition = new Vector3(0.0f, -serverListEntryTemplateHeight * index, 0.0f);
    }

    class ServerListItemData
    {
        public string hostname;
        public ServerListEntry listItem;

        public SQP.SQPClient.SQPQuery sqpQuery;
        public float nextUpdate;
    }


    int m_SelectedServer;
    List<ServerListItemData> m_Servers = new List<ServerListItemData>();
    float serverListEntryTemplateHeight;
}
