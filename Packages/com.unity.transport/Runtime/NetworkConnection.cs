using System;
using System.Net;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Networking.Transport
{
    public struct NetworkConnection
    {
        internal int m_NetworkId;
        internal int m_NetworkVersion;

        /// <summary>
        /// ConnectionState enumerates available connection states a connection can have.
        /// </summary>
        public enum State
        {
            Disconnected,
            Connecting,
            AwaitingResponse,
            Connected
        }

        public int Disconnect<T>(T driver) where T : struct, INetworkDriver
        {
            return driver.Disconnect(this);
        }

        public NetworkEvent.Type PopEvent<T>(T driver, out DataStreamReader stream) where T : struct, INetworkDriver
        {
            return driver.PopEventForConnection(this, out stream);
        }

        public int Send<T>(T driver, DataStreamWriter bs) where T : struct, INetworkDriver
        {
            return driver.Send(this, bs);
        }

        public int Close<T>(T driver) where T : struct, INetworkDriver
        {
            if (m_NetworkId >= 0)
                return driver.Disconnect(this);
            return -1;
        }

        public bool IsCreated
        {
            get { return m_NetworkVersion != 0; }
        }

        public State GetState<T>(T driver) where T : struct, INetworkDriver
        {
            return driver.GetConnectionState(this);
        }

        public static bool operator ==(NetworkConnection lhs, NetworkConnection rhs)
        {
            return lhs.m_NetworkId == rhs.m_NetworkId && lhs.m_NetworkVersion == rhs.m_NetworkVersion;
        }

        public static bool operator !=(NetworkConnection lhs, NetworkConnection rhs)
        {
            return lhs.m_NetworkId != rhs.m_NetworkId || lhs.m_NetworkVersion != rhs.m_NetworkVersion;
        }

        public override bool Equals(object o)
        {
            return this == (NetworkConnection)o;
        }
        public bool Equals(NetworkConnection o)
        {
            return this == o;
        }

        public override int GetHashCode()
        {
            return (m_NetworkId << 8) ^ m_NetworkVersion;
        }

        public int InternalId => m_NetworkId;
    }
}