using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ChatSystemServer
{
    // TODO : The integration is annoying because we don't have a proper permanent place for player
    // info (world is destroyed for each level). We should try to make this smoother
    public ChatSystemServer(Dictionary<int, ServerGameLoop.ClientInfo> clients, NetworkServer networkServer)
    {
        m_Clients = clients;
        m_NetworkServer = networkServer;
    }

    public void ResetChatTime()
    {
        m_StartTime = Game.Clock.ElapsedMilliseconds;
    }

    char[] _msgBuf = new char[256];
    public void SendChatAnnouncement(string message)
    {
        var c = Mathf.Min(256, message.Length);
        message.CopyTo(0, _msgBuf, 0, c);
        SendChatAnnouncement(new CharBufView(_msgBuf, c));
    }

    char[] _buf = new char[256];
    public void SendChatAnnouncement(CharBufView message)
    {
        var time = (Game.Clock.ElapsedMilliseconds - m_StartTime) / 1000;
        var minutes = (int)time / 60;
        var seconds = (int)time % 60;

        var formatted_length = StringFormatter.Write(ref _buf, 0, "<color=#ffffffff>[{0}:{1:00}]</color><color=#ffa500ff> {2}</color>", minutes, seconds, message);

        m_NetworkServer.QueueEventBroadcast((ushort)GameNetworkEvents.EventType.Chat, true, (ref NetworkWriter writer) =>
        {
            writer.WriteString("message", _buf, formatted_length, 256, NetworkWriter.OverrunBehaviour.WarnAndTrunc);
        });
    }

    public void ReceiveMessage(ServerGameLoop.ClientInfo from, string message)
    {
        ChatMessageType type;
        ServerGameLoop.ClientInfo target;

        var time = (Game.Clock.ElapsedMilliseconds - m_StartTime) / 1000;
        var minutes = time / 60;
        var seconds = time % 60;

        var text = ParseMessage(from, message, out type, out target);
        if (type == ChatMessageType.Whisper)
        {
            if (target != null)
            {
                var fromLine = string.Format("<color=#ffffffff>[{0}:{1:00}]</color><color=#ff00ffff> [From {2}] {3}</color>", minutes, seconds, from.playerSettings.playerName, text);
                SendChatMessage(target.id, fromLine);

                var toLine = string.Format("<color=#ffffffff>[{0}:{1:00}]</color><color=#ff00ffff> [To {2}] {3}</color>", minutes, seconds, target.playerSettings.playerName, text);
                SendChatMessage(from.id, toLine);
            }
            else
                SendChatMessage(from.id, string.Format("<color=#ff0000ff> Player not found</color>"));
        }
        else if(type == ChatMessageType.All || type == ChatMessageType.Team)
        {
            var marker = type == ChatMessageType.All ? "[All] " : "";

            var friendly = string.Format("[{0}:{1:00}] <color=#1D89CC>{2}{3}</color> {4}", minutes, seconds, marker, from.playerSettings.playerName, text);
            var hostile = string.Format("[{0}:{1:00}] <color=#FF3E3E>{2}{3}</color> {4}", minutes, seconds, marker, from.playerSettings.playerName, text);

            var fromTeamIndex = from.player != null ? from.player.teamIndex : -1;
            foreach (var pair in m_Clients)
            {
                var targetTeamIndex = pair.Value.player != null ? pair.Value.player.teamIndex : -1;
                if(fromTeamIndex == targetTeamIndex)
                    SendChatMessage(pair.Key, friendly);
                else if(type == ChatMessageType.All)
                    SendChatMessage(pair.Key, hostile);
            }
        }
    }

    string ParseMessage(ServerGameLoop.ClientInfo from, string message, out ChatMessageType type, out ServerGameLoop.ClientInfo target)
    {
        type = ChatMessageType.All;
        target = null;

        var match = m_CommandRegex.Match(message);
        if (match.Success)
        {
            var command = match.Groups[1].Value.ToLower();
            var actualMessage = match.Groups[2].Value;
            switch (command)
            {
                case "t":
                case "team":
                    type = ChatMessageType.Team;
                    return match.Groups[2].Value;

                case "w":
                case "whisper":
                    var match2 = m_TargetRegex.Match(actualMessage);
                    if (match2.Success)
                    {
                        type = ChatMessageType.Whisper;

                        // try to find client
                        var name = !String.IsNullOrEmpty(match2.Groups[1].Value) ? match2.Groups[1].Value : match2.Groups[2].Value;
                        foreach (var pair in m_Clients)
                        {
                            if (pair.Value.playerSettings.playerName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                            {
                                target = pair.Value;
                                m_ReplyTracker[target] = from;
                                return match2.Groups[3].Value;
                            }
                        }
                    }
                    return actualMessage;

                case "r":
                case "reply":
                    if (m_ReplyTracker.TryGetValue(from, out target))
                    {
                        type = ChatMessageType.Whisper;
                        m_ReplyTracker[target] = from;
                    }
                    return actualMessage;
                case "a":
                case "all":
                default:
                    return actualMessage;
            }
        }
        return message;
    }

    public void SendChatMessage(int clientId, string message)
    {
        m_NetworkServer.QueueEvent(clientId, (ushort)GameNetworkEvents.EventType.Chat, true, (ref NetworkWriter writer) =>
        {
            writer.WriteString("message", message, 256);
        });
    }

    long m_StartTime;

    Regex m_CommandRegex = new Regex(@"^/(\w+)\s+(.*)"); // e.g. "/all hey"
    Regex m_TargetRegex = new Regex(@"^(?:""(.*)""|([^\s]*))\s*(.+)"); // e.g. "some user" hey there

    Dictionary<int, ServerGameLoop.ClientInfo> m_Clients;
    Dictionary<ServerGameLoop.ClientInfo, ServerGameLoop.ClientInfo> m_ReplyTracker = new Dictionary<ServerGameLoop.ClientInfo, ServerGameLoop.ClientInfo>();
    NetworkServer m_NetworkServer;
}
