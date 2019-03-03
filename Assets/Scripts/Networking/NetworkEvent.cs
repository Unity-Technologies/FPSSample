using System.Collections.Generic;
using UnityEngine;
using NetworkCompression;

public delegate void NetworkEventGenerator(ref NetworkWriter data);
public delegate void NetworkEventProcessor(ushort typeId, ref NetworkReader data);


public class NetworkEventType
{
    public ushort typeId;
    public NetworkSchema schema;
}


public class NetworkEvent
{
    public int sequence;
    public bool reliable;
    public NetworkEventType type;
    public uint[] data = new uint[NetworkConfig.maxEventDataSize];

    public void AddRef()
    {
        ++m_RefCount;
    }

    public void Release()
    {
        GameDebug.Assert(m_RefCount > 0, "Trying to release an event that has refcount 0 (seq: {0})",sequence);
        if (--m_RefCount == 0)
        {
            if (NetworkConfig.netDebug.IntValue > 0)
                GameDebug.Log("Releasing event " + ((GameNetworkEvents.EventType)this.type.typeId) + ":" + this.sequence);
            s_Pool.Release(this);
        }
    }

    public static NetworkEvent Create(NetworkEventType type, bool reliable = false)
    {
        var result = s_Pool.Allocate();
        GameDebug.Assert(result.m_RefCount == 0);
        result.m_RefCount = 1;
        result.sequence = 0;
        result.reliable = reliable;
        result.type = type;

        return result;
    }

    public unsafe static NetworkEvent Serialize(ushort typeId, bool reliable, Dictionary<ushort,NetworkEventType> eventTypes, NetworkEventGenerator generator)
    {
        bool generateSchema = false;
        NetworkEventType type;
        if (!eventTypes.TryGetValue(typeId, out type))
        {
            generateSchema = true;
            type = new NetworkEventType() { typeId = typeId, schema = new NetworkSchema(NetworkConfig.firstEventTypeSchemaId + typeId) };
            eventTypes.Add(typeId, type);
        }

        var result = Create(type, reliable);
        result.sequence = ++s_Sequence;
        if (NetworkConfig.netDebug.IntValue > 0)
            GameDebug.Log("Serializing event " + ((GameNetworkEvents.EventType)result.type.typeId) + " in seq no: " + result.sequence);

        fixed (uint* data = result.data)
        {
            NetworkWriter writer = new NetworkWriter(data, result.data.Length, type.schema, generateSchema);
            generator(ref writer);
            writer.Flush();
        }
        return result;
    }

    public static int ReadEvents<TInputStream>(Dictionary<ushort,NetworkEventType> eventTypesIn, int connectionId, ref TInputStream input, INetworkCallbacks networkConsumer) where TInputStream : NetworkCompression.IInputStream
    {
        var eventCount = input.ReadPackedUInt(NetworkConfig.eventCountContext);
        for (var eventCounter = 0; eventCounter < eventCount; ++eventCounter)
        {
            var typeId = (ushort)input.ReadPackedUInt(NetworkConfig.eventTypeIdContext);
            var schemaIncluded = input.ReadRawBits(1) != 0;
            if (schemaIncluded)
            {
                var eventType = new NetworkEventType() { typeId = typeId };
                eventType.schema = NetworkSchema.ReadSchema(ref input);

                if (!eventTypesIn.ContainsKey(typeId))
                    eventTypesIn.Add(typeId, eventType);
            }

            // TODO (petera) do we need to Create an info (as we are just releasing it right after?)
            var type = eventTypesIn[typeId];
            var info = Create(type);
            NetworkSchema.CopyFieldsToBuffer(type.schema, ref input, info.data);
            if (NetworkConfig.netDebug.IntValue > 0)
                GameDebug.Log("Received event " + ((GameNetworkEvents.EventType)info.type.typeId + ":" + info.sequence));

            networkConsumer.OnEvent(connectionId, info);

            info.Release();
        }
        return (int)eventCount;
    }

    unsafe public static void WriteEvents<TOutputStream>(List<NetworkEvent> events, List<NetworkEventType> knownEventTypes, ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
    {
        output.WritePackedUInt((uint)events.Count, NetworkConfig.eventCountContext);
        foreach (var info in events)
        {
            // Write event schema if the client haven't acked this event type
            output.WritePackedUInt(info.type.typeId, NetworkConfig.eventCountContext);
            if (!knownEventTypes.Contains(info.type))
            {
                output.WriteRawBits(1, 1);
                NetworkSchema.WriteSchema(info.type.schema, ref output);
            }
            else
                output.WriteRawBits(0, 1);

            // Write event data
            fixed (uint* data = info.data)
            {
                NetworkSchema.CopyFieldsFromBuffer(info.type.schema, data, ref output);
            }
        }
    }

    int m_RefCount;
    static int s_Sequence;
    static NetworkObjectPool<NetworkEvent> s_Pool = new NetworkObjectPool<NetworkEvent>(100);
}
