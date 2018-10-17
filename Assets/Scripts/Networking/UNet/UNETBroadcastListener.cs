using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class UNETBroadcastListener
{
    public UNETBroadcastConfig config;

    public UNETBroadcastListener()
    {
    }

    public bool Init()
    {
        NetworkTransport.Init();

        var connectionConfig = new ConnectionConfig();
        var topology = new HostTopology(connectionConfig, 16);

        // Start listening for broadcasts from servers
        byte error;
        m_BroadcastHostId = NetworkTransport.AddHost(topology, config.port);
        if (m_BroadcastHostId != -1)
        {
            NetworkTransport.SetBroadcastCredentials(m_BroadcastHostId, config.key, config.version, config.subVersion, out error);
            Console.AddCommand("servers", CmdServers, "servers: list known servers on lan", this.GetHashCode());
            return true;
        }
        else
        {
            GameDebug.Log("ERROR : Could not setup server broadcast listener!");
            return false;
        }
    }

    public void Shutdown()
    {
        Console.RemoveCommandsWithTag(this.GetHashCode());

        if (m_BroadcastHostId != -1)
        {
            NetworkTransport.RemoveHost(m_BroadcastHostId);
            m_BroadcastHostId = -1;
        }
    }

    public void ProcessBroadcasts()
    {
        int connectionId;
        int channelId;
        int receivedSize;
        byte error;

        while (m_BroadcastHostId > -1)
        {
            UnityEngine.Networking.NetworkEventType ne = NetworkTransport.ReceiveFromHost(m_BroadcastHostId, out connectionId, out channelId, m_Buffer, m_Buffer.Length, out receivedSize, out error);

            if (ne == UnityEngine.Networking.NetworkEventType.Nothing)
                break;

            switch (ne)
            {
                case UnityEngine.Networking.NetworkEventType.BroadcastEvent:
                    {
                        int _port;
                        string address;
                        NetworkTransport.GetBroadcastConnectionInfo(m_BroadcastHostId, out address, out _port, out error);
                        NetworkTransport.GetBroadcastConnectionMessage(m_BroadcastHostId, m_Buffer, m_Buffer.Length, out receivedSize, out error);

                        var reader = new NetworkReader(m_Buffer, null);

                        int port = reader.ReadInt32();
                        uint token = reader.ReadUInt32();
                        string servername = reader.ReadString();
                        string levelname = reader.ReadString();
                        string gamemode = reader.ReadString();
                        int connectedPlayers = reader.ReadInt32();
                        int maxPlayers = reader.ReadInt32();

                        if (address.StartsWith("::ffff:"))
                            address = address.AfterFirst("::ffff:");
                        var key = address + ":" + port;
                        ServerInfo si = null;
                        for (var i = 0; i < m_KnownServers.Count; i++)
                        {
                            if (m_KnownServers[i].Address == key)
                            {
                                si = m_KnownServers[i];
                                break;
                            }
                        }
                        if (si == null)
                        {
                            si = new ServerInfo();
                            m_KnownServers.Add(si);
                            GameDebug.Log("New server: " + servername + " " + address + ":" + port + "  _port: " + _port);
                        }
                        si.Token = token;
                        si.Name = servername;
                        si.LevelName = levelname;
                        si.GameMode = gamemode;
                        si.Players = connectedPlayers;
                        si.MaxPlayers = maxPlayers;
                        si.Address = key;
                        si.LastSeenTime = Time.time;
                    }
                    break;
            }
        }
        for (var i = m_KnownServers.Count - 1; i >= 0; --i)
        {
            if (m_KnownServers[i].LastSeenTime < Time.time - 5.0f)
            {
                m_KnownServers.RemoveAt(i);
            }
        }
    }

    public List<ServerInfo> GetKnownServers()
    {
        return m_KnownServers;
    }

    void CmdServers(string[] args)
    {
        if (m_KnownServers.Count == 0)
        {
            Console.Write("No servers");
            return;
        }
        for (var i = 0; i < m_KnownServers.Count; i++)
        {
            var s = m_KnownServers[i];
            Console.Write(s.Name + " " + s.Address);
        }
    }

    int m_BroadcastHostId;
    byte[] m_Buffer = new byte[1024];
    List<ServerInfo> m_KnownServers = new List<ServerInfo>();
}

