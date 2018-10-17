using NUnit.Framework;

namespace Unity.Entities.Tests
{
    public class WorldTests
    {
        World m_PreviousWorld;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.Active;
        }

        [TearDown]
        public virtual void TearDown()
        {
            World.Active = m_PreviousWorld;
        }

        
        [Test]
        public void ActiveWorldResets()
        {
            int count = World.AllWorlds.Count;
            var worldA = new World("WorldA");
            var worldB = new World("WorldB");

            World.Active = worldB; 
            
            Assert.AreEqual(worldB, World.Active);
            Assert.AreEqual(count + 2, World.AllWorlds.Count);
            Assert.AreEqual(worldA, World.AllWorlds[World.AllWorlds.Count-2]);
            Assert.AreEqual(worldB, World.AllWorlds[World.AllWorlds.Count-1]);
            
            worldB.Dispose();
            
            Assert.IsFalse(worldB.IsCreated);
            Assert.IsTrue(worldA.IsCreated);
            Assert.AreEqual(null, World.Active);
            
            worldA.Dispose();
            
            Assert.AreEqual(count, World.AllWorlds.Count);
        }

        [DisableAutoCreation]
        private class TestManager : ComponentSystem
        {
            protected override void OnUpdate() {}
        }

        [Test]
        public void WorldVersionIsConsistent()
        {
            var world = new World("WorldX");

            Assert.AreEqual(0, world.Version);

            var version = world.Version;
            world.GetOrCreateManager<TestManager>();
            Assert.AreNotEqual(version, world.Version);

            version = world.Version;
            var manager = world.GetOrCreateManager<TestManager>();
            Assert.AreEqual(version, world.Version);

            version = world.Version;
            world.DestroyManager(manager);
            Assert.AreNotEqual(version, world.Version);
            
            world.Dispose();
        }
    }
}
