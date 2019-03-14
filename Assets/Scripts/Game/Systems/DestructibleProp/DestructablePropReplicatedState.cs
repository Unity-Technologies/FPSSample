using System;
using Unity.Entities;

[Serializable]
public struct DestructablePropReplicatedData : IComponentData, IReplicatedComponent
{
    public int destroyedTick;      

    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<DestructablePropReplicatedData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt32("destroyed",destroyedTick);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        destroyedTick = reader.ReadInt32();
    }
}

public class DestructablePropReplicatedState : ComponentDataProxy<DestructablePropReplicatedData>
{
}
