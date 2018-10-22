using UnityEngine;
using UnityEngine.Networking;
//using NetworkWriter = UnityEngine.Networking.NetworkWriter;

public class UNETServerBroadcast
{
    public struct GameInfo
    {
        public uint token;
        public string servername;
        public string levelname;
        public string gamemode;
        public int connectedPlayers;
        public int maxPlayers;
    }

    public GameInfo gameInfo;

    public UNETServerBroadcast(int hostId, UNETBroadcastConfig config, int serverPort)
    {
        m_HostId = hostId;
        m_Config = config;
        m_ServerPort = serverPort;

        m_Msg = new byte[1024];
    }

    public void Start()
    {
        UpdateGameInfo();
        m_Broadcasting = true;
        m_PendingStartAttempts = 3;
    }

    public void UpdateBroadcast()
    {
        if(m_PendingStartAttempts > 0)
        {
            --m_PendingStartAttempts;
            StartInternal();
        }
    }

    public void Stop()
    {
        m_Broadcasting = false;
        NetworkTransport.StopBroadcastDiscovery();
        m_PendingStartAttempts = 0;
    }

    public void UpdateGameInfo()
    {
        var writer = new NetworkWriter(m_Msg, null);

        writer.WriteInt32("serverPort", m_ServerPort);
        writer.WriteUInt32("token", gameInfo.token);
        writer.WriteString("servername", gameInfo.servername);
        writer.WriteString("levelname", gameInfo.levelname);
        writer.WriteString("gamemode", gameInfo.gamemode);
        writer.WriteInt32("connectedPlayers", gameInfo.connectedPlayers);
        writer.WriteInt32("maxPlayers", gameInfo.maxPlayers);
        writer.Flush();
        m_MsgSize = writer.GetLength();

        if (m_Broadcasting)
        {
            // TODO : We have to restart the broadcast to update the message but we cannot
            // do this in a single frame since we have to wait for some internal state in UNET
            // to reset, so we try 3 times
            NetworkTransport.StopBroadcastDiscovery();
            m_PendingStartAttempts = 3;
        }
    }

    void StartInternal()
    {
        GameDebug.Assert(m_Broadcasting);

        if (NetworkTransport.IsBroadcastDiscoveryRunning())
            NetworkTransport.StopBroadcastDiscovery();

        byte err;
        if (NetworkTransport.StartBroadcastDiscovery(m_HostId, m_Config.port, m_Config.key, m_Config.version, m_Config.subVersion, m_Msg, m_MsgSize, k_BroadcastInterval, out err))
            m_PendingStartAttempts = 0;
    }

    int m_HostId;
    int m_ServerPort;
    UNETBroadcastConfig m_Config;

    bool m_Broadcasting = false;
    int m_PendingStartAttempts;

    byte[] m_Msg = new byte[1024];
    int m_MsgSize;

    const int k_BroadcastInterval = 2000;
}
