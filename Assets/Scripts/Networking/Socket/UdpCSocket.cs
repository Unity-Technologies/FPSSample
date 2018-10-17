/*using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace SocketExtensions
{
    public enum SocketEvent
    {
        Empty,
        Data,
        Connect,
        Disconnect
    }

    public class UdpCSocket
    {
        public UdpCSocket()
        {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.Blocking = false;
            m_TimeoutMS = 30 * 1000;
        }

        public void Bind(EndPoint ep)
        {
            try
            {
                m_Socket.Bind(ep);
            }
            catch (SocketException e)
            {
                m_SocketError =  e.SocketErrorCode;
                throw e;
            }
        }

        public void Listen()
        {
            m_Listening = true;
        }

        public int Connect(EndPoint ep)
        {
            if (m_Listening)
                throw new SocketException(10048);

            int addressLenght = 0;
            var address = SocketExtension.MarshalAddress(ep, out addressLenght);

            var id = m_NextConnectionId++;

            var c = new Connection()
            {
                Id = id,
                State = ConnectionState.Connecting,
                Address = address,
                AddressLength = addressLenght,
                Attempts = 1,
                LastAttempt = NetworkUtils.stopwatch.ElapsedMilliseconds
            };

            m_IdToConnection.Add(c.Id, c);
            SendPacket(UdpCProtocol.ConnectionRequest, id);

            return id;
        }

        public void Disconnect(int id = 0)
        {
            Connection connection;
            if (!m_IdToConnection.TryGetValue(id, out connection))
                return;

            if (connection.State == ConnectionState.Connected)
            {
                SendPacket(UdpCProtocol.Disconnect, id);

                m_IdToConnection.Remove(id);
            }
        }

        public void Close()
        {
            Disconnect();
        }

        public SocketEvent ReceiveFrom(ref byte[] data, out int size, out int id)
        {
            var now = NetworkUtils.stopwatch.ElapsedMilliseconds;
            foreach (KeyValuePair<int, Connection> entry in m_IdToConnection)
            {
                if (entry.Value.State == ConnectionState.Connecting ||
                    entry.Value.State == ConnectionState.AwaitingResponse)
                {
                    var elapsed = now - entry.Value.LastAttempt;
                    if (elapsed > ConnectTimeout * entry.Value.Attempts)
                    {
                        entry.Value.Attempts++;
                        entry.Value.LastAttempt = NetworkUtils.stopwatch.ElapsedMilliseconds;
                        if (entry.Value.State == ConnectionState.Connecting)
                            SendPacket(UdpCProtocol.ConnectionRequest, entry.Value.Id);
                        else
                            SendPacket(UdpCProtocol.ConnectionAccept, entry.Value.Id);
                    }
                }
            }

            if (m_Disconnected.Count > 0)
            {
                var connection = m_Disconnected.Dequeue();
                id = connection.Id;
                size = 0;

                return SocketEvent.Disconnect;
            }
            return ProcessPackets(ref data, out size, out id);
        }

        public void SendTo(byte[] data, int size, int id)
        {
            Connection connection;
            if (!m_IdToConnection.TryGetValue(id, out connection))
                return;

            var now = NetworkUtils.stopwatch.ElapsedMilliseconds;
            if (now - connection.LastAttempt > m_TimeoutMS)
            {
                m_Disconnected.Enqueue(connection);
                Disconnect(connection.Id);
                return;
            }

            var header = new UdpCHeader()
            {
                Type = (int)UdpCProtocol.Data
            };

            unsafe
            {
                fixed (void* ptr = data)
                {
                    NativeSend(ref header, ptr, size, connection.Address, connection.AddressLength);
                }
            }
        }

#region Properties

        public SocketError LastError { get { return m_SocketError; } }
        public bool Listening { get { return m_Listening; } }
        public int Timeout { get { return m_TimeoutMS; } set { m_TimeoutMS = value; } }

#endregion

#region Private Members

        enum ConnectionState
        {
            Disconnected,
            Connecting,
            AwaitingResponse,
            Connected
        }

        int m_TimeoutMS;

        class Connection
        {
            public int Id;
            public sockaddr_storage Address;
            public int Attempts;
            public long LastAttempt;
            public int AddressLength;
            public ConnectionState State;
        }

        int m_NextConnectionId;

        Socket m_Socket;
        SocketError m_SocketError;
        bool m_Listening;

        Dictionary<int, Connection> m_IdToConnection = new Dictionary<int, Connection>();

        const int ConnectTimeout = 1000;
        Queue<Connection> m_Disconnected = new Queue<Connection>();

        // Protocol

        enum UdpCProtocol
        {
            ConnectionRequest,
            ConnectionReject,
            ConnectionAccept,
            Disconnect,
            Data
        }

        [StructLayout(LayoutKind.Explicit)]
        unsafe struct UdpCHeader
        {
            public const int Length = 4;
            [FieldOffset(0)] public fixed byte Data[Length];
            [FieldOffset(0)] public byte Type;
            [FieldOffset(1)] public byte Reserved;
            [FieldOffset(2)] public ushort ConnectionId;
        }

        #endregion

#region Private Impl

        Connection FindConnectionByAddress(sockaddr_storage address)
        {
            if (m_IdToConnection.Count == 0)
                return null;

            foreach (KeyValuePair<int, Connection> entry in m_IdToConnection)
            {
                if (address.ReallyEquals(entry.Value.Address))
                    return entry.Value;
            }
            return null;
        }

        int SendPacket(UdpCProtocol type, int id)
        {
            Connection connection;
            if (!m_IdToConnection.TryGetValue(id, out connection))
                return 0;

            var header = new UdpCHeader()
            {
                Type = (byte)type
            };

            unsafe
            {
                return NativeSend(ref header, null, 0, connection.Address, connection.AddressLength);
            }
        }

        unsafe SocketEvent ProcessPackets(ref byte[] data, out int size, out int id)
        {
            size = 0;
            id = -1;

            var address = new sockaddr_storage();
            var addressLength = sizeof(sockaddr_storage);
            var header = new UdpCHeader();

            var result = NativeReceive(ref header, ref data, ref address, ref addressLength);
            if (result == -1)
            {
                throw new SocketException();
            }

            if (result == 0)
                return SocketEvent.Empty;

            switch ((UdpCProtocol)header.Type)
            {
                case UdpCProtocol.ConnectionRequest:
                {
                    var retval = SocketEvent.Empty;
                    if (!m_Listening)
                        return retval;

                    Connection c = FindConnectionByAddress(address);
                    if (c == null)
                    {
                        c = new Connection()
                        {
                            Id = m_NextConnectionId++,
                            State = ConnectionState.Connected,
                            Address = address,
                            AddressLength = addressLength,
                            Attempts = 1,
                            LastAttempt = NetworkUtils.stopwatch.ElapsedMilliseconds
                        };
                        m_IdToConnection.Add(c.Id, c);

                        retval = SocketEvent.Connect;
                        id = c.Id;
                    }
                    c.Attempts++;
                    c.LastAttempt = NetworkUtils.stopwatch.ElapsedMilliseconds;
                    SendPacket(UdpCProtocol.ConnectionAccept, c.Id);

                    return retval;

                } break;
                case UdpCProtocol.ConnectionReject:
                {

                } break;
                case UdpCProtocol.ConnectionAccept:
                {
                    Connection c = FindConnectionByAddress(address);
                    if (c != null && c.State == ConnectionState.Connecting)
                    {
                        id = c.Id;
                        c.State = ConnectionState.Connected;
                        return SocketEvent.Connect;
                    }

                } break;
                case UdpCProtocol.Disconnect:
                {
                    Connection c = FindConnectionByAddress(address);
                    if (c != null)
                    {
                        id = c.Id;
                        m_IdToConnection.Remove(id);

                        return SocketEvent.Disconnect;
                    }

                } break;
                case UdpCProtocol.Data:
                {
                    Connection c = FindConnectionByAddress(address);
                    if (c == null)
                        return SocketEvent.Empty;

                    c.LastAttempt = NetworkUtils.stopwatch.ElapsedMilliseconds;
                    if (c.State == ConnectionState.Connected)
                    {
                        id = c.Id;
                        size = result - UdpCHeader.Length;
                        return SocketEvent.Data;
                    }
                    else if (c.State == ConnectionState.Connecting)
                    {
                        id = c.Id;
                        c.State = ConnectionState.Connected;
                        return SocketEvent.Connect;
                    }
                } break;
            }
            return SocketEvent.Empty;
        }

        unsafe int NativeSend(ref UdpCHeader header, void* payload, int payloadSize, sockaddr_storage remote, int remoteSize)
        {
            var iov = stackalloc iovec[2];
            int result, sent = 0;

            fixed (byte* ptr = header.Data) 
            {
                iov[0].buf = ptr;
                iov[0].len = UdpCHeader.Length;

                iov[1].buf = (byte*)payload;
                iov[1].len = (ulong)payloadSize;

                sent = 0;
                int flags = 0;

                int addressLen = remoteSize;
                result = m_Socket.SendMessageEx(iov, 2, out sent, flags, ref remote, addressLen, null, null);
            }

            if (result == -1)
            {
                int error = Marshal.GetLastWin32Error();
                GameDebug.Log(string.Format("error on send {0}", error));
                throw new SocketException(error);
            }
            else
                result = sent;

            return result;
        }

        unsafe int NativeReceive(ref UdpCHeader header, ref byte[] data, ref sockaddr_storage address, ref int addressSize)
        {
            var iov = stackalloc iovec[2];

            fixed (byte* ptr = header.Data, ptr2 = data) 
            {
                iov[0].buf = ptr;
                iov[0].len = UdpCHeader.Length;

                iov[1].buf = ptr2;
                iov[1].len = (ulong)data.Length;
            }

            int flags = 0;
            int received = 0;

            int addressLen = addressSize;
            var result = m_Socket.ReceiveMessageEx(iov, 2, out received, out flags, ref address, out addressLen, null, null);
            if (result == -1)
            {
                int err2 = Marshal.GetLastWin32Error();
                if (err2 == 10035 || err2 == 35)
                    return 0;
                GameDebug.Log(string.Format("error on receive {0}", err2));
                throw new SocketException();
            }
            else
                result = received;

            addressSize = addressLen;

            return result;
        }

#endregion
    }

}*/