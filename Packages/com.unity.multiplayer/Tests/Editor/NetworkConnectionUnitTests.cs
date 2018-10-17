using System;
using System.Net;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Experimental.Multiplayer.Protocols;
using Experimental.Multiplayer.Tests.Helpers;
using UnityEngine.Apple.TV;
using Random = UnityEngine.Random;

namespace Experimental.Multiplayer.Tests
{
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;
    using UdpCNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;

    public static class SharedConstants
    {
        public static byte[] ping = {
            (byte)'p',
            (byte)'i',
            (byte)'n',
            (byte)'g'
        };

        public static byte[] pong = {
            (byte)'p',
            (byte)'o',
            (byte)'n',
            (byte)'g'
        };
    }

    public class NetworkConnectionUnitTests
    {
        private LocalNetworkDriver Driver;
        private LocalNetworkDriver RemoteDriver;
        private BitStream Stream;
        
        [SetUp]
        public void IPC_Setup()
        {
            IPCManager.Instance.Initialize(100);

            Stream = new BitStream(64, Allocator.Persistent);
            Driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
            RemoteDriver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
            
            RemoteDriver.Bind(IPCManager.Instance.CreateEndPoint("remote_host"));
            RemoteDriver.Listen();
        }

        [TearDown]
        public void IPC_TearDown()
        {
            IPCManager.Instance.Destroy();

            Driver.Dispose();
            RemoteDriver.Dispose();
            Stream.Dispose();
        }

        [Test]
        public void CreateAndConnect_NetworkConnection_ToRemoteEndPoint()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);

            RemoteDriver.Update();
            Assert.That(RemoteDriver.Accept().IsCreated);

            Driver.Update();
            BitSlice slice;
            Assert.That(connection.PopEvent(Driver, out slice) == NetworkEvent.Type.Connect);
        }
        
        
        [Test]
        public void CreateConnectPopAndClose_NetworkConnection_ToRemoteEndPoint()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);

            RemoteDriver.Update();
            var remoteId = default(NetworkConnection);
            Assert.That((remoteId = RemoteDriver.Accept()) != default(NetworkConnection));
            
            BitSlice slice;

            var ev = RemoteDriver.PopEventForConnection(remoteId, out slice);
            Assert.That(ev == NetworkEvent.Type.Connect);

            Driver.Update();
            Assert.That(connection.PopEvent(Driver, out slice) == NetworkEvent.Type.Connect);
            
            connection.Close(Driver);
            Assert.That(connection.PopEvent(Driver, out slice) == NetworkEvent.Type.Disconnect);
            Driver.Update();

            RemoteDriver.Update();
            Assert.That(
                RemoteDriver.PopEventForConnection(remoteId, out slice) == NetworkEvent.Type.Disconnect);
        }

        [Test]
        public void Connection_SetupSendAndReceive()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);

            RemoteDriver.Update();
            var remoteId = default(NetworkConnection);
            Assert.That((remoteId = RemoteDriver.Accept()) != default(NetworkConnection));
            
            BitSlice slice;

            var ev = RemoteDriver.PopEventForConnection(remoteId, out slice);
            Assert.That(ev == NetworkEvent.Type.Connect);

            Driver.Update();
            Assert.That(connection.PopEvent(Driver, out slice) == NetworkEvent.Type.Connect);
            
            // Send to endpoint
            Stream.Write(SharedConstants.ping);
            connection.Send(Driver, Stream);

            RemoteDriver.Update();
            ev = RemoteDriver.PopEventForConnection(remoteId, out slice);
            Assert.That(ev == NetworkEvent.Type.Data);
            Assert.That(slice.Length == SharedConstants.ping.Length);
            
            connection.Close(Driver);
            Assert.That(connection.PopEvent(Driver, out slice) == NetworkEvent.Type.Disconnect);
            Driver.Update();

            RemoteDriver.Update();
            Assert.That(
                RemoteDriver.PopEventForConnection(remoteId, out slice) == NetworkEvent.Type.Disconnect);
        }
        
    }
}
