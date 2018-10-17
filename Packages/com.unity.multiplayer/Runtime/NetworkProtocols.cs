using System.Runtime.InteropServices;

namespace Experimental.Multiplayer.Protocols
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
        [FieldOffset(1)] public byte Reserved;
        [FieldOffset(2)] public ushort Reserved2;
    }
}
