using System.Collections.Generic;
using UnityEngine;

namespace NetcodeTests
{
    public class TestTransport : INetworkTransport
    {
        public static void Reset()
        {
            for (int i = 0; i < s_EndPoints.Length; ++i)
                s_EndPoints[i] = null;
        }

        public TestTransport(int id)
        {
            Debug.Assert(s_EndPoints[id] == null);
            s_EndPoints[id] = this;
            m_Id = id;
        }

        public void Update()
        {
        }

        public bool NextEvent(ref TransportEvent e)
        {
            // Pass back connects, disconnects and data

            if (m_Connects.Count > 0)
            {
                e.type = TransportEvent.Type.Connect;
                e.connectionId = m_Connects.Dequeue();
            }
            else if (m_Disconnects.Count > 0)
            {
                e.type = TransportEvent.Type.Disconnect;
                e.connectionId = m_Disconnects.Dequeue();
            }
            else if (m_IncomingPackages.Count > 0)
            {
                var p = m_IncomingPackages.Dequeue();
                e.type = TransportEvent.Type.Data;
                e.connectionId = p.from;
                e.data = p.data;
                e.dataSize = p.size;
            }
            else
                return false;

            return true;
        }

        public int Connect(string ip, int port)
        {
            int id = int.Parse(ip);

            var remote = s_EndPoints[id];
            if (remote != null)
            {
                m_Connects.Enqueue(id);
                remote.m_Connects.Enqueue(m_Id);
                return id;
            }
            else
                return -1;
        }

        public void Disconnect(int connectionId)
        {
            var remote = s_EndPoints[m_Id];
            if (remote != null)
                remote.m_Disconnects.Enqueue(m_Id);
        }

        public void Shutdown()
        {}

        public void SendData(int connectionId, byte[] data, int sendSize)
        {
            var remote = s_EndPoints[connectionId];
            Debug.Assert(remote != null);

            var package = new Package();
            package.from = m_Id;
            package.size = sendSize;
            NetworkUtils.MemCopy(data, 0, package.data, 0, sendSize);
            remote.m_IncomingPackages.Enqueue(package);
        }

        public string GetConnectionDescription(int connectionId)
        {
            return "" + connectionId;
        }

        public void DropPackages()
        {
            m_IncomingPackages.Clear();
        }

        class Package
        {
            public int from;
            public int size;
            public byte[] data = new byte[2048];
        }

        const int k_MaxEndPoints = 32;
        static TestTransport[] s_EndPoints = new TestTransport[k_MaxEndPoints];

        int m_Id;
        Queue<Package> m_IncomingPackages = new Queue<Package>();
        Queue<int> m_Connects = new Queue<int>();
        Queue<int> m_Disconnects = new Queue<int>();
    }
}

