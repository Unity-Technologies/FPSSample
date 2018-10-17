using System;
using System.Net;
using Unity.Collections;

namespace Experimental.Multiplayer
{
    public interface INetworkDriver : IDisposable
    {
        // :: Driver Helpers
        int Update();

        // :: Connection Helpers
        int Bind(NetworkEndPoint endpoint);
        int Listen();
        NetworkConnection Accept();

        NetworkConnection Connect(NetworkEndPoint endpoint);
        int Disconnect(NetworkConnection con);
        NetworkConnection.State GetConnectionState(NetworkConnection con);

        NetworkEndPoint RemoteEndPoint(NetworkConnection con);
        NetworkEndPoint LocalEndPoint();

        // :: Events
        int Send(NetworkConnection con, BitStream bs);
        NetworkEvent.Type PopEvent(out NetworkConnection con, out BitSlice bs);
        NetworkEvent.Type PopEventForConnection(NetworkConnection con, out BitSlice bs);
    }
}
