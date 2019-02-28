using System;
using System.Collections.Generic;
using NUnit.Framework;


namespace NetcodeTests
{
    /*
    // Would like to do this but doesn't work with TestRunner right now
    [SetUpFixture]
    public class GlobalSetupClass
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            ConfigVar.Init();
        }
    }
    */

    // For now, we derive from this to get setup on the fixtures that need it
    public class NetTestBase : UnityEngine.TestTools.IPrebuildSetup
    {
        public void Setup()
        {
            ConfigVar.Init();
        }
    }

    [TestFixture]
    public class EventTests : NetTestBase
    {
        enum EventType
        {
            MyEvent = 1,
        }

        public class MyEvent : TestSerializable
        {
            public int intValue = 42;
            public bool boolValue = true;
            public float floatValue = 2981.212f;
            public string stringValue = "Hello world";

            public override void AssertReplicatedCorrectly(TestSerializable clientEntity, bool isPredicting)
            {
                var c = clientEntity as MyEvent;
                Assert.IsTrue(c != null);
                Assert.IsTrue(c.intValue == intValue);
                Assert.IsTrue(c.boolValue == boolValue);
                Assert.IsTrue(c.floatValue == floatValue);
                Assert.IsTrue(c.stringValue == stringValue);
            }

            public override void Deserialize(ref NetworkReader reader)
            {
                intValue = reader.ReadInt32();
                boolValue = reader.ReadBoolean();
                floatValue = reader.ReadFloat();
                stringValue = reader.ReadString();
            }

            public override void Serialize(ref NetworkWriter writer)
            {
                writer.WriteInt32("intValue", intValue);
                writer.WriteBoolean("boolValue", boolValue);
                writer.WriteFloat("floatValue", floatValue);
                writer.WriteString("stringValue", stringValue);
            }
        }

        public class ServerCallbacks : INetworkCallbacks
        {
            private EventTests m_Test;

            public ServerCallbacks(EventTests test)
            {
                m_Test = test;
            }

            public void OnConnect(int clientId) { }

            public void OnDisconnect(int clientId) { }

            unsafe public void OnEvent(int clientId, NetworkEvent info)
            {
                var received = new MyEvent();
                fixed(uint* data = info.data)
                {
                    var reader = new NetworkReader(data, info.type.schema);
                    received.Deserialize(ref reader);
                    received.AssertReplicatedCorrectly(m_Test.lastEventSent, false);
                }
                ++m_Test.eventReceived;
            }

            public void OnMapUpdate(ref NetworkReader data)
            {
            }
        }

        public class ClientCallbacks : INetworkClientCallbacks
        {
            private EventTests m_Test;

            public ClientCallbacks(EventTests test)
            {
                m_Test = test;
            }

            public void OnConnect(int clientId) { }

            public void OnDisconnect(int clientId) { }

            unsafe public void OnEvent(int clientId, NetworkEvent info)
            {
                var received = new MyEvent();
                fixed(uint* data = info.data)
                {
                    var reader = new NetworkReader(data, info.type.schema);
                    received.Deserialize(ref reader);
                    received.AssertReplicatedCorrectly(m_Test.lastEventSent, false);
                }
                ++m_Test.eventReceived;
            }

            public void OnMapUpdate(ref NetworkReader data)
            {
            }

            public void ProcessSnapshot(int serverTime)
            {
                throw new NotImplementedException();
            }
        }

        public MyEvent lastEventSent;
        public int eventSent;
        public int eventReceived;
        [Test]
        public void Events_ClientToServer_SendUnreliable()
        {
            TestTransport.Reset();

            var serverTransport = new TestTransport("127.0.0.1", 1);
            var clientTransport = new TestTransport("127.0.0.1", 2);
            var snapshotConsumer = new NullSnapshotConsumer();

            var serverCallbacks = new ServerCallbacks(this);
            var clientCallbacks = new ClientCallbacks(this);

            var server = new NetworkServer(serverTransport);
            var client = new NetworkClient(clientTransport);
            client.Connect("127.0.0.1:1");

            server.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "TestMap"); });

            server.Update(serverCallbacks);

            var RUNS = 1000;
            eventSent = 0;
            eventReceived = 0;
            lastEventSent = null;
            for (int i = 0; i < RUNS; ++i)
            {
                server.Update(serverCallbacks);

                server.SendData();

                client.Update(clientCallbacks, snapshotConsumer);

                if (eventSent == eventReceived && i < RUNS - 2)
                {
                    client.QueueEvent((ushort)EventType.MyEvent, false, (ref NetworkWriter writer) =>
                    {
                        lastEventSent = new MyEvent();
                        lastEventSent.Serialize(ref writer);
                    });
                    ++eventSent;
                }

                client.SendData();
            }
            Assert.AreEqual(eventReceived, eventSent);
        }

        [Test]
        public void Events_ServerToClient_SendUnreliable()
        {
            TestTransport.Reset();

            var serverTransport = new TestTransport("127.0.0.1", 1);
            var clientTransport = new TestTransport("127.0.0.1", 2);
            var snapshotConsumer = new NullSnapshotConsumer();

            var serverCallbacks = new ServerCallbacks(this);
            var clientCallbacks = new ClientCallbacks(this);

            var server = new NetworkServer(serverTransport);
            var client = new NetworkClient(clientTransport);
            client.Connect("127.0.0.1:1");

            server.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "TestMap"); });

            server.Update(serverCallbacks);

            var RUNS = 1000;
            eventSent = 0;
            eventReceived = 0;
            lastEventSent = null;
            for (int i = 0; i < RUNS; ++i)
            {
                server.Update(serverCallbacks);

                if (eventSent == eventReceived && i < RUNS - 2)
                {
                    server.QueueEvent(1, (ushort)EventType.MyEvent, false, (ref NetworkWriter writer) =>
                    {
                        lastEventSent = new MyEvent();
                        lastEventSent.Serialize(ref writer);
                    });
                    ++eventSent;
                }
                server.SendData();

                client.Update(clientCallbacks, snapshotConsumer);

                client.SendData();
            }
            Assert.AreEqual(eventReceived, eventSent);
        }

        [Test]
        public void Events_ServerToClient_BroadcastUnreliable()
        {
            TestTransport.Reset();

            var snapshotConsumer = new NullSnapshotConsumer();
            var serverTransport = new TestTransport("127.0.0.1", 1);
            var server = new NetworkServer(serverTransport);

            var serverCallbacks = new ServerCallbacks(this);
            var clientCallbacks = new ClientCallbacks(this);

            const int NUM_CLIENTS = 3;
            var clientTransports = new TestTransport[NUM_CLIENTS];
            var clients = new NetworkClient[NUM_CLIENTS];
            for (int i = 0; i < NUM_CLIENTS; ++i)
            {
                clientTransports[i] = new TestTransport("127.0.0.1", i + 2);
                clients[i] = new NetworkClient(clientTransports[i]);
                clients[i].Connect("127.0.0.1:1");
            }
            server.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "TestMap"); });

            server.Update(serverCallbacks);

            var RUNS = 1000;
            eventSent = 0;
            eventReceived = 0;
            lastEventSent = null;
            for (int i = 0; i < RUNS; ++i)
            {
                server.Update(serverCallbacks);
                if (eventSent == eventReceived * NUM_CLIENTS && i < RUNS - 2)
                {
                    server.QueueEventBroadcast((ushort)EventType.MyEvent, false, (ref NetworkWriter writer) =>
                    {
                        lastEventSent = new MyEvent();
                        lastEventSent.Serialize(ref writer);
                    });
                    ++eventSent;
                }
                server.SendData();

                foreach(var client in clients)
                {
                    client.Update(clientCallbacks, snapshotConsumer);
                    client.SendData();
                }
            }
            Assert.AreEqual(eventSent, eventReceived / NUM_CLIENTS);
        }

        [Test]
        public void Events_ServerToClient_SendReliable()
        {
            TestTransport.Reset();

            var serverTransport = new TestTransport("127.0.0.1", 1);
            var clientTransport = new TestTransport("127.0.0.1", 2);
            var snapshotConsumer = new NullSnapshotConsumer();

            var serverCallbacks = new ServerCallbacks(this);
            var clientCallbacks = new ClientCallbacks(this);

            var server = new NetworkServer(serverTransport);
            var client = new NetworkClient(clientTransport);
            server.Update(serverCallbacks);
            client.Connect("127.0.0.1:1");

            server.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "TestMap"); });

            server.Update(serverCallbacks);
            server.SendData();
            client.Update(clientCallbacks, snapshotConsumer);

            var RUNS = 1000;
            eventSent = 0;
            eventReceived = 0;
            lastEventSent = null;
            for (int i = 0; i < RUNS; ++i)
            {
                server.Update(serverCallbacks);
                if (eventSent == eventReceived && i < RUNS - 32)
                {
                    server.QueueEvent(1, (ushort)EventType.MyEvent, true, (ref NetworkWriter writer) =>
                    {
                        lastEventSent = new MyEvent();
                        lastEventSent.Serialize(ref writer);
                    });
                    ++eventSent;
                }
                server.SendData();

                if (i % 3 == 0)
                    clientTransport.DropPackages();

                client.Update(clientCallbacks, snapshotConsumer);

                Assert.IsTrue(eventReceived <= eventSent);

                client.SendData();
            }
            Assert.AreEqual(eventSent, eventReceived);
        }
    }
}
