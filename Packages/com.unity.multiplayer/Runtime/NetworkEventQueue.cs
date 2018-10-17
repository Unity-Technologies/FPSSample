using System;
using Unity.Collections;

namespace Experimental.Multiplayer
{
    public struct NetworkEvent
    {
        /// <summary>
        /// NetworkEvent.Type enumerates available network events for this driver.
        /// </summary>
        public enum Type
        {
            Empty = 0,
            Data,
            Connect,
            Disconnect
        }
        public Type type;

        public int connectionId;
        public int offset;
        public int size;
    }

    public struct NetworkEventQueue : IDisposable
    {
        public NetworkEventQueue(int maxConnections, int queueSizePerConnection)
        {
            m_MaxConnections = maxConnections;
            m_MaxEvents = queueSizePerConnection;
            m_MasterEventQ = new NativeQueue<SubQueueItem>(Allocator.Persistent);
            m_ConnectionEventQ = new NativeArray<NetworkEvent>(m_MaxConnections * m_MaxEvents, Allocator.Persistent);
            m_ConnectionEventHeadTail = new NativeArray<int>(m_MaxConnections * 2, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_MasterEventQ.Dispose();
            m_ConnectionEventQ.Dispose();
            m_ConnectionEventHeadTail.Dispose();
        }
        // The returned stream is valid until PopEvent is called again or until the main driver updates
        public NetworkEvent.Type PopEvent(out int id, out int offset, out int size)
        {
            offset = 0;
            size = 0;
            id = -1;

            while (true)
            {
                SubQueueItem ev;
                if (!m_MasterEventQ.TryDequeue(out ev))
                {
                    return (int) NetworkEvent.Type.Empty;
                }

                if (m_ConnectionEventHeadTail[ev.connection * 2] == ev.idx)
                {
                    id = ev.connection;
                    return PopEventForConnection(ev.connection, out offset, out size);
                }
            }
        }

        public NetworkEvent.Type PopEventForConnection(int connectionId, out int offset, out int size)
        {
            offset = 0;
            size = 0;

            int idx = m_ConnectionEventHeadTail[connectionId * 2];
            if (idx >= m_ConnectionEventHeadTail[connectionId * 2 + 1])
                return (int) NetworkEvent.Type.Empty;
            m_ConnectionEventHeadTail[connectionId * 2] = idx + 1;
            NetworkEvent ev = m_ConnectionEventQ[connectionId * m_MaxEvents + idx];
            if (ev.type == NetworkEvent.Type.Data)
            {
                offset = ev.offset;
                size = ev.size;
            }
            return ev.type;
        }

        public int Count
        {
            get
            {
                int cnt = 0;
                for (int i = 0; i < m_MaxConnections; ++i)
                    cnt += m_ConnectionEventHeadTail[i * 2 + 1] - m_ConnectionEventHeadTail[i * 2];
                return cnt;
            }
        }

        /// ::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        /// internal helper functions ::::::::::::::::::::::::::::::::::::::::::

        internal bool PushEvent(NetworkEvent ev)
        {
            int idx = m_ConnectionEventHeadTail[ev.connectionId * 2 + 1];
            if (idx >= m_MaxEvents)
                return false;
            m_ConnectionEventQ[ev.connectionId*m_MaxEvents+idx] = ev;
            m_ConnectionEventHeadTail[ev.connectionId * 2 + 1] = idx + 1;
            
            m_MasterEventQ.Enqueue(new SubQueueItem {connection = ev.connectionId, idx = idx});
            return true;
        }
        
        internal void Reset()
        {
            m_MasterEventQ.Clear();
            for (int i = 0; i < m_MaxConnections; ++i)
            {
                m_ConnectionEventHeadTail[i * 2] = 0;
                m_ConnectionEventHeadTail[i * 2 + 1] = 0;
            }
        }

        struct SubQueueItem
        {
            public int connection;
            public int idx;
        }
        private NativeQueue<SubQueueItem> m_MasterEventQ;
        private NativeArray<NetworkEvent> m_ConnectionEventQ;
        private NativeArray<int> m_ConnectionEventHeadTail;
        private int m_MaxConnections;
        private int m_MaxEvents;
    }
}