using System.Net;
using Experimental.Multiplayer;
using Unity.Collections;
using UdpNetworkDriver = Experimental.Multiplayer.BasicNetworkDriver<Experimental.Multiplayer.IPv4UDPSocket>;
using EventType = Experimental.Multiplayer.NetworkEvent.Type; 

public class SocketTransport : INetworkTransport
{
    public SocketTransport(int port = 0, int maxConnections = 16)
    {
        m_IdToConnection = new NativeArray<NetworkConnection>(maxConnections, Allocator.Persistent);
        m_Socket = new UdpNetworkDriver(new NetworkBitStreamParameter { size = 10 * NetworkConfig.maxPackageSize });
        m_Socket.Bind(new IPEndPoint(IPAddress.Any, port));

        if (port != 0)
            m_Socket.Listen();
    }

    public int Connect(string ip, int port)
    {
        var connection = m_Socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
        m_IdToConnection[connection.InternalId] = connection;
        return connection.InternalId;
    }

    public void Disconnect(int connection)
    {
        m_Socket.Disconnect(m_IdToConnection[connection]);
        m_IdToConnection[connection] = default(NetworkConnection);
    }

    public int Update()
    {
        return m_Socket.Update();
    }

    public bool NextEvent(ref TransportEvent e)
    {
        NetworkConnection connection;

        BitSlice slice;
        var ev = m_Socket.PopEvent(out connection, out slice);
        
        GameDebug.Assert(m_Buffer.Length >= slice.Length);
        slice.ReadBytesIntoArray(ref m_Buffer, slice.Length);
        var size = slice.Length;
        
        switch (ev)
        {
            case EventType.Data:
                e.type = TransportEvent.Type.Data;
                e.data = m_Buffer;
                e.dataSize = size;
                e.connectionId = connection.InternalId;
                break;
            case EventType.Connect:
                e.type = TransportEvent.Type.Connect;
                e.connectionId = connection.InternalId;
                m_IdToConnection[connection.InternalId] = connection;
                break;
            case EventType.Disconnect:
                e.type = TransportEvent.Type.Disconnect;
                e.connectionId = connection.InternalId;
                break;
            default:
                return false;
        }

        return true;
    }

    public void SendData(int connectionId, byte[] data, int sendSize)
    {
        using (var sendStream = new BitStream(data, Allocator.Temp, sendSize))
        {
            m_Socket.Send(m_IdToConnection[connectionId], sendStream);
        }
    }

    public string GetConnectionDescription(int connectionId)
    {
        return "";
    }

    public void Shutdown()
    {
        m_Socket.Dispose();
        m_IdToConnection.Dispose();
    }

    byte[] m_Buffer = new byte[1024 * 8];
    BasicNetworkDriver<IPv4UDPSocket> m_Socket;
    NativeArray<NetworkConnection> m_IdToConnection;
}
