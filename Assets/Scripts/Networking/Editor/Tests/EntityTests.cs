using System;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace NetcodeTests
{

    [TestFixture]
    public class GameTests : NetTestBase
    {

        public class MyEntity : TestEntity
        {
            public Vector3 position = new Vector3();
            bool flag;
            public string message = "";
            public int health = 100;
            public float predictedData = 100.0f;
            public float nonpredictedData = 100.0f;
            public float justData = 100.0f;

            public override void UpdateServer(TestWorld world)
            {
                var t = world.tick * Mathf.PI / 100.0f;
                if((world.tick + this.id) % 100 < 50)
                {
                    position.x = Mathf.Cos(t);
                    position.y = Mathf.Sin(t);
                }

                justData = world.tick % 100 < 50 ? 0.0f : world.tick * 0.01f;

                flag = world.random.Next(0, 100) < 95;

                if((world.tick+this.id) % 200 == 0)
                    message = "abcdefghijklmn".Substring(world.random.Next(0, 2), world.random.Next(3, 5));

                var t2 = world.tick % 200;
                position.z = t2 < 100 ? t2 : 100 -t2;

                // delay changing value one frame so test verification can tell apart
                // if clients are getting updates or not
                if(spawnTick > world.tick)
                {
                    predictedData = Mathf.Sin(t * 1.0f);
                    nonpredictedData = Mathf.Sin(t * 1.1f);
                }

                if (health > 0)
                    --health;
            }

            public override void Serialize(ref NetworkWriter writer)
            {
                base.Serialize(ref writer);
                writer.WriteVector3Q("position", position, 3);
                writer.WriteInt32("health", health);
                writer.WriteBoolean("flag", flag);
                writer.WriteString("message", message);
                writer.WriteFloat("justData", justData);

                writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
                writer.WriteFloat("predictedData", predictedData);
                writer.ClearFieldSection();

                writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyNotPredicting);
                writer.WriteFloat("nonpredictedData", nonpredictedData);
                writer.ClearFieldSection();
            }

            public override void AssertReplicatedCorrectly(TestSerializable clientEntity, bool isPredicting)
            {
                base.AssertReplicatedCorrectly(clientEntity, isPredicting);
                var c = clientEntity as MyEntity;
                Assert.IsTrue(c != null);
                //Assert.IsTrue(position == c.position);
                    Assert.IsTrue(Math.Abs(position.x - c.position.x) < Math.Pow(10, -3));
                    Assert.IsTrue(Math.Abs(position.y - c.position.y) < Math.Pow(10, -3));
                    Assert.IsTrue(Math.Abs(position.z - c.position.z) < Math.Pow(10, -3));
                Assert.IsTrue(justData == c.justData);
                Assert.IsTrue(health == c.health);
                Assert.IsTrue(flag == c.flag);
                Assert.IsTrue(message.CompareTo(c.message) == 0);

                Assert.IsTrue(c.predictingClientId == -1); // Clients never know anything about if they predict or not
                if(isPredicting)
                {
                    Assert.IsTrue(predictingClientId != -1);
                    Assert.IsTrue(predictedData == c.predictedData);
                    Assert.IsTrue(100.0f == c.nonpredictedData);  // we should not be getting anything beyond the value at spawn
                }
                else
                {
                    Assert.IsTrue(100.0f == c.predictedData);  // we should not be getting anything beyond the value at spawn
                    Assert.IsTrue(nonpredictedData == c.nonpredictedData);
                }
            }

            public override void Deserialize(ref NetworkReader reader)
            {
                base.Deserialize(ref reader);
                position = reader.ReadVector3Q();
                health = reader.ReadInt32();
                flag = reader.ReadBoolean();
                message = reader.ReadString();
                justData = reader.ReadFloat();
                predictedData = reader.ReadFloat();
                nonpredictedData = reader.ReadFloat();
            }
        }

        [SetUp]
        public void TestSetup()
        {
            ConfigVar.ResetAllToDefault();
        }

        [Test]
        public void GameTests_SpawnAndUpdate100Entities()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer();
            TestGameClient client = new TestGameClient(2);

            for (int i = 0; i < 100; ++i)
                server.SpawnEntity<MyEntity>(-1);

            for(int i = 0; i < 1000; ++i)
            {
                server.Update();
                client.Update();

                if(i > 2)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }
            var c = server.networkServer.GetConnections()[server.clients[0]];
            GameDebug.Log("Sent bytes:  " + c.counters.bytesOut);
            GameDebug.Log("Sent packages: " + c.counters.packagesOut);
            GameDebug.Log("Generated snapshots: " + server.networkServer.statsGeneratedEntitySnapshots);
            GameDebug.Log("Sent snapshotdata: " + server.networkServer.statsSentOutgoing);
        }

        [Test]
        public void GameTests_RandomSpawnAndDespawn()
        {
            TestTransport.Reset();

            var random = new System.Random(129315);

            TestGameServer server = new TestGameServer();
            TestGameClient client = new TestGameClient(2);

            for (int i = 0; i < 1000; ++i)
            {
                // 20 % of spawning in frames 0-100, 200-300, etc. and 10% otherwise and wise
                // verse for despawning, so we can oscilating number oFf entities

                if (random.Next(0, i % 200 < 100 ? 5 : 10) == 0)
                {
                    server.SpawnEntity<MyEntity>(-1);
                }

                if (random.Next(0, i % 200 > 100 ? 5 : 10) == 0 && server.world.entities.Count > 0)
                {
                    var index = random.Next(0, server.world.entities.Count);

                    var enumerator = server.world.entities.GetEnumerator();
                    while (index >= 0)
                    {
                        enumerator.MoveNext();
                        --index;
                    }

                    server.DespawnEntity(enumerator.Current.Value);
                }

                server.Update();
                client.Update();

                if (i > 2)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }
        }

        [Test]
        public void GameTests_SpawnAndDespawnDuringConnect()
        {
            TestTransport.Reset();

            // Once upon a time there was a server...
            TestGameServer server = new TestGameServer(); server.Update(); // One tick to get away from 0

            // And it had a nice, old entity. Born all the way back in tick 1
            var oldOne = server.SpawnEntity<MyEntity>(-1);

            // Then a client joined the server
            TestGameClient client = new TestGameClient(2);

            // They did the handshake / map / client ready
            server.Update();
            client.Update();

            // And exchanged / client config/info
            server.Update();
            client.Update();

            // Meanwhile in another part of the net, a router decides to start dropping
            // packages like santa himself. Horrors! Our client is now unable to speak
            // a word. In particualar unable to say ACK
            NetworkClient.clientBlockOut.Value = "1";

            // The server suspects nothing and passes on a snapshot. Of course no
            // ack will come back from the poor client
            server.Update();
            client.Update();

            // And then, in a freak accident, just as we have sent the first snapshot
            // to the client, the old entity decide to die...
            server.DespawnEntity(oldOne);

            // Since we have not heard acks from the client, the server dutifully 
            // keeps sending snapshots with no baseline. It is very tempting
            // for the server to not send out anything about the oldOne now.
            // After all it was born eons ago AND now it is gone.
            // How could an old, dead entity be of interest to a young client? 
            // But what has been spawned must be despawned!
            // And we already told the client about the oldOne in our first snapshot.
            server.Update();
            client.Update();

            // Now the router comes back to life. Server begins to get acks
            // so snapshots can now be delta compressed. Skies seem to be clearing up.
            NetworkClient.clientBlockOut.Value = "0";

            // Look at them sync again. Perhaps this is the beginning of a beautiful friendship
            for(int i = 0; i < 10; i++)
            {
                server.Update();
                client.Update();

                server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }

            // THE END
        }

        [Test]
        public void GameTests_AfterNonBaselineNewEntityTypeInSameSlot()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer(); server.Update();
            TestGameClient client = new TestGameClient(2);

            NetworkServer.serverDebug.Value = "2";
            NetworkClient.clientDebug.Value = "2";

            // Handshake
            server.Update();
            client.Update();

            // Map
            server.Update();
            client.Update();

            // Spawn entity of one type
            TestEntity entity = server.SpawnEntity<MyEntity>(-1);

            server.Update();
            client.Update();

            server.world.AssertReplicatedToClient(client.world, server.clients[0]);

            // Despawn and respawn with new type
            server.DespawnEntity(entity);

            NetworkClient.clientBlockIn.Value = "-1";
            NetworkConfig.netChokeSendInterval.Value = "0";

            // Run enough updates so that server consider id for despawned entity reusable
            for(int i = 0; i < NetworkConfig.snapshotDeltaCacheSize; i++)
            {
                server.Update();
                client.Update();
            }

            // Spawn new entity. Different type but will have same id
            entity = server.SpawnEntity<TestEntity>(-1);

            NetworkClient.clientBlockIn.Value = "0";
            server.Update();
            client.Update();
            server.Update();
            client.Update();

            server.world.AssertReplicatedToClient(client.world, server.clients[0]);
        }

        [Test]
        public void GameTests_AfterNonBaselineStaleEntitiesRemoved()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer(); server.Update();
            TestGameClient client = new TestGameClient(2);

            NetworkServer.serverDebug.Value = "2";
            NetworkClient.clientDebug.Value = "2";

            // Handshake
            server.Update();
            client.Update();

            // Map
            server.Update();
            client.Update();

            // Spawn entity of one type
            TestEntity entity = server.SpawnEntity<MyEntity>(-1);

            server.Update();
            client.Update();

            server.world.AssertReplicatedToClient(client.world, server.clients[0]);

            NetworkClient.clientBlockIn.Value = "-1";
            NetworkConfig.netChokeSendInterval.Value = "0";

            // Run enough updates so that server consider id for despawned entity reusable
            for(int i = 0; i < NetworkConfig.snapshotDeltaCacheSize; i++)
            {
                server.Update();
                client.Update();
            }

            // Despawn. Since next update going out is without baseline, no explicit despawn will
            // be sent. We rely on client to prune this entity as a stale entity.
            server.DespawnEntity(entity);

            NetworkClient.clientBlockIn.Value = "0";
            server.Update();
            client.Update();
            server.Update();
            client.Update();

            server.world.AssertReplicatedToClient(client.world, server.clients[0]);
        }

        [Test]
        public void GameTests_StaleBaselineTest()
        {
            NetworkConfig.netChokeSendInterval.Value = "0";
            TestTransport.Reset();

            TestGameServer server = new TestGameServer();
            server.Update(); // Server tick away from 0

            TestGameClient client = new TestGameClient(2);

            // Handshakes
            server.Update();
            client.Update();
            server.Update();
            client.Update();

            var e = server.SpawnEntity<MyEntity>(-1);
            server.DespawnEntity(e);

            // Server send first snapshot. No BL. Contains SPAWN
            server.Update();
            server.Update();

            client.Update();

            for(var i = 0; i < 200; i++)
            {
                server.Update();
            }

            client.m_Transport.DropPackages();

            e = server.SpawnEntity<MyEntity>(-1);

            server.Update();
            client.Update();

            server.Update();
            client.Update();

            server.Update();
            client.Update();

            NetworkConfig.netChokeSendInterval.Value = "0.3";
        }

        [Test]
        public void GameTests_PredictingClientData()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer();
            TestGameClient client1 = new TestGameClient(2);
            TestGameClient client2 = new TestGameClient(3);


            // Allow server to get connections
            server.Update();
            server.Update();

            // NOTE: this relies on incoming clients getting assigned id's 1, 2, 3 ..
            Assert.AreEqual(2, server.clients.Count);
            Assert.AreEqual(1, server.clients[0]);
            Assert.AreEqual(2, server.clients[1]);

            for (int i = 0; i < 100; ++i)
            {
                // Spawn entities that are non predicted as well as predicted by either client
                var idx = i % (server.clients.Count + 1);
                var simulatingClient = idx < server.clients.Count ? server.clients[idx] : -1;
                server.SpawnEntity<MyEntity>(simulatingClient);

                server.Update();
                client1.Update();
                client2.Update();

                if (i > 2)
                {
                    server.world.AssertReplicatedToClient(client1.world, server.clients[0]);
                    server.world.AssertReplicatedToClient(client2.world, server.clients[1]);
                }
            }
        }

        [Test]
        public void GameTests_FullSpawnAndDespawn()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer();
            TestGameClient client = new TestGameClient(2);

            for (int i = 0; i < 100; ++i)
            {
                server.SpawnEntity<MyEntity>(-1);

                server.Update();
                client.Update();

                if (i > 2)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }

            for (int i = 0; i < 100; ++i)
            {
                Assert.AreNotEqual(server.world.entities.Count, 0);

                var enumerator = server.world.entities.GetEnumerator();
                enumerator.MoveNext();
                server.DespawnEntity(enumerator.Current.Value);

                server.Update();
                client.Update();

                if (i > 2)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }

            // Test spawn and despawn in same frame
            for (int i = 0; i < 4; ++i)
            {
                var entity = server.SpawnEntity<MyEntity>(-1);
                server.DespawnEntity(entity);

                server.Update();
                client.Update();

                if (i > 2)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }

            // Run server for enough ticks so that it can assume all despawned entities are too old to be of interest
            // to any client.
            for (int i = 0; i < NetworkConfig.snapshotDeltaCacheSize + 1; i++)
            {
                server.Update();
                client.Update();
            }

            // Then server should have deleted them all from its internal list
            Assert.AreEqual(0, server.networkServer.NumEntities);
        }


        [Test]
        public void GameTests_MapReset()
        {
            TestTransport.Reset();

            var random = new System.Random(129315);

            TestGameServer server = new TestGameServer();
            TestGameClient client = new TestGameClient(2);

            for(int mapIndex = 0; mapIndex < 10; ++mapIndex)
            {
                server.SetMap("Map " + mapIndex);
                for (int j = 0; j < 20; j++)
                    server.SpawnEntity<MyEntity>(-1);

                for(int j = 0; j < 20; ++j)
                {
                    server.Update();
                    client.Update();

                    if (j > 2)
                        server.world.AssertReplicatedToClient(client.world, server.clients[0]);
                }
            }

            for (int i = 0; i < 20; ++i)
            {
                server.Update();
                client.Update();
            }

            server.SetMap("Haha");

            for (int i = 0; i < 20; ++i)
            {
                server.Update();
                client.Update();
            }

            server.SetMap("Huhu");

            for (int i = 0; i < 1000; ++i)
            {
                // 20 % of spawning in frames 0-100, 200-300, etc. and 10% otherwise and wise
                // verse for despawning, so we can oscilating number oFf entities

                if (random.Next(0, i % 200 < 10 ? 5 : 10) == 0)
                {
                    server.SpawnEntity<MyEntity>(-1);
                }

                if (random.Next(0, i % 200 > 100 ? 5 : 10) == 0 && server.world.entities.Count > 0)
                {
                    var index = random.Next(0, server.world.entities.Count);

                    var enumerator = server.world.entities.GetEnumerator();
                    while (index >= 0)
                    {
                        enumerator.MoveNext();
                        --index;
                    }

                    server.DespawnEntity(enumerator.Current.Value);
                }

                server.Update();
                client.Update();

                if (i > 5)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }
        }

    }
}
