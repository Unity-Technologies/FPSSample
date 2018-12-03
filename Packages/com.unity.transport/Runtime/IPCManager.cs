using System;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
using System.IO;
#endif
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Socket = Unity.Networking.Transport.IPCSocket;

namespace Unity.Networking.Transport
{
    public struct IPCManager
    {
        public static IPCManager Instance = new IPCManager();

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct IPCData
        {
            [FieldOffset(0)] public int from;
            [FieldOffset(4)] public int length;
            [FieldOffset(8)] public fixed byte data[NetworkParameterConstants.MTU];
        }
        internal struct IPCQueuedMessage
        {
            public IPCManager.IPCData data;
            public int dest;
        }


        private NativeQueue<int> m_FreeList;
        private NativeList<ushort> m_IPCEndPoints;
        private NativeMultiQueue<IPCData> m_IPCQueue;

        public int QueueSize { get; private set; }
        internal static JobHandle ManagerAccessHandle;

        public bool IsCreated => m_IPCEndPoints.IsCreated;

        public void Initialize(int receiveQueueSize)
        {
            m_IPCQueue = new NativeMultiQueue<IPCData>(receiveQueueSize);
            m_FreeList = new NativeQueue<int>(Allocator.Persistent);
            m_IPCEndPoints = new NativeList<ushort>(1, Allocator.Persistent);
            QueueSize = receiveQueueSize;
        }

        public void Destroy()
        {
            ManagerAccessHandle.Complete();
            m_IPCQueue.Dispose();
            m_FreeList.Dispose();
            m_IPCEndPoints.Dispose();
        }

        internal void Update(NativeQueue<IPCQueuedMessage> queue)
        {
            IPCQueuedMessage val;
            while (queue.TryDequeue(out val))
            {
                m_IPCQueue.Enqueue(val.dest, val.data);
            }
        }

        /// <summary>
        /// Create a NetworkEndPoint for IPC. If the EndPoint is passed to Bind the driver will assume ownership,
        /// otherwise the EndPoint must be destroyed by calling ReleaseEndPoint.
        /// </summary>
        public unsafe NetworkEndPoint CreateEndPoint(string name = null)
        {
            ManagerAccessHandle.Complete();
            int id;
            if (!m_FreeList.TryDequeue(out id))
            {
                id = m_IPCEndPoints.Length;
                m_IPCEndPoints.Add(1);
            }

            var endpoint = new NetworkEndPoint
            {
                Family = NetworkFamily.IPC,
                ipc_handle = id,
                length = 6,
                Port = m_IPCEndPoints[id]
            };

            return endpoint;
        }

        public unsafe void ReleaseEndPoint(NetworkEndPoint endpoint)
        {
            ManagerAccessHandle.Complete();
            if (endpoint.Family == NetworkFamily.IPC)
            {
                int handle = endpoint.ipc_handle;
                m_IPCQueue.Clear(handle);
                // Bump the version of the endpoint
                ushort version = m_IPCEndPoints[handle];
                ++version;
                if (version == 0)
                    version = 1;
                m_IPCEndPoints[handle] = version;

                m_FreeList.Enqueue(handle);
            }
        }

        public unsafe int PeekNext(NetworkEndPoint local, void* slice, out int length, out NetworkEndPoint from)
        {
            ManagerAccessHandle.Complete();
            IPCData data;
            from = default(NetworkEndPoint);
            length = 0;

            if (m_IPCQueue.Peek(local.ipc_handle, out data))
            {
                ushort version = m_IPCEndPoints[data.from];

                UnsafeUtility.MemCpy(slice, data.data, data.length);

                length = data.length;
            }

            NetworkEndPoint endpoint;
            if (!TryGetEndPointByHandle(data.from, out endpoint))
                return -1;
            from = endpoint;

            return length;
        }

        static internal unsafe int SendMessageEx(NativeQueue<IPCQueuedMessage>.Concurrent queue, NetworkEndPoint local,
            network_iovec* iov, int iov_len, ref NetworkEndPoint address)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (address.Family != NetworkFamily.IPC || local.Family != NetworkFamily.IPC ||
                address.Port == 0 || local.Port == 0)
                throw new InvalidOperationException("Sending data over IPC requires both local and remote EndPoint to be valid IPC EndPoints");
#endif

            var data = new IPCData();
            data.from = local.ipc_handle;
            data.length = 0;

            for (int i = 0; i < iov_len; i++)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (data.length + iov[i].len >= NetworkParameterConstants.MTU)
                    throw new ArgumentOutOfRangeException("Cannot send more data than an MTU");
#endif
                UnsafeUtility.MemCpy(data.data+data.length, iov[i].buf, iov[i].len);
                data.length += iov[i].len;
            }
            queue.Enqueue(new IPCQueuedMessage {dest = address.ipc_handle, data = data});
            return data.length;
        }

        public unsafe int ReceiveMessageEx(NetworkEndPoint local, network_iovec* iov, int iov_len, ref NetworkEndPoint remote)
        {
            IPCData data;
            if (!m_IPCQueue.Peek(local.ipc_handle, out data))
                return 0;
            NetworkEndPoint endpoint;
            if (!TryGetEndPointByHandle(data.from, out endpoint))
                return -1;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (endpoint.Family != NetworkFamily.IPC)
                throw new InvalidDataException("An incorrect message was pushed to the IPC message queue");
#endif

#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
            remote.family.sa_family = (byte) AddressFamily.Unspecified;
#else
            remote.family.sa_family = (ushort) AddressFamily.Unspecified;
#endif
            remote.ipc_handle = endpoint.ipc_handle;
            remote.Port = endpoint.Port;
            remote.length = 6;

            int totalLength = 0;
            for (int i = 0; i < iov_len; i++)
            {
                var curLength = Math.Min(iov[i].len, data.length - totalLength);
                UnsafeUtility.MemCpy(iov[i].buf, data.data + totalLength, curLength);
                totalLength += curLength;
                iov[i].len = curLength;
            }

            if (totalLength < data.length)
                return 0;
            m_IPCQueue.Dequeue(local.ipc_handle, out data);

            return totalLength;
        }

        public unsafe bool TryGetEndPointByHandle(int handle, out NetworkEndPoint endpoint)
        {
            var temp = new NetworkEndPoint();
            temp.Family = NetworkFamily.IPC;
            temp.ipc_handle = handle;

            temp.Port = m_IPCEndPoints[handle];
            endpoint = temp;
            endpoint.length = 6;
            return true;
        }
    }
}