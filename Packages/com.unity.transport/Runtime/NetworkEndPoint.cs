using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Networking.Transport
{
    public enum NetworkFamily
    {
        UdpIpv4 = AddressFamily.InterNetwork,
        IPC = AddressFamily.Unspecified
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct NetworkEndPoint
    {
        internal const int Length = 28;
        [FieldOffset(0)] internal fixed byte data[28];
        [FieldOffset(0)] internal sa_family_t family;
        [FieldOffset(2)] private ushort port;
        [FieldOffset(4)] internal int ipc_handle;
        [FieldOffset(28)] internal int length;

        public ushort Port
        {
            get { return (ushort) IPAddress.NetworkToHostOrder((short) port); }
            set { port = (ushort) IPAddress.HostToNetworkOrder((short) value); }
        }

        public NetworkFamily Family
        {
            get => (NetworkFamily) family.sa_family;
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
            set => family.sa_family = (byte) value;
#else
            set => family.sa_family = (ushort)value;
#endif
        }

        public bool IsValid => Family != 0;

        public static implicit operator NetworkEndPoint(EndPoint ep)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!(ep is IPEndPoint))
                throw new InvalidOperationException("NetworkEndPoint can only be created from IPEndPoint");
#endif
            var endpoint = ep as IPEndPoint;

            switch (endpoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                {
                    var sai = new sockaddr_in();

#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
                    sai.sin_family.sa_family = (byte) AddressFamily.InterNetwork;
                    sai.sin_family.sa_len = (byte) sizeof(sockaddr_in);
#else
                    sai.sin_family.sa_family = (ushort) AddressFamily.InterNetwork;
#endif
                    sai.sin_port = (ushort) IPAddress.HostToNetworkOrder((short) endpoint.Port);
                    sai.sin_addr.s_addr = (uint) BitConverter.ToInt32(endpoint.Address.GetAddressBytes(), 0);

                    var len = sizeof(sockaddr_in);
                    var address = new NetworkEndPoint
                    {
                        length = len
                    };

                    UnsafeUtility.MemCpy(address.data, sai.data, len);
                    return address;
                }
            }

            return default(NetworkEndPoint);
        }

        public string GetIp()
        {
            var ipAndPort = ToEndPoint(this).ToString();
            return ipAndPort.Substring(0, ipAndPort.IndexOf(':'));
        }

        public static EndPoint ToEndPoint(NetworkEndPoint ep)
        {
            switch (ep.Family)
            {
                case NetworkFamily.UdpIpv4:
                {
                    var soi = new sockaddr_in();
                    UnsafeUtility.MemCpy(soi.data, ep.data, 16);
                    return new IPEndPoint(new IPAddress(soi.sin_addr.s_addr),
                        (int) ((ushort) System.Net.IPAddress.NetworkToHostOrder((short) (soi.sin_port))));
                }
                case NetworkFamily.IPC:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        public static bool operator ==(NetworkEndPoint lhs, NetworkEndPoint rhs)
        {
            return lhs.Compare(rhs);
        }

        public static bool operator !=(NetworkEndPoint lhs, NetworkEndPoint rhs)
        {
            return !lhs.Compare(rhs);
        }

        public override bool Equals(object other)
        {
            return this == (NetworkEndPoint) other;
        }

        public override int GetHashCode()
        {
            fixed (byte* p = data)
                unchecked
                {
                    var result = 0;

                    for (int i = 0; i < Length; i++)
                    {
                        result = (result * 31) ^ (int) (p + 1);
                    }

                    return result;
                }
        }

#if !UNITY_2018_3_OR_NEWER
        private int memcmp(void* ptr1, void* ptr2, int size)
        {
            for (int i = 0; i < size; ++i)
            {
                if (((byte*) ptr1)[i] != ((byte*) ptr2)[i])
                    return 1;
            }

            return 0;
        }
#endif

        bool Compare(NetworkEndPoint other)
        {
            if (length != other.length)
                return false;

            fixed (void* p = this.data)
            {
#if UNITY_2018_3_OR_NEWER
                if (UnsafeUtility.MemCmp(p, other.data, length) == 0)
#else
                if (memcmp(p, other.data, length) == 0)
#endif
                    return true;
            }

            return false;
        }
    }
}