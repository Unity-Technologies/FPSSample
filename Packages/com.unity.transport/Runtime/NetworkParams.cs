namespace Unity.Networking.Transport
{
    public interface INetworkParameter
    {
    }

    public struct NetworkParameterConstants
    {
        public const int InitialEventQueueSize = 100;
        public const int InvalidConnectionId = -1;

        public const int DriverDataStreamSize = 64 * 1024;
        public const int ConnectTimeout = 1000;
        public const int MaxConnectAttempts = 60;
        public const int DisconnectTimeout = 30 * 1000;

        public const int MTU = 1400;
    }

    public struct NetworkDataStreamParameter : INetworkParameter
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