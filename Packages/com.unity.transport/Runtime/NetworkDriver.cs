using System;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Networking.Transport.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Utilities;
using Unity.Jobs;
using UnityEngine.Assertions;
using Random = System.Random;

namespace Unity.Networking.Transport
{
    using UdpNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;


    public struct BasicNetworkDriver<T> : INetworkDriver, INetworkPacketReceiver where T : struct, INetworkInterface
    {
        public Concurrent ToConcurrent()
        {
            Concurrent concurrent;
            concurrent.m_EventQueue = m_EventQueue.ToConcurrent();
            concurrent.m_ConnectionList = m_ConnectionList;
            concurrent.m_DataStream = m_DataStream;
            concurrent.m_NetworkInterface = m_NetworkInterface;
            return concurrent;
        }
        public struct Concurrent
        {
            public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out DataStreamReader slice)
            {
                int offset, size;
                slice = default(DataStreamReader);
                if (connectionId.m_NetworkId < 0 || connectionId.m_NetworkId >= m_ConnectionList.Length ||
                    m_ConnectionList[connectionId.m_NetworkId].Version != connectionId.m_NetworkVersion)
                    return (int) NetworkEvent.Type.Empty;
                var type = m_EventQueue.PopEventForConnection(connectionId.m_NetworkId, out offset, out size);
                if (size > 0)
                    slice = new DataStreamReader(m_DataStream, offset, size);
                return type;
            }

            public unsafe int Send(NetworkConnection id, DataStreamWriter strm)
            {
                if (strm.IsCreated && strm.Length > 0)
                {
                    return Send(id, (IntPtr) strm.GetUnsafeReadOnlyPtr(), strm.Length);
                }
                return 0;
            }

            public int Send(NetworkConnection id, IntPtr data, int len)
            {
                if (id.m_NetworkId < 0 || id.m_NetworkId >= m_ConnectionList.Length)
                    return 0;
                var connection = m_ConnectionList[id.m_NetworkId];
                if (connection.Version != id.m_NetworkVersion)
                    return 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (connection.State == NetworkConnection.State.Connecting)
                    throw new InvalidOperationException("Cannot send data while connecting");
#endif

                // update last attempt;
                var header = new UdpCHeader
                {
                    Type = (int) UdpCProtocol.Data,
                    SessionToken = connection.SendToken
                };

                unsafe
                {
                    if (connection.DidReceiveData == 0)
                    {
                        header.Flags = 1;
                        return NativeSend(m_NetworkInterface, ref header, (void*) data, len, &connection.ReceiveToken, 2, connection.Address);
                    }
                    return NativeSend(m_NetworkInterface, ref header, (void*) data, len, null, 0, connection.Address);
                }
            }

            internal NetworkEventQueue.Concurrent m_EventQueue;
            [ReadOnly] internal NativeList<Connection> m_ConnectionList;
            [ReadOnly] internal DataStreamWriter m_DataStream;
            internal T m_NetworkInterface;
        }

        /// <summary>
        /// Connection is the internal representation of a connection.
        /// </summary>
        internal struct Connection
        {
            public NetworkEndPoint Address;
            public long LastAttempt;
            public int Id;
            public int Version;
            public int Attempts;
            public NetworkConnection.State State;
            public ushort ReceiveToken;
            public ushort SendToken;
            public byte DidReceiveData;

            public static bool operator ==(Connection lhs, Connection rhs)
            {
                return lhs.Id == rhs.Id && lhs.Version == rhs.Version && lhs.Address == rhs.Address;
            }

            public static bool operator !=(Connection lhs, Connection rhs)
            {
                return lhs.Id != rhs.Id || lhs.Version != rhs.Version || lhs.Address != rhs.Address;
            }

            public override bool Equals(object compare)
            {
                return this == (Connection) compare;
            }

            public static Connection Null => new Connection() {Id = 0, Version = 0};

            public override int GetHashCode()
            {
                return Id;
            }

            public bool Equals(Connection connection)
            {
                return connection.Id == Id && connection.Version == Version && connection.Address == Address;
            }
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// internal variables :::::::::::::::::::::::::::::::::::::::::::::::::
        T m_NetworkInterface;

        NetworkEventQueue m_EventQueue;

        NativeQueue<int> m_FreeList;
        NativeQueue<int> m_NetworkAcceptQueue;
        NativeList<Connection> m_ConnectionList;
        NativeArray<int> m_InternalState;
        NativeQueue<int> m_PendingFree;
        NativeArray<ushort> m_SessionIdCounter;

#pragma warning disable 649
        struct Parameters
        {
            public NetworkDataStreamParameter dataStream;
            public NetworkConfigParameter config;
        }
#pragma warning restore 649

        private Parameters m_NetworkParams;
        private DataStreamWriter m_DataStream;

        [NativeSetClassTypeToNullOnSchedule]
        private Timer m_timer;
        private long m_updateTime;

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// properties :::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private const int InternalStateListening = 0;
        private const int InternalStateBound = 1;
        public bool Listening
        {
            get { return (m_InternalState[InternalStateListening] != 0); }
            internal set { m_InternalState[InternalStateListening] = value ? 1 : 0; }
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public BasicNetworkDriver(params INetworkParameter[] param)
        {
            m_timer = new Timer();
            m_updateTime = m_timer.ElapsedMilliseconds;
            m_NetworkParams = default(Parameters);
            object boxedParams = m_NetworkParams;
            foreach (var field in typeof(Parameters).GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                for (int i = 0; i < param.Length; ++i)
                {
                    if (field.FieldType.IsAssignableFrom(param[i].GetType()))
                        field.SetValue(boxedParams, param[i]);
                }
            }

            m_NetworkParams = (Parameters) boxedParams;

            if (m_NetworkParams.config.maxConnectAttempts == 0)
                m_NetworkParams.config.maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts;

            if (m_NetworkParams.config.connectTimeout == 0)
                m_NetworkParams.config.connectTimeout = NetworkParameterConstants.ConnectTimeout;

            if (m_NetworkParams.config.disconnectTimeout == 0)
                m_NetworkParams.config.disconnectTimeout = NetworkParameterConstants.DisconnectTimeout;

            int initialStreamSize = m_NetworkParams.dataStream.size;
            if (initialStreamSize == 0)
                initialStreamSize = NetworkParameterConstants.DriverDataStreamSize;
            m_DataStream = new DataStreamWriter(initialStreamSize, Allocator.Persistent);

            m_NetworkInterface = new T();
            m_NetworkInterface.Initialize();

            m_NetworkAcceptQueue = new NativeQueue<int>(Allocator.Persistent);

            m_ConnectionList = new NativeList<Connection>(1, Allocator.Persistent);

            m_FreeList = new NativeQueue<int>(Allocator.Persistent);
            m_EventQueue = new NetworkEventQueue(NetworkParameterConstants.InitialEventQueueSize);

            m_InternalState = new NativeArray<int>(2, Allocator.Persistent);
            m_PendingFree = new NativeQueue<int>(Allocator.Persistent);

            m_ReceiveCount = new NativeArray<int>(1, Allocator.Persistent);
            m_SessionIdCounter = new NativeArray<ushort>(1, Allocator.Persistent);
            m_SessionIdCounter[0] = (ushort)(new Random().Next() & 0xFFFF);
            ReceiveCount = 0;
            Listening = false;
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// interface implementation :::::::::::::::::::::::::::::::::::::::::::
        public void Dispose()
        {
            m_NetworkInterface.Dispose();
            m_DataStream.Dispose();

            m_EventQueue.Dispose();

            m_NetworkAcceptQueue.Dispose();
            m_ConnectionList.Dispose();
            m_FreeList.Dispose();
            m_InternalState.Dispose();
            m_PendingFree.Dispose();
            m_ReceiveCount.Dispose();
            m_SessionIdCounter.Dispose();
        }

        struct UpdateJob : IJob
        {
            public BasicNetworkDriver<T> driver;

            public void Execute()
            {
                driver.InternalUpdate();
            }
        }

        public JobHandle ScheduleUpdate(JobHandle dep = default(JobHandle))
        {
            m_updateTime = m_timer.ElapsedMilliseconds;
            var job = new UpdateJob {driver = this};
            var handle = job.Schedule(dep);
            return m_NetworkInterface.ScheduleReceive(this, handle);
        }

        void InternalUpdate()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            for (int i = 0; i < m_ConnectionList.Length; ++i)
            {
                int conCount = m_EventQueue.GetCountForConnection(i);
                if (conCount != 0 && m_ConnectionList[i].State != NetworkConnection.State.Disconnected)
                {
                    UnityEngine.Debug.LogError("Resetting event queue with pending events (Count=" +
                                               conCount +
                                               ", ConnectionID="+i+") Listening: " + Listening);
                }
            }
#endif
            int free;
            while (m_PendingFree.TryDequeue(out free))
            {
                int ver = m_ConnectionList[free].Version + 1;
                if (ver == 0)
                    ver = 1;
                m_ConnectionList[free] = new Connection {Id = free, Version = ver};
                m_FreeList.Enqueue(free);
            }

            m_EventQueue.Clear();
            m_DataStream.Clear();
            CheckTimeouts();
        }

        public int Bind(NetworkEndPoint endpoint)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");
            if (m_InternalState[InternalStateBound] != 0)
                throw new InvalidOperationException(
                    "Bind can only be called once per NetworkDriver");
            if (m_ConnectionList.Length > 0)
                throw new InvalidOperationException(
                    "Bind cannot be called after establishing connections");
#endif
            var ret = m_NetworkInterface.Bind(endpoint);
            if (ret == 0)
                m_InternalState[InternalStateBound] = 1;
            return ret;
        }

        public int Listen()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");
            if (Listening)
                throw new InvalidOperationException(
                    "Listen can only be called once per NetworkDriver");
            if (m_InternalState[InternalStateBound] == 0)
                throw new InvalidOperationException(
                    "Listen can only be called after a successful call to Bind");
#endif
            if (m_InternalState[InternalStateBound] == 0)
                return -1;
            Listening = true;
            return 0;
        }

        public NetworkConnection Accept()
        {
            if (!Listening)
                return default(NetworkConnection);

            int id;
            if (!m_NetworkAcceptQueue.TryDequeue(out id))
                return default(NetworkConnection);
            return new NetworkConnection {m_NetworkId = id, m_NetworkVersion = m_ConnectionList[id].Version};
        }

        public NetworkConnection Connect(NetworkEndPoint endpoint)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");
#endif
            int id;
            if (!m_FreeList.TryDequeue(out id))
            {
                id = m_ConnectionList.Length;
                m_ConnectionList.Add(new Connection{Id = id, Version = 1});
            }

            int ver = m_ConnectionList[id].Version;
            var c = new Connection
            {
                Id = id,
                Version = ver,
                State = NetworkConnection.State.Connecting,
                Address = endpoint,
                Attempts = 1,
                LastAttempt = m_updateTime,
                SendToken = 0,
                ReceiveToken = m_SessionIdCounter[0]
            };
            m_SessionIdCounter[0] = (ushort)(m_SessionIdCounter[0] + 1);

            m_ConnectionList[id] = c;
            var netcon = new NetworkConnection {m_NetworkId = id, m_NetworkVersion = ver};
            SendConnectionRequest(c);

            return netcon;
        }

        void SendConnectionRequest(Connection c)
        {
            var header = new UdpCHeader
            {
                Type = (int) UdpCProtocol.ConnectionRequest,
                SessionToken = c.ReceiveToken
            };

            unsafe
            {
                // TODO: Actually use data part instead of header
                if (NativeSend(m_NetworkInterface, ref header, null, 0, null, 0, c.Address) <= 0)
                {
                    UnityEngine.Debug.LogError("Failed to send connect message");
                }
            }
        }

        public int Disconnect(NetworkConnection id)
        {
            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null)
                return 0;

            if (connection.State == NetworkConnection.State.Connected)
            {
                SendPacket(UdpCProtocol.Disconnect, id);
            }
            RemoveConnection(connection);

            return 0;
        }

        public NetworkConnection.State GetConnectionState(NetworkConnection con)
        {
            Connection connection;
            if ((connection = GetConnection(con)) == Connection.Null)
                return NetworkConnection.State.Disconnected;
            return connection.State;
        }

        public NetworkEndPoint RemoteEndPoint(NetworkConnection id)
        {
            if (id.m_NetworkId == 0)
                return m_NetworkInterface.RemoteEndPoint;

            throw new NotImplementedException();
        }

        public NetworkEndPoint LocalEndPoint()
        {
            return m_NetworkInterface.LocalEndPoint;
        }

        public int Send(NetworkConnection id, DataStreamWriter strm)
        {
            unsafe
            {
                return Send(id, (IntPtr) strm.GetUnsafeReadOnlyPtr(), strm.Length);
            }
        }

        public int Send(NetworkConnection id, IntPtr data, int len)
        {
            if (id.m_NetworkId < 0 || id.m_NetworkId >= m_ConnectionList.Length)
                return 0;
            var connection = m_ConnectionList[id.m_NetworkId];
            if (connection.Version != id.m_NetworkVersion)
                return 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (connection.State == NetworkConnection.State.Connecting)
                throw new InvalidOperationException("Cannot send data while connecting");
#endif

            // update last attempt;
            var header = new UdpCHeader
            {
                Type = (int) UdpCProtocol.Data,
                SessionToken = connection.SendToken
            };

            unsafe
            {
                return NativeSend(m_NetworkInterface, ref header, (void*) data, len, null, 0, connection.Address);
            }
        }

        public NetworkEvent.Type PopEvent(out NetworkConnection con, out DataStreamReader slice)
        {
            int offset, size;
            slice = default(DataStreamReader);
            int id;
            var type = m_EventQueue.PopEvent(out id, out offset, out size);
            if (size > 0)
                slice = new DataStreamReader(m_DataStream, offset, size);
            con = id < 0
                ? default(NetworkConnection)
                : new NetworkConnection {m_NetworkId = id, m_NetworkVersion = m_ConnectionList[id].Version};
            return type;
        }

        public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out DataStreamReader slice)
        {
            int offset, size;
            slice = default(DataStreamReader);
            if (connectionId.m_NetworkId < 0 || connectionId.m_NetworkId >= m_ConnectionList.Length ||
                 m_ConnectionList[connectionId.m_NetworkId].Version != connectionId.m_NetworkVersion)
                return (int) NetworkEvent.Type.Empty;
            var type = m_EventQueue.PopEventForConnection(connectionId.m_NetworkId, out offset, out size);
            if (size > 0)
                slice = new DataStreamReader(m_DataStream, offset, size);
            return type;
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// internal helper functions ::::::::::::::::::::::::::::::::::::::::::
        void AddConnection(int id)
        {
            m_EventQueue.PushEvent(new NetworkEvent {connectionId = id, type = NetworkEvent.Type.Connect});
        }

        void AddDisconnection(int id)
        {
            m_EventQueue.PushEvent(new NetworkEvent {connectionId = id, type = NetworkEvent.Type.Disconnect});
        }

        Connection GetConnection(NetworkConnection id)
        {
            var con = m_ConnectionList[id.m_NetworkId];
            if (con.Version != id.m_NetworkVersion)
                return Connection.Null;
            return con;
        }

        Connection GetConnection(NetworkEndPoint address, ushort sessionId)
        {
            for (int i = 0; i < m_ConnectionList.Length; i++)
            {
                if (address == m_ConnectionList[i].Address && m_ConnectionList[i].ReceiveToken == sessionId )
                    return m_ConnectionList[i];
            }

            return Connection.Null;
        }
        
        Connection GetNewConnection(NetworkEndPoint address, ushort sessionId)
        {
            for (int i = 0; i < m_ConnectionList.Length; i++)
            {
                if (address == m_ConnectionList[i].Address && m_ConnectionList[i].SendToken == sessionId )
                    return m_ConnectionList[i];
            }

            return Connection.Null;
        }

        void SetConnection(Connection connection)
        {
            m_ConnectionList[connection.Id] = connection;
        }

        bool RemoveConnection(Connection connection)
        {
            if (connection.State != NetworkConnection.State.Disconnected && connection == m_ConnectionList[connection.Id])
            {
                connection.State = NetworkConnection.State.Disconnected;
                m_ConnectionList[connection.Id] = connection;
                m_PendingFree.Enqueue(connection.Id);

                return true;
            }

            return false;
        }

        bool UpdateConnection(Connection connection)
        {
            if (connection == m_ConnectionList[connection.Id])
            {
                SetConnection(connection);
                return true;
            }

            return false;
        }

        int SendPacket(UdpCProtocol type, Connection connection)
        {
            var header = new UdpCHeader
            {
                Type = (byte) type,
                SessionToken = connection.SendToken
            };

            unsafe
            {
                if (connection.DidReceiveData == 0)
                {
                    header.Flags = 1;
                    return NativeSend(m_NetworkInterface, ref header, &connection.ReceiveToken, 2, null, 0, connection.Address);
                }
                return NativeSend(m_NetworkInterface, ref header, null, 0, null, 0, connection.Address);
            }
        }

        int SendPacket(UdpCProtocol type, NetworkConnection id)
        {
            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null)
                return 0;

            return SendPacket(type, connection);
        }

        static unsafe int NativeSend(T networkInterface, ref UdpCHeader header, void* payload, int payloadSize, 
            void* footer, int footerLen, NetworkEndPoint remote)
        {
            var iov = stackalloc network_iovec[3];
            int result;

            fixed (byte* ptr = header.Data)
            {
                iov[0].buf = ptr;
                iov[0].len = UdpCHeader.Length;

                iov[1].buf = payload;
                iov[1].len = payloadSize;

                iov[2].buf = footer;
                iov[2].len = footerLen;

                result = networkInterface.SendMessage(iov, 3, ref remote);
            }

            if (result == -1)
            {
                int error = Marshal.GetLastWin32Error();
                throw new SocketException(error);
            }

            return result;
        }

        void CheckTimeouts()
        {
            for (int i = 0; i < m_ConnectionList.Length; ++i)
            {
                var connection = m_ConnectionList[i];
                if (connection == Connection.Null)
                    continue;

                long now = m_updateTime;

                var netcon = new NetworkConnection {m_NetworkId = connection.Id, m_NetworkVersion = connection.Version};
                if ((connection.State == NetworkConnection.State.Connecting ||
                     connection.State == NetworkConnection.State.AwaitingResponse) &&
                    now - connection.LastAttempt > m_NetworkParams.config.connectTimeout)
                {
                    if (connection.Attempts >= m_NetworkParams.config.maxConnectAttempts)
                    {
                        RemoveConnection(connection);
                        AddDisconnection(connection.Id);
                        continue;
                    }

                    connection.Attempts = ++connection.Attempts;
                    connection.LastAttempt = now;
                    SetConnection(connection);

                    if (connection.State == NetworkConnection.State.Connecting)
                        SendConnectionRequest(connection);
                    else
                        SendPacket(UdpCProtocol.ConnectionAccept, netcon);
                }

                if (connection.State == NetworkConnection.State.Connected &&
                    now - connection.LastAttempt > m_NetworkParams.config.disconnectTimeout)
                {
                    Disconnect(netcon);
                    AddDisconnection(connection.Id);
                }
            }
        }

        public DataStreamWriter GetDataStream()
        {
            return m_DataStream;
        }

        private NativeArray<int> m_ReceiveCount;
        public int ReceiveCount {
            get { return m_ReceiveCount[0]; }
            set { m_ReceiveCount[0] = value; }
        }

        public bool DynamicDataStreamSize()
        {
            return m_NetworkParams.dataStream.size == 0;
        }

        public unsafe int AppendPacket(NetworkEndPoint address, UdpCHeader header, int dataLen)
        {
            int count = 0;
            switch ((UdpCProtocol) header.Type)
            {
                case UdpCProtocol.ConnectionRequest:
                {
                    if (!Listening)
                        return 0;

                    Connection c;
                    if ((c = GetNewConnection(address, header.SessionToken)) == Connection.Null || c.State == NetworkConnection.State.Disconnected)
                    {
                        int id;
                        var sessionId = m_SessionIdCounter[0];
                        m_SessionIdCounter[0] = (ushort) (m_SessionIdCounter[0] + 1);
                        if (!m_FreeList.TryDequeue(out id))
                        {
                            id = m_ConnectionList.Length;
                            m_ConnectionList.Add(new Connection{Id = id, Version = 1});
                        }

                        int ver = m_ConnectionList[id].Version;
                        c = new Connection
                        {
                            Id = id,
                            Version = ver,
                            ReceiveToken = sessionId,
                            SendToken = header.SessionToken,
                            State = NetworkConnection.State.Connected,
                            Address = address,
                            Attempts = 1,
                            LastAttempt = m_updateTime
                        };
                        SetConnection(c);
                        m_NetworkAcceptQueue.Enqueue(id);
                        count++;
                    }
                    else
                    {
                        c.Attempts++;
                        c.LastAttempt = m_updateTime;
                        SetConnection(c);
                    }

                    SendPacket(UdpCProtocol.ConnectionAccept,
                        new NetworkConnection {m_NetworkId = c.Id, m_NetworkVersion = c.Version});
                }
                    break;
                case UdpCProtocol.ConnectionReject:
                {
                    // m_EventQ.Enqueue(Id, (int)NetworkEvent.Connect);
                }
                    break;
                case UdpCProtocol.ConnectionAccept:
                {
                    if (header.Flags != 1)
                    {
                        UnityEngine.Debug.LogError("Accept message received without flag set");
                        return 0;
                    }
                    
                    Connection c = GetConnection(address, header.SessionToken);
                    if (c != Connection.Null)
                    {
                        c.DidReceiveData = 1;
                        
                        if (c.State == NetworkConnection.State.Connected)
                        {
                            //DebugLog("Dropping connect request for an already connected endpoint [" + address + "]");
                            return 0;
                        }

                        if (c.State == NetworkConnection.State.Connecting)
                        {
                            var sliceOffset = m_DataStream.Length;
                            m_DataStream.WriteBytesWithUnsafePointer(2);
                            var dataStreamReader = new DataStreamReader(m_DataStream, sliceOffset, 2);
                            var context = default(DataStreamReader.Context);
                            c.SendToken = dataStreamReader.ReadUShort(ref context);
                            m_DataStream.WriteBytesWithUnsafePointer(-2);
                            
                            c.State = NetworkConnection.State.Connected;
                            UpdateConnection(c);
                            AddConnection(c.Id);
                            count++;
                        }
                    }
                }
                    break;
                case UdpCProtocol.Disconnect:
                {
                    Connection c = GetConnection(address, header.SessionToken);
                    if (c != Connection.Null)
                    {
                        if (RemoveConnection(c))
                            AddDisconnection(c.Id);
                        count++;
                    }
                }
                    break;
                case UdpCProtocol.Data:
                {
                    Connection c = GetConnection(address, header.SessionToken);
                    if (c == Connection.Null)
                        return 0;

                    c.DidReceiveData = 1;
                    c.LastAttempt = m_updateTime;
                    UpdateConnection(c);
                    
                    var length = dataLen - UdpCHeader.Length;

                    if (c.State == NetworkConnection.State.Connecting)
                    {
                        if (header.Flags != 1)
                        {
                            UnityEngine.Debug.LogError("Received data without connection (no send token)");
                            return 0;
                        }
                        
                        var tokenOffset = m_DataStream.Length + length - 2;
                        m_DataStream.WriteBytesWithUnsafePointer(length);
                        var dataStreamReader = new DataStreamReader(m_DataStream, tokenOffset, 2);
                        var context = default(DataStreamReader.Context);
                        c.SendToken = dataStreamReader.ReadUShort(ref context);
                        m_DataStream.WriteBytesWithUnsafePointer(-length);
                        
                        c.State = NetworkConnection.State.Connected;
                        UpdateConnection(c);
                        Assert.IsTrue(!Listening);
                        AddConnection(c.Id);
                        count++;
                    }

                    if (header.Flags == 1)
                        length -= 2;
                    
                    var sliceOffset = m_DataStream.Length;
                    m_DataStream.WriteBytesWithUnsafePointer(length);

                    m_EventQueue.PushEvent(new NetworkEvent
                    {
                        connectionId = c.Id,
                        type = NetworkEvent.Type.Data,
                        offset = sliceOffset,
                        size = length
                    });
                    count++;
                } break;
            }

            return count;
        }
    }
}
