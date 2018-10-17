using UnityEngine.Rendering;

namespace Experimental.Multiplayer
{
    public interface INetworkParameter
    {
    }
    public struct NetworkParameterConstants
    {
        public const int MaximumConnectionsSupported = 16;
        public const int NetworkEventQLength = 100;
        public const int InvalidConnectionId = -1;

        public const int DriverBitStreamSize = 64 * 1024;
        public const int ConnectTimeout = 1000;
        public const int MaxConnectAttempts = 60;
        public const int DisconnectTimeout = 30 * 1000;

        public const int MTU = 1400;
    }

    public struct NetworkBitStreamParameter : INetworkParameter
    {
        public int size;
    }

    public struct NetworkConfigParameter : INetworkParameter
    {
        public int connectTimeout;
        public int maxConnectAttempts;
        public int disconnectTimeout;
    }
}
