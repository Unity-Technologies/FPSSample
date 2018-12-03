using System;
using System.Net;
using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Tests.Helpers;
using UnityEngine.Apple.TV;
using Random = UnityEngine.Random;

namespace Unity.Networking.Transport.Tests
{
    using LocalNetworkDriver = BasicNetworkDriver<IPCSocket>;
    using UdpCNetworkDriver = BasicNetworkDriver<IPv4UDPSocket>;

    public static class SharedConstants
    {
        public static byte[] ping =
        {
            (byte) 'p',
            (byte) 'i',
            (byte) 'n',
            (byte) 'g'
        };

        public static byte[] pong =
        {
            (byte) 'p',
            (byte) 'o',
            (byte) 'n',
            (byte) 'g'
        };
    }

    public class NetworkConnectionUnitTests
    {
        private LocalNetworkDriver Driver;
        private LocalNetworkDriver RemoteDriver;
        private DataStreamWriter Stream;

        [SetUp]
        public void IPC_Setup()
        {
            IPCManager.Instance.Initialize(100);

            Stream = new DataStreamWriter(64, Allocator.Persistent);
            Driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            RemoteDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});

            RemoteDriver.Bind(IPCManager.Instance.CreateEndPoint("remote_host"));
            RemoteDriver.Listen();
        }

        [TearDown]
        public void IPC_TearDown()
        {
            Driver.Dispose();
            RemoteDriver.Dispose();
            Stream.Dispose();

            IPCManager.Instance.Destroy();
        }

        [Test]
        public void CreateAndConnect_NetworkConnection_ToRemoteEndPoint()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            Assert.That(RemoteDriver.Accept().IsCreated);

            Driver.ScheduleUpdate().Complete();
            DataStreamReader reader;
            Assert.That(connection.PopEvent(Driver, out reader) == NetworkEvent.Type.Connect);
        }


        [Test]
        public void CreateConnectPopAndClose_NetworkConnection_ToRemoteEndPoint()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            var remoteId = default(NetworkConnection);
            Assert.That((remoteId = RemoteDriver.Accept()) != default(NetworkConnection));

            DataStreamReader reader;

            Driver.ScheduleUpdate().Complete();
            Assert.That(connection.PopEvent(Driver, out reader) == NetworkEvent.Type.Connect);

            connection.Close(Driver);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            Assert.That(
                RemoteDriver.PopEventForConnection(remoteId, out reader) == NetworkEvent.Type.Disconnect);
        }

        [Test]
        public void Connection_SetupSendAndReceive()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            var remoteId = default(NetworkConnection);
            Assert.That((remoteId = RemoteDriver.Accept()) != default(NetworkConnection));

            DataStreamReader reader;

            Driver.ScheduleUpdate().Complete();
            Assert.That(connection.PopEvent(Driver, out reader) == NetworkEvent.Type.Connect);

            // Send to endpoint
            Stream.Write(SharedConstants.ping);
            connection.Send(Driver, Stream);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            var ev = RemoteDriver.PopEventForConnection(remoteId, out reader);
            Assert.That(ev == NetworkEvent.Type.Data);
            Assert.That(reader.Length == SharedConstants.ping.Length);

            connection.Close(Driver);
            Driver.ScheduleUpdate().Complete();
            RemoteDriver.ScheduleUpdate().Complete();
            
            Assert.That(
                RemoteDriver.PopEventForConnection(remoteId, out reader) == NetworkEvent.Type.Disconnect);
        }
    }
}