using System;
using System.Net;
using System.Net.Sockets;
using Experimental.Multiplayer.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Experimental.Multiplayer
{
    public interface INetworkInterface : IDisposable
    {
        NetworkFamily Family { get; }
        
        NetworkEndPoint LocalEndPoint { get; }
        NetworkEndPoint RemoteEndPoint { get; }

        void Initialize();

        void Update();
        
        void Bind(NetworkEndPoint endpoint);
        void Close();
            
        unsafe int ReceiveFrom(void* slice, out int length, out NetworkEndPoint remote);
        unsafe int SendTo(void* slice, int length, NetworkEndPoint remote);
        unsafe int SendMessage(void* iov, int iov_len, ref network_address address);
        unsafe int ReceiveMessage(void* iov, int iov_len, ref network_address address);
    }

    public struct IPv4UDPSocket : INetworkInterface
    {
        [NativeSetClassTypeToNullOnSchedule]
        private Socket m_Socket;
        [NativeDisableUnsafePtrRestriction]
        private IntPtr m_SocketHandle;
        public void Initialize()
        {
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.SendBufferSize = ushort.MaxValue;
            m_Socket.ReceiveBufferSize = ushort.MaxValue;
            m_Socket.Blocking = false;
            m_SocketHandle = m_Socket.Handle;
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            // Avoid WSAECONNRESET errors when sending to an endpoint which isn't open yet (unclean connect/disconnects)
            m_Socket.IOControl(unchecked((int)SocketConstants.SIO_UDP_CONNRESET), new byte[] { 0, 0, 0, 0 }, null);
#endif
        }

        public void Dispose()
        {
            m_Socket.Close();
            m_Socket.Dispose();
        }

        public void Update()
        {
        }

        public NetworkFamily Family => NetworkFamily.UdpIpv4;
        public NetworkEndPoint LocalEndPoint => m_Socket.LocalEndPoint as IPEndPoint;
        public NetworkEndPoint RemoteEndPoint => m_Socket.RemoteEndPoint as IPEndPoint;

        public unsafe void Bind(NetworkEndPoint endpoint)
        {
            Assert.IsTrue(endpoint.family == NetworkFamily.UdpIpv4);
            var addr = new byte[4];
            for (int i = 0; i < 4; ++i)
                addr[i] = endpoint.address[i];
            m_Socket.Bind(new IPEndPoint(new IPAddress(addr), endpoint.port));
        }
        
        public void Close()
        {
            m_Socket.Close();
        }

        public unsafe int ReceiveFrom(void* slice, out int length, out NetworkEndPoint remote)
        {
            throw new NotImplementedException();
        }

        public unsafe int SendTo(void* slice, int length, NetworkEndPoint remote)
        {
            throw new NotImplementedException();
        }

        public unsafe int SendMessage(void* iov, int iov_len, ref network_address address)
        {
            return SocketExtension.SendMessageEx(m_SocketHandle, iov, iov_len, ref address);
        }

        public unsafe int ReceiveMessage(void* iov, int iov_len, ref network_address address)
        {
            return SocketExtension.ReceiveMessageEx(m_SocketHandle, iov, iov_len, ref address);
        }
    }
    
    public struct IPCSocket : INetworkInterface
    {
        private NativeArray<NetworkEndPoint> m_LocalEndPoint;

        public NetworkEndPoint LocalEndPoint => m_LocalEndPoint[0];
        public NetworkEndPoint RemoteEndPoint { get; }
        
        public NetworkFamily Family { get; }
        
        public void Initialize()
		{
		    m_LocalEndPoint = new NativeArray<NetworkEndPoint>(1, Allocator.Persistent);
		}
        public void Dispose()
        {
            m_LocalEndPoint.Dispose();
        }

        public void Update()
        {
        }

        public void Bind(NetworkEndPoint endpoint)
        {
            Assert.IsTrue(endpoint.family == NetworkFamily.IPC && endpoint.port != 0);
            m_LocalEndPoint[0] = endpoint;
        }

        public void Close()
        {
            IPCManager.Instance.ReleaseEndPoint(m_LocalEndPoint[0]);
        }

        public unsafe int ReceiveFrom(void* slice, out int length, out NetworkEndPoint remote)
        {
            return IPCManager.Instance.RecvFrom(m_LocalEndPoint[0], slice, out length, out remote);
        }

        unsafe int INetworkInterface.SendTo(void* slice, int length, NetworkEndPoint remote)
        {
            return IPCManager.Instance.SendTo(m_LocalEndPoint[0], slice, length, remote);
        }

        public unsafe int SendMessage(void* iov, int iov_len, ref network_address address)
        {
            if (m_LocalEndPoint[0].family != NetworkFamily.IPC || m_LocalEndPoint[0].port == 0)
                m_LocalEndPoint[0] = IPCManager.Instance.CreateEndPoint();

            return IPCManager.Instance.SendMessageEx(m_LocalEndPoint[0], iov, iov_len, ref address);
        }

        public unsafe int ReceiveMessage(void* iov, int iov_len, ref network_address address)
        {
            Assert.IsTrue(m_LocalEndPoint[0].family == NetworkFamily.IPC && m_LocalEndPoint[0].port != 0);
            return IPCManager.Instance.ReceiveMessageEx(m_LocalEndPoint[0], iov, iov_len, ref address);
        }
    }

}