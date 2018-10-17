using NUnit.Framework;
using UnityEngine;
using Unity.Entities.Tests;

namespace Unity.Entities.Tests
{
    public class InjectComponentGroupTestsHybrid : ECSTestsFixture
    {
        [DisableAutoCreation]
        [AlwaysUpdateSystem]
        public class SubtractiveSystem : ComponentSystem
        {
            public struct Datas
            {
                public ComponentDataArray<EcsTestData> Data;
                public SubtractiveComponent<EcsTestData2> Data2;
                public SubtractiveComponent<Rigidbody> Rigidbody;
            }

            [Inject]
            public Datas Group;

            protected override void OnUpdate()
            {
            }
        }

        [Test]
        public void SubtractiveComponent()
        {
            var subtractiveSystem = World.GetOrCreateManager<SubtractiveSystem> ();

            var entity = m_Manager.CreateEntity (typeof(EcsTestData));

            var go = new GameObject("Test", typeof(EcsTestComponent));
            go.GetComponent<GameObjectEntity>().OnEnable();

            // Ensure entities without the subtractive components are present
            subtractiveSystem.Update ();
            Assert.AreEqual (2, subtractiveSystem.Group.Data.Length);
            Assert.AreEqual (0, subtractiveSystem.Group.Data[0].value);
            Assert.AreEqual (0, subtractiveSystem.Group.Data[1].value);

            // Ensure adding the subtractive components, removes them from the injection
            m_Manager.AddComponentData (entity, new EcsTestData2());

            // TODO: This should be automatic...
            go.AddComponent<Rigidbody>();
            go.GetComponent<GameObjectEntity>().OnDisable(); go.GetComponent<GameObjectEntity>().OnEnable();

            subtractiveSystem.Update ();
            Assert.AreEqual (0, subtractiveSystem.Group.Data.Length);

            Object.DestroyImmediate(go);
        }
    }
}
