using System.Runtime.InteropServices;

namespace Unity.Networking.Transport.Protocols
{
    public enum UdpCProtocol
    {
        ConnectionRequest,
        ConnectionReject,
        ConnectionAccept,
        Disconnect,
        Data
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct UdpCHeader
    {
        public const int Length = 4;
        [FieldOffset(0)] public fixed byte Data[Length];
        [FieldOffset(0)] public byte Type;
        [FieldOffset(1)] public byte Flags;
        [FieldOffset(2)] public ushort SessionToken;
    }
}