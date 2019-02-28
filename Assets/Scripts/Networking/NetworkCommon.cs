
using Unity.Networking.Transport;

public struct TransportEvent
{
    public enum Type
    {
        Data,
        Connect,
        Disconnect
    }
    public Type type;
    public int connectionId;
    public byte[] data;
    public int dataSize;
}

public interface INetworkTransport
{
    int Connect(string ip, int port);
    void Disconnect(int connectionId);

    bool NextEvent(ref TransportEvent e);
    void SendData(int connectionId, byte[] data, int sendSize);

    string GetConnectionDescription(int connectionId);

    void Update();

    void Shutdown();
}

public interface INetworkCallbacks
{
    void OnConnect(int clientId);
    void OnDisconnect(int clientId);
    void OnEvent(int clientId, NetworkEvent info);
}

public enum NetworkMessage
{
    // Shared messages
    Events      = 1 << 0,

    // Server -> Client messages
    ClientInfo  = 1 << 1,
    MapInfo     = 1 << 2,
    Snapshot    = 1 << 3,

    // Client -> Server messages
    ClientConfig  = 1 << 1,
    Commands    = 1 << 2,

    FRAGMENT = 1 << 7,   // Special flag used when package has been fragmented into several packages and needs reassembly
}

public static class NetworkConfig
{
    [ConfigVar(Name = "net.stats", DefaultValue = "0", Description = "Show net statistics")]
    public static ConfigVar netStats;
    [ConfigVar(Name = "net.printstats", DefaultValue = "0", Description = "Print stats to console every N frame")]
    public static ConfigVar netPrintStats;
    [ConfigVar(Name = "net.debug", DefaultValue = "0", Description = "Dump lots of debug info about network")]
    public static ConfigVar netDebug;

    [ConfigVar(Name = "server.port", DefaultValue = "7913", Description = "Port listened to by server")]
    public static ConfigVar serverPort;

    [ConfigVar(Name = "server.sqp_port", DefaultValue = "0", Description = "Port used for server query protocol. server.port + 1 if not set")]
    public static ConfigVar serverSQPPort;

    [ConfigVar(Name = "net.chokesendinterval", DefaultValue = "0.3", Description = "If connection is choked, send tiny keep alive packs at this interval")]
    public static ConfigVar netChokeSendInterval;


    // Increase this when you make a change to the protocol
    public const uint protocolVersion = 4;

    public const int defaultServerPort = 7913;
    public static int sqpPortOffset = 10; // By default (if not specified) the SQP be at the server port + this number

    public const int commandServerQueueSize = 32;

    // Number of commands the client stores - also maximum number of predictive steps the client can take
    public const int commandClientBufferSize = 32;

    public const int maxFragments = 16;
    public const int packageFragmentSize = NetworkParameterConstants.MTU - 128;  // 128 is just a random safety distance to MTU
    public const int maxPackageSize = maxFragments * packageFragmentSize;

    // Number of serialized snapshots kept on server. Each server tick generate a snapshot. 
    public const int snapshotDeltaCacheSize = 128;  // Number of snapshots to cache for deltas

    // Size of client ack buffers. These buffers are used to keep track of ack'ed baselines
    // from clients. Theoretically the 'right' size is snapshotDeltaCacheSize / (server.tickrate / client.updaterate)
    // e.g. 128 / (60 / 20) = 128 / 3, but since client.updaterate <= server.tickrate we use
    public const int clientAckCacheSize = snapshotDeltaCacheSize;

    public const int maxEventDataSize = 512;
    public const int maxCommandDataSize = 128;
    public const int maxEntitySnapshotDataSize = 512;
    public const int maxWorldSnapshotDataSize = 64*1024; // The entire world snapshot has to fit in this number of bytes
    public readonly static System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

    public readonly static float[] encoderPrecisionScales = new float[] { 1.0f, 10.0f, 100.0f, 1000.0f };
    public readonly static float[] decoderPrecisionScales = new float[] { 1.0f, 0.1f, 0.01f, 0.001f };


    // compression
    public const NetworkCompression.IOStreamType ioStreamType = NetworkCompression.IOStreamType.Huffman;    //TODO: make this dynamic

    public const int maxFixedSchemaIds = 2;
    public const int maxEventTypeSchemaIds = 8;
    public const int maxEntityTypeSchemaIds = 40;

    public const int networkClientQueueCommandSchemaId = 0;
    public const int mapSchemaId = 1;
    public const int firstEventTypeSchemaId = maxFixedSchemaIds;
    public const int firstEntitySchemaId = maxFixedSchemaIds + maxEventTypeSchemaIds;

    public const int maxSchemaIds = maxFixedSchemaIds + maxEventTypeSchemaIds + maxEntityTypeSchemaIds;

    public const int maxFieldsPerSchema = 128;
    public const int maxContextsPerField = 4;
    public const int maxSkipContextsPerSchema = maxFieldsPerSchema / 4;
    public const int maxContextsPerSchema = maxSkipContextsPerSchema + maxFieldsPerSchema * maxContextsPerField;

    public const int miscContext = 0;
    public const int baseSequenceContext = 1;
    public const int baseSequence1Context = 2;
    public const int baseSequence2Context = 3;
    public const int serverTimeContext = 4;
    public const int schemaCountContext = 5;
    public const int schemaTypeIdContext = 6;
    public const int spawnCountContext = 7;
    public const int idContext = 8;
    public const int spawnTypeIdContext = 9;
    public const int despawnCountContext = 10;
    public const int updateCountContext = 11;
    public const int commandTimeContext = 12;
    public const int eventCountContext = 13;
    public const int eventTypeIdContext = 14;
    public const int skipContext = 15;

    public const int firstSchemaContext = 16;

    public const int maxContexts = firstSchemaContext + maxSchemaIds * maxContextsPerSchema;
}
