using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

using Experimental.Multiplayer.Protocols;
using Experimental.Multiplayer.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Socket = Experimental.Multiplayer.IPCSocket;

namespace Experimental.Multiplayer
{
    public sealed class IPCManager
    {
        public static IPCManager Instance { get; } = new IPCManager();
        
        [StructLayout(LayoutKind.Explicit)]
        unsafe struct IPCData
        {
            [FieldOffset(0)]public int from;
            [FieldOffset(4)]public int length;
            [FieldOffset(8)]public fixed byte data[NetworkParameterConstants.MTU];
        }

        private ConnectionFreeList m_FreeList;
        private Dictionary<int, ushort> m_IPCEndPoints;
        private BucketQ<IPCData> m_IPCQueue;
        
        static IPCManager()
        {
        }

        private IPCManager()
        {
        }

        public void Initialize(int receiveQueueSize)
        {
            m_IPCQueue = new BucketQ<IPCData>(NetworkParameterConstants.MaximumConnectionsSupported, receiveQueueSize);
            m_FreeList = new ConnectionFreeList(NetworkParameterConstants.MaximumConnectionsSupported);
            m_IPCEndPoints = new Dictionary<int, ushort>();
        }

        public void Destroy()
        {
            m_IPCQueue.Dispose();
            m_FreeList.Dispose();
        }

        /// <summary>
        /// Create a NetworkEndPoint for IPC. If the EndPoint is passed to Bind the driver will assume ownership,
        /// otherwise the EndPoint must be destroyed by calling ReleaseEndPoint.
        /// </summary>
        public unsafe NetworkEndPoint CreateEndPoint(string name = null)
        {
            int id;
            if (!m_FreeList.AquireConnectionId(out id))
            {
                throw new IndexOutOfRangeException(string.Format("No more free connections"));
            }

            NetworkEndPoint endpoint;
            int* handle = (int*) endpoint.address;
            *handle = id;
            endpoint.family = NetworkFamily.IPC;
            if (!m_IPCEndPoints.TryGetValue(id, out endpoint.port))
			{
				endpoint.port = 1;
                m_IPCEndPoints.Add(id, endpoint.port);
			}

            return endpoint;
        }

        public unsafe void ReleaseEndPoint(NetworkEndPoint endpoint)
        {
            if (endpoint.family == NetworkFamily.IPC)
            {
                int* handle = (int*) endpoint.address;                
                m_IPCQueue.Reset(*handle);
                // Bunp the version of the endpoint
                ++m_IPCEndPoints[*handle];
				if (m_IPCEndPoints[*handle] == 0)
                    ++m_IPCEndPoints[*handle];
                m_FreeList.ReleaseConnectionId(*handle);
            }
        }

        public unsafe int SendTo(NetworkEndPoint local, void* slice, int length, NetworkEndPoint remote)
        {
            Assert.IsTrue(remote.family == NetworkFamily.IPC);
            Assert.IsTrue(local.family == NetworkFamily.IPC);
            Assert.IsTrue(remote.port != 0);
            Assert.IsTrue(local.port != 0);
                
            var data = new IPCData();
            data.from = *(int*)local.address;
            data.length = length;
            
            UnsafeUtility.MemCpy(data.data, slice, length);

            m_IPCQueue.Enqueue(*(int*)remote.address, data);
            return length;
        }

        public unsafe int PeekNext(NetworkEndPoint local, void* slice, out int length, out NetworkEndPoint from)
        {
            IPCData data;
            from = default(NetworkEndPoint);
            length = 0;

            if (m_IPCQueue.TryPeek(*(int*)local.address, out data))
            {
                if (!m_IPCEndPoints.ContainsKey(data.from))
                    return -1;
                    
                UnsafeUtility.MemCpy(slice, data.data, data.length);

                length = data.length;
            }

            NetworkEndPoint endpoint;
            if (!TryGetEndPointByHandle(data.from, out endpoint))
                    return -1;
            from = endpoint;

            return length;
        }
        
        public unsafe int RecvFrom(NetworkEndPoint local, void* slice, out int length, out NetworkEndPoint from)
        {
            IPCData data;
            from = default(NetworkEndPoint);
            length = 0;

            if (m_IPCQueue.TryDequeue(*(int*)local.address, out data))
            {
                if (!m_IPCEndPoints.ContainsKey(data.from))
                    return -1;
                    
                UnsafeUtility.MemCpy(slice, data.data, data.length);

                length = data.length;
            }

            NetworkEndPoint endpoint;
            if (!TryGetEndPointByHandle(data.from, out endpoint))
                    return -1;
            from = endpoint;

            return length;
        }

        public unsafe int SendMessageEx(NetworkEndPoint local, void* iov, int iov_len, ref network_address address)
        {
            var vec = stackalloc network_iovec[iov_len];
            NetworkEndPoint endpoint;
            
            if (!TryGetEndPointByHandle(address.ipc_handle, out endpoint))
                return -1;
            
            for (int i = 0; i < iov_len; i++)
                vec[i] = UnsafeUtility.ReadArrayElement<network_iovec>(iov, i);

            int length = 0;
            for (int i = 0; i < iov_len; i++)
            {
                int result;
                if ((result = SendTo(local, vec[i].buf, vec[i].len, endpoint)) < 0)
                    return result;
                length += result;
            }
            
            return length;
        }

        public unsafe int ReceiveMessageEx(NetworkEndPoint local, void* iov, int iov_len, ref network_address remote)
        {
            var vec = stackalloc network_iovec[iov_len];
            
            int totalLength = 0;
            NetworkEndPoint from;
            for (int i = 0; i < iov_len; i++)
            {
                int length;
                int result;
                
                vec[i] = UnsafeUtility.ReadArrayElement<network_iovec>(iov, i);
                
                if ((result = RecvFrom(local, vec[i].buf, out length, out from)) < 0)
                    return result;

                Assert.IsTrue(from.family == NetworkFamily.IPC);
                vec[i].len = length;
                totalLength += length;
                
                // TODO: Double check this works on ios as well.
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
                remote.family.sa_family = (byte) AddressFamily.Unspecified;
#else
                remote.family.sa_family = (ushort) AddressFamily.Unspecified;
#endif
                remote.ipc_handle = *(int*)from.address;
                remote.length = 6;
            }
            
            return totalLength;
        }

        public unsafe bool TryGetEndPointByHandle(int handle, out NetworkEndPoint endpoint)
        {
            NetworkEndPoint temp;
            temp.family = NetworkFamily.IPC;
            *(int*)temp.address = handle;
            bool status = m_IPCEndPoints.TryGetValue(handle, out temp.port);
            endpoint = temp;
            return status;
        }
    }
}
