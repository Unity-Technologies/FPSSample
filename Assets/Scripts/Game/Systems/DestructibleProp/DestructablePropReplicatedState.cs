using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructablePropReplicatedState : MonoBehaviour, INetSerialized
{
    public int destroyedTick;      

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteInt32("destroyed",destroyedTick);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        destroyedTick = reader.ReadInt32();
    }
}
