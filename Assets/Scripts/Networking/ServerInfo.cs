using System.Collections.Generic;

public class ServerInfo
{
    public uint Token;
    public int Port;
    public string Address;
    public string Name;
    public string LevelName;
    public string GameMode;
    public int Players;
    public int MaxPlayers;
    public int Ping;
    public float LastSeenTime;

    public static readonly List<ServerInfo> EmptyList = new List<ServerInfo>();
}
