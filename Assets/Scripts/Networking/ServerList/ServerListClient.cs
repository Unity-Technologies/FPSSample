using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ServerListClient
{
    public ServerListConfig Config { get; }

    public List<ServerInfo> KnownServers { get; private set; }

    private float nextUpdate;

    public ServerListClient(ServerListConfig config)
    {
        this.Config = config;
        this.KnownServers = new List<ServerInfo>();
        this.nextUpdate = Time.time;
    }

    public void UpdateKnownServers()
    {
        // This is called every cycle. Use the configured period to throttle the number of REST calls being made offbox
        float now = Time.time;
        if (m_WebRequestAsyncOp == null)
        {
            if (now > nextUpdate)
            {
                nextUpdate = now + this.Config.Period;
                StartGetRequest(this.Config.Url);
            }
        }
        else if (!m_WebRequestAsyncOp.isDone)
        {
            return;
        }
        else
        {
            var response = ProcessRequestReponse();

            // Add or update servers 
            foreach (ServerListInfo serverInfo in response.Servers)
            {
                // Check if server info already known
                var idx = -1;
                for(var i = 0; i < KnownServers.Count; i++)
                {
                    if(KnownServers[i].Address == serverInfo.Ip && KnownServers[i].Port == serverInfo.Port)
                    {
                        idx = i;
                        break;
                    }
                }

                // .. if not create one
                if (idx == -1)
                {
                    this.KnownServers.Add(new ServerInfo());
                    idx = this.KnownServers.Count - 1;
                }
                var s = this.KnownServers[idx];
                s.Address = serverInfo.Ip;
                s.Port = serverInfo.Port;
                s.Name = serverInfo.Name;
                s.LevelName = serverInfo.Map;
                s.GameMode = serverInfo.Description;
                s.Players = serverInfo.PlayerCount;
                s.MaxPlayers = serverInfo.MaxPlayerCount;
                s.LastSeenTime = now;
            }

            // Remove servers that wasn't in the response
            for (var i = this.KnownServers.Count - 1; i > 0; --i)
            {
                if (this.KnownServers[i].LastSeenTime < now)
                    this.KnownServers.RemoveAt(i);
            }
        }
    }

    private UnityWebRequestAsyncOperation m_WebRequestAsyncOp;
    private UnityWebRequest m_Request;

    private void StartGetRequest(string url)
    {
        m_Request = UnityWebRequest.Get(url);
        m_Request.downloadHandler = new DownloadHandlerBuffer();
        m_WebRequestAsyncOp = m_Request.SendWebRequest();
    }

    private ServerListResponse ProcessRequestReponse()
    {
        if (m_Request.isNetworkError || m_Request.isHttpError || m_Request.isNetworkError)
        {
            GameDebug.LogError("There was an error calling server list. Error: " + m_WebRequestAsyncOp.webRequest.error);
            return new ServerListResponse(); // TODO: What is the current methodolgy for handling exceptions and errors?
        }
        m_WebRequestAsyncOp = null;

        return JsonUtility.FromJson<ServerListResponse>(m_Request.downloadHandler.text);
    }

#pragma warning disable 0649 // unassigned variables
    [Serializable]
    private class ServerListResponse
    {
        public int Skip;
        public int Take;
        public int Total;
        public List<ServerListInfo> Servers;
    }
#pragma warning restore

#pragma warning disable 0649 // unassigned variables
    [Serializable]
    private class ServerListInfo
    {
        public string Id;
        public string Ip;
        public int Port;
        public string Name;
        public string Description;
        public string Map;
        public int PlayerCount;
        public int MaxPlayerCount;
        public string Custom;
    }
#pragma warning restore
}
