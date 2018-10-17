using NUnit.Framework;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    [DisableAutoCreation]
    public class EmptySystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle dep) { return dep; }


        new public ComponentGroup GetComponentGroup(params ComponentType[] componentTypes)
        {
            return base.GetComponentGroup(componentTypes);
        }

        new public ComponentGroupArray<T> GetEntities<T>() where T : struct
        {
            return base.GetEntities<T>();
        }
    }

    public class ECSTestsFixture
    {
        protected World m_PreviousWorld;
        protected World World;
        protected EntityManager m_Manager;
        protected EntityManager.EntityManagerDebug m_ManagerDebug;

        protected int StressTestEntityCount = 1000;
        
        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.Active;
            World = World.Active = new World("Test World");

            m_Manager = World.GetOrCreateManager<EntityManager>();
            m_ManagerDebug = new EntityManager.EntityManagerDebug(m_Manager);
        }

        [TearDown]
        public virtual void TearDown()
        {
            if (m_Manager != null)
            {
                World.Dispose();
                World = null;

                World.Active = m_PreviousWorld;
                m_PreviousWorld = null;
                m_Manager = null;
            }
        }

        public void AssertDoesNotExist(Entity entity)
        {
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData2>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));
            Assert.IsFalse(m_Manager.Exists(entity));
        }

        public void AssertComponentData(Entity entity, int index)
        {
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));
            Assert.IsTrue(m_Manager.Exists(entity));

            Assert.AreEqual(-index, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
            Assert.AreEqual(-index, m_Manager.GetComponentData<EcsTestData2>(entity).value1);
            Assert.AreEqual(index, m_Manager.GetComponentData<EcsTestData>(entity).value);
        }

        public Entity CreateEntityWithDefaultData(int index)
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            // HasComponent & Exists setup correctly
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));
            Assert.IsTrue(m_Manager.Exists(entity));

            // Create must initialize values to zero
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity).value1);
            Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);

            // Setup some non zero default values
            m_Manager.SetComponentData(entity, new EcsTestData2(-index));
            m_Manager.SetComponentData(entity, new EcsTestData(index));

            AssertComponentData(entity, index);

            return entity;
        }

        public EmptySystem EmptySystem
        {
            get
            {
                return World.Active.GetOrCreateManager<EmptySystem>();
            }
        }
    }
}
