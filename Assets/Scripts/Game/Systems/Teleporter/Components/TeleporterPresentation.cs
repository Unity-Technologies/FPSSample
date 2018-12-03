using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TeleporterPresentation : MonoBehaviour, INetSerialized
{
    [NonSerialized] public int effectTick;
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("effectTick", effectTick);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        effectTick = reader.ReadInt32();
    }
}
