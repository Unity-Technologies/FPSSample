using System.Runtime.InteropServices;

namespace Unity.Networking.Transport
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct network_iovec
    {
        public int len;
        public void* buf;
    }

    // TODO: Fix this internally incase there are other platforms that also does
    // it differently so it may result in similar issues
# if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_IOS)
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct sa_family_t
    {
        public const int size = sizeof(byte) * 2;
        [FieldOffset(0)] public byte sa_len;
        [FieldOffset(1)] public byte sa_family;
    }
# else
    internal unsafe struct sa_family_t
    {
        public const int size = sizeof(ushort);
        public ushort sa_family;
    }
# endif

    internal unsafe struct in_addr
    {
        public uint s_addr;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct sockaddr
    {
        [FieldOffset(0)] public fixed byte data[16];

        [FieldOffset(0)] public sa_family_t sin_family;
        [FieldOffset(2)] public fixed byte sin_zero[14];
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct sockaddr_in
    {
        [FieldOffset(0)] public fixed byte data[16];

        [FieldOffset(0)] public sa_family_t sin_family;
        [FieldOffset(2)] public ushort sin_port;
        [FieldOffset(4)] public in_addr sin_addr;
        [FieldOffset(8)] public fixed byte sin_zero[8];
    }

    internal unsafe struct in_addr6
    {
        public fixed byte s6_addr[16];
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct sockaddr_in6
    {
        [FieldOffset(0)] public fixed byte data[28];

        [FieldOffset(0)] public sa_family_t sin6_family;
        [FieldOffset(2)] public ushort sin6_port;
        [FieldOffset(4)] public uint sin6_flowinfo;
        [FieldOffset(8)] public in_addr6 sin6_addr;
        [FieldOffset(24)] public uint sin6_scope_id;
    }

    public static unsafe class NativeBindings
    {
#if UNITY_IOS && !UNITY_EDITOR
        const string m_DllName = "__Internal";
#else
        const string m_DllName = "network.bindings";
#endif
        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_initialize();

        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_terminate();

        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int network_create_and_bind(ref long socket_handle, string address, int port);

        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_close(ref long socket_handle);

        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_set_nonblocking(long socket_handle);
        
        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_set_send_buffer_size(long socket_handle, int size);
        
        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_set_receive_buffer_size(long socket_handle, int size);
        
        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_set_connection_reset(long socket_handle, int value);

        [DllImport(m_DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_get_socket_address(long socket_handle, ref NetworkEndPoint own_address);

        [DllImport(m_DllName, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_sendmsg(long socket_handle, void* iov, int iov_len,
            ref NetworkEndPoint address);

        [DllImport(m_DllName, SetLastError = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern int network_recvmsg(long socket_handle, void* iov, int iov_len,
            ref NetworkEndPoint remote);        
    }
}
