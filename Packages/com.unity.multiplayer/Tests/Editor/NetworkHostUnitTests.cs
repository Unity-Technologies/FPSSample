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

    public class NetworkHostUnitTests
    {
        private LocalNetworkDriver Driver;
        private LocalNetworkDriver RemoteDriver;

        [SetUp]
        public void IPC_Setup()
        {
            IPCManager.Instance.Initialize(100);

            Driver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
            RemoteDriver = new LocalNetworkDriver(new NetworkBitStreamParameter{size=64});
        }

        [TearDown]
        public void IPC_TearDown()
        {
            IPCManager.Instance.Destroy();

            Driver.Dispose();
            RemoteDriver.Dispose();
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
            var remote = RemoteDriver.Connect(Driver.LocalEndPoint());

            NetworkConnection id;
            BitSlice slice;
            const int maximumIterations = 10;
            int count = 0;
            bool connected = false;
            while (count++ < maximumIterations)
            {
                // Clear pending events
                Driver.PopEvent(out id, out slice);
                RemoteDriver.PopEvent(out id, out slice);

                Driver.Update();
                RemoteDriver.Update();
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
