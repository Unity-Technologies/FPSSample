using System;
using UnityEngine;

public class UserCommandComponent: MonoBehaviour, INetworkSerializable    
{   
    [NonSerialized] public UserCommand command;
    [NonSerialized] public UserCommand prevCommand;
    
    [NonSerialized] public int resetCommandTick;
    [NonSerialized] public float resetCommandLookYaw;          
    [NonSerialized] public float resetCommandLookPitch = 90;
    [NonSerialized] public int lastResetCommandTick;

    public void ResetCommand(int tick, float lookYaw, float lookPitch)
    {
        resetCommandTick = tick;
        resetCommandLookYaw = lookYaw;
        resetCommandLookPitch = lookPitch;
    }
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("resetCamTick", resetCommandTick);
        writer.WriteFloatQ("lookYaw", resetCommandLookYaw, 1);
        writer.WriteFloatQ("lookPitch", resetCommandLookPitch, 1);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        resetCommandTick = reader.ReadInt32();
        resetCommandLookYaw = reader.ReadFloatQ();
        resetCommandLookPitch = reader.ReadFloatQ();
    }
}
