using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.Tests;

namespace UnityEngine.Entities.Tests
{
    public class GameObjectEntityTests : ECSTestsFixture
    {
        ComponentArrayInjectionHook m_ComponentArrayInjectionHook = new ComponentArrayInjectionHook();
        GameObjectArrayInjectionHook m_GameObjectArrayInjectionHook = new GameObjectArrayInjectionHook();

        [OneTimeSetUp]
        public void Init()
        {
            InjectionHookSupport.RegisterHook(m_ComponentArrayInjectionHook);
            InjectionHookSupport.RegisterHook(m_GameObjectArrayInjectionHook);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            InjectionHookSupport.UnregisterHook(m_GameObjectArrayInjectionHook);
            InjectionHookSupport.RegisterHook(m_ComponentArrayInjectionHook);
        }

        [DisableAutoCreation]
        public class GameObjectArraySystem : ComponentSystem
        {
            public struct Group
            {
                public readonly int Length;
                public GameObjectArray gameObjects;

                public ComponentArray<BoxCollider> colliders;
            }

            [Inject]
            public Group group;

            protected override void OnUpdate()
            {
            }

            public new void UpdateInjectedComponentGroups()
            {
                base.UpdateInjectedComponentGroups();
            }
        }

        [Test]
        public void GameObjectArrayIsPopulated()
        {
            var go = new GameObject("test", typeof(BoxCollider));
            GameObjectEntity.AddToEntityManager(m_Manager, go);

            var manager = World.GetOrCreateManager<GameObjectArraySystem>();

            manager.UpdateInjectedComponentGroups();

            Assert.AreEqual(1, manager.group.Length);
            Assert.AreEqual(go, manager.group.gameObjects[0]);
            Assert.AreEqual(go, manager.group.colliders[0].gameObject);

            Object.DestroyImmediate (go);
            TearDown();
        }

        [Test]
        public void ComponentDataAndTransformArray()
        {
            var go = new GameObject("test", typeof(EcsTestComponent));
            var entity = GameObjectEntity.AddToEntityManager(m_Manager, go);

            m_Manager.SetComponentData(entity, new EcsTestData(5));

            var grp = EmptySystem.GetComponentGroup(typeof(Transform), typeof(EcsTestData));
            var arr = grp.GetComponentArray<Transform>();

            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(go.transform, arr[0]);
            Assert.AreEqual(5, grp.GetComponentDataArray<EcsTestData>()[0].value);

            Object.DestroyImmediate (go);
        }

        [Test]
        public void RigidbodyComponentArray()
        {
            var go = new GameObject("test", typeof(Rigidbody));
            /*var entity =*/ GameObjectEntity.AddToEntityManager(m_Manager, go);

            var grp = EmptySystem.GetComponentGroup(typeof(Rigidbody));

            var arr = grp.GetComponentArray<Rigidbody>();
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(go.GetComponent<Rigidbody>(), arr[0]);

            Object.DestroyImmediate(go);
        }
    }
}
