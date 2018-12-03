using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Unity.Networking.Transport.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Utilities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Networking.Transport
{
    public interface INetworkPacketReceiver
    {
        int ReceiveCount { get; set; }
        int AppendPacket(NetworkEndPoint address, UdpCHeader header, int dataLen);
        DataStreamWriter GetDataStream();
        bool DynamicDataStreamSize();
    }

    public interface INetworkInterface : IDisposable
    {
        NetworkFamily Family { get; }

        NetworkEndPoint LocalEndPoint { get; }
        NetworkEndPoint RemoteEndPoint { get; }

        void Initialize();

        JobHandle ScheduleReceive<T>(T receiver, JobHandle dep) where T : struct, INetworkPacketReceiver;

        int Bind(NetworkEndPoint endpoint);

        unsafe int SendMessage(network_iovec* iov, int iov_len, ref NetworkEndPoint address);
    }

    public struct IPv4UDPSocket : INetworkInterface
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private class SocketList
        {
            public HashSet<long> OpenSockets = new HashSet<long>();

            ~SocketList()
            {
                foreach (var socket in OpenSockets)
                {
                    long sockHand = socket;
                    NativeBindings.network_close(ref sockHand);
                }
            }
        }
        private static SocketList AllSockets = new SocketList();
#endif

        private long m_SocketHandle;
        private NetworkEndPoint m_RemoteEndPoint;
        
        public NetworkFamily Family => NetworkFamily.UdpIpv4;

        public unsafe NetworkEndPoint LocalEndPoint
        {
            get
            {
                var localEndPoint = new NetworkEndPoint {length = sizeof(NetworkEndPoint)};
                var result = NativeBindings.network_get_socket_address(m_SocketHandle, ref localEndPoint);
                if (result != 0)
                {
                    throw new SocketException(result);
                }
                return localEndPoint;
            }
        }
        
        public NetworkEndPoint RemoteEndPoint {
            get { return m_RemoteEndPoint; }
        }

        public void Initialize()
        {
            NativeBindings.network_initialize();
            int ret = CreateAndBindSocket(out m_SocketHandle, "0.0.0.0", 0);
            if (ret != 0)
                throw new SocketException(ret);
        }

        public void Dispose()
        {
            Close();
            NativeBindings.network_terminate();
        }

        struct ReceiveJob<T> : IJob where T : struct, INetworkPacketReceiver
        {
            public T receiver;
            public long socket;

            public unsafe void Execute()
            {
                var address = new NetworkEndPoint {length = sizeof(NetworkEndPoint)};
                var header = new UdpCHeader();
                var stream = receiver.GetDataStream();
                receiver.ReceiveCount = 0;

                while (true)
                {
                    if (receiver.DynamicDataStreamSize())
                    {
                        while (stream.Length+NetworkParameterConstants.MTU >= stream.Capacity)
                            stream.Capacity *= 2;
                    }
                    else if (stream.Length >= stream.Capacity)
                        return;
                    var sliceOffset = stream.Length;
                    var result = NativeReceive(ref header, stream.GetUnsafePtr() + sliceOffset,
                        Math.Min(NetworkParameterConstants.MTU, stream.Capacity - stream.Length), ref address);
                    if (result <= 0)
                    {
                        if (result < 0)
                            Debug.LogWarning("Error on received " + result);
                        // FIXME: handle error
                        return;
                    }
                    receiver.ReceiveCount += receiver.AppendPacket(address, header, result);
                }

            }

            unsafe int NativeReceive(ref UdpCHeader header, void* data, int length, ref NetworkEndPoint address)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (length <= 0)
                {
                    Debug.LogError("Can't receive into 0 bytes or less of buffer memory");
                    return 0;
                }
#endif
                var iov = stackalloc network_iovec[2];

                fixed (byte* ptr = header.Data)
                {
                    iov[0].buf = ptr;
                    iov[0].len = UdpCHeader.Length;

                    iov[1].buf = data;
                    iov[1].len = length;
                }

                var result = NativeBindings.network_recvmsg(socket, iov, 2, ref address);
                if (result == -1)
                {
                    int err = Marshal.GetLastWin32Error();
                    if (err == 10035 || err == 35 || err == 11)
                        return 0;

                    Debug.LogError(string.Format("error on receive {0}", err));
                    throw new SocketException(err);
                }
                return result;
            }
        }

        public JobHandle ScheduleReceive<T>(T receiver, JobHandle dep) where T : struct, INetworkPacketReceiver
        {
            var job = new ReceiveJob<T> {receiver = receiver, socket = m_SocketHandle};
            return job.Schedule(dep);
        }

        public int Bind(NetworkEndPoint endpoint)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (endpoint.Family != NetworkFamily.UdpIpv4)
                throw new InvalidOperationException();
#endif

            long newSocket;
            int ret = CreateAndBindSocket(out newSocket, endpoint.GetIp(), endpoint.Port);
            if (ret != 0)
                return ret;
            Close();

            m_RemoteEndPoint = endpoint;
            m_SocketHandle = newSocket;

            return 0;
        }

        private void Close()
        {
            if (m_SocketHandle < 0)
                return;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AllSockets.OpenSockets.Remove(m_SocketHandle);
#endif
            NativeBindings.network_close(ref m_SocketHandle);
            m_RemoteEndPoint = default(NetworkEndPoint);
            m_SocketHandle = -1;
        }

        public unsafe int SendMessage(network_iovec* iov, int iov_len, ref NetworkEndPoint address)
        {
            return NativeBindings.network_sendmsg(m_SocketHandle, iov, iov_len, ref address);
        }

        int CreateAndBindSocket(out long socket, string ip, int port)
        {
            socket = -1;
            int ret = NativeBindings.network_create_and_bind(ref socket, ip, port);
            if (ret != 0)
                return ret;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AllSockets.OpenSockets.Add(socket);
#endif
            NativeBindings.network_set_nonblocking(socket);
            NativeBindings.network_set_send_buffer_size(socket, ushort.MaxValue);
            NativeBindings.network_set_receive_buffer_size(socket, ushort.MaxValue);
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            // Avoid WSAECONNRESET errors when sending to an endpoint which isn't open yet (unclean connect/disconnects)
            NativeBindings.network_set_connection_reset(socket, 0);
#endif
            return 0;
        }
    }

    public struct IPCSocket : INetworkInterface
    {
        class QueueWrapper : IDisposable
        {
            public QueueWrapper()
            {
                m_IPCQueue = new NativeQueue<IPCManager.IPCQueuedMessage>(Allocator.Persistent);
            }

            public void Dispose()
            {
                m_IPCQueue.Dispose();
            }

            public NativeQueue<IPCManager.IPCQueuedMessage> m_IPCQueue;
        }

        [NativeSetClassTypeToNullOnSchedule] private QueueWrapper m_queue;
        private NativeQueue<IPCManager.IPCQueuedMessage>.Concurrent m_ConcurrentIPCQueue;
        [ReadOnly] private NativeArray<NetworkEndPoint> m_LocalEndPoint;

        public NetworkEndPoint LocalEndPoint => m_LocalEndPoint[0];
        public NetworkEndPoint RemoteEndPoint { get; }

        public NetworkFamily Family { get; }

        public void Initialize()
        {
            m_LocalEndPoint = new NativeArray<NetworkEndPoint>(1, Allocator.Persistent);
            m_LocalEndPoint[0] = IPCManager.Instance.CreateEndPoint();
            m_queue = new QueueWrapper();
            m_ConcurrentIPCQueue = m_queue.m_IPCQueue.ToConcurrent();
        }

        public void Dispose()
        {
            IPCManager.Instance.ReleaseEndPoint(m_LocalEndPoint[0]);
            m_LocalEndPoint.Dispose();
            m_queue.Dispose();
        }

        struct SendUpdate : IJob
        {
            public IPCManager ipcManager;
            public NativeQueue<IPCManager.IPCQueuedMessage> ipcQueue;

            public void Execute()
            {
                ipcManager.Update(ipcQueue);
            }
        }

        struct ReceiveJob<T> : IJob where T : struct, INetworkPacketReceiver
        {
            public T receiver;
            public IPCManager ipcManager;
            public NetworkEndPoint localEndPoint;

            public unsafe void Execute()
            {
                var address = new NetworkEndPoint {length = sizeof(NetworkEndPoint)};
                var header = new UdpCHeader();
                var stream = receiver.GetDataStream();

                while (true)
                {
                    if (receiver.DynamicDataStreamSize())
                    {
                        while (stream.Length+NetworkParameterConstants.MTU >= stream.Capacity)
                            stream.Capacity *= 2;                        
                    }
                    else if (stream.Length >= stream.Capacity)
                        return;
                    var sliceOffset = stream.Length;
                    var result = NativeReceive(ref header, stream.GetUnsafePtr() + sliceOffset,
                        Math.Min(NetworkParameterConstants.MTU, stream.Capacity - stream.Length), ref address);
                    if (result <= 0)
                    {
                        // FIXME: handle error
                        return;
                    }

                    receiver.AppendPacket(address, header, result);
                }
            }

            unsafe int NativeReceive(ref UdpCHeader header, void* data, int length, ref NetworkEndPoint address)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (length <= 0)
                {
                    Debug.LogError("Can't receive into 0 bytes or less of buffer memory");
                    return 0;
                }
#endif
                var iov = stackalloc network_iovec[2];

                fixed (byte* ptr = header.Data)
                {
                    iov[0].buf = ptr;
                    iov[0].len = UdpCHeader.Length;

                    iov[1].buf = data;
                    iov[1].len = length;
                }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (localEndPoint.Family != NetworkFamily.IPC || localEndPoint.Port == 0)
                    throw new InvalidOperationException();
#endif
                return ipcManager.ReceiveMessageEx(localEndPoint, iov, 2, ref address);
            }
        }

        public JobHandle ScheduleReceive<T>(T receiver, JobHandle dep) where T : struct, INetworkPacketReceiver
        {
            var sendJob = new SendUpdate {ipcManager = IPCManager.Instance, ipcQueue = m_queue.m_IPCQueue};
            var job = new ReceiveJob<T>
                {receiver = receiver, ipcManager = IPCManager.Instance, localEndPoint = m_LocalEndPoint[0]};
            dep = job.Schedule(JobHandle.CombineDependencies(dep, IPCManager.ManagerAccessHandle));
            dep = sendJob.Schedule(dep);
            IPCManager.ManagerAccessHandle = dep;
            return dep;
        }

        public int Bind(NetworkEndPoint endpoint)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (endpoint.Family != NetworkFamily.IPC || endpoint.Port == 0)
                throw new InvalidOperationException();
#endif
            IPCManager.Instance.ReleaseEndPoint(m_LocalEndPoint[0]);
            m_LocalEndPoint[0] = endpoint;
            return 0;
        }

        public unsafe int SendMessage(network_iovec* iov, int iov_len, ref NetworkEndPoint address)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_LocalEndPoint[0].Family != NetworkFamily.IPC || m_LocalEndPoint[0].Port == 0)
                throw new InvalidOperationException();
#endif
            return IPCManager.SendMessageEx(m_ConcurrentIPCQueue, m_LocalEndPoint[0], iov, iov_len, ref address);
        }
    }
}
