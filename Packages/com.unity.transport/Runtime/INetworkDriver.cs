using System;
using System.Net;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Networking.Transport
{
    /// <summary>
    /// The NetworkDriver interface is the main entry point for the transport.
    /// The Driver is similar to a UDP socket which can handle many connections.
    /// </summary>
    public interface INetworkDriver : IDisposable
    {
        // :: Driver Helpers
        /// <summary>
        /// Schedule a job to update the state of the NetworkDriver, read messages and events from the underlying
        /// network interface and populate the event queues to allow reading from connections concurrently.
        /// </summary>
        JobHandle ScheduleUpdate(JobHandle dep = default(JobHandle));

        // :: Connection Helpers
        /// <summary>
        /// Bind the NetworkDriver to a port locally. This must be called before
        /// the socket can listen for incoming connections.
        /// </summary>
        int Bind(NetworkEndPoint endpoint);

        /// <summary>
        /// Enable listening for incoming connections on this driver. Before calling this
        /// all connection attempts will be rejected.
        /// </summary>
        int Listen();

        /// <summary>
        /// Accept a pending connection attempt and get the established connection.
        /// This should be called until it returns an invalid connection to make sure
        /// all connections are accepted.
        /// </summary>
        NetworkConnection Accept();

        /// <summary>
        /// Establish a new connection to a server with a specific address and port.
        /// </summary>
        NetworkConnection Connect(NetworkEndPoint endpoint);

        /// <summary>
        /// Disconnect an existing connection.
        /// </summary>
        int Disconnect(NetworkConnection con);

        /// <summary>
        /// Get the state of an existing connection. If called with an invalid connection the call will return the Destroyed state.
        /// </summary>
        NetworkConnection.State GetConnectionState(NetworkConnection con);

        NetworkEndPoint RemoteEndPoint(NetworkConnection con);
        NetworkEndPoint LocalEndPoint();

        // :: Events
        /// <summary>
        /// Send a message to the specific connection.
        /// </summary>
        int Send(NetworkConnection con, DataStreamWriter strm);

        /// <summary>
        /// Send a message to the specific connection.
        /// </summary>
        int Send(NetworkConnection con, IntPtr data, int len);

        /// <summary>
        /// Receive an event for any connection.
        /// </summary>
        NetworkEvent.Type PopEvent(out NetworkConnection con, out DataStreamReader bs);

        /// <summary>
        /// Receive an event for a specific connection. Should be called until it returns Empty, even if the socket is disconnected.
        /// </summary>
        NetworkEvent.Type PopEventForConnection(NetworkConnection con, out DataStreamReader bs);
    }
}