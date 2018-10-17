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

            public override void UpdateServer(TestWorld world)
            {
                var t = world.tick * Mathf.PI / 100.0f;
                position.x = Mathf.Cos(t);
                position.y = Mathf.Sin(t);

                flag = UnityEngine.Random.Range(0.0f, 1.0f) < 0.5f;
                message = "abcdefghijklmn".Substring(UnityEngine.Random.Range(0, 2), UnityEngine.Random.Range(3, 5));

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
                writer.WriteVector3("position", position);
                writer.WriteInt32("health", health);
                writer.WriteBoolean("flag", flag);
                writer.WriteString("message", message);

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
                Assert.IsTrue(position == c.position);
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
                position = reader.ReadVector3();
                health = reader.ReadInt32();
                flag = reader.ReadBoolean();
                message = reader.ReadString();
                predictedData = reader.ReadFloat();
                nonpredictedData = reader.ReadFloat();
            }
        }

        [Test]
        public void GameTests_SpawnAndUpdate100Entities()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer();
            TestGameClient client = new TestGameClient(1);

            for (int i = 0; i < 100; ++i)
                server.SpawnEntity<MyEntity>(-1);

            for(int i = 0; i < 1000; ++i)
            {
                server.Update();
                client.Update();

                if(i > 2)
                    server.world.AssertReplicatedToClient(client.world, server.clients[0]);
            }
        }

        [Test]
        public void GameTests_RandomSpawnAndDespawn()
        {
            TestTransport.Reset();

            var random = new System.Random(129315);

            TestGameServer server = new TestGameServer();
            TestGameClient client = new TestGameClient(1);

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
        public void GameTests_PredictingClientData()
        {
            TestTransport.Reset();

            TestGameServer server = new TestGameServer();
            TestGameClient client1 = new TestGameClient(1);
            TestGameClient client2 = new TestGameClient(2);


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
            TestGameClient client = new TestGameClient(1);

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
            TestGameClient client = new TestGameClient(1);

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
