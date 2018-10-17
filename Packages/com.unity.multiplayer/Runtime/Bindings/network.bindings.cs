using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using Unity.Collections.LowLevel.Unsafe; 

namespace Experimental.Multiplayer
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct network_iovec
    {
        public int len;
        public void* buf;
    }

    // TODO: Fix this internally incase there are other platforms that also does
    // it differently so it may result in similar issues
#   if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct sa_family_t
    {
        public const int size = sizeof(byte) * 2;
        [FieldOffset(0)] public byte sa_len;
        [FieldOffset(1)]public byte sa_family;
    }
#   else
    public unsafe struct sa_family_t
    {
        public const int size = sizeof(ushort);
        public ushort sa_family;
    }
#   endif

    public unsafe struct in_addr
    {
        public uint s_addr;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct sockaddr
    {
        [FieldOffset(0)]
        public fixed byte data[16];

        [FieldOffset(0)]
        public sa_family_t sin_family;
        [FieldOffset(2)]
        public fixed byte sin_zero[14];
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct sockaddr_in
    {
        [FieldOffset(0)]
        public fixed byte data[16];

        [FieldOffset(0)]
        public sa_family_t sin_family;
        [FieldOffset(2)]
        public ushort sin_port;
        [FieldOffset(4)]
        public in_addr sin_addr;
        [FieldOffset(8)]
        public fixed byte sin_zero[8];
    }

    public unsafe struct in_addr6
    {
        public fixed byte s6_addr[16];
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct sockaddr_in6
    {
        [FieldOffset(0)] public fixed byte data[28];

        [FieldOffset(0)] public sa_family_t sin6_family;
        [FieldOffset(2)] public ushort sin6_port;
        [FieldOffset(4)] public uint sin6_flowinfo;
        [FieldOffset(8)] public in_addr6 sin6_addr;
        [FieldOffset(24)] public uint sin6_scope_id;
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct network_address
    {
        /*
        typedef struct
        {
            union
            {
                struct sockaddr     addr;
                struct sockaddr_in  addr_in;
                struct sockaddr_in6 addr_in6;
            };
            int length;

        } network_address;
        */
        [FieldOffset(0)] public fixed byte data[28];
        [FieldOffset(0)] public sa_family_t family;
        [FieldOffset(2)] public int ipc_handle;
        [FieldOffset(28)] public int length;

        public bool ReallyEquals(network_address other)
        {
            fixed (void* p1 = this.data)
            {
                if (NativeBindings.memcmp(p1, other.data, length) == 0)
                    return true;
            }
            return false;
        }
    }

    public static class SocketExtension
    {
        public static unsafe int SendMessageEx(IntPtr socket, void *iov, int iov_len, ref network_address address)
        {
            return NativeBindings.network_sendmsg(socket, iov, iov_len, ref address);
        }

        public static unsafe int ReceiveMessageEx(IntPtr socket, void *iov, int iov_len, ref network_address remote)
        {
            return NativeBindings.network_recvmsg(socket, iov, iov_len, ref remote);
        }

        #region Address Marshalling

        public static unsafe bool UnmarshalIpV4Address(network_address address, byte* addr, out ushort port)
        {
            port = 0;
            switch ((AddressFamily)address.family.sa_family)
            {
                case AddressFamily.InterNetwork:
                    {
                        var soi = new sockaddr_in();
                        UnsafeUtility.MemCpy(soi.data, address.data, 16);
                        UnsafeUtility.MemCpy(addr, &soi.sin_addr.s_addr, 4);
                        port = ((ushort)System.Net.IPAddress.NetworkToHostOrder((short)(soi.sin_port)));
                        return true;
                    }
                case AddressFamily.InterNetworkV6:
                    {
                        throw new NotImplementedException();
                    }
                default:
                    return false;
            }
        }

        public static unsafe network_address MarshalIpV4Address(byte* addr, ushort port)
        {
            // add asserts on sizes
            var sai = new sockaddr_in();

#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            sai.sin_family.sa_family = (byte) AddressFamily.InterNetwork;
            sai.sin_family.sa_len = (byte) sizeof(sockaddr_in);
#else
            sai.sin_family.sa_family = (ushort)AddressFamily.InterNetwork;
#endif
            sai.sin_port = (ushort) System.Net.IPAddress.HostToNetworkOrder((short)port);
            UnsafeUtility.MemCpy(&sai.sin_addr.s_addr, addr, 4);

            var address = new network_address();
            address.length = sizeof(sockaddr_in);

            UnsafeUtility.MemCpy(address.data, sai.data, address.length);
            return address;
        }

        public static unsafe network_address MarshalIpV6Address(byte* addr, ushort port)
        {
            // add asserts on sizes
            var sai6 = new sockaddr_in6();
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            sai6.sin6_family.sa_family = (byte)AddressFamily.InterNetworkV6;
            sai6.sin6_family.sa_len = (byte)sizeof(sockaddr_in6);
#else
            sai6.sin6_family.sa_family = (ushort)AddressFamily.InterNetworkV6;
#endif
            sai6.sin6_port = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)port);

            UnsafeUtility.MemCpy(sai6.sin6_addr.s6_addr, addr, 8);

            var address = new network_address();
            address.length = sizeof(sockaddr_in6);
            UnsafeUtility.MemCpy(address.data, sai6.data, address.length);

            return address;
        }

#endregion
    }

    public static unsafe class NativeBindings
    {
        [DllImport("msvcrt.dll")]
        public static extern int memcmp(void* p1, void* p2, int count);

        /*

        [DllImport("network.bindings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_initialize();

        [DllImport("network.bindings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_terminate();

        [DllImport("network.bindings", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int network_create_and_bind(ref long socket_handle, string address, int port);

        [DllImport("network.bindings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_close(ref long socket_handle);

        [DllImport("network.bindings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_set_nonblocking(long socket_handle);

        [DllImport("network.bindings", CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_get_socket_address(long socket_handle, ref network_address own_address);
        */

        [DllImport("network.bindings", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_sendmsg(IntPtr socket_handle, void *iov, int iov_len, ref network_address address);

        [DllImport("network.bindings", SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_recvmsg(IntPtr socket_handle, void *iov, int iov_len, ref network_address remote);
    }
}
