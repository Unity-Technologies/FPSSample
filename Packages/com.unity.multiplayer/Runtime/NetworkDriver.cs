//#define DEBUG_LOG

using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;

using Experimental.Multiplayer.Protocols;
using Experimental.Multiplayer.Utilities;
using UnityEngine;

using Random = Experimental.Multiplayer.Utilities.Random;

namespace Experimental.Multiplayer
{
    using UdpNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;
    
    public enum NetworkFamily
    {
        UdpIpv4 = 0,
        IPC = 1
    }

    public unsafe struct NetworkEndPoint
    {
        public NetworkFamily family;
        public fixed byte address[4];
        public ushort port;
        
        public bool IsValid
        {
            get
            {
                var addr = this;
                return (int)family != 0 || addr.address[0] != 0 || addr.address[1] != 0 || addr.address[2] != 0 || addr.address[3] != 0 || port != 0;
            }
        }

        public static implicit operator NetworkEndPoint(IPEndPoint endpoint)
        {
            var netep = new NetworkEndPoint
            {
                family = NetworkFamily.UdpIpv4,
                port = (ushort)endpoint.Port
            };
            for (int i = 0; i < 4; ++i)
                netep.address[i] = endpoint.Address.GetAddressBytes()[i];
            return netep;
        }

        public static unsafe network_address ToNetworkAddress(NetworkEndPoint ep)
        {
            switch (ep.family)
            {
                case NetworkFamily.UdpIpv4:
                    return SocketExtension.MarshalIpV4Address(ep.address, ep.port);
                case NetworkFamily.IPC:
                    network_address addr = default(network_address);
                    // TODO: Double check this works on ios as well.
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
                    addr.family.sa_family = (byte) AddressFamily.Unspecified;
#else
                    addr.family.sa_family = (ushort) AddressFamily.Unspecified;
#endif
                    addr.ipc_handle = *(int*) ep.address;
                    addr.length = 6;
                    return addr;
                default:
                    throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            fixed (byte* ptr = address)
            {
                return ptr[0] + "." + ptr[1] + "." + ptr[2] + "." + ptr[3] + ":" + port;
            }
        }
    }
    
    public struct BasicNetworkDriver<T> : INetworkDriver where T : struct, INetworkInterface
    {
        /// <summary>
        /// Connection is the internal representation of a connection.
        /// </summary>
        struct Connection
        {
            public int Id;
            public int Version;
            public network_address Address;
            public int Attempts;
            public long LastAttempt;
            public NetworkConnection.State State;

            public static bool operator ==(Connection lhs, Connection rhs)
            {
                return lhs.Id == rhs.Id && lhs.Version == rhs.Version && lhs.Address.ReallyEquals(rhs.Address);
            }

            public static bool operator !=(Connection lhs, Connection rhs)
            {
                return lhs.Id != rhs.Id || lhs.Version != rhs.Version || !lhs.Address.ReallyEquals(rhs.Address);
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
                return connection.Id == Id && connection.Version == Version && connection.Address.ReallyEquals(Address);
            }
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// internal variables :::::::::::::::::::::::::::::::::::::::::::::::::
        T m_NetworkInterface;

        NetworkEventQueue m_EventQueue;
        
        ConnectionFreeList m_FreeList;
        NativeQueue<int> m_NetworkAcceptQueue;
        NativeArray<Connection> m_ConnectionList;
        NativeArray<int> m_InternalState;
        NativeQueue<int> m_PendingFree;

        struct Parameters
        {
            public NetworkBitStreamParameter bitStream;
            public NetworkConfigParameter config;
        }

        private Parameters m_NetworkParams;
        private BitStream m_BitStream;
        
        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// properties :::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public bool Listening
        {
            get { return (m_InternalState[0] != 0); }
            internal set { m_InternalState[0] = value ? 1 : 0; }
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public BasicNetworkDriver(params INetworkParameter[] param)
        {
            m_NetworkParams = default(Parameters);
            object boxedParams = m_NetworkParams;
            foreach (var field in typeof(Parameters).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                for (int i = 0; i < param.Length; ++i)
                {
                    if (field.FieldType.IsAssignableFrom(param[i].GetType()))
                        field.SetValue(boxedParams, param[i]);
                }
            }

            m_NetworkParams = (Parameters)boxedParams;

            if (m_NetworkParams.bitStream.size == 0)
                m_NetworkParams.bitStream.size = NetworkParameterConstants.DriverBitStreamSize;

            if (m_NetworkParams.config.maxConnectAttempts == 0)
                m_NetworkParams.config.maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts;

            if (m_NetworkParams.config.connectTimeout == 0)
                m_NetworkParams.config.connectTimeout = NetworkParameterConstants.ConnectTimeout;

            if (m_NetworkParams.config.disconnectTimeout == 0)
                m_NetworkParams.config.disconnectTimeout = NetworkParameterConstants.DisconnectTimeout;

            m_BitStream = new BitStream(m_NetworkParams.bitStream.size, Allocator.Persistent);

            m_NetworkInterface = new T();
            m_NetworkInterface.Initialize();

            m_NetworkAcceptQueue = new NativeQueue<int>(Allocator.Persistent);

            m_ConnectionList = new NativeArray<Connection>(NetworkParameterConstants.MaximumConnectionsSupported, Allocator.Persistent);
                
            m_FreeList = new ConnectionFreeList(NetworkParameterConstants.MaximumConnectionsSupported);
            m_EventQueue = new NetworkEventQueue(NetworkParameterConstants.MaximumConnectionsSupported,
                NetworkParameterConstants.NetworkEventQLength);
            
            for (int i = 0; i < NetworkParameterConstants.MaximumConnectionsSupported; i++)
                m_ConnectionList[i] = new Connection{Id = i, Version = 1};

            m_InternalState = new NativeArray<int>(1, Allocator.Persistent);
            m_PendingFree = new NativeQueue<int>(Allocator.Persistent);
            Listening = false;            
        }
        
        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// interface implementation :::::::::::::::::::::::::::::::::::::::::::

        public void Dispose()
        {
            m_NetworkInterface.Dispose();
            m_BitStream.Dispose();

            m_EventQueue.Dispose();

            m_NetworkAcceptQueue.Dispose();
            m_ConnectionList.Dispose();
            m_FreeList.Dispose();
            m_InternalState.Dispose();
            m_PendingFree.Dispose();
        }

        public int Update()
        {
            int free;
            while (m_PendingFree.TryDequeue(out free))
            {
                int ver = m_ConnectionList[free].Version + 1;
                if (ver == 0)
                    ver = 1;
                m_ConnectionList[free] = new Connection {Id = free, Version = ver};
                m_FreeList.ReleaseConnectionId(free);
            }

            if (m_EventQueue.Count != 0)
                Debug.LogError("Resetting event queue with pending events (Count=" + m_EventQueue.Count + ")");
            m_EventQueue.Reset();
            m_BitStream.Reset();
            CheckTimeouts();
            return ProcessPackets(m_BitStream);
        }

        public int Bind(NetworkEndPoint endpoint)
        {
            try
            {
                m_NetworkInterface.Bind(endpoint);
            }
            catch (SocketException e)
            {
                var SocketError = e.SocketErrorCode;
                return (int)SocketError;
            }

            DebugLog("Binding " + endpoint);
            return 0;
        }

        public int Listen()
        {
            DebugLog("Listening=true");
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
			DebugLog("Accept ID=" + id);
            return new NetworkConnection{m_NetworkId = id, m_NetworkVersion = m_ConnectionList[id].Version};
        }

        public unsafe NetworkConnection Connect(NetworkEndPoint endpoint)
        {
            if (Listening)
                throw new SocketException(10048);

            var address = NetworkEndPoint.ToNetworkAddress(endpoint);

            int id;
            if (!m_FreeList.AquireConnectionId(out id))
            {
                return default(NetworkConnection);
            }

            int ver = m_ConnectionList[id].Version;
            var c = new Connection
            {
                Id = id,
                Version = ver,
                State = NetworkConnection.State.Connecting,
                Address = address,
                Attempts = 1,
                LastAttempt = Timer.ElapsedMilliseconds
                // PacketProcessor = packetProcessor
            };

            DebugLog("Connecting to EndPoint=" + endpoint + " ID=" + id);
            m_ConnectionList[id] = c;
            var netcon = new NetworkConnection {m_NetworkId = id, m_NetworkVersion = ver};
            SendPacket(UdpCProtocol.ConnectionRequest, netcon);

            return netcon;
        }

        public int Disconnect(NetworkConnection id)
        {
            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null)
                return 0;

            if (connection.State == NetworkConnection.State.Connected)
            {
                SendPacket(UdpCProtocol.Disconnect, id);
                AddDisconnection(id.m_NetworkId);
                RemoveConnection(connection);
            }
            
            DebugLog("Disconnecting ID=" + id.m_NetworkId);
            return 0;
        }

        public NetworkConnection.State GetConnectionState(NetworkConnection con)
        {
            Connection connection;
            if ((connection = GetConnection(con)) == Connection.Null)
                return NetworkConnection.State.Destroyed;
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

        public int Send(NetworkConnection id, BitStream bs)
        {
            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null || connection.State == NetworkConnection.State.Destroyed)
                return 0;

            // update last attempt;
            var header = new UdpCHeader
            {
                Type = (int) UdpCProtocol.Data
            };

            unsafe
            {
                return NativeSend(ref header, bs.UnsafeDataPtr, bs.GetBytesWritten(), connection.Address);
            }
        }

        public NetworkEvent.Type PopEvent(out NetworkConnection con, out BitSlice slice)
        {
            int offset, size;
            slice = default(BitSlice);
            int id;
            var type = m_EventQueue.PopEvent(out id, out offset, out size);
            if (size > 0)
                slice = m_BitStream.GetBitSlice(offset, size);
            con = id < 0 ? default(NetworkConnection) : new NetworkConnection {m_NetworkId = id, m_NetworkVersion = m_ConnectionList[id].Version};
            DebugLog("PopEvent ID=" + id + " Type=" + type + " Offset=" + offset + " Size=" + size);
            return type;
        }

        public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out BitSlice slice)
        {
            int offset, size;
            slice = default(BitSlice);
            if (m_ConnectionList[connectionId.m_NetworkId].Version != connectionId.m_NetworkVersion)
                return (int)NetworkEvent.Type.Empty;
            var type = m_EventQueue.PopEventForConnection(connectionId.m_NetworkId, out offset, out size);
            if (size > 0)
                slice = m_BitStream.GetBitSlice(offset, size);
            return type;
        }

        public void DebugLog(string message)
        {
#if DEBUG_LOG
            Debug.Log("[" + DateTime.Now.ToString("yyyyMMddHHmmss") + "] [DEBUG] " + "[" + LocalEndPoint() + "] " + message);
#endif
        }
        
        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// internal helper functions ::::::::::::::::::::::::::::::::::::::::::

        void AddConnection(int id)
        {
            DebugLog("PushEvent ID=" + id + " Type=" + NetworkEvent.Type.Connect + " Offset=" + 0 + " Size=" + 0);
            m_EventQueue.PushEvent(new NetworkEvent {connectionId = id, type = NetworkEvent.Type.Connect});
            m_NetworkAcceptQueue.Enqueue(id);
        }

        void AddDisconnection(int id)
        {
            DebugLog("PushEvent ID=" + id + " Type=" + NetworkEvent.Type.Disconnect + " Offset=" + 0 + " Size=" + 0);
            m_EventQueue.PushEvent(new NetworkEvent {connectionId = id, type = NetworkEvent.Type.Disconnect});
        }
        
        Connection GetConnection(NetworkConnection id)
        {
            var con = m_ConnectionList[id.m_NetworkId];
            if (con.Version != id.m_NetworkVersion)
                return Connection.Null;
            return con;
        }

        Connection GetConnection(network_address address)
        {
            for (int i = 0; i < m_ConnectionList.Length; i++)
            {
                if (address.ReallyEquals(m_ConnectionList[i].Address))
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
            if (connection.State != NetworkConnection.State.Destroyed && connection == m_ConnectionList[connection.Id])
            {
                connection.State = NetworkConnection.State.Destroyed;
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

        int SendPacket(UdpCProtocol type, network_address address)
        {
            var header = new UdpCHeader
            {
                Type = (byte) type
            };

            unsafe
            {
                return NativeSend(ref header, null, 0, address);
            }
        }
        
        int SendPacket(UdpCProtocol type, NetworkConnection id)
        {
            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null)
                return 0;

            DebugLog("SendPacket " + " ID=" + id.m_NetworkId + " Attempts=" + connection.Attempts + " LastAttempt=" + connection.LastAttempt);

            return SendPacket(type, connection.Address);
        }
        
        unsafe int NativeSend(ref UdpCHeader header, void* payload, int payloadSize, network_address remote)
        {
            var iov = stackalloc network_iovec[2];
            int result;

            fixed (byte* ptr = header.Data)
            {
                iov[0].buf = ptr;
                iov[0].len = UdpCHeader.Length;

                iov[1].buf = payload;
                iov[1].len = payloadSize;

                result = m_NetworkInterface.SendMessage(iov, 2, ref remote);
            }
            
            DebugLog("NativeSend Type=" + (UdpCProtocol)header.Type + " HdrLength=" + UdpCHeader.Length + " PayloadSize=" + payloadSize + " Result=" + result);
            string dump = "(";
            fixed (byte* ptr = header.Data)
            {
                for (int i = 0; i < UdpCHeader.Length; ++i)
                    dump += ptr[i].ToString("X");
            }
            dump += ") ";
            for (int i = 0; i < payloadSize; ++i)
                dump += ((byte*)payload)[i].ToString("X");
            DebugLog("Dump=" + dump);

            if (result == -1)
            {
                int error = Marshal.GetLastWin32Error();
                throw new SocketException(error);
            }

            return result;
        }

        unsafe int NativeReceive(ref UdpCHeader header, void* data, int length, ref network_address address)
        {
            if (length <= 0)
            {
                Debug.LogError("Can't receive into 0 bytes or less of buffer memory");
                return 0;
            }
            var iov = stackalloc network_iovec[2];

            fixed (byte* ptr = header.Data)
            {
                iov[0].buf = ptr;
                iov[0].len = UdpCHeader.Length;

                iov[1].buf = data;
                iov[1].len = length;
            }

            var result = m_NetworkInterface.ReceiveMessage(iov, 2, ref address);
            if (result == -1)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == 10035 || err == 35 || err == 11)
                    return 0;
                
                Debug.LogError(string.Format("error on receive {0}", err));
                throw new SocketException(err);
            }

            if (result > 0)
            {
                DebugLog("NativeReceive Type=" + (UdpCProtocol)header.Type + " HdrLength=" + UdpCHeader.Length + " PayloadSize=" + length + " Result=" + result);
                string dump = "(";
                fixed (byte* ptr = header.Data)
                {
                    for (int i = 0; i < UdpCHeader.Length; ++i)
                        dump += ptr[i].ToString("X");
                }
                dump += ") ";
                for (int i = 0; i < result - UdpCHeader.Length; ++i)
                    dump += ((byte*)data)[i].ToString("X");
                DebugLog("Dump=" + dump);
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

                long now = Timer.ElapsedMilliseconds;

                var netcon = new NetworkConnection { m_NetworkId = connection.Id, m_NetworkVersion = connection.Version };
                if ((connection.State == NetworkConnection.State.Connecting || connection.State == NetworkConnection.State.AwaitingResponse) &&
                    now - connection.LastAttempt > m_NetworkParams.config.connectTimeout)
                {
                    DebugLog("Current " + now + " LastAttempt " + connection.LastAttempt);
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
                        SendPacket(UdpCProtocol.ConnectionRequest, netcon);
                    else
                        SendPacket(UdpCProtocol.ConnectionAccept, netcon);
                }

                if (connection.State == NetworkConnection.State.Connected && 
                    now - connection.LastAttempt > m_NetworkParams.config.disconnectTimeout)
                {
                    DebugLog("Disconnect Timeout " + now + " " + connection.LastAttempt + " " + m_NetworkParams.config.disconnectTimeout);
                    Disconnect(netcon);
                }
            }
        }
        
        unsafe int ProcessPackets(BitStream bs)
        {
            var address = new network_address();
            address.length = sizeof(network_address);
            var header = new UdpCHeader();
            while (true)
            {
                if (bs.WriteCapacity <= 0)
                    return 0;
                var sliceOffset = bs.GetBytesWritten();
                var result = NativeReceive(ref header, bs.UnsafeDataPtr + sliceOffset,
                    Math.Min(NetworkParameterConstants.MTU, bs.WriteCapacity), ref address);
                if (result <= 0)
                {
                    return result;
                }

                switch ((UdpCProtocol) header.Type)
                {
                    case UdpCProtocol.ConnectionRequest:
                    {
                        if (!Listening)
                            continue;

                        Connection c;
                        if ((c = GetConnection(address)) == Connection.Null || c.State == NetworkConnection.State.Destroyed)
                        {
                            int id;
                            if (!m_FreeList.AquireConnectionId(out id))
                            {
                                SendPacket(UdpCProtocol.ConnectionReject, address);
                                continue;
                            }

                            int ver = m_ConnectionList[id].Version;
                            c = new Connection()
                            {
                                Id = id,
                                Version = ver,
                                State = NetworkConnection.State.Connected,
                                Address = address,
                                Attempts = 1,
                                LastAttempt = Timer.ElapsedMilliseconds
                            };
                            SetConnection(c);
                            AddConnection(c.Id);
                        }
                        else
                        {
                            c.Attempts++;
                            c.LastAttempt = Timer.ElapsedMilliseconds;
                            SetConnection(c);
                        }

                        SendPacket(UdpCProtocol.ConnectionAccept, new NetworkConnection{m_NetworkId = c.Id, m_NetworkVersion = c.Version});
                    }
                    break;
                    case UdpCProtocol.ConnectionReject:
                    {
                        // m_EventQ.Enqueue(Id, (int)NetworkEvent.Connect);
                    }
                    break;
                    case UdpCProtocol.ConnectionAccept:
                    {
                        Connection c = GetConnection(address);
                        if (c != Connection.Null)
                        {
                            if (c.State == NetworkConnection.State.Connected)
                            {
                                //DebugLog("Dropping connect request for an already connected endpoint [" + address + "]");
                                continue;
                            }
                            if (c.State == NetworkConnection.State.Connecting)
                            {
                                c.State = NetworkConnection.State.Connected;
                                UpdateConnection(c);
                                AddConnection(c.Id);
                            }
                        }

                    }
                    break;
                    case UdpCProtocol.Disconnect:
                    {
                        DebugLog("Disconnect packet received from " + address);
                        Connection c = GetConnection(address);
                        if (c != Connection.Null)
                        {
                            RemoveConnection(c);
                            AddDisconnection(c.Id);
                        }
                    }
                    break;
                    case UdpCProtocol.Data:
                    {
                        Connection c = GetConnection(address);
                        if (c == Connection.Null)
                            continue;

                        c.LastAttempt = Timer.ElapsedMilliseconds;
                        UpdateConnection(c);

                        if (c.State == NetworkConnection.State.Connecting)
                        {
                            c.State = NetworkConnection.State.Connected;
                            UpdateConnection(c);
                            AddConnection(c.Id);
                        }

                        var length = result - UdpCHeader.Length;
                        bs.IncreaseWritePtr(length);

                        m_EventQueue.PushEvent(new NetworkEvent
                        {
                            connectionId = c.Id,
                            type = NetworkEvent.Type.Data,
                            offset = sliceOffset,
                            size = length
                        });
                    }
                    break;
                }
            }
        }
    }
}
