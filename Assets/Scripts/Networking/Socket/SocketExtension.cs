/*using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

using Unity.Collections.LowLevel.Unsafe; 

namespace SocketExtensions
{
    public static class SocketExtension
    {
        public static unsafe int ReceiveFromEx(this Socket socket, byte[] buffer, int len, int flags, ref sockaddr_storage address, out int addressLen)
        {
            return NativeBindings.recvfrom(socket.Handle, buffer, len, flags, ref address, out addressLen);
        }
        public static unsafe int SendToEx(this Socket socket, byte[] buffer, int len, int flags, ref sockaddr_storage address, int addressLen)
        {
            return NativeBindings.sendto(socket.Handle, buffer, len, flags, ref address, addressLen);
        }

        public static unsafe int ReceiveMessageEx(this Socket socket, void* buffers, int bufferCount, out int bytesReceived, out int flags, ref sockaddr_storage address, out int addressLen, void* overlapped, void* completion)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            return NativeBindings.WSARecvFrom(socket.Handle, buffers, bufferCount, out bytesReceived, out flags, ref address, out addressLen, overlapped, completion);
#else
            msghdr msg;
            fixed (byte* p = address.data)
            {
                msg = new msghdr
                {
                    iov = (iovec*)buffers,
                    iovlen = (ulong)bufferCount,
                    name = p,
                    namelen = (ulong)sizeof(sockaddr_storage)
                };
            }

            var ret = NativeBindings.recvmsg(socket.Handle, ref msg, 0);
            if (ret == -1)
            { 
                bytesReceived = 0;
                flags = 0;
                addressLen = 0;

                int error = Marshal.GetLastWin32Error();
                if (error == 9976)
                    return 0;
                return -1;
            }

            addressLen = (int)msg.namelen;
            bytesReceived = ret;
            flags = 0;

            return ret;
#endif
        }

        public static unsafe int SendMessageEx(this Socket socket, void* buffers, int bufferCount, out int bytesSent, int flags, ref sockaddr_storage address, int addressLen, void* overlapped, void* completion)
        {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            return NativeBindings.WSASendTo(socket.Handle, buffers, bufferCount, out bytesSent, flags, ref address, addressLen, overlapped, completion);
#else
            msghdr msg;
            fixed (byte* p = address.data)
            {
                msg = new msghdr
                {
                    iov = (iovec*)buffers,
                    iovlen = (ulong)bufferCount,
                    name = p,
                    namelen = (ulong)addressLen
                };
            }

            var ret = NativeBindings.sendmsg(socket.Handle, ref msg, 0);
            if (ret == -1)
            { 
                bytesSent = 0;
                int error = Marshal.GetLastWin32Error();
                if (error == 35)
                {
                    return 0;
                }
                return -1;
            }

            bytesSent = ret;
            return ret;
#endif
        }

        #region Address Marshalling

        public static unsafe IPEndPoint UnmarshalAddress(sockaddr_storage address)
        {
            switch ((AddressFamily)address.ss_family.sa_family)
            {
                case AddressFamily.InterNetwork:
                    {
                        var soi = new sockaddr_in();
                        UnsafeUtility.MemCpy(soi.data, address.data, 16);
                        return new IPEndPoint(new IPAddress(soi.sin_addr.s_addr), (int)((ushort)System.Net.IPAddress.NetworkToHostOrder((short)(soi.sin_port))));
                    }
                case AddressFamily.InterNetworkV6:
                    {
                        throw new NotImplementedException();
                    }
                default:
                    return null;
            }
        }

        public static unsafe sockaddr_storage MarshalAddress(EndPoint ep, out int addressLenght)
        {
            var endpoint = ep as IPEndPoint;
            switch (ep.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                {
                    var sai = new sockaddr_in();

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
                    sai.sin_family.sa_family = (ushort)AddressFamily.InterNetwork;
#else
                    sai.sin_family.sa_family = (byte)AddressFamily.InterNetwork;
                    sai.sin_family.sa_len = (byte)sizeof(sockaddr_in);
#endif
                    sai.sin_port = (ushort)System.Net.IPAddress.HostToNetworkOrder((short)endpoint.Port);
                    sai.sin_addr.s_addr = (uint)BitConverter.ToInt32(endpoint.Address.GetAddressBytes(), 0);

                    addressLenght = sizeof(sockaddr_in);
                    var ss = new sockaddr_storage();

                    UnsafeUtility.MemCpy(ss.data, sai.data, addressLenght);
                    return ss;
                }
                case AddressFamily.InterNetworkV6:
                {
                    //return new IPEndPoint(new IPAddress(address.Buffer), (int)((ushort)System.Net.IPAddress.NetworkToHostOrder((short)address.Port)));
                    var sai6 = new sockaddr_in6();
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
                    sai6.sin6_family.sa_family = (ushort)AddressFamily.InterNetworkV6;
#else
                    sai6.sin6_family.sa_family = (byte)AddressFamily.InterNetworkV6;
                    sai6.sin6_family.sa_len = (byte)sizeof(sockaddr_in6);
#endif
                    sai6.sin6_port = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)endpoint.Port);

                    var bytes = endpoint.Address.GetAddressBytes();
                    fixed (byte* p = bytes)
                    {
                        UnsafeUtility.MemCpy(sai6.sin6_addr.s6_addr, p, bytes.Length);
                    }

                    addressLenght = sizeof(sockaddr_in6);
                    var ss = new sockaddr_storage();
                    UnsafeUtility.MemCpy(ss.data, sai6.data, addressLenght);

                    return ss;
                }
                default:
                    addressLenght = 0;
                    return default(sockaddr_storage);
            }
        }

#endregion

    }

#region Native Bindings

    internal static unsafe class NativeBindings
    {
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        [DllImport("kernel32.dll")]
        public static extern int WSAGetLastError();

        [DllImport("msvcrt.dll")]
        public static extern int memcmp(void* p1, void* p2, int count);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int recvfrom(IntPtr socket, byte[] buffer, int len, int flags, ref sockaddr_storage address, out int addressLen);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int sendto(IntPtr socket, byte[] buffer, int len, int flags, ref sockaddr_storage address, int addressLen);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int WSARecvFrom(IntPtr socket, void* buffers, int bufferCount, out int bytesReceived, out int flags, ref sockaddr_storage address, out int addressLen, void* overlapped, void* completion);

        [DllImport("ws2_32.dll", SetLastError = true)]
        public static extern int WSASendTo(IntPtr socket, void* buffers, int bufferCount, out int bytesSent, int flags, ref sockaddr_storage address, int addressLen, void* overlapped, void* completion);

#elif (UNITY_STANDALONE_LINUX)
        [DllImport("libc.so.6")]
        public static extern int memcmp(void* p1, void* p2, int count);

        [DllImport("libc.so.6", SetLastError = true)]
        public static extern int recvfrom(IntPtr socket, byte[] buffer, int len, int flags, ref sockaddr_storage address, out int addressLen);

        [DllImport("libc.so.6", SetLastError = true)]
        public static extern int sendto(IntPtr socket, byte[] buffer, int len, int flags, ref sockaddr_storage address, int addressLen);

        [DllImport("libc.so.6", SetLastError = true)]
        public static extern int recvmsg(IntPtr socket, ref msghdr msg, int flags);

        [DllImport("libc.so.6", SetLastError = true)]
        public static extern int sendmsg(IntPtr socket, ref msghdr msg, int flags);

#elif (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
#endif
    }

#endregion


#region Native Structures

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)

    unsafe struct iovec
    {
        public ulong len;
        public byte* buf;
    }

#else

    public unsafe struct iovec
    {
        public void* buf;
        public ulong len;
    }

    public unsafe struct msghdr
    {
        public byte* name;
        public ulong namelen;
        public iovec* iov;
        public ulong iovlen;
        public void* control;
        public ulong controllen;
        public int flags;
    }

#endif


#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
    public unsafe struct sa_family_t
    {
        public const int size = sizeof(ushort);
        public ushort sa_family;
    }
#else
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct sa_family_t
    {
        public const int size = sizeof(byte) * 2;
        [FieldOffset(0)] public byte sa_len;
        [FieldOffset(1)]public byte sa_family;
    }
#endif

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct sockaddr_storage
    {
        const int _ss_max_size = 128;
        const int _ss_align_size = (sizeof(long));

        const int _ss_pad1_size = _ss_align_size - sa_family_t.size;
        const int _ss_pad2_size = _ss_max_size - (sa_family_t.size + _ss_pad1_size + _ss_align_size);

        [FieldOffset(0)]
        public fixed byte data[_ss_max_size];

        [FieldOffset(0)]
        public sa_family_t ss_family;

        [FieldOffset(sa_family_t.size)]
        fixed byte _ss_pad1[_ss_pad1_size];
        [FieldOffset(sa_family_t.size + _ss_pad1_size)]
        long _ss_align;
        [FieldOffset(sa_family_t.size + _ss_pad1_size + _ss_align_size)]
        fixed byte _ss_pad2[_ss_pad2_size];

        public bool ReallyEquals(sockaddr_storage other)
        {
            fixed (void* p1 = this.data)
            {
                if (NativeBindings.memcmp(p1, other.data, _ss_max_size) == 0)
                    return true;
            }
            return false;
        }
    }

    public unsafe struct in_addr
    {
        public uint s_addr;
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

#endregion

}*/