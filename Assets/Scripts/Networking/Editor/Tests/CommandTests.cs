using System;
using System.Collections.Generic;
using NUnit.Framework;


namespace NetcodeTests 
{
    [TestFixture]
    public class CommandTests : NetTestBase, IClientCommandProcessor, INetworkClientCallbacks
    {
        class MyCommand : TestSerializable
        {
            public int intValue;
            public bool boolValue;
            public float floatValue;

            public override void AssertReplicatedCorrectly(TestSerializable clientEntity, bool isPredicting)
            {
                var c = clientEntity as MyCommand;
                Assert.IsTrue(c != null);
                Assert.IsTrue(c.intValue == intValue);
                Assert.IsTrue(c.boolValue == boolValue);
                Assert.IsTrue(c.floatValue == floatValue);
            }

            public override void Deserialize(ref NetworkReader reader)
            {
                intValue = reader.ReadInt32();
                boolValue = reader.ReadBoolean();
                floatValue = reader.ReadFloat();
            }

            public override void Serialize(ref NetworkWriter writer)
            {
                writer.WriteInt32("intValue", intValue);
                writer.WriteBoolean("boolValue", boolValue);
                writer.WriteFloat("floatValue", floatValue);
            }
        }

        [Test]
        public void Commands_TickTest()
        {
            TestTransport.Reset();

            var random = new Random(9904);

            var serverTransport = new TestTransport("127.0.0.1", 1);
            var clientTransport = new TestTransport("127.0.0.1", 2);
            var snapshotConsumer = new NullSnapshotConsumer();

            var server = new NetworkServer(serverTransport);
            var client = new NetworkClient(clientTransport);
            client.Connect("127.0.0.1:1");

            server.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "TestMap"); });

            server.Update(this);

            var sentCommands = new Dictionary<int, MyCommand>();
            m_ReceivedCommands.Clear();

            var RUNS = 1000;
            var serverTick = 0;
            var clientTick = 10;

            while(serverTick < RUNS)
            {
                server.Update(this);
                server.HandleClientCommands(serverTick, this);
                ++serverTick;
                server.SendData();

                client.Update(this, snapshotConsumer);

                if (clientTick < RUNS)
                {
                    client.QueueCommand(clientTick, (ref NetworkWriter writer) =>
                    {
                        var sent = new MyCommand();
                        sent.intValue = random.Next(-1000, 1000);
                        sent.boolValue = random.Next(0, 1) == 1;
                        sent.floatValue = (float)random.NextDouble();

                        sentCommands.Add(clientTick, sent);
                        sent.Serialize(ref writer);
                    });
                }
                ++clientTick;
                client.SendData();
            }

            foreach (var sent in sentCommands)
                sent.Value.AssertReplicatedCorrectly(m_ReceivedCommands[sent.Key], false);
        }

        [Test]
        public void Commands_TickJumpBack()
        {
            TestTransport.Reset();

            var random = new Random(9904);

            var serverTransport = new TestTransport("127.0.0.1", 1);
            var clientTransport = new TestTransport("127.0.0.1", 2);
            var snapshotConsumer = new NullSnapshotConsumer();

            var server = new NetworkServer(serverTransport);
            var client = new NetworkClient(clientTransport);
            client.Connect("127.0.0.1:1");

            server.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "TestMap"); });

            server.Update(this);

            var sentCommands = new Dictionary<int, MyCommand>();
            m_ReceivedCommands.Clear();

            var RUNS = 100;
            var serverTick = 0;
            var clientTick = 10;
            bool jumped = false;

            while (serverTick < RUNS)
            {
                server.Update(this);
                server.HandleClientCommands(serverTick, this);
                ++serverTick;
                server.SendData();

                client.Update(this, snapshotConsumer);

                if (clientTick < RUNS)
                {
                    if (!jumped && clientTick == 50)
                    {
                        jumped = true;
                        clientTick = 45;
                        for (int i = serverTick; i < 50; ++i)
                            sentCommands.Remove(i);
                    }

                    client.QueueCommand(clientTick, (ref NetworkWriter writer) =>
                    {
                        var sent = new MyCommand();
                        sent.intValue = random.Next(-1000, 1000);
                        sent.boolValue = random.Next(0, 1) == 1;
                        sent.floatValue = (float)random.NextDouble();

                        sentCommands.Add(clientTick, sent);
                        sent.Serialize(ref writer);
                    });
                }
                ++clientTick;
                client.SendData();
            }

            foreach (var sent in sentCommands)
                sent.Value.AssertReplicatedCorrectly(m_ReceivedCommands[sent.Key], false);
        }

        public void OnConnect(int clientId)
        {
        }

        public void OnDisconnect(int clientId)
        {
        }

        public void OnEvent(int clientId, NetworkEvent info)
        {
        }

        public void ProcessCommand(int connectionId, int tick, ref NetworkReader data)
        {
            var received = new MyCommand();
            received.Deserialize(ref data);
            m_ReceivedCommands.Add(tick, received);
        }

        public void OnMapUpdate(ref NetworkReader data)
        {
        }

        Dictionary<int, MyCommand> m_ReceivedCommands = new Dictionary<int, MyCommand>();
    }
}
