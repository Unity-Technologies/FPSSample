using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ChatSystemClient
{
    public Queue<string> incomingMessages = new Queue<string>();

    int m_LocalTeamIndex;
    Regex m_CommandRegex = new Regex(@"^/(\w+)\s+(.*)"); // e.g. "/all hey"

    public void UpdateLocalTeamIndex(int teamIndex)
    {
        m_LocalTeamIndex = teamIndex;
    }

    public ChatSystemClient(NetworkClient networkClient)
    {
        m_NetworkClient = networkClient;
    }

    public void ReceiveMessage(string message)
    {
        // TODO (petera) this garbage factory must be killed with fire
        if(m_LocalTeamIndex == 1)
        {
            message = message.Replace("#1EA00001", "#1D89CCFF");
            message = message.Replace("#1EA00000", "#FF3E3EFF");
        }
        if(m_LocalTeamIndex == 0)
        {
            message = message.Replace("#1EA00000", "#1D89CCFF");
            message = message.Replace("#1EA00001", "#FF3E3EFF");
        }
        incomingMessages.Enqueue(message);
    }

    public void SendMessage(string message)
    {
        var match = m_CommandRegex.Match(message);
        if (match.Success)
        {
            var command = match.Groups[1].Value.ToLower();
            var actualMessage = match.Groups[2].Value;
            switch (command)
            {
                case "vteam":
                    VivoxSettings.SendTeamMessage(string.Format("<color=#33FF39>[team] {0}</color> {1}", ClientGameLoop.clientPlayerName.Value, actualMessage));
                    return;
                case "vall":
                    VivoxSettings.SendGlobalMessage(string.Format("<color=#33FF39>[all] {0}</color> {1}", ClientGameLoop.clientPlayerName.Value, actualMessage));
                    return;
                default:
                    break;
            }
        }

        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.Chat, true, (ref NetworkWriter writer) =>
        {
            writer.WriteString("message", message, 256);
        });
    }

    NetworkClient m_NetworkClient;
}