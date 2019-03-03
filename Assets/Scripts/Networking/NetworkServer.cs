using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NetworkCompression;
using UnityEngine.Profiling;
using Unity.Collections.LowLevel.Unsafe;

public interface ISnapshotGenerator
{
    int WorldTick { get; }
    void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer);
    string GenerateEntityName(int entityId);
}

public interface IClientCommandProcessor
{
    void ProcessCommand(int connectionId, int tick, ref NetworkReader data);
}

unsafe public class NetworkServer
{
    [ConfigVar(Name = "server.debug", DefaultValue = "0", Description = "Enable debug printing of server handshake etc.", Flags = ConfigVar.Flags.None)]
    public static ConfigVar serverDebug;

    [ConfigVar(Name = "server.debugentityids", DefaultValue = "0", Description = "Enable debug printing entity id recycling.", Flags = ConfigVar.Flags.None)]
    public static ConfigVar serverDebugEntityIds;

    // Each client needs to receive this on connect and when any of the values changes
    public class ServerInfo
    {
        public int serverTickRate;
        public NetworkCompressionModel compressionModel = NetworkCompressionModel.DefaultModel;
    }

    /// <summary>
    /// The game time on the server
    /// </summary>

    public int serverTime { get; private set; }

    // Used for stats
    public float serverSimTime { get { return m_ServerSimTime; } }

    // TODO (petera) remove this. 
    // We need to split ClientInfo (tickrate etc.) from the connection
    // handshake (protocol version etc.)
    public void UpdateClientInfo()
    {
        serverInfo.serverTickRate = Game.serverTickRate.IntValue;

        foreach (var pair in m_Connections)
            pair.Value.clientInfoAcked = false;
    }

    public class Counters : NetworkConnectionCounters
    {
        public int snapshotsOut;
        public int commandsIn;
    }

    List<Counters> m_Counters = new List<Counters>();
    public List<Counters> GetCounters()
    {
        // Gather counters from connections
        m_Counters.Clear();
        foreach (var pair in m_Connections)
        {
            m_Counters.Add(pair.Value.counters);
        }

        return m_Counters;
    }
    public int NumEntities { get { return m_Entities.Count - m_FreeEntities.Count; } }

    public delegate void DataGenerator(ref NetworkWriter writer);
    public delegate void SnapshotGenerator(int entityId, ref NetworkWriter writer);
    public delegate void CommandProcessor(int time, ref NetworkReader reader);
    public delegate void EventProcessor(ushort typeId, ref NetworkReader data);
    public delegate string EntityTypeNameGenerator(int typeId);

    [ConfigVar(Name = "server.dump_client_streams", DefaultValue = "0", Description = "Store client streams raw in files on server")]
    public static ConfigVar dump_client_streams;
    [ConfigVar(Name = "server.print_senddata_time", DefaultValue = "0", Description = "Print average server time spent in senddata")]
    public static ConfigVar print_senddata_time;
    [ConfigVar(Name = "server.network_prediction", DefaultValue = "1", Description = "Predict snapshots data to improve compression and minimize bandwidth")]
    public static ConfigVar network_prediction;
    [ConfigVar(Name = "server.debug_hashing", DefaultValue = "1", Description = "Send entity hashes to clients for debugging.")]
    public static ConfigVar debug_hashing;

    public ServerInfo serverInfo;

    unsafe public NetworkServer(INetworkTransport transport)
    {
        m_Transport = transport;
        serverInfo = new ServerInfo();

        // Allocate array to hold world snapshots
        m_Snapshots = new WorldSnapshot[NetworkConfig.snapshotDeltaCacheSize];
        for (int i = 0; i < m_Snapshots.Length; ++i)
        {
            m_Snapshots[i] = new WorldSnapshot();
            m_Snapshots[i].data = (uint*)UnsafeUtility.Malloc(NetworkConfig.maxWorldSnapshotDataSize, UnsafeUtility.AlignOf<UInt32>(), Unity.Collections.Allocator.Persistent);
        }

        // Allocate scratch*) buffer to hold predictions. *) This is overwritten every time
        // a snapshot is being written to a specific client
        m_Prediction = (uint*)UnsafeUtility.Malloc(NetworkConfig.maxWorldSnapshotDataSize, UnsafeUtility.AlignOf<UInt32>(), Unity.Collections.Allocator.Persistent);
    }

    public void Shutdown()
    {
        UnsafeUtility.Free(m_Prediction, Unity.Collections.Allocator.Persistent);
        for (int i = 0; i < m_Snapshots.Length; ++i)
        {
            UnsafeUtility.Free(m_Snapshots[i].data, Unity.Collections.Allocator.Persistent);
        }
    }

    unsafe public void InitializeMap(DataGenerator generator)
    {
        // Generate schema the first time we set map info
        bool generateSchema = false;
        if (m_MapInfo.schema == null)
        {
            m_MapInfo.schema = new NetworkSchema(NetworkConfig.mapSchemaId);
            generateSchema = true;
        }

        // Update map info
        var writer = new NetworkWriter(m_MapInfo.data, 1024, m_MapInfo.schema, generateSchema);
        generator(ref writer);
        writer.Flush();

        m_MapInfo.serverInitSequence = m_ServerSequence;
        ++m_MapInfo.mapId;

        // Reset map and connection state
        serverTime = 0;
        m_Entities.Clear();
        m_FreeEntities.Clear();
        foreach (var pair in m_Connections)
            pair.Value.Reset();
    }

    public void MapReady(int clientId)
    {
        GameDebug.Log("Client " + clientId + " is ready");
        GameDebug.Assert(m_Connections.ContainsKey(clientId), "Got MapReady from unknown client?");
        m_Connections[clientId].mapReady = true;
    }

    // Reserve scene entities with sequential id's starting from 0
    public void ReserveSceneEntities(int count)
    {
        GameDebug.Assert(m_Entities.Count == 0, "ReserveSceneEntities: Only allowed before other entities have been registrered");
        for (var i = 0; i < count; i++)
        {
            m_Entities.Add(new EntityInfo());
        }
    }

    // Currently predictingClient can only be set on an entity at time of creation
    // in the future it should be something you can change if you for example enter/leave
    // a vehicle. There are subtle but tricky replication issues when predicting 'ownership' changes, though...
    public int RegisterEntity(int id, ushort typeId, int predictingClientId)
    {
        Profiler.BeginSample("NetworkServer.RegisterEntity()");
        EntityInfo entityInfo;
        int freeCount = m_FreeEntities.Count;

        if (id >= 0)
        {
            GameDebug.Assert(m_Entities[id].spawnSequence == 0, "RegisterEntity: Trying to reuse an id that is used by a scene entity");
            entityInfo = m_Entities[id];
        }
        else if (freeCount > 0)
        {
            id = m_FreeEntities[freeCount - 1];
            m_FreeEntities.RemoveAt(freeCount - 1);
            entityInfo = m_Entities[id];
            entityInfo.Reset();
        }
        else
        {
            entityInfo = new EntityInfo();
            m_Entities.Add(entityInfo);
            id = m_Entities.Count - 1;
        }

        entityInfo.typeId = typeId;
        entityInfo.predictingClientId = predictingClientId;
        entityInfo.spawnSequence = m_ServerSequence + 1; // NOTE : Associate the spawn with the next snapshot

        if (serverDebugEntityIds.IntValue > 1)
            GameDebug.Log("Registred entity id: " + id);

        Profiler.EndSample();
        return id;
    }

    public void UnregisterEntity(int id)
    {
        Profiler.BeginSample("NetworkServer.UnregisterEntity()");
        m_Entities[id].despawnSequence = m_ServerSequence + 1;
        Profiler.EndSample();
    }

    public void HandleClientCommands(int tick, IClientCommandProcessor processor)
    {
        foreach (var c in m_Connections)
            c.Value.ProcessCommands(tick, processor);
    }

    public void QueueEvent(int clientId, ushort typeId, bool reliable, NetworkEventGenerator generator)
    {
        ServerConnection connection;
        if (m_Connections.TryGetValue(clientId, out connection))
        {
            var e = NetworkEvent.Serialize(typeId, reliable, m_EventTypesOut, generator);
            connection.QueueEvent(e);
            e.Release();
        }
    }

    public void QueueEventBroadcast(ushort typeId, bool reliable, NetworkEventGenerator generator)
    {
        var info = NetworkEvent.Serialize(typeId, reliable, m_EventTypesOut, generator);
        foreach (var pair in m_Connections)
            pair.Value.QueueEvent(info);
        info.Release();
    }

    unsafe public void GenerateSnapshot(ISnapshotGenerator snapshotGenerator, float simTime)
    {
        var time = snapshotGenerator.WorldTick;
        GameDebug.Assert(time > serverTime);      // Time should always flow forward
        GameDebug.Assert(m_MapInfo.mapId > 0);    // Initialize map before generating snapshot

        ++m_ServerSequence;

        // We currently keep entities around until every client has ack'ed the snapshot with the despawn
        // Then we delete them from our list and recycle the id
        // TODO: we do not need this anymore?

        // Find oldest (smallest seq no) acked snapshot.
        var minClientAck = int.MaxValue;
        foreach (var pair in m_Connections)
        {
            var c = pair.Value;
            // If a client is so far behind that we have to send non-baseline updates to it
            // there is no reason to keep despawned entities around for this clients sake
            if (m_ServerSequence - c.maxSnapshotAck >= NetworkConfig.snapshotDeltaCacheSize - 2) // -2 because we want 3 baselines!
                continue;
            var acked = c.maxSnapshotAck;
            if (acked < minClientAck)
                minClientAck = acked;
        }

        // Recycle despawned entities that have been acked by all
        for (int i = 0; i < m_Entities.Count; i++)
        {
            var e = m_Entities[i];
            if (e.despawnSequence > 0 && e.despawnSequence < minClientAck)
            {
                if (serverDebugEntityIds.IntValue > 1)
                    GameDebug.Log("Recycling entity id: " + i + " because despawned in " + e.despawnSequence + " and minAck is now " + minClientAck);
                e.Reset();
                m_FreeEntities.Add(i);
            }
        }

        serverTime = time;
        m_ServerSimTime = simTime;

        m_LastEntityCount = 0;

        // Grab world snapshot from circular buffer
        var worldsnapshot = m_Snapshots[m_ServerSequence % m_Snapshots.Length];
        worldsnapshot.serverTime = time;
        worldsnapshot.length = 0;

        // Run through all the registered network entities and serialize the snapshot
        for (var id = 0; id < m_Entities.Count; id++)
        {
            var entity = m_Entities[id];

            // Skip freed
            if (entity.spawnSequence == 0)
                continue;

            // Skip entities that are depawned
            if (entity.despawnSequence > 0)
                continue;

            // If we are here and are despawned, we must be a despawn/spawn in same frame situation
            GameDebug.Assert(entity.despawnSequence == 0 || entity.despawnSequence == entity.spawnSequence, "Snapshotting entity that was deleted in the past?");
            GameDebug.Assert(entity.despawnSequence == 0 || entity.despawnSequence == m_ServerSequence, "WUT");

            // For now we generate the entity type info the first time we generate a snapshot
            // for the particular entity as a more lightweight approach rather than introducing
            // a full schema system where the game code must generate and register the type
            EntityTypeInfo typeInfo;
            bool generateSchema = false;
            if (!m_EntityTypes.TryGetValue(entity.typeId, out typeInfo))
            {
                typeInfo = new EntityTypeInfo() { name = snapshotGenerator.GenerateEntityName(id), typeId = entity.typeId, createdSequence = m_ServerSequence, schema = new NetworkSchema(entity.typeId + NetworkConfig.firstEntitySchemaId) };
                m_EntityTypes.Add(entity.typeId, typeInfo);
                generateSchema = true;
            }

            // Generate entity snapshot
            var snapshotInfo = entity.snapshots.Acquire(m_ServerSequence);
            snapshotInfo.start = worldsnapshot.data + worldsnapshot.length;

            var writer = new NetworkWriter(snapshotInfo.start, NetworkConfig.maxWorldSnapshotDataSize / 4 - worldsnapshot.length, typeInfo.schema, generateSchema);
            snapshotGenerator.GenerateEntitySnapshot(id, ref writer);
            writer.Flush();
            snapshotInfo.length = writer.GetLength();

            worldsnapshot.length += snapshotInfo.length;

            if (entity.despawnSequence == 0)
            {
                m_LastEntityCount++;
            }

            GameDebug.Assert(snapshotInfo.length > 0, "Tried to generate a entity snapshot but no data was delivered by generator?");

            if (generateSchema)
            {
                GameDebug.Assert(typeInfo.baseline == null, "Generating schema twice?");
                // First time a type/schema is encountered, we clone the serialized data and
                // use it as the type-baseline
                typeInfo.baseline = (uint*)UnsafeUtility.Malloc(snapshotInfo.length * 4, UnsafeUtility.AlignOf<UInt32>(), Unity.Collections.Allocator.Persistent);// new uint[snapshot.length];// (uint[])snapshot.data.Clone();
                for (int i = 0; i < snapshotInfo.length; i++)
                    typeInfo.baseline[i] = *(snapshotInfo.start + i);
            }

            // Check if it is different from the previous generated snapshot
            var dirty = !entity.snapshots.Exists(m_ServerSequence - 1);
            if (!dirty)
            {
                var previousSnapshot = entity.snapshots[m_ServerSequence - 1];
                if (previousSnapshot.length != snapshotInfo.length || // TODO (petera) how could length differ???
                    UnsafeUtility.MemCmp(previousSnapshot.start, snapshotInfo.start, snapshotInfo.length) != 0)
                {
                    dirty = true;
                }
            }

            if (dirty)
                entity.updateSequence = m_ServerSequence;

            statsGeneratedEntitySnapshots++;
            statsSnapshotData += snapshotInfo.length;
        }
        statsGeneratedSnapshotSize += worldsnapshot.length * 4;
    }

    public void Update(INetworkCallbacks loop)
    {
        m_Transport.Update();

        TransportEvent e = new TransportEvent();
        while (m_Transport.NextEvent(ref e))
        {
            switch (e.type)
            {
                case TransportEvent.Type.Connect:
                    OnConnect(e.connectionId, loop);
                    break;
                case TransportEvent.Type.Disconnect:
                    OnDisconnect(e.connectionId, loop);
                    break;
                case TransportEvent.Type.Data:
                    OnData(e.connectionId, e.data, e.dataSize, loop);
                    break;
            }
        }
    }

    private long accumSendDataTicks = 0;
    private long lastUpdateTick = 0;
    public void SendData()
    {
        Profiler.BeginSample("NetworkServer.SendData");

        long startTick = 0;
        if (NetworkServer.print_senddata_time.IntValue > 0)
            startTick = Game.Clock.ElapsedTicks;

        foreach (var pair in m_Connections)
        {
#pragma warning disable 0162 // unreachable code
            switch (NetworkConfig.ioStreamType)
            {
                case NetworkCompression.IOStreamType.Raw:
                    pair.Value.SendPackage<RawOutputStream>(m_NetworkCompressionCapture);
                    break;
                case NetworkCompression.IOStreamType.Huffman:
                    pair.Value.SendPackage<HuffmanOutputStream>(m_NetworkCompressionCapture);
                    break;
                default:
                    GameDebug.Assert(false);
            }
#pragma warning restore
        }


        if (NetworkServer.print_senddata_time.IntValue > 0)
        {
            long stopTick = Game.Clock.ElapsedTicks;
            long ticksPerSecond = System.Diagnostics.Stopwatch.Frequency;
            accumSendDataTicks += stopTick - startTick;

            if (stopTick >= lastUpdateTick + ticksPerSecond)
            {
                GameDebug.Log("SendData Time per second: " + accumSendDataTicks * 1000.0 / ticksPerSecond);
                accumSendDataTicks = 0;
                lastUpdateTick = Game.Clock.ElapsedTicks;
            }
        }

        Profiler.EndSample();
    }

    public Dictionary<int, ServerConnection> GetConnections()
    {
        return m_Connections;
    }

    public Dictionary<ushort, EntityTypeInfo> GetEntityTypes()
    {
        return m_EntityTypes;
    }

    void OnConnect(int connectionId, INetworkCallbacks loop)
    {
        GameDebug.Assert(!m_Connections.ContainsKey(connectionId));

        if (m_Connections.Count >= ServerGameLoop.serverMaxClients.IntValue)
        {
            GameDebug.Log("Refusing incoming connection " + connectionId + " due to server.maxclients");
            m_Transport.Disconnect(connectionId);
            return;
        }

        GameDebug.Log("Incoming connection: #" + connectionId + " from: " + m_Transport.GetConnectionDescription(connectionId));

        var connection = new ServerConnection(this, connectionId, m_Transport);

        m_Connections.Add(connectionId, connection);

        loop.OnConnect(connectionId);
    }

    void OnDisconnect(int connectionId, INetworkCallbacks loop)
    {
        ServerConnection connection;
        if (m_Connections.TryGetValue(connectionId, out connection))
        {
            loop.OnDisconnect(connectionId);

            GameDebug.Log(string.Format("Client {0} disconnected", connectionId));
            GameDebug.Log(string.Format("Last package sent : {0} . Last package received {1} {2} ms ago",
                connection.outSequence,
                connection.inSequence,
                NetworkUtils.stopwatch.ElapsedMilliseconds - connection.inSequenceTime));

            connection.Shutdown();
            m_Connections.Remove(connectionId);
        }
    }

    void OnData(int connectionId, byte[] data, int size, INetworkCallbacks loop)
    {
#pragma warning disable 0162 // unreachable code
        switch (NetworkConfig.ioStreamType)
        {
            case NetworkCompression.IOStreamType.Raw:
                {
                    m_Connections[connectionId].ReadPackage<RawInputStream>(data, size, NetworkCompressionModel.DefaultModel, loop);
                    break;
                }
            case NetworkCompression.IOStreamType.Huffman:
                {
                    m_Connections[connectionId].ReadPackage<HuffmanInputStream>(data, size, NetworkCompressionModel.DefaultModel, loop);
                    break;
                }
            default:
                GameDebug.Assert(false);
        }
#pragma warning restore
    }

    unsafe class MapInfo
    {
        public int serverInitSequence;                  // The server frame the map was initialized
        public ushort mapId;                            // Unique sequence number for the map (to deal with redudant mapinfo messages)
        public NetworkSchema schema;                    // Schema for the map info
        public uint* data = (uint*)UnsafeUtility.Malloc(1024, UnsafeUtility.AlignOf<uint>(), Unity.Collections.Allocator.Persistent);            // Game specific payload
    }

    unsafe public class EntityTypeInfo
    {
        public string name;
        public ushort typeId;
        public NetworkSchema schema;
        public int createdSequence;
        public uint* baseline;
        public int stats_count;
        public int stats_bits;
    }

    // Holds a information about a snapshot for an entity.
    // Each entity has a circular history of these
    unsafe class EntitySnapshotInfo
    {
        public uint* start;     // pointer into WorldSnapshot.data block (see below)
        public int length;      // length of data in words
    }

    // Each tick a WorldSnapshot is generated. The data buffer contains serialized data
    // from all serializable entitites
    unsafe class WorldSnapshot
    {
        public int serverTime;  // server tick for this snapshot
        public int length;      // length of data in data field
        public uint* data;
    }

    unsafe class EntityInfo
    {
        public EntityInfo()
        {
            snapshots = new SequenceBuffer<EntitySnapshotInfo>(NetworkConfig.snapshotDeltaCacheSize, () => new EntitySnapshotInfo());
        }

        public void Reset()
        {
            typeId = 0;
            spawnSequence = 0;
            despawnSequence = 0;
            updateSequence = 0;
            snapshots.Clear();
            for (var i = 0; i < fieldsChangedPrediction.Length; i++)
                fieldsChangedPrediction[i] = 0;
            predictingClientId = -1;
        }

        public ushort typeId;
        public int predictingClientId = -1;

        public int spawnSequence;
        public int despawnSequence;
        public int updateSequence;

        public SequenceBuffer<EntitySnapshotInfo> snapshots;
        public uint* prediction;                                // NOTE: used in WriteSnapshot but invalid outside that function
        public byte[] fieldsChangedPrediction = new byte[(NetworkConfig.maxFieldsPerSchema + 7) / 8];

        // On server the fieldmask of an entity is different depending on what client we are sending to
        // Flags:
        //    1 : receiving client is predicting
        //    2 : receiving client is not predicting
        public byte GetFieldMask(int connectionId)
        {
            byte mask = 0;
            if (predictingClientId == -1)
                return 0;
            if (predictingClientId == connectionId)
                mask |= 0x1;
            else
                mask |= 0x2;
            return mask;
        }
    }

    public class ServerPackageInfo : PackageInfo
    {
        public int serverSequence;              // Used to map package sequences back to server sequence
        public int serverTime;

        public override void Reset()
        {
            base.Reset();
            serverSequence = 0;
        }
    }

    public class ServerConnection : NetworkConnection<NetworkServer.Counters, ServerPackageInfo>
    {
        public void SetSnapshotInterval(int _snapshotInterval)
        {
            if (_snapshotInterval < 1)
                _snapshotInterval = 1;
            snapshotInterval = _snapshotInterval;
        }

        public ServerConnection(NetworkServer server, int connectionId, INetworkTransport transport) : base(connectionId, transport)
        {
            this.server = server;

            if (NetworkServer.dump_client_streams.IntValue > 0)
            {
                var name = "client_stream_" + connectionId + ".bin";
                this.debugSendStreamWriter = new BinaryWriter(File.Open(name, FileMode.Create));
                GameDebug.Log("Storing client data stream in " + name);
            }

            // update rate overridden by client info right after connect. Start at 1, i.e. update every tick, to allow fast handshake
            snapshotInterval = 1;
            maxBPS = 0;
            nextOutPackageTime = 0;
        }

        public new void Reset()
        {
            base.Reset();

            mapAcked = false;
            mapReady = false;
            snapshotPackageBaseline = 0;
            maxSnapshotAck = 0;
            maxSnapshotTime = 0;
            lastClearedAck = 0;
            snapshotSeqs.Clear();
            snapshotAcks.Clear();
        }

        unsafe public void ProcessCommands(int maxTime, IClientCommandProcessor processor)
        {
            // Check for time jumps backward in the command stream and reset the queue in case
            // we find one. (This will happen if the client determines that it has gotten too
            // far ahead and recalculate the client time.)

            // TODO : We should be able to do this in a smarter way
            for (var sequence = commandSequenceProcessed + 1; sequence <= commandSequenceIn; ++sequence)
            {
                CommandInfo previous;
                CommandInfo current;

                commandsIn.TryGetValue(sequence, out current);
                commandsIn.TryGetValue(sequence - 1, out previous);

                if (current != null && previous != null && current.time <= previous.time)
                    commandSequenceProcessed = sequence - 1;
            }

            for (var sequence = commandSequenceProcessed + 1; sequence <= commandSequenceIn; ++sequence)
            {
                CommandInfo info;
                if (commandsIn.TryGetValue(sequence, out info))
                {
                    if (info.time <= maxTime)
                    {
                        fixed(uint* data = info.data)
                        {
                            var reader = new NetworkReader(data, commandSchema);
                            processor.ProcessCommand(connectionId, info.time, ref reader);
                        }
                        commandSequenceProcessed = sequence;
                    }
                    else
                        return;
                }
            }
        }

        public void ReadPackage<TInputStream>(byte[] packageData, int packageSize, NetworkCompressionModel model, INetworkCallbacks loop) where TInputStream : struct, NetworkCompression.IInputStream
        {
            counters.bytesIn += packageSize;

            NetworkMessage content;
            byte[] assembledData;
            int assembledSize;
            int headerSize;
            var packageSequence = ProcessPackageHeader(packageData, packageSize, out content, out assembledData, out assembledSize, out headerSize);

            // Bail out if the package was bad (duplicate or too old)
            if (packageSequence == 0)
                return;

            var input = default(TInputStream);// new TInputStream();  Due to bug new generates garbage here
            input.Initialize(model, assembledData, headerSize);

            if ((content & NetworkMessage.ClientConfig) != 0)
                ReadClientConfig(ref input);

            if ((content & NetworkMessage.Commands) != 0)
                ReadCommands(ref input);

            if ((content & NetworkMessage.Events) != 0)
                ReadEvents(ref input, loop);
        }

        public void SendPackage<TOutputStream>(NetworkCompressionCapture networkCompressionCapture) where TOutputStream : struct, NetworkCompression.IOutputStream
        {
            // Check if we can and should send new package

            var rawOutputStream = new BitOutputStream(m_PackageBuffer);

            var canSendPackage = CanSendPackage(ref rawOutputStream);
            if (!canSendPackage)
            {
                //GameDebug.Log("SERVER: Choked by missing acks from client!");
                return;
            }

            // Distribute clients evenly according to their with snapshotInterval > 1
            // TODO: This kind of assumes same update interval by all ....
            if ((server.m_ServerSequence + connectionId) % snapshotInterval != 0)
                return;

            // Respect max bps rate cap
            if (Game.frameTime < nextOutPackageTime)
                return;

            ServerPackageInfo packageInfo;
            BeginSendPackage(ref rawOutputStream, out packageInfo);

            int endOfHeaderPos = rawOutputStream.Align();
            var output = default(TOutputStream);// new TOutputStream();  Due to bug new generates garbage here
            output.Initialize(server.serverInfo.compressionModel, m_PackageBuffer, endOfHeaderPos, networkCompressionCapture);


            // We store the server sequence in the package info to be able to map back to 
            // the snapshot baseline when we get delivery notification for the package and 
            // similarly for the time as we send the server time as a delta relative to 
            // the last acknowledged server time

            packageInfo.serverSequence = server.m_ServerSequence;   // the server snapshot sequence
            packageInfo.serverTime = server.serverTime;             // Server time (could be ticks or could be ms)

            // The ifs below are in essence the 'connection handshake' logic.
            if (!clientInfoAcked)
            {
                // Keep sending client info until it is acked
                WriteClientInfo(ref output);
            }
            else if (!mapAcked)
            {
                if (server.m_MapInfo.serverInitSequence > 0)
                {
                    // Keep sending map info until it is acked
                    WriteMapInfo(ref output);
                }
            }
            else
            {
                // Send snapshot, buf only
                //   if client has declared itself ready
                //   if we have not already sent for this tick (because we need to be able to map a snapshot 
                //     sequence to a package sequence we cannot send the same snapshot multiple times).
                if (mapReady && server.m_ServerSequence > snapshotServerLastWritten)
                {
                    WriteSnapshot(ref output);
                }
                WriteEvents(packageInfo, ref output);
            }

            // TODO (petera) this is not nice. We need one structure only to keep track of outstanding packages / acks
            // We have to ensure all sequence numbers that have been used by packages sent elsewhere from here
            // gets cleared as 'not ack'ed' so they don't show up later as snapshots we think the client has
            for (int i = lastClearedAck + 1; i <= outSequence; ++i)
            {
                snapshotAcks[i % NetworkConfig.clientAckCacheSize] = false;
            }
            lastClearedAck = outSequence;

            int compressedSize = output.Flush();
            rawOutputStream.SkipBytes(compressedSize);

            var messageSize = CompleteSendPackage(packageInfo, ref rawOutputStream);

            // Decide when next package can go out
            if (maxBPS > 0)
            {
                double timeLimitBPS = messageSize / maxBPS;
                if (timeLimitBPS > (float)snapshotInterval / Game.serverTickRate.FloatValue)
                {
                    GameDebug.Log("SERVER: Choked by BPS sending " + messageSize);
                    nextOutPackageTime = Game.frameTime + timeLimitBPS;
                }
            }
        }
        int lastClearedAck = 0;

        void WriteClientInfo<TOutputStream>(ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
        {
            AddMessageContentFlag(NetworkMessage.ClientInfo);
            output.WriteRawBits((uint)connectionId, 8);
            output.WriteRawBits((uint)server.serverInfo.serverTickRate, 8);
            output.WriteRawBits(NetworkConfig.protocolVersion, 8);

            byte[] modelData = server.serverInfo.compressionModel.modelData;
            output.WriteRawBits((uint)modelData.Length, 16);
            for (int i = 0; i < modelData.Length; i++)
                output.WriteRawBits(modelData[i], 8);

            if (serverDebug.IntValue > 0)
            {
                GameDebug.Log(string.Format("WriteClientInfo: connectionId {0}   serverTickRate {1}", connectionId, server.serverInfo.serverTickRate));
            }
        }

        unsafe void WriteMapInfo<TOutputStream>(ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
        {
            AddMessageContentFlag(NetworkMessage.MapInfo);

            output.WriteRawBits(server.m_MapInfo.mapId, 16);

            // Write schema if client haven't acked it
            output.WriteRawBits(mapSchemaAcked ? 0 : 1U, 1);
            if (!mapSchemaAcked)
                NetworkSchema.WriteSchema(server.m_MapInfo.schema, ref output);

            // Write map data
            NetworkSchema.CopyFieldsFromBuffer(server.m_MapInfo.schema, server.m_MapInfo.data, ref output);
        }

        unsafe void WriteSnapshot<TOutputStream>(ref TOutputStream output) where TOutputStream : NetworkCompression.IOutputStream
        {
            server.statsSentUpdates++;

            Profiler.BeginSample("NetworkServer.WriteSnapshot()");
            // joinSequence is the *first* snapshot that could have received.
            if (joinSequence == 0)
            {
                joinSequence = server.m_ServerSequence;
                if (serverDebug.IntValue > 0)
                    GameDebug.Log("Client " + connectionId + " got first snapshot at " + joinSequence);
            }

            AddMessageContentFlag(NetworkMessage.Snapshot);
            counters.snapshotsOut++;

            bool enableNetworkPrediction = network_prediction.IntValue != 0;
            bool enableHashing = debug_hashing.IntValue != 0;


            // Check if the baseline from the client is too old. We keep N number of snapshots on the server 
            // so if the client baseline is older than that we cannot generate the snapshot. Furthermore, we require
            // the client to keep the last N updates for any entity, so even though the client might have much older
            // baselines for some entities we cannot guarantee it. 
            // TODO : Can we make this simpler?
            var haveBaseline = maxSnapshotAck != 0;
            if (server.m_ServerSequence - maxSnapshotAck >= NetworkConfig.snapshotDeltaCacheSize - 2) // -2 because we want 3 baselines!
            {
                if (serverDebug.IntValue > 0)
                    GameDebug.Log("ServerSequence ahead of latest ack'ed snapshot by more than cache size. " + (haveBaseline ? "nobaseline" : "baseline"));
                haveBaseline = false;
            }
            var baseline = haveBaseline ? maxSnapshotAck : 0;


            int snapshot0Baseline = baseline;
            int snapshot1Baseline = baseline;
            int snapshot2Baseline = baseline;
            int snapshot0BaselineClient = snapshotPackageBaseline;
            int snapshot1BaselineClient = snapshotPackageBaseline;
            int snapshot2BaselineClient = snapshotPackageBaseline;
            if (enableNetworkPrediction && haveBaseline)
            {
                var end = snapshotPackageBaseline - NetworkConfig.clientAckCacheSize;
                end = end < 0 ? 0 : end;
                var a = snapshotPackageBaseline - 1;
                while (a > end)
                {
                    if (snapshotAcks[a % NetworkConfig.clientAckCacheSize])
                    {
                        var base1 = snapshotSeqs[a % NetworkConfig.clientAckCacheSize];
                        if (server.m_ServerSequence - base1 < NetworkConfig.snapshotDeltaCacheSize - 2)
                        {
                            snapshot1Baseline = base1;
                            snapshot1BaselineClient = a;
                            snapshot2Baseline = snapshotSeqs[a % NetworkConfig.clientAckCacheSize];
                            snapshot2BaselineClient = a;
                        }
                        break;
                    }
                    a--;
                }
                a--;
                while (a > end)
                {
                    if (snapshotAcks[a % NetworkConfig.clientAckCacheSize])
                    {
                        var base2 = snapshotSeqs[a % NetworkConfig.clientAckCacheSize];
                        if (server.m_ServerSequence - base2 < NetworkConfig.snapshotDeltaCacheSize - 2)
                        {
                            snapshot2Baseline = base2;
                            snapshot2BaselineClient = a;
                        }
                        break;
                    }
                    a--;
                }
            }

            // NETTODO: Write up a list of all sequence numbers. Ensure they are all needed
            output.WriteRawBits(haveBaseline ? 1u : 0, 1);
            output.WritePackedIntDelta(snapshot0BaselineClient, outSequence - 1, NetworkConfig.baseSequenceContext);
            output.WriteRawBits(enableNetworkPrediction ? 1u : 0u, 1);
            output.WriteRawBits(enableHashing ? 1u : 0u, 1);
            if (enableNetworkPrediction)
            {
                output.WritePackedIntDelta(haveBaseline ? snapshot1BaselineClient : 0, snapshot0BaselineClient - 1, NetworkConfig.baseSequence1Context);
                output.WritePackedIntDelta(haveBaseline ? snapshot2BaselineClient : 0, snapshot1BaselineClient - 1, NetworkConfig.baseSequence2Context);
            }

            // NETTODO: For us serverTime == tick but network layer only cares about a growing int
            output.WritePackedIntDelta(server.serverTime, haveBaseline ? maxSnapshotTime : 0, NetworkConfig.serverTimeContext);

            // NETTODO: a more generic way to send stats
            var temp = server.m_ServerSimTime * 10;
            output.WriteRawBits((byte)temp, 8);

            // NETTODO: Rename TempListType etc.
            // NETTODO: Consider if we need to distinguish between Type & Schema
            server.m_TempTypeList.Clear();
            server.m_TempSpawnList.Clear();
            server.m_TempDespawnList.Clear();
            server.m_TempUpdateList.Clear();

            Profiler.BeginSample("NetworkServer.PredictSnapshot");
            server.m_PredictionIndex = 0;
            for (int id = 0, c = server.m_Entities.Count; id < c; id++)
            {
                var entity = server.m_Entities[id];

                // Skip freed
                if (entity.spawnSequence == 0)
                    continue;

                bool spawnedSinceBaseline = (entity.spawnSequence > baseline);
                bool despawned = (entity.despawnSequence > 0);

                // Note to future self: This is a bit tricky... We consider lifetimes of entities
                // re the baseline (last ack'ed, so in the past) and the snapshot we are building (now)
                // There are 6 cases (S == spawn, D = despawn):
                //
                //  --------------------------------- time ----------------------------------->
                //
                //                   BASELINE          SNAPSHOT
                //                      |                 |
                //                      v                 v
                //  1.    S-------D                                                  IGNORE
                //  2.    S------------------D                                       SEND DESPAWN
                //  3.    S-------------------------------------D                    SEND UPDATE
                //  4.                        S-----D                                IGNORE
                //  5.                        S-----------------D                    SEND SPAWN + UPDATE
                //  6.                                         S----------D          INVALID (FUTURE)
                //

                if (despawned && entity.despawnSequence <= baseline)
                    continue;                               // case 1: ignore

                if(despawned && !spawnedSinceBaseline)
                {
                    server.m_TempDespawnList.Add(id);       // case 2: despawn
                    continue;
                }

                if (spawnedSinceBaseline && despawned)
                    continue;                               // case 4: ignore

                if(spawnedSinceBaseline)
                    server.m_TempSpawnList.Add(id);         // case 5: send spawn + update

                // case 5. and 3. fall through to here and gets updated

                // Send data from latest tick
                var tickToSend = server.m_ServerSequence;
                // If despawned, however, we have stopped generating updates so pick latest valid
                if (despawned)
                    tickToSend = Mathf.Max(entity.updateSequence, entity.despawnSequence - 1);
                //GameDebug.Assert(tickToSend == server.m_ServerSequence || tickToSend == entity.despawnSequence - 1, "Sending snapshot. Expect to send either current tick or last tick before despawn.");

                {
                    var entityType = server.m_EntityTypes[entity.typeId];

                    var snapshot = entity.snapshots[tickToSend];

                    // NOTE : As long as the server haven't gotten the spawn acked, it will keep sending
                    // delta relative to 0 as we cannot know if we have a valid baseline on the client or not

                    uint num_baselines = 1; // if there is no normal baseline, we use schema baseline so there is always one
                    uint* baseline0 = entityType.baseline;
                    int time0 = maxSnapshotTime;

                    if (haveBaseline && entity.spawnSequence <= maxSnapshotAck)
                    {
                        baseline0 = entity.snapshots[snapshot0Baseline].start;
                    }

                    if (enableNetworkPrediction)
                    {
                        uint* baseline1 = entityType.baseline;
                        uint* baseline2 = entityType.baseline;
                        int time1 = maxSnapshotTime;
                        int time2 = maxSnapshotTime;

                        if (haveBaseline && entity.spawnSequence <= maxSnapshotAck)
                        {
                            GameDebug.Assert(server.m_Snapshots[snapshot0Baseline % server.m_Snapshots.Length].serverTime == maxSnapshotTime, "serverTime == maxSnapshotTime");
                            GameDebug.Assert(entity.snapshots.Exists(snapshot0Baseline), "Exists(snapshot0Baseline)");

                            // Newly spawned entities might not have earlier baselines initially
                            if (snapshot1Baseline != snapshot0Baseline && entity.snapshots.Exists(snapshot1Baseline))
                            {
                                num_baselines = 2;
                                baseline1 = entity.snapshots[snapshot1Baseline].start;
                                time1 = server.m_Snapshots[snapshot1Baseline % server.m_Snapshots.Length].serverTime;

                                if (snapshot2Baseline != snapshot1Baseline && entity.snapshots.Exists(snapshot2Baseline))
                                {
                                    num_baselines = 3;
                                    baseline2 = entity.snapshots[snapshot2Baseline].start;
                                    //time2 = entity.snapshots[snapshot2Baseline].serverTime;
                                    time2 = server.m_Snapshots[snapshot2Baseline % server.m_Snapshots.Length].serverTime;
                                }
                            }
                        }

                        entity.prediction = server.m_Prediction + server.m_PredictionIndex;
                        NetworkPrediction.PredictSnapshot(entity.prediction,  entity.fieldsChangedPrediction, entityType.schema, num_baselines, (uint)time0, baseline0, (uint)time1, baseline1, (uint)time2, baseline2, (uint)server.serverTime, entity.GetFieldMask(connectionId));
                        server.m_PredictionIndex += entityType.schema.GetByteSize() / 4;
                        server.statsProcessedOutgoing += entityType.schema.GetByteSize();

                        if (UnsafeUtility.MemCmp(entity.prediction, snapshot.start, entityType.schema.GetByteSize()) != 0)
                        {
                            server.m_TempUpdateList.Add(id);
                        }

                        if (serverDebug.IntValue > 2)
                        {
                            GameDebug.Log((haveBaseline ? "Upd [BL]" : "Upd [  ]") +
                                "num_baselines: " + num_baselines + " serverSequence: " + tickToSend + " " +
                                snapshot0Baseline + "(" + snapshot0BaselineClient + "," + time0 + ") - " +
                                snapshot1Baseline + "(" + snapshot1BaselineClient + "," + time1 + ") - " +
                                snapshot2Baseline + "(" + snapshot2BaselineClient + "," + time2 + "). Sche: " +
                                server.m_TempTypeList.Count + " Spwns: " + server.m_TempSpawnList.Count + " Desp: " + server.m_TempDespawnList.Count + " Upd: " + server.m_TempUpdateList.Count);
                        }
                    }
                    else
                    {
                        var prediction = baseline0;

                        var fcp = entity.fieldsChangedPrediction;
                        for (int i = 0, l = fcp.Length; i < l; ++i)
                            fcp[i] = 0;

                        if(UnsafeUtility.MemCmp(prediction, snapshot.start, entityType.schema.GetByteSize()) != 0)
                        {
                            server.m_TempUpdateList.Add(id);
                        }

                        if (serverDebug.IntValue > 2)
                        {
                            GameDebug.Log((haveBaseline ? "Upd [BL]" : "Upd [  ]") + snapshot0Baseline + "(" + snapshot0BaselineClient + "," + time0 + "). Sche: " + server.m_TempTypeList.Count + " Spwns: " + server.m_TempSpawnList.Count + " Desp: " + server.m_TempDespawnList.Count + " Upd: " + server.m_TempUpdateList.Count);
                        }
                    }
                }
            }
            Profiler.EndSample();

            if (serverDebug.IntValue > 1 && (server.m_TempSpawnList.Count > 0 || server.m_TempDespawnList.Count > 0))
            {
                GameDebug.Log(connectionId + ": spwns: " + string.Join(",", server.m_TempSpawnList) + "    despwans: " + string.Join(",", server.m_TempDespawnList));
            }

            foreach (var pair in server.m_EntityTypes)
            {
                if (pair.Value.createdSequence > maxSnapshotAck)
                    server.m_TempTypeList.Add(pair.Value);
            }

            output.WritePackedUInt((uint)server.m_TempTypeList.Count, NetworkConfig.schemaCountContext);
            foreach (var typeInfo in server.m_TempTypeList)
            {
                output.WritePackedUInt(typeInfo.typeId, NetworkConfig.schemaTypeIdContext);
                NetworkSchema.WriteSchema(typeInfo.schema, ref output);

                GameDebug.Assert(typeInfo.baseline != null);
                NetworkSchema.CopyFieldsFromBuffer(typeInfo.schema, typeInfo.baseline, ref output);
            }

            int previousId = 1;
            output.WritePackedUInt((uint)server.m_TempSpawnList.Count, NetworkConfig.spawnCountContext);
            foreach (var id in server.m_TempSpawnList)
            {
                output.WritePackedIntDelta(id, previousId, NetworkConfig.idContext);
                previousId = id;

                var entity = server.m_Entities[id];

                output.WritePackedUInt((uint)entity.typeId, NetworkConfig.spawnTypeIdContext);
                output.WriteRawBits(entity.GetFieldMask(connectionId), 8);
            }

            output.WritePackedUInt((uint)server.m_TempDespawnList.Count, NetworkConfig.despawnCountContext);
            foreach (var id in server.m_TempDespawnList)
            {
                output.WritePackedIntDelta(id, previousId, NetworkConfig.idContext);
                previousId = id;
            }

            int numUpdates = server.m_TempUpdateList.Count;
            output.WritePackedUInt((uint)numUpdates, NetworkConfig.updateCountContext);

            if (serverDebug.IntValue > 0)
            {
                foreach (var t in server.m_EntityTypes)
                {
                    t.Value.stats_count = 0;
                    t.Value.stats_bits = 0;
                }
            }

            foreach (var id in server.m_TempUpdateList)
            {
                var entity = server.m_Entities[id];
                var entityType = server.m_EntityTypes[entity.typeId];

                uint* prediction = null;
                if (enableNetworkPrediction)
                {
                    prediction = entity.prediction;
                }
                else
                {
                    prediction = entityType.baseline;
                    if (haveBaseline && entity.spawnSequence <= maxSnapshotAck)
                    {
                        prediction = entity.snapshots[snapshot0Baseline].start;
                    }
                }

                output.WritePackedIntDelta(id, previousId, NetworkConfig.idContext);
                previousId = id;

                // TODO (petera) It is a mess that we have to repeat the logic about tickToSend from above here
                int tickToSend = server.m_ServerSequence;
                if (entity.despawnSequence > 0)
                    tickToSend = Mathf.Max(entity.despawnSequence - 1, entity.updateSequence);

                GameDebug.Assert(server.m_ServerSequence - tickToSend < NetworkConfig.snapshotDeltaCacheSize);

                if (!entity.snapshots.Exists(tickToSend))
                {
                    GameDebug.Log("maxSnapAck: " + maxSnapshotAck);
                    GameDebug.Log("lastWritten: " + snapshotServerLastWritten);
                    GameDebug.Log("spawn: " + entity.spawnSequence);
                    GameDebug.Log("despawn: " + entity.despawnSequence);
                    GameDebug.Log("update: " + entity.updateSequence);
                    GameDebug.Log("tick: " + server.m_ServerSequence);
                    GameDebug.Log("id: " + id);
                    GameDebug.Log("snapshots: " + entity.snapshots.ToString());
                    //GameDebug.Log("WOULD HAVE crashed looking for " + tickToSend + " changing to " + (entity.despawnSequence - 1));
                    //tickToSend = entity.despawnSequence - 1;
                    GameDebug.Assert(false, "Unable to find " + tickToSend + " in snapshots. Would update have worked?");
                }
                var snapshotInfo = entity.snapshots[tickToSend];

                // NOTE : As long as the server haven't gotten the spawn acked, it will keep sending
                // delta relative to 0 as we cannot know if we have a valid baseline on the client or not
                uint entity_hash = 0;
                var bef = output.GetBitPosition2();
                DeltaWriter.Write(ref output, entityType.schema, snapshotInfo.start, prediction, entity.fieldsChangedPrediction, entity.GetFieldMask(connectionId), ref entity_hash);
                var aft = output.GetBitPosition2();
                if (serverDebug.IntValue > 0)
                {
                    entityType.stats_count++;
                    entityType.stats_bits += (aft - bef);
                }

                if (enableHashing)
                    output.WriteRawBits(entity_hash, 32);
            }
            if (!haveBaseline && serverDebug.IntValue > 0)
            {
                Debug.Log("Sending no-baseline snapshot. C: " + connectionId + " Seq: " + outSequence + " Max: " + maxSnapshotAck + "  Total entities sent: " + server.m_TempUpdateList.Count + " Type breakdown:");
                foreach (var c in server.m_EntityTypes)
                {
                    Debug.Log(c.Value.name + " " + c.Key + " #" + (c.Value.stats_count) + " " + (c.Value.stats_bits / 8) + " bytes");
                }
            }

            if (enableHashing)
            {
                output.WriteRawBits(server.m_LastEntityCount, 32);
            }

            server.statsSentOutgoing += output.GetBitPosition2() / 8;

            snapshotServerLastWritten = server.m_ServerSequence;
            snapshotSeqs[outSequence % NetworkConfig.clientAckCacheSize] = server.m_ServerSequence;

            Profiler.EndSample();
        }

        void ReadClientConfig<TInputStream>(ref TInputStream input) where TInputStream : NetworkCompression.IInputStream
        {
            maxBPS = (int)input.ReadRawBits(32);
            var snapshotInterval = (int)input.ReadRawBits(16);
            SetSnapshotInterval(snapshotInterval);

            if (serverDebug.IntValue > 0)
            {
                GameDebug.Log(string.Format("ReadClientConfig: updateRate: {0}  snapshotRate: {1}", maxBPS, snapshotInterval));
            }
        }

        void ReadCommands<TInputStream>(ref TInputStream input) where TInputStream : NetworkCompression.IInputStream
        {
            counters.commandsIn++;
            var schema = input.ReadRawBits(1) != 0;
            if (schema)
            {
                commandSchema = NetworkSchema.ReadSchema(ref input);    // might be overridden
            }

            // NETTODO Reconstruct the wide sequence
            // NETTODO Rename to commandMessageSequence?
            var sequence = Sequence.FromUInt16((ushort)input.ReadRawBits(16), commandSequenceIn);
            if (sequence > commandSequenceIn)
                commandSequenceIn = sequence;

            CommandInfo previous = defaultCommandInfo;
            while (input.ReadRawBits(1) != 0)
            {
                var command = commandsIn.Acquire(sequence);
                command.time = (int)input.ReadPackedIntDelta(previous.time, NetworkConfig.commandTimeContext);

                uint hash = 0;
                DeltaReader.Read(ref input, commandSchema, command.data, previous.data, zeroFieldsChanged, 0, ref hash);

                previous = command;
                --sequence;
            }
        }
        byte[] zeroFieldsChanged = new byte[(NetworkConfig.maxFieldsPerSchema + 7) / 8];

        // when incoming package, this is called up to 16 times, one for each pack that gets acked
        // sequence: the 'top' package that is being acknowledged in this package
        // TODO (petera) shouldn't sequence be in info?
        protected override void NotifyDelivered(int sequence, ServerPackageInfo info, bool madeIt)
        {
            base.NotifyDelivered(sequence, info, madeIt);

            if (madeIt)
            {
                if ((info.content & NetworkMessage.ClientInfo) != 0)
                    clientInfoAcked = true;

                // Check if the client received the map info
                if ((info.content & NetworkMessage.MapInfo) != 0 && info.serverSequence >= server.m_MapInfo.serverInitSequence)
                {
                    mapAcked = true;
                    mapSchemaAcked = true;
                }

                // Update the snapshot baseline if the client received the snapshot
                if (mapAcked && (info.content & NetworkMessage.Snapshot) != 0)
                {
                    snapshotPackageBaseline = sequence;

                    GameDebug.Assert(snapshotSeqs[sequence % NetworkConfig.clientAckCacheSize] > 0, "Got ack for package we did not expect?");
                    snapshotAcks[sequence % NetworkConfig.clientAckCacheSize] = true;

                    // Keep track of newest ack'ed snapshot
                    if (info.serverSequence > maxSnapshotAck)
                    {
                        if (maxSnapshotAck == 0 && serverDebug.IntValue > 0)
                            Debug.Log("SERVER: first max ack for " + info.serverSequence);
                        maxSnapshotAck = info.serverSequence;
                        maxSnapshotTime = info.serverTime;
                    }
                }
            }
        }

        class CommandInfo
        {
            public int time = 0;
            public uint[] data = new uint[NetworkConfig.maxCommandDataSize];
        }

        NetworkServer server;

        // Connection handshake
        public bool clientInfoAcked;
        bool mapAcked;
        public bool mapReady;
        bool mapSchemaAcked;

        double nextOutPackageTime;
        int snapshotInterval;
        int maxBPS;
        int snapshotServerLastWritten;

        int snapshotPackageBaseline;

        // flags for ack of individual snapshots indexed by client sequence
        bool[] snapshotAcks = new bool[NetworkConfig.clientAckCacheSize];
        // corresponding server baseline no for each client seq
        int[] snapshotSeqs = new int[NetworkConfig.clientAckCacheSize];
        public int maxSnapshotAck;
        int maxSnapshotTime;

        int joinSequence;

        int commandSequenceIn;
        int commandSequenceProcessed;
        NetworkSchema commandSchema;
        SequenceBuffer<CommandInfo> commandsIn = new SequenceBuffer<CommandInfo>(NetworkConfig.commandServerQueueSize, () => new CommandInfo());

        CommandInfo defaultCommandInfo = new CommandInfo();
    }

    public void StartNetworkProfile()
    {
        m_NetworkCompressionCapture = new NetworkCompressionCapture(NetworkConfig.maxContexts, serverInfo.compressionModel);
    }

    public void EndNetworkProfile(string filepath)
    {
        byte[] model = m_NetworkCompressionCapture.AnalyzeAndGenerateModel();
        if (filepath != null)
        {
            System.IO.File.WriteAllBytes(filepath, model);
        }

        m_NetworkCompressionCapture = null;
    }

    INetworkTransport m_Transport;

    float m_ServerSimTime;                                  // The time it took to simulate the last update
    MapInfo m_MapInfo = new MapInfo();

    int m_ServerSequence = 1;

    // Entity count of entire snapshot
    uint m_LastEntityCount;

    Dictionary<ushort, NetworkEventType> m_EventTypesOut = new Dictionary<ushort, NetworkEventType>();

    Dictionary<ushort, EntityTypeInfo> m_EntityTypes = new Dictionary<ushort, EntityTypeInfo>();
    List<EntityInfo> m_Entities = new List<EntityInfo>();
    List<int> m_FreeEntities = new List<int>();

    Dictionary<int, ServerConnection> m_Connections = new Dictionary<int, ServerConnection>();

    List<EntityTypeInfo> m_TempTypeList = new List<EntityTypeInfo>();
    List<int> m_TempSpawnList = new List<int>();
    List<int> m_TempDespawnList = new List<int>();
    List<int> m_TempUpdateList = new List<int>();

    NetworkCompressionCapture m_NetworkCompressionCapture = null;

    WorldSnapshot[] m_Snapshots;
    uint* m_Prediction;
    int m_PredictionIndex;

    public int statsSnapshotData;
    public int statsGeneratedEntitySnapshots;
    public int statsSentUpdates;
    public int statsProcessedOutgoing;
    public int statsSentOutgoing;
    public int statsGeneratedSnapshotSize;

    //Counters m_Counters = new Counters();

}
