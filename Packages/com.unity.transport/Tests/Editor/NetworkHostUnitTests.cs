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

    public class NetworkHostUnitTests
    {
        private LocalNetworkDriver Driver;
        private LocalNetworkDriver RemoteDriver;

        [SetUp]
        public void IPC_Setup()
        {
            IPCManager.Instance.Initialize(100);

            Driver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
            RemoteDriver = new LocalNetworkDriver(new NetworkDataStreamParameter {size = 64});
        }

        [TearDown]
        public void IPC_TearDown()
        {
            Driver.Dispose();
            RemoteDriver.Dispose();
            IPCManager.Instance.Destroy();
        }

        [Test]
        public void Listen()
        {
            Driver.Bind(IPCManager.Instance.CreateEndPoint("network_host"));
            Driver.Listen();
            Assert.That(Driver.Listening);
        }

        [Test]
        public void Accept()
        {
            Driver.Bind(IPCManager.Instance.CreateEndPoint("network_host"));
            Driver.Listen();
            Assert.That(Driver.Listening);

            // create connection to test to connect.
            /*var remote =*/ RemoteDriver.Connect(Driver.LocalEndPoint());

            NetworkConnection id;
            DataStreamReader reader;
            const int maximumIterations = 10;
            int count = 0;
            bool connected = false;
            while (count++ < maximumIterations)
            {
                // Clear pending events
                Driver.PopEvent(out id, out reader);
                RemoteDriver.PopEvent(out id, out reader);

                Driver.ScheduleUpdate().Complete();
                RemoteDriver.ScheduleUpdate().Complete();
                var connection = Driver.Accept();
                if (connection != default(NetworkConnection))
                {
                    connected = true;
                }
            }

            Assert.That(connected);
        }
    }
}