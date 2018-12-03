using System;
using System.Threading;
using System.Net;
using NUnit.Framework;

using System.Net.Sockets;

using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Networking.Transport.LowLevel.Unsafe;
using UdpNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;
using ExperimentalEventType = Unity.Networking.Transport.NetworkEvent.Type;

namespace TransportTests
{
    public class SocketTests
    {
        [Test]
        public void UdpC_BindToEndpoint_ReturnSocketHandle()
        {
            using (var socket = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {
                var endpoint = new IPEndPoint(IPAddress.Any, 0);
                var socketError = socket.Bind(endpoint);

                Assert.AreEqual(socketError, (int)SocketError.Success);
            }
        }

        [Test]
        public void UdpC_BindMultipleToSameEndpoint_ReturnSocketError()
        {
            using (var first = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var second = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {

                var endpoint = new IPEndPoint(IPAddress.Any, 50001);

                var socketError = first.Bind(endpoint);
                Assert.AreEqual(socketError, (int)SocketError.Success);

                var error = second.Bind(endpoint);
                Assert.AreEqual(error, (int) SocketError.AddressAlreadyInUse);
            }
        }

        [Test]
        public void UdpC_ListenThenConnect_ShouldFail()
        {
            using (var socket = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {
                var endpoint = new IPEndPoint(IPAddress.Any, 50007);
                socket.Bind(endpoint);

                socket.Listen();

                var error = Assert.Throws<SocketException>(() => { socket.Connect(endpoint); });
                Assert.AreEqual(error.SocketErrorCode, SocketError.AddressNotAvailable);
            }
        }

        [Test]
        public void UdpC_ConnectTest_ShouldConnect()
        {
            using (var server = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {
                var serverPort = 50009;

                server.Bind(new IPEndPoint(IPAddress.Loopback, serverPort));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                server.Listen();

                var id = client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);
            }
        }

        [Test]
        public void UdpC_MultipleConnectTest_ShouldConnect()
        {
            using (var server = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client0 = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client1 = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client2 = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client3 = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {
                var serverPort = 50005;

                server.Bind(new IPEndPoint(IPAddress.Loopback, serverPort));
                client0.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                client1.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                client2.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                client3.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                server.Listen();

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;


                var id = client0.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
                ConnectTogether(server, client0, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                id = client1.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
                ConnectTogether(server, client1, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                id = client2.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
                ConnectTogether(server, client2, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                id = client3.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
                ConnectTogether(server, client3, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);
            }
        }

        [Test]
        public void UdpC_ConnectSendTest_ShouldConnectAndReceiveData()
        {
            using (var server = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {
                var serverPort = 50008;

                server.Bind(new IPEndPoint(IPAddress.Loopback, serverPort));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                server.Listen();

                client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);

                var message = new byte[]
                {
                    (byte) 'm',
                    (byte) 'e',
                    (byte) 's',
                    (byte) 's',
                    (byte) 'a',
                    (byte) 'g',
                    (byte) 'e'
                };

                SendReceive(client, server, clientConnection, serverConnection, message, maxIterations);
            }
        }

        [Test]
        public void UdpC_ReconnectAndResend_ShouldReconnectAndResend()
        {
            using (var server = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            using (var client = new UdpNetworkDriver(new NetworkDataStreamParameter{}))
            {
                var serverPort = 50007;

                server.Bind(new IPEndPoint(IPAddress.Loopback, serverPort));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                server.Listen();

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                var id = client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);

                client.Disconnect(id);

                server.ScheduleUpdate().Complete();
                
                var data = new byte[1472];
                var size = 1472;
                NetworkConnection from;

                Assert.AreEqual(ExperimentalEventType.Disconnect, PollEvent(ExperimentalEventType.Disconnect, maxIterations, server, ref data, out size, out from));

                id = client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);

                var message = new byte[]
                {
                    (byte) 'm',
                    (byte) 'e',
                    (byte) 's',
                    (byte) 's',
                    (byte) 'a',
                    (byte) 'g',
                    (byte) 'e'
                };

                SendReceive(client, server, clientConnection, serverConnection, message, maxIterations);
            }
        }

        [Test]
        public void UdpC_Timeout_ShouldDisconnect()
        {
            int customTimeout = 1000;

            using (var server = new UdpNetworkDriver(new NetworkConfigParameter { disconnectTimeout = customTimeout }))
            using (var client = new UdpNetworkDriver(new NetworkConfigParameter { disconnectTimeout = customTimeout }))
            {

                var serverPort = 50006;

                server.Bind(new IPEndPoint(IPAddress.Loopback, serverPort));
                client.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                
                server.Listen();

                var id = client.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));

                NetworkConnection serverConnection, clientConnection;
                int maxIterations = 100;

                ConnectTogether(server, client, maxIterations, out serverConnection, out clientConnection);
                Assert.AreEqual(id, serverConnection);

                // Force timeout
                Thread.Sleep(customTimeout + 500);
                
                var message = new DataStreamWriter(7, Allocator.Persistent);
                message.Write((byte) 'm');
                message.Write((byte) 'e');
                message.Write((byte) 's');
                message.Write((byte) 's');
                message.Write((byte) 'a');
                message.Write((byte) 'g');
                message.Write((byte) 'e');
                server.Send(clientConnection, message);

                var data = new byte[1472];
                int size = -1;
                NetworkConnection from;
                Assert.AreEqual(ExperimentalEventType.Disconnect, PollEvent(ExperimentalEventType.Disconnect, maxIterations, server, ref data, out size, out from));
                Assert.AreEqual(from, clientConnection);
            }
        }

        ExperimentalEventType PollEvent(ExperimentalEventType ev, int maxIterations, UdpNetworkDriver socket, ref byte[] buffer, out int size, out NetworkConnection connection)
        {
            int iterator = 0;
            size = 0;
            connection = default(NetworkConnection);
            
            while (iterator++ < maxIterations)
            {
                DataStreamReader reader;
                var context = default(DataStreamReader.Context);
                ExperimentalEventType e;
                if ((e = socket.PopEvent(out connection, out reader)) == ev)
                {
                    if (reader.IsCreated)
                    {
                        reader.ReadBytesIntoArray(ref context, ref buffer, reader.Length);
                        size = reader.Length;
                    }
                    return e;
                }
                socket.ScheduleUpdate().Complete();
            }
            return ExperimentalEventType.Empty;
        }

        void SendReceive(UdpNetworkDriver sender, UdpNetworkDriver receiver, NetworkConnection from, NetworkConnection to, byte[] data, int maxIterations)
        {
            using (var writer = new DataStreamWriter(data.Length, Allocator.Persistent))
            {
                writer.Write(data, data.Length);
                sender.Send(to, writer);

                sender.ScheduleUpdate().Complete();
                receiver.ScheduleUpdate().Complete();

                var buffer = new byte[1472];
                int size = 0;
                NetworkConnection connection;
                PollEvent(ExperimentalEventType.Data, maxIterations, receiver, ref buffer, out size, out connection);

                Assert.AreEqual(from, connection);
                Assert.AreEqual(data.Length, size);

                for (int i = 0; i < data.Length; i++)
                    Assert.AreEqual(data[i], buffer[i]);
            }
        }

        void ConnectTogether(UdpNetworkDriver server, UdpNetworkDriver client, int maxIterations, out NetworkConnection serverConnection, out NetworkConnection clientConnection)
        {
            int servers = 0, clients = 0, iterations = 0;
            serverConnection = default(NetworkConnection);
            clientConnection = default(NetworkConnection);

            DataStreamReader reader;

            NetworkConnection poppedConnection = default(NetworkConnection);
            while (clients != 1 || servers != 1)
            {
                Assert.Less(iterations++, maxIterations);

                server.ScheduleUpdate().Complete();

                var newConnection = server.Accept();
                if (newConnection != default(NetworkConnection))
                {
                    clients++;
                    clientConnection = newConnection;
                }

                if (client.PopEvent(out poppedConnection, out reader) == ExperimentalEventType.Connect)
                {
                    serverConnection = poppedConnection;
                    servers++;
                }

                client.ScheduleUpdate().Complete();
                Assert.AreNotEqual(clientConnection, default(NetworkConnection));
            }
        }

        [Test]
        public void UdpC_LongGoingTest()
        {
            using (UdpCClient server = new UdpCClient(12000))
            using (UdpCClient c0     = new UdpCClient(12001, 12000))
            using (UdpCClient c1     = new UdpCClient(12002, 12000))
            using (UdpCClient c2     = new UdpCClient(12003, 12000))
            using (UdpCClient c3     = new UdpCClient(12004, 12000))
            using (UdpCClient c4     = new UdpCClient(12005, 12000))
            using (UdpCClient c5     = new UdpCClient(12006, 12000))
            {
                long start = 0, now = 0;
                start = NetworkUtils.stopwatch.ElapsedMilliseconds;

                while (now - start < 30000)
                {
                    server.Update();
                    c0.Update();
                    c1.Update();
                    c2.Update();
                    c3.Update();
                    c4.Update();
                    c5.Update();
                    now = NetworkUtils.stopwatch.ElapsedMilliseconds;
                }

                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", server.connectCounter,
                    server.disconnectCounter, server.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c0.connectCounter, c0.disconnectCounter,
                    c0.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c1.connectCounter, c1.disconnectCounter,
                    c1.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c2.connectCounter, c2.disconnectCounter,
                    c2.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c3.connectCounter, c3.disconnectCounter,
                    c3.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c4.connectCounter, c4.disconnectCounter,
                    c4.dataCounter));
                GameDebug.Log(string.Format("con: {0}, disc {1}, data {2}", c5.connectCounter, c5.disconnectCounter,
                    c5.dataCounter));
            }
        }
    }

    public class UdpCClient : IDisposable
    {
        UdpNetworkDriver m_Socket;

        NetworkConnection conn = default(NetworkConnection);
        int serverPort;

        public int connectCounter;
        public int disconnectCounter;
        public int dataCounter;

        public UdpCClient(int port, int serverPort = -1)
        {
            m_Socket = new UdpNetworkDriver(new NetworkDataStreamParameter{});
            m_Socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            if (serverPort == -1)
                m_Socket.Listen();

            this.serverPort = serverPort;
        }

        public void Update()
        {
            if (!m_Socket.Listening && !conn.IsCreated)
            {
                conn = m_Socket.Connect(new IPEndPoint(IPAddress.Loopback, serverPort));
            }
            else if (!m_Socket.Listening && dataCounter == 0 && !conn.IsCreated)
            {
                using (var message = new DataStreamWriter(7, Allocator.Persistent))
                {
                    message.Write((byte)'m');
                    message.Write((byte)'e');
                    message.Write((byte)'s');
                    message.Write((byte)'s');
                    message.Write((byte)'a');
                    message.Write((byte)'g');
                    message.Write((byte)'e');

                    m_Socket.Send(conn, message);
                }
            }
            else if (!m_Socket.Listening && conn.IsCreated &&
                     UnityEngine.Random.Range(0, 1000) < 10)
            {
                m_Socket.Disconnect(conn);
                conn = default(NetworkConnection);
            }

            NetworkConnection connection;
            DataStreamReader reader;
            var context = default(DataStreamReader.Context);
            var ev = m_Socket.PopEvent(out connection, out reader);
            if (ev == ExperimentalEventType.Empty)
                return;

            using (var writer = new DataStreamWriter(reader.Length, Allocator.Temp))
            {
                unsafe
                {
                    reader.ReadBytes(ref context, writer.GetUnsafePtr(), reader.Length);
                }
                switch (ev)
                {
                    case ExperimentalEventType.Connect:
                        connectCounter++;
                        break;
                    case ExperimentalEventType.Disconnect:
                        conn = default(NetworkConnection);
                        disconnectCounter++;
                        break;
                    case ExperimentalEventType.Data:
                        dataCounter++;
                        m_Socket.Send(connection, writer);
                        break;
                }
            }
        }

        public void Dispose()
        {
            m_Socket.Dispose();
        }
    }
}
