using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Experimental.Multiplayer.Protocols;
using Experimental.Multiplayer.Utilities;
using Random = UnityEngine.Random;

namespace Experimental.Multiplayer.Tests
{
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;
    using UdpCNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;

    public struct LocalDriverHelper : IDisposable
    {
        public NetworkEndPoint Address { get; }
        public LocalNetworkDriver m_LocalDriver;
        private BitStream m_LocalBitStream;
        public NetworkConnection Connection { get; internal set; }
        public List<NetworkConnection> ClientConnections;
        
        public LocalDriverHelper(NetworkEndPoint endpoint, params INetworkParameter[] networkParams)
        {
            if (networkParams.Length == 0)
                m_LocalDriver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=NetworkParameterConstants.MTU});
            else
                m_LocalDriver = new LocalNetworkDriver(networkParams);
            m_LocalBitStream = new BitStream(NetworkParameterConstants.MTU, Allocator.Persistent);

            if (endpoint.family == NetworkFamily.IPC && endpoint.port != 0)
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
            m_LocalBitStream.Dispose();
        }

        public void Update()
        {
            m_LocalDriver.Update();
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
            Assert.True(endpoint.family == NetworkFamily.IPC);
            Connection = m_LocalDriver.Connect(endpoint);
        }

        public unsafe void Assert_GotConnectionRequest(NetworkEndPoint from, bool accept = false)
        {
            int length;
            NetworkEndPoint remote;
            Assert.True(IPCManager.Instance.PeekNext(Address, m_LocalBitStream.GetUnsafePtr(), out length, out remote) == sizeof(UdpCHeader));

            UdpCHeader header = new UdpCHeader();
            var slice = m_LocalBitStream.GetBitSlice(0, sizeof(UdpCHeader));
            Assert.True(slice.isValid);
            slice.ReadBytes(header.Data, sizeof(UdpCHeader));
            Assert.True(header.Type == (int)UdpCProtocol.ConnectionRequest);
            
            Assert.True(remote.family == NetworkFamily.IPC);
            Assert.True(*(int*)remote.address == *(int*)from.address);
            Assert.True(remote.port == from.port);

            if (accept)
            {
                m_LocalDriver.Update();
                var con = m_LocalDriver.Accept();
                ClientConnections.Add(con);
                Assert.True(con != default(NetworkConnection));
            }
        }
        
        public unsafe void Assert_GotDisconnectionRequest(NetworkEndPoint from)
        {
            int length;
            NetworkEndPoint remote;
            Assert.True(IPCManager.Instance.PeekNext(Address, m_LocalBitStream.GetUnsafePtr(), out length, out remote) == sizeof(UdpCHeader));

            UdpCHeader header = new UdpCHeader();
            var slice = m_LocalBitStream.GetBitSlice(0, sizeof(UdpCHeader));
            Assert.True(slice.isValid);
            slice.ReadBytes(header.Data, sizeof(UdpCHeader));
            Assert.True(header.Type == (int)UdpCProtocol.Disconnect);
            
            Assert.True(remote.family == NetworkFamily.IPC);
            Assert.True(*(int*)remote.address == *(int*)from.address);
            Assert.True(remote.port == from.port);
        }

        public unsafe void Assert_GotDataRequest(NetworkEndPoint from, byte[] dataToCompare)
        {
            int length;
            NetworkEndPoint remote;
            Assert.True(IPCManager.Instance.RecvFrom(Address, m_LocalBitStream.GetUnsafePtr(), out length, out remote) == sizeof(UdpCHeader));

            UdpCHeader header = new UdpCHeader();
            var slice = m_LocalBitStream.GetBitSlice(0, sizeof(UdpCHeader));
            Assert.True(slice.isValid);
            slice.ReadBytes(header.Data, sizeof(UdpCHeader));
            Assert.True(header.Type == (int) UdpCProtocol.Data);
            
            Assert.True(remote.family == NetworkFamily.IPC);
            Assert.True(*(int*)remote.address == *(int*)from.address);
            Assert.True(remote.port == from.port);
            
            Assert.True(IPCManager.Instance.RecvFrom(Address, m_LocalBitStream.GetUnsafePtr(), out length, out remote) == dataToCompare.Length);
            
            slice = m_LocalBitStream.GetBitSlice(0, dataToCompare.Length);
            var received = slice.ReadBytesAsArray(dataToCompare.Length);

            for (int i = 0, n = dataToCompare.Length; i < n; ++i)
                Assert.True(received[i] == dataToCompare[i]);
        }

        public unsafe void Assert_PopEventForConnection(NetworkConnection connection, NetworkEvent.Type evnt)
        {
            BitSlice slice;
            var retval = m_LocalDriver.PopEventForConnection(connection, out slice);
            Assert.True(retval == evnt);
        }
        
        public unsafe void Assert_PopEvent(out NetworkConnection connection, NetworkEvent.Type evnt)
        {
            BitSlice slice;
            
            var retval = m_LocalDriver.PopEvent(out connection, out slice);
            Assert.True(retval == evnt);
        }
    }

    public class NetworkDriverUnitTests
    {
        [SetUp]
        public void IPC_Setup()
        {
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
            var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
            driver.Dispose();
        }

        [Test]
        public void BindDriverToAEndPoint()
        {
            var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});

            driver.Bind(IPCManager.Instance.CreateEndPoint("host"));
            driver.Dispose();
        }

        [Test]
        public void ListenOnDriver()
        {
            var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});

            // Make sure we Bind before we Listen.
            driver.Bind(IPCManager.Instance.CreateEndPoint("host"));
            driver.Listen();

            Assert.True(driver.Listening);
            driver.Dispose();
        }

        [Test]
        public void AcceptNewConnectionsOnDriver()
        {
            var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});

            // Make sure we Bind before we Listen.
            driver.Bind(IPCManager.Instance.CreateEndPoint("host"));
            driver.Listen();

            Assert.True(driver.Listening);

            NetworkConnection connection;
            while ((connection = driver.Accept()) != default(NetworkConnection))
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
                var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});

                NetworkConnection connectionId = driver.Connect(host.Address);
                Assert.True(connectionId != default(NetworkConnection));
                
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
            BitSlice slice;
            var hostAddress = IPCManager.Instance.CreateEndPoint(Utilities.Random.String(32));

            // Tiny connect timeout for this test to be quicker
            using (var client = new LocalNetworkDriver(new NetworkBitStreamParameter { size = 64 }, new NetworkConfigParameter { connectTimeout = 1, maxConnectAttempts = 10 }))
            {
                client.Connect(hostAddress);
                
                // Wait past the connect timeout so there will be unanswered connect requests
                long timeout = Timer.ElapsedMilliseconds + 2;
                while (Timer.ElapsedMilliseconds < timeout)
                    client.Update();

                using (var host = new LocalDriverHelper(hostAddress))
                {
                    host.Host();

                    // Now give the next connect attempt time to happen
                    // TODO: Would be better to be able to see internal state here and explicitly wait until next connect attempt happens
                    timeout = Timer.ElapsedMilliseconds + 10;
                    while (Timer.ElapsedMilliseconds < timeout)
                        client.Update();

                    host.Assert_GotConnectionRequest(client.LocalEndPoint(), true);

                    // Wait for the client to get the connect event back
                    timeout = Timer.ElapsedMilliseconds + 2;
                    while (Timer.ElapsedMilliseconds < timeout)
                    {
                        client.Update();
                        eventType = client.PopEvent(out connection, out slice);
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
                var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
                
                // Need to be connected in order to be able to send a disconnect packet.
                NetworkConnection connectionId = driver.Connect(host.Address);
                Assert.True(connectionId != default(NetworkConnection));
                
                var local = driver.LocalEndPoint();
                host.Assert_GotConnectionRequest(local, true);

                // Pump so we get the accept message back.
                driver.Update();
                driver.Disconnect(connectionId);
                
                host.Assert_GotDisconnectionRequest(local);
                
                driver.Dispose();
            } 
        }

        [Test]
        public void DisconnectTimeoutOnServer()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint), new NetworkConfigParameter {disconnectTimeout = 40 }))
            using (var client = new LocalNetworkDriver(new NetworkConfigParameter { disconnectTimeout = 40 }))
            {
                NetworkConnection id;
                NetworkEvent.Type popEvent;
                BitSlice slice;

                host.Host();

                client.Connect(host.Address);
                host.Assert_GotConnectionRequest(client.LocalEndPoint(), true);

                // Eat connect event
                popEvent = host.m_LocalDriver.PopEvent(out id, out slice);
                Assert.AreEqual(NetworkEvent.Type.Connect, popEvent);

                var stream = new BitStream(100, Allocator.Persistent);
                for (int i = 0; i < 100; i++)
                    stream.Write((byte)i);

                // Host sends stuff but gets nothing back, until disconnect timeout happens
                var timeout = Timer.ElapsedMilliseconds + 100;
                while (Timer.ElapsedMilliseconds < timeout)
                {
                    host.m_LocalDriver.Send(host.ClientConnections[0], stream);
                    popEvent = host.m_LocalDriver.PopEvent(out id, out slice);
                    if (popEvent != NetworkEvent.Type.Empty)
                        break;
                    host.Update();
                }
                Assert.AreEqual(NetworkEvent.Type.Disconnect, popEvent);
            }
        }

        [Test]
        public void SendDataToRemoteEndPoint()
        {
            using (var host = new LocalDriverHelper(default(NetworkEndPoint)))
            using (var stream = new BitStream(64, Allocator.Persistent))
            {
                host.Host();
                var driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
                
                // Need to be connected in order to be able to send a disconnect packet.
                NetworkConnection connectionId = driver.Connect(host.Address);
                Assert.True(connectionId != default(NetworkConnection));
                
                var local = driver.LocalEndPoint();
                host.Assert_GotConnectionRequest(local, true);

                // Pump so we get the accept message back.
                driver.Update();

                stream.Reset();
                var data = Encoding.ASCII.GetBytes("data to send");
                stream.Write(data);
                driver.Send(connectionId, stream);
                
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
                
                host.Assert_PopEventForConnection(clientConnectionId1, NetworkEvent.Type.Connect);
                host.Assert_PopEventForConnection(clientConnectionId0, NetworkEvent.Type.Connect);
                
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
                host.Assert_PopEvent(out id, NetworkEvent.Type.Connect);
                Assert.True(id == clientConnectionId0 || id == clientConnectionId1);
                host.Assert_PopEvent(out id, NetworkEvent.Type.Connect);
                Assert.True(id == clientConnectionId0 || id == clientConnectionId1);
                
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
            const int k_PacketCount = 21;   // Exactly enough to fill the receive buffer + 1 too much
            const int k_PacketSize = 50;

            using (var host = new LocalNetworkDriver(new NetworkBitStreamParameter { size = k_InternalBufferSize }))
            using (var client = new LocalNetworkDriver(new NetworkBitStreamParameter { size = 64 }))
            using (var stream = new BitStream(64, Allocator.Persistent))
            {
                host.Bind(IPCManager.Instance.CreateEndPoint(Utilities.Random.String(32)));
                host.Listen();

                NetworkConnection connectionId = client.Connect(host.LocalEndPoint());

                client.Update();
                host.Update();

                NetworkConnection poppedId;
                BitSlice slice;
                var retval = host.PopEvent(out poppedId, out slice);
                Assert.AreEqual(retval, NetworkEvent.Type.Connect);
                host.Accept();

                client.Update();

                retval = client.PopEvent(out poppedId, out slice);
                Assert.AreEqual(retval, NetworkEvent.Type.Connect);

                var dataBlob = new Dictionary<int, byte[]>();
                for (int i = 0; i < k_PacketCount; ++i)
                {
                    // Scramble each packet contents so you can't match reading the same data twice as success
                    dataBlob.Add(i, Encoding.ASCII.GetBytes(Utilities.Random.String(k_PacketSize)));
                }

                for (int i = 0; i < k_PacketCount; ++i)
                {
                    stream.Reset();
                    stream.Write(dataBlob[i]);
                    client.Send(connectionId, stream);
                }

                // Process the pending events
                client.Update();
                host.Update();

                for (int i = 0; i < k_PacketCount; ++i)
                {
                    retval = host.PopEvent(out poppedId, out slice);

                    if (i == k_PacketCount - 1)
                    {
                        Assert.AreEqual(retval, NetworkEvent.Type.Empty);
                        Assert.AreEqual(slice.Length, 0);
                        host.Update();
                        retval = host.PopEvent(out poppedId, out slice);
                    }

                    Assert.AreEqual(retval, NetworkEvent.Type.Data);
                    Assert.AreEqual(k_PacketSize, slice.Length);

                    for (int j = 0; j < k_PacketSize; ++j)
                    {
                        Assert.AreEqual(dataBlob[i][j], slice.ReadByte());
                    }
                }
            }
        }

        [Test]
        public void SendAndReceiveMessage_RealNetwork()
        {
            using (var clientSendData = new BitStream(64, Allocator.Persistent))
            {
                BitSlice stream;
                var serverEndpoint = new IPEndPoint(IPAddress.Loopback, Random.Range(2000, 65000));

                var serverDriver = new UdpCNetworkDriver(new NetworkBitStreamParameter{size=64});
                serverDriver.Bind(serverEndpoint);

                serverDriver.Listen();

                var clientDriver = new UdpCNetworkDriver(new NetworkBitStreamParameter{size=64});
                clientDriver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                var clientToServerId = clientDriver.Connect(serverEndpoint);

                NetworkConnection serverToClientId = default(NetworkConnection);
                // Retry a few times since the network might need some time to process
                for (int i = 0; i < 10 && serverToClientId == default(NetworkConnection); ++i)
                {
                    serverDriver.Update();

                    serverToClientId = serverDriver.Accept();
                }

                Assert.That(serverToClientId != default(NetworkConnection));

                var eventId = serverDriver.PopEventForConnection(serverToClientId, out stream);
                Assert.That(eventId == NetworkEvent.Type.Connect);

                clientDriver.Update();

                eventId = clientDriver.PopEventForConnection(clientToServerId, out stream);
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
                Assert.AreEqual(clientSendData.GetBytesWritten() + 4, sentBytes);

                clientDriver.Update();
                serverDriver.Update();

                BitSlice serverReceiveBitSlice;
                eventId = serverDriver.PopEventForConnection(serverToClientId, out serverReceiveBitSlice);

                Assert.True(eventId == NetworkEvent.Type.Data);
                var receivedInt = serverReceiveBitSlice.ReadInt();
                var receivedFloat = serverReceiveBitSlice.ReadFloat();
                var byteArrayLength = serverReceiveBitSlice.ReadInt();
                var receivedBytes = serverReceiveBitSlice.ReadBytesAsArray(byteArrayLength);

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
            using (var clientSendData = new BitStream(64, Allocator.Persistent))
            {
                BitSlice stream;
                var serverEndpoint = IPCManager.Instance.CreateEndPoint("server");

                var serverDriver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
                serverDriver.Bind(serverEndpoint);

                serverDriver.Listen();

                var clientDriver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
                clientDriver.Bind(IPCManager.Instance.CreateEndPoint("client"));

                var clientToServerId = clientDriver.Connect(serverEndpoint);

                serverDriver.Update();

                NetworkConnection serverToClientId = serverDriver.Accept();
                Assert.That(serverToClientId != default(NetworkConnection));

                var eventId = serverDriver.PopEventForConnection(serverToClientId, out stream);
                Assert.That(eventId == NetworkEvent.Type.Connect);

                clientDriver.Update();

                eventId = clientDriver.PopEventForConnection(clientToServerId, out stream);
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
                Assert.AreEqual(clientSendData.GetBytesWritten() + 4, sentBytes);

                clientDriver.Update();
                serverDriver.Update();

                BitSlice serverReceiveBitSlice;
                eventId = serverDriver.PopEventForConnection(serverToClientId, out serverReceiveBitSlice);

                Assert.True(eventId == NetworkEvent.Type.Data);
                var receivedInt = serverReceiveBitSlice.ReadInt();
                var receivedFloat = serverReceiveBitSlice.ReadFloat();
                var byteArrayLength = serverReceiveBitSlice.ReadInt();
                var receivedBytes = serverReceiveBitSlice.ReadBytesAsArray(byteArrayLength);

                Assert.True(testInt == receivedInt);
                Assert.That(Mathf.Approximately(testFloat, receivedFloat));
                Assert.AreEqual(testByteArray, receivedBytes);

                clientDriver.Dispose();
                serverDriver.Dispose();
            }
        }
    }
}
