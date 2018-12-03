using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Unity.Networking.Transport.LowLevel.Unsafe;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Utilities;
using Random = UnityEngine.Random;

namespace Unity.Networking.Transport.Tests.Utilities
{
    using System.Linq;
    public static class Random
    {
        private static System.Random random = new System.Random();

        public static string String(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}

namespace Unity.Networking.Transport.Tests
{
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;
    using UdpCNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;

    public struct LocalDriverHelper : IDisposable
    {
        public NetworkEndPoint Address { get; }
        public LocalNetworkDriver m_LocalDriver;
        private DataStreamWriter m_LocalDataStream;
        public NetworkConnection Connection { get; internal set; }
        public List<NetworkConnection> ClientConnections;

        public LocalDriverHelper(NetworkEndPoint endpoint, params INetworkParameter[] networkParams)
        {
            if (networkParams.Length == 0)
                m_LocalDriver = new LocalNetworkDriver(new NetworkDataStreamParameter
                    {size = NetworkParameterConstants.MTU});
            else
                m_LocalDriver = new LocalNetworkDriver(networkParams);
            m_LocalDataStream = new DataStreamWriter(NetworkParameterConstants.MTU, Allocator.Persistent);

            if (endpoint.Family == NetworkFamily.IPC && endpoint.Port != 0)
            {
                Address = endpoint;
            }
            else
            {
                Address = IPCManager.Instance.CreateEndPoint(Utilities.Random.String(32));
            }

            Connection = default(NetworkConnection);
            ClientConnections = new List<NetworkConnection>();
        }

        public void Dispose()
        {
            m_LocalDriver.Dispose();
            m_LocalDataStream.Dispose();
        }

        public void Update()
        {
            m_LocalDriver.ScheduleUpdate().Complete();
        }

        public NetworkConnection Accept()
        {
            return m_LocalDriver.Accept();
        }

        public void Host()
        {
            m_LocalDriver.Bind(Address);
            m_LocalDriver.Listen();
        }

        public void Connect(NetworkEndPoint endpoint)
        {
            Assert.True(endpoint.Family == NetworkFamily.IPC);
            Connection = m_LocalDriver.Connect(endpoint);
            m_LocalDriver.ScheduleUpdate().Complete();
        }

        public unsafe void Assert_GotConnectionRequest(NetworkEndPoint from, bool accept = false)
        {
            int length;
            NetworkEndPoint remote;
            m_LocalDataStream.Clear();
            Assert.True(
                IPCManager.Instance.PeekNext(Address, m_LocalDataStream.GetUnsafePtr(), out length, out remote) >=
                sizeof(UdpCHeader));
            m_LocalDataStream.WriteBytesWithUnsafePointer(length);

            UdpCHeader header = new UdpCHeader();
            var reader = new DataStreamReader(m_LocalDataStream, 0, sizeof(UdpCHeader));
            var readerCtx = default(DataStreamReader.Context);
            Assert.True(reader.IsCreated);
            reader.ReadBytes(ref readerCtx, header.Data, sizeof(UdpCHeader));
            Assert.True(header.Type == (int) UdpCProtocol.ConnectionRequest);

            Assert.True(remote.Family == NetworkFamily.IPC);
            //Assert.True(remote.ipc_handle == from.ipc_handle);
            Assert.True(remote.Port == from.Port);

            if (accept)
            {
                m_LocalDriver.ScheduleUpdate().Complete();
                var con = m_LocalDriver.Accept();
                ClientConnections.Add(con);
                Assert.True(con != default(NetworkConnection));
            }
        }

        public unsafe void Assert_GotDisconnectionRequest(NetworkEndPoint from)
        {
            int length;
            NetworkEndPoint remote;
            m_LocalDataStream.Clear();
            Assert.True(
                IPCManager.Instance.PeekNext(Address, m_LocalDataStream.GetUnsafePtr(), out length, out remote) >=
                sizeof(UdpCHeader));
            m_LocalDataStream.WriteBytesWithUnsafePointer(length);

            UdpCHeader header = new UdpCHeader();
            var reader = new DataStreamReader(m_LocalDataStream, 0, sizeof(UdpCHeader));
            var readerCtx = default(DataStreamReader.Context);
            Assert.True(reader.IsCreated);
            reader.ReadBytes(ref readerCtx, header.Data, sizeof(UdpCHeader));
            Assert.True(header.Type == (int) UdpCProtocol.Disconnect);

            Assert.True(remote.Family == NetworkFamily.IPC);
            //Assert.True(remote.ipc_handle == from.ipc_handle);
            Assert.True(remote.Port == from.Port);
        }

        public unsafe void Assert_GotDataRequest(NetworkEndPoint from, byte[] dataToCompare)
        {
            NetworkEndPoint remote = default(NetworkEndPoint);
            m_LocalDataStream.Clear();
            network_iovec[] iovecs = new network_iovec[2];
            iovecs[0].buf = m_LocalDataStream.GetUnsafePtr();
            iovecs[0].len = sizeof(UdpCHeader);
            iovecs[1].buf = m_LocalDataStream.GetUnsafePtr() + sizeof(UdpCHeader);
            iovecs[1].len = NetworkParameterConstants.MTU;
            int dataLen = 0;
            fixed (network_iovec* iovptr = &iovecs[0])
            {
                dataLen = IPCManager.Instance.ReceiveMessageEx(Address, iovptr, 2, ref remote);
            }

            if (dataLen <= 0)
            {
                iovecs[0].len = iovecs[1].len = 0;
            }

            Assert.True(iovecs[0].len+iovecs[1].len == dataLen);
            Assert.True(iovecs[0].len == sizeof(UdpCHeader));
            m_LocalDataStream.WriteBytesWithUnsafePointer(iovecs[0].len);

            UdpCHeader header = new UdpCHeader();
            var reader = new DataStreamReader(m_LocalDataStream, 0, sizeof(UdpCHeader));
            var readerCtx = default(DataStreamReader.Context);
            Assert.True(reader.IsCreated);
            reader.ReadBytes(ref readerCtx, header.Data, sizeof(UdpCHeader));
            Assert.True(header.Type == (int) UdpCProtocol.Data);

            Assert.True(remote.Family == NetworkFamily.IPC);
            //Assert.True(remote.ipc_handle == from.ipc_handle);
            Assert.True(remote.Port == from.Port);

            Assert.True(iovecs[1].len == dataToCompare.Length);
            m_LocalDataStream.WriteBytesWithUnsafePointer(iovecs[1].len);

            reader = new DataStreamReader(m_LocalDataStream, iovecs[0].len, dataToCompare.Length);
            readerCtx = default(DataStreamReader.Context);
            var received = reader.ReadBytesAsArray(ref readerCtx, dataToCompare.Length);

            for (int i = 0, n = dataToCompare.Length; i < n; ++i)
                Assert.True(received[i] == dataToCompare[i]);
        }

        public unsafe void Assert_PopEventForConnection(NetworkConnection connection, NetworkEvent.Type evnt)
        {
            DataStreamReader reader;
            var retval = m_LocalDriver.PopEventForConnection(connection, out reader);
            Assert.True(retval == evnt);
        }

        public unsafe void Assert_PopEvent(out NetworkConnection connection, NetworkEvent.Type evnt)
        {
            DataStreamReader reader;

            var retval = m_LocalDriver.PopEvent(out connection, out reader);
            Assert.True(retval == evnt);
        }
    }

    public class NetworkDriverUnitTests
    {
        private Timer m_timer;
        [SetUp]
        public void IPC_Setup()
        {
            m_timer = new Timer();
            IPCManager.Instance.Initialize(100);
        }

        [TearDown]
        public void IPC_TearDown()
        {
            IPCManager.Instance.Destroy();
        }

        [Test]
        public void InitializeAndDestroyDriver()
        {
            var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            driver.Dispose();
        }

        [Test]
        public void BindDriverToAEndPoint()
        {
            var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

            driver.Bind(IPCManager.Instance.CreateEndPoint("host"));
            driver.Dispose();
        }

        [Test]
        public void ListenOnDriver()
        {
            var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

            // Make sure we Bind before we Listen.
            driver.Bind(IPCManager.Instance.CreateEndPoint("host"));
            driver.Listen();

            Assert.True(driver.Listening);
            driver.Dispose();
        }

        [Test]
        public void AcceptNewConnectionsOnDriver()
        {
            var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

            // Make sure we Bind before we Listen.
            driver.Bind(IPCManager.Instance.CreateEndPoint("host"));
            driver.Listen();

            Assert.True(driver.Listening);

            //NetworkConnection connection;
            while ((/*connection =*/ driver.Accept()) != default(NetworkConnection))
            {
                //Assert.True(connectionId != NetworkParameterConstants.InvalidConnectionId);
            }

            driver.Dispose();
        }

        [Test]
        public void ConnectToARemoteEndPoint()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint)))
            {
                host.Host();
                var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

                NetworkConnection connectionId = driver.Connect(host.Address);
                Assert.True(connectionId != default(NetworkConnection));
                driver.ScheduleUpdate().Complete();

                var local = driver.LocalEndPoint();
                host.Assert_GotConnectionRequest(local);

                driver.Dispose();
            }
        }

        // TODO: Add tests where connection attempts are exceeded (connect fails)
        // TODO: Test dropped connection accept messages (accept retries happen)
        // TODO: Needs a way to explicitly assert on connect attempt stats
        // In this test multiple connect requests are received on the server, from client, might be this is expected
        // because of how the IPC driver works, but this situation is handled properly at least by basic driver logic.
        [Test]
        public void ConnectAttemptWithRetriesToARemoteEndPoint()
        {
            NetworkConnection connection;
            NetworkEvent.Type eventType = 0;
            DataStreamReader reader;
            var hostAddress = IPCManager.Instance.CreateEndPoint(Utilities.Random.String(32));

            // Tiny connect timeout for this test to be quicker
            using (var client = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64},
                new NetworkConfigParameter {connectTimeout = 1, maxConnectAttempts = 10}))
            {
                client.Connect(hostAddress);

                // Wait past the connect timeout so there will be unanswered connect requests
                long timeout = m_timer.ElapsedMilliseconds + 2;
                while (m_timer.ElapsedMilliseconds < timeout)
                    client.ScheduleUpdate().Complete();

                using (var host = new LocalDriverHelper(hostAddress))
                {
                    host.Host();

                    // Now give the next connect attempt time to happen
                    // TODO: Would be better to be able to see internal state here and explicitly wait until next connect attempt happens
                    timeout = m_timer.ElapsedMilliseconds + 10;
                    while (m_timer.ElapsedMilliseconds < timeout)
                        client.ScheduleUpdate().Complete();

                    host.Assert_GotConnectionRequest(client.LocalEndPoint(), true);

                    // Wait for the client to get the connect event back
                    timeout = m_timer.ElapsedMilliseconds + 2;
                    while (m_timer.ElapsedMilliseconds < timeout)
                    {
                        client.ScheduleUpdate().Complete();
                        eventType = client.PopEvent(out connection, out reader);
                        if (eventType != NetworkEvent.Type.Empty)
                            break;
                    }

                    Assert.AreEqual(NetworkEvent.Type.Connect, eventType);
                }
            }
        }

        [Test]
        public void DisconnectFromARemoteEndPoint()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint)))
            {
                host.Host();
                var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

                // Need to be connected in order to be able to send a disconnect packet.
                NetworkConnection connectionId = driver.Connect(host.Address);
                Assert.True(connectionId != default(NetworkConnection));
                driver.ScheduleUpdate().Complete();

                var local = driver.LocalEndPoint();
                host.Assert_GotConnectionRequest(local, true);

                NetworkConnection con;
                DataStreamReader slice;
                // Pump so we get the accept message back.
                driver.ScheduleUpdate().Complete();
                Assert.AreEqual(NetworkEvent.Type.Connect, driver.PopEvent(out con, out slice));
                driver.Disconnect(connectionId);
                driver.ScheduleUpdate().Complete();

                host.Assert_GotDisconnectionRequest(local);

                driver.Dispose();
            }
        }

        [Test]
        public void DisconnectTimeoutOnServer()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint),
                new NetworkConfigParameter {disconnectTimeout = 40}))
            using (var client = new LocalNetworkDriver(new NetworkConfigParameter {disconnectTimeout = 40}))
            {
                NetworkConnection id;
                NetworkEvent.Type popEvent = NetworkEvent.Type.Empty;
                DataStreamReader reader;

                host.Host();

                client.Connect(host.Address);
                client.ScheduleUpdate().Complete();
                host.Assert_GotConnectionRequest(client.LocalEndPoint(), true);

                var stream = new DataStreamWriter(100, Allocator.Persistent);
                for (int i = 0; i < 100; i++)
                    stream.Write((byte) i);

                // Host sends stuff but gets nothing back, until disconnect timeout happens
                var timeout = m_timer.ElapsedMilliseconds + 100;
                while (m_timer.ElapsedMilliseconds < timeout)
                {
                    host.m_LocalDriver.Send(host.ClientConnections[0], stream);
                    popEvent = host.m_LocalDriver.PopEvent(out id, out reader);
                    if (popEvent != NetworkEvent.Type.Empty)
                        break;
                    host.Update();
                }

                stream.Dispose();
                Assert.AreEqual(NetworkEvent.Type.Disconnect, popEvent);
            }
        }

        [Test]
        public void SendDataToRemoteEndPoint()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint)))
            using (var stream = new DataStreamWriter(64, Allocator.Persistent))
            {
                host.Host();
                var driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

                // Need to be connected in order to be able to send a disconnect packet.
                NetworkConnection connectionId = driver.Connect(host.Address);
                Assert.True(connectionId != default(NetworkConnection));
                driver.ScheduleUpdate().Complete();
                var local = driver.LocalEndPoint();
                host.Assert_GotConnectionRequest(local, true);

                NetworkConnection con;
                DataStreamReader slice;
                // Pump so we get the accept message back.
                driver.ScheduleUpdate().Complete();
                Assert.AreEqual(NetworkEvent.Type.Connect, driver.PopEvent(out con, out slice));

                stream.Clear();
                var data = Encoding.ASCII.GetBytes("data to send");
                stream.Write(data);
                driver.Send(connectionId, stream);
                driver.ScheduleUpdate().Complete();

                host.Assert_GotDataRequest(local, data);

                driver.Dispose();
            }
        }

        [Test]
        public void HandleEventsFromSpecificEndPoint()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint)))
            using (var client0 = new LocalDriverHelper(default(NetworkEndPoint)))
            using (var client1 = new LocalDriverHelper(default(NetworkEndPoint)))
            {
                host.Host();
                client0.Connect(host.Address);
                client1.Connect(host.Address);

                host.Assert_PopEventForConnection(client0.Connection, NetworkEvent.Type.Empty);
                host.Assert_PopEventForConnection(client1.Connection, NetworkEvent.Type.Empty);

                host.Update();

                var clientConnectionId0 = host.Accept();
                Assert.True(clientConnectionId0 != default(NetworkConnection));
                var clientConnectionId1 = host.Accept();
                Assert.True(clientConnectionId1 != default(NetworkConnection));

                client1.Update();
                client1.Assert_PopEventForConnection(client1.Connection, NetworkEvent.Type.Connect);

                client0.Update();
                client0.Assert_PopEventForConnection(client0.Connection, NetworkEvent.Type.Connect);
            }
        }

        [Test]
        public void HandleEventsFromAnyEndPoint()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint)))
            using (var client0 = new LocalDriverHelper(default(NetworkEndPoint)))
            using (var client1 = new LocalDriverHelper(default(NetworkEndPoint)))
            {
                host.Host();
                client0.Connect(host.Address);
                client1.Connect(host.Address);

                host.Assert_PopEventForConnection(client0.Connection, NetworkEvent.Type.Empty);
                host.Assert_PopEventForConnection(client1.Connection, NetworkEvent.Type.Empty);

                host.Update();

                var clientConnectionId0 = host.Accept();
                Assert.True(clientConnectionId0 != default(NetworkConnection));
                var clientConnectionId1 = host.Accept();
                Assert.True(clientConnectionId1 != default(NetworkConnection));

                NetworkConnection id;

                client1.Update();
                client1.Assert_PopEvent(out id, NetworkEvent.Type.Connect);
                Assert.True(id == client1.Connection);

                client0.Update();
                client0.Assert_PopEvent(out id, NetworkEvent.Type.Connect);
                Assert.True(id == client0.Connection);
            }
        }

        [Test]
        public void FillInternalBitStreamBuffer()
        {
            const int k_InternalBufferSize = 1000;
            const int k_PacketCount = 21; // Exactly enough to fill the receive buffer + 1 too much
            const int k_PacketSize = 50;

            using (var host = new LocalNetworkDriver(new NetworkDataStreamParameter {size = k_InternalBufferSize}))
            using (var client = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64}))
            using (var stream = new DataStreamWriter(64, Allocator.Persistent))
            {
                host.Bind(IPCManager.Instance.CreateEndPoint(Utilities.Random.String(32)));
                host.Listen();

                NetworkConnection connectionId = client.Connect(host.LocalEndPoint());

                client.ScheduleUpdate().Complete();
                host.ScheduleUpdate().Complete();

                NetworkConnection poppedId;
                DataStreamReader reader;
                host.Accept();

                client.ScheduleUpdate().Complete();

                var retval = client.PopEvent(out poppedId, out reader);
                Assert.AreEqual(retval, NetworkEvent.Type.Connect);

                var dataBlob = new Dictionary<int, byte[]>();
                for (int i = 0; i < k_PacketCount; ++i)
                {
                    // Scramble each packet contents so you can't match reading the same data twice as success
                    dataBlob.Add(i, Encoding.ASCII.GetBytes(Utilities.Random.String(k_PacketSize)));
                }

                for (int i = 0; i < k_PacketCount; ++i)
                {
                    stream.Clear();
                    stream.Write(dataBlob[i]);
                    client.Send(connectionId, stream);
                }

                // Process the pending events
                client.ScheduleUpdate().Complete();
                host.ScheduleUpdate().Complete();

                for (int i = 0; i < k_PacketCount; ++i)
                {
                    retval = host.PopEvent(out poppedId, out reader);

                    if (i == k_PacketCount - 1)
                    {
                        Assert.AreEqual(retval, NetworkEvent.Type.Empty);
                        Assert.IsFalse(reader.IsCreated);
                        host.ScheduleUpdate().Complete();
                        retval = host.PopEvent(out poppedId, out reader);
                    }

                    Assert.AreEqual(retval, NetworkEvent.Type.Data);
                    Assert.AreEqual(k_PacketSize, reader.Length);

                    var readerCtx = default(DataStreamReader.Context);
                    for (int j = 0; j < k_PacketSize; ++j)
                    {
                        Assert.AreEqual(dataBlob[i][j], reader.ReadByte(ref readerCtx));
                    }
                }
            }
        }

        [Test]
        public void SendAndReceiveMessage_RealNetwork()
        {
            using (var clientSendData = new DataStreamWriter(64, Allocator.Persistent))
            {
                DataStreamReader stream;
                var serverEndpoint = new IPEndPoint(IPAddress.Loopback, Random.Range(2000, 65000));

                var serverDriver = new UdpCNetworkDriver(new NetworkDataStreamParameter {size = 64});
                serverDriver.Bind(serverEndpoint);

                serverDriver.Listen();

                var clientDriver = new UdpCNetworkDriver(new NetworkDataStreamParameter {size = 64});
                clientDriver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                var clientToServerId = clientDriver.Connect(serverEndpoint);

                NetworkConnection serverToClientId = default(NetworkConnection);
                // Retry a few times since the network might need some time to process
                for (int i = 0; i < 10 && serverToClientId == default(NetworkConnection); ++i)
                {
                    serverDriver.ScheduleUpdate().Complete();

                    serverToClientId = serverDriver.Accept();
                }

                Assert.That(serverToClientId != default(NetworkConnection));

                clientDriver.ScheduleUpdate().Complete();

                var eventId = clientDriver.PopEventForConnection(clientToServerId, out stream);
                Assert.That(eventId == NetworkEvent.Type.Connect);


                int testInt = 100;
                float testFloat = 555.5f;
                byte[] testByteArray = Encoding.ASCII.GetBytes("Some bytes blablabla 1111111111111111111");
                clientSendData.Write(testInt);
                clientSendData.Write(testFloat);
                clientSendData.Write(testByteArray.Length);
                clientSendData.Write(testByteArray);
                var sentBytes = clientDriver.Send(clientToServerId, clientSendData);

                // Header size is included in the sent bytes count (4 bytes overhead)
                Assert.AreEqual(clientSendData.Length + 4, sentBytes);

                clientDriver.ScheduleUpdate().Complete();
                serverDriver.ScheduleUpdate().Complete();

                DataStreamReader serverReceiveStream;
                eventId = serverDriver.PopEventForConnection(serverToClientId, out serverReceiveStream);
                var readerCtx = default(DataStreamReader.Context);

                Assert.True(eventId == NetworkEvent.Type.Data);
                var receivedInt = serverReceiveStream.ReadInt(ref readerCtx);
                var receivedFloat = serverReceiveStream.ReadFloat(ref readerCtx);
                var byteArrayLength = serverReceiveStream.ReadInt(ref readerCtx);
                var receivedBytes = serverReceiveStream.ReadBytesAsArray(ref readerCtx, byteArrayLength);

                Assert.True(testInt == receivedInt);
                Assert.That(Mathf.Approximately(testFloat, receivedFloat));
                Assert.AreEqual(testByteArray, receivedBytes);

                clientDriver.Dispose();
                serverDriver.Dispose();
            }
        }

        [Test]
        public void SendAndReceiveMessage()
        {
            using (var clientSendData = new DataStreamWriter(64, Allocator.Persistent))
            {
                DataStreamReader stream;
                var serverEndpoint = IPCManager.Instance.CreateEndPoint("server");

                var serverDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
                serverDriver.Bind(serverEndpoint);

                serverDriver.Listen();

                var clientDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
                clientDriver.Bind(IPCManager.Instance.CreateEndPoint("client"));

                var clientToServerId = clientDriver.Connect(serverEndpoint);
                clientDriver.ScheduleUpdate().Complete();

                serverDriver.ScheduleUpdate().Complete();

                NetworkConnection serverToClientId = serverDriver.Accept();
                Assert.That(serverToClientId != default(NetworkConnection));

                clientDriver.ScheduleUpdate().Complete();

                var eventId = clientDriver.PopEventForConnection(clientToServerId, out stream);
                Assert.That(eventId == NetworkEvent.Type.Connect);


                int testInt = 100;
                float testFloat = 555.5f;
                byte[] testByteArray = Encoding.ASCII.GetBytes("Some bytes blablabla 1111111111111111111");
                clientSendData.Write(testInt);
                clientSendData.Write(testFloat);
                clientSendData.Write(testByteArray.Length);
                clientSendData.Write(testByteArray);
                var sentBytes = clientDriver.Send(clientToServerId, clientSendData);

                // Header size is included in the sent bytes count (4 bytes overhead)
                Assert.AreEqual(clientSendData.Length + 4, sentBytes);

                clientDriver.ScheduleUpdate().Complete();
                serverDriver.ScheduleUpdate().Complete();

                DataStreamReader serverReceiveStream;
                eventId = serverDriver.PopEventForConnection(serverToClientId, out serverReceiveStream);
                var readerCtx = default(DataStreamReader.Context);

                Assert.True(eventId == NetworkEvent.Type.Data);
                var receivedInt = serverReceiveStream.ReadInt(ref readerCtx);
                var receivedFloat = serverReceiveStream.ReadFloat(ref readerCtx);
                var byteArrayLength = serverReceiveStream.ReadInt(ref readerCtx);
                var receivedBytes = serverReceiveStream.ReadBytesAsArray(ref readerCtx, byteArrayLength);

                Assert.True(testInt == receivedInt);
                Assert.That(Mathf.Approximately(testFloat, receivedFloat));
                Assert.AreEqual(testByteArray, receivedBytes);

                clientDriver.Dispose();
                serverDriver.Dispose();
            }
        }
    }
}