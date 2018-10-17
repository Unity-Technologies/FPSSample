using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO (petera) Rename this to GameModeState or something even better

// This is data is replicated to the clients about the 'global' state of
// the game mode, scores etc.

public class GameMode : MonoBehaviour, INetworkSerializable
{
    public int gameTimerSeconds;
    public string gameTimerMessage;
    public string teamName0;
    public string teamName1;
    public int teamScore0;
    public int teamScore1;

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("gameTimerSeconds", gameTimerSeconds);
        writer.WriteString("gameTimerMessage", gameTimerMessage);

        writer.WriteString("teamName0", teamName0);
        writer.WriteString("teamName1", teamName1);
        writer.WriteInt32("teamScore0", teamScore0);
        writer.WriteInt32("teamScore1", teamScore1);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        gameTimerSeconds = reader.ReadInt32();
        gameTimerMessage = reader.ReadString();

        teamName0 = reader.ReadString();
        teamName1 = reader.ReadString();
        teamScore0 = reader.ReadInt32();
        teamScore1 = reader.ReadInt32();
    }
}
