using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NetcodeTests
{
    public class TestEntity : TestSerializable
    {
        public int id { get; set; }
        public ushort typeId { get; set; }
        public int despawnTick { get; set; }
        public int spawnTick { get; set; }

        public int predictingClientId = -1;

        // Assumes 'this' is a server entity and asserts that it has been replicated correctly to clientEntity
        public override void AssertReplicatedCorrectly(TestSerializable clientEntity, bool isPredicting)
        {
            var c = clientEntity as TestEntity;
            Assert.IsTrue(c != null);
            Assert.IsTrue(c.id == id);
            Assert.IsTrue(c.despawnTick == despawnTick);
            Assert.IsTrue(c.spawnTick == spawnTick);
        }

        public override void Deserialize(ref NetworkReader reader)
        {
            id = reader.ReadInt32();
            typeId = reader.ReadUInt16();
            despawnTick = reader.ReadInt32();
            spawnTick = reader.ReadInt32();
        }

        public override void Serialize(ref NetworkWriter writer)
        {
            writer.WriteInt32("id", id);
            writer.WriteUInt16("typeId", typeId);
            writer.WriteInt32("despawnTick", despawnTick);
            writer.WriteInt32("spawnTick", spawnTick);
        }

        public virtual void UpdateServer(TestWorld world)
        {
        }
    }

    public class TestWorld : ISnapshotGenerator, ISnapshotConsumer
    {
        public int tick;
        public Dictionary<int, TestEntity> entities = new Dictionary<int, TestEntity>();

        public System.Random random = new System.Random(1234);

        public TestWorld()
        {
            RegisterEntityType(typeof(TestEntity));
            RegisterEntityType(typeof(GameTests.MyEntity));
        }

        public void UpdateServer()
        {
            ++tick;
            foreach (var entity in entities)
                entity.Value.UpdateServer(this);
        }

        public void UpdateClient()
        {
            ++tick;
        }

        List<int> dyingEntities = new List<int>();
        public void PurgeDespawnedEntitites()
        {
            dyingEntities.Clear();

            foreach (var entity in entities)
                if (entity.Value.despawnTick > 0)
                    dyingEntities.Add(entity.Key);

            foreach(var i in dyingEntities)
                entities.Remove(i);
        }

        public void AssertReplicatedToClient(TestWorld clientWorld, int serversideClientId)
        {
            //Assert.AreEqual(other.entities.Count, entities.Count);
            foreach (var pair in entities)
            {
                Debug.Assert(pair.Key >= 0);

                if (pair.Value.despawnTick > 0)
                    continue;

                TestEntity clientEntity;
                if (!clientWorld.entities.TryGetValue(pair.Key, out clientEntity))
                    Assert.Fail("Entity " + pair.Key + " isn't replicated to client world");

                pair.Value.AssertReplicatedCorrectly(clientEntity, pair.Value.predictingClientId == serversideClientId);
            }
        }

        public void ProcessEntityUpdate(int serverTime, int id, ref NetworkReader reader)
        {
            entities[id].Deserialize(ref reader);
        }

        // Clientside spawning incoming entities
        public void ProcessEntitySpawn(int serverTime, int entityId, ushort typeId)
        {
            SpawnInternal(entityId, typeId, -1);
        }

        // Serverside creating new entities in world
        public T SpawnEntity<T>(NetworkServer networkServer, int predictingClientId) where T : TestEntity, new()
        {
            var typeId = s_EntityTypeToId[typeof(T)];
            var id = networkServer.RegisterEntity(-1, typeId, predictingClientId);
            return (T)SpawnInternal(id, typeId, predictingClientId);
        }

        TestEntity SpawnInternal(int entityId, ushort typeId, int predictingClientId)
        {
            var type = s_IdToEntityType[typeId];
            if(entities.ContainsKey(entityId))
                Debug.Log("Trying to spawn entity with id that is in use");
            Debug.Assert(!entities.ContainsKey(entityId), "Trying to spawn entity with id that is in use");

            TestEntity entity = (TestEntity)Activator.CreateInstance(type);
            entity.id = entityId;
            entity.spawnTick = tick;
            entity.typeId = s_EntityTypeToId[type];
            entity.predictingClientId = predictingClientId;
            entities.Add(entityId, entity);
            return entity;
        }

        public void DespawnEntity(TestEntity entity)
        {
            Debug.Assert(entity.despawnTick == 0 || entity.spawnTick == entity.despawnTick);
            Debug.Assert(entities.ContainsKey(entity.id));
            entity.despawnTick = tick;
        }

        public static void RegisterEntityType(Type type)
        {
            if (s_EntityTypeToId.ContainsKey(type))
                return;
            ++s_TypeId;
            s_IdToEntityType.Add(s_TypeId, type);
            s_EntityTypeToId.Add(type, s_TypeId);
            return;
        }

        public void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer)
        {
            entities[entityId].Serialize(ref writer);
        }

        public string GenerateEntityName(int entityId)
        {
            return "";
        }

        public void ProcessEntityDespawns(int serverTime, List<int> despawns)
        {
            foreach(int id in despawns)
            {
                DespawnEntity(entities[id]);
            }
            PurgeDespawnedEntitites();
        }

        static ushort s_TypeId;
        static Dictionary<ushort, Type> s_IdToEntityType = new Dictionary<ushort, Type>();
        static Dictionary<Type, ushort> s_EntityTypeToId = new Dictionary<Type, ushort>();

        public int WorldTick
        {
            get
            {
                return tick;
            }
        }
    }

    public class TestGameServer : INetworkCallbacks
    {
        public TestWorld world;
        public NetworkServer networkServer { get { return m_NetworkServer; } }
        public List<int> clients;

        public TestGameServer()
        {
            world = new TestWorld();
            m_Transport = new TestTransport("127.0.0.1", 1);
            clients = new List<int>();

            m_NetworkServer = new NetworkServer(m_Transport);
            m_NetworkServer.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", "Default"); });
        }

        public void SetMap(string name)
        {
            m_NetworkServer.InitializeMap((ref NetworkWriter data) => { data.WriteString("name", name); });
            world = new TestWorld();

            // TODO (petera) fix this (see also comments below)
            foreach(var c in m_NetworkServer.GetConnections())
            {
                m_NetworkServer.MapReady(c.Value.connectionId);
            }
        }

        public void OnConnect(int clientId)
        {
            clients.Add(clientId);
            // TODO (petera) we should test for a proper flow where clients are not immediately ready to take snapshots.
            // Today the game uses game specific events (PlayerReady) to indicate this. Should it be part of network
            // Layer? Perhaps having a few reserved events on top of which user events sits
            m_NetworkServer.MapReady(clientId);
        }

        public void OnDisconnect(int clientId)
        {
            clients.Remove(clientId);
        }

        public void OnEvent(int clientId, NetworkEvent info)
        {
        }

        public void Update()
        {
            m_NetworkServer.Update(this);

            world.UpdateServer();

            m_NetworkServer.GenerateSnapshot(world, 0);

            world.PurgeDespawnedEntitites();

            m_NetworkServer.SendData();
        }

        public T SpawnEntity<T>(int predictingClientId) where T : TestEntity, new()
        {
            return world.SpawnEntity<T>(m_NetworkServer, predictingClientId);
        }

        public void DespawnEntity(TestEntity entity)
        {
            m_NetworkServer.UnregisterEntity(entity.id);
            world.DespawnEntity(entity);
        }

        public void OnMapUpdate(ref NetworkReader data)
        {
            throw new NotImplementedException();
        }

        TestTransport m_Transport;
        NetworkServer m_NetworkServer;
    }

    public class TestGameClient : INetworkClientCallbacks
    {
        public TestWorld world;

        public TestGameClient(int port)
        {
            m_Transport = new TestTransport("127.0.0.1", port);
            m_NetworkClient = new NetworkClient(m_Transport);
            m_NetworkClient.Connect("127.0.0.1:1");
        }

        public void Update()
        {
            m_NetworkClient.Update(this, world);

            if (world != null)
                world.UpdateClient();

            m_NetworkClient.SendData();
        }

        public void OnConnect(int clientId) { }
        public void OnDisconnect(int clientId) { }
        public void OnEvent(int clientId, NetworkEvent info) { }

        public void OnMapUpdate(ref NetworkReader data)
        {
            var levelName = data.ReadString();
            GameDebug.Log("TestGameClient getting map: " + levelName);
            world = new TestWorld();
        }

        public TestTransport m_Transport;
        NetworkClient m_NetworkClient;
    }

}
