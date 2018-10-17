using System;
using System.Collections.Generic;
using UnityEngine;

public class ChatSystemClient
{
    public Queue<string> incomingMessages = new Queue<string>();

    int m_LocalTeamIndex;
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
        m_NetworkClient.QueueEvent((ushort)GameNetworkEvents.EventType.Chat, true, (ref NetworkWriter writer) =>
        {
            writer.WriteString("message", message, 256);
        });
    }

    NetworkClient m_NetworkClient;
}