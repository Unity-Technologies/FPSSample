using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSettings 
{
    public string playerName;
    public int characterType;
    public short teamId;

    public void Serialize(ref NetworkWriter writer)
    {
        writer.WriteString("playerName", playerName);
        writer.WriteInt16("characterType", (short)characterType);
        writer.WriteInt16("teamId", teamId);
    }

    public void Deserialize(ref NetworkReader reader)
    {
        playerName = reader.ReadString();
        characterType = reader.ReadInt16();
        teamId = reader.ReadInt16();
    }
}
