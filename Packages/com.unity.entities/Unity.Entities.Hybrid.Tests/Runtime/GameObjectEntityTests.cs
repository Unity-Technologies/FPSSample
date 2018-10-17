using System;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Entities.Tests
{
    //@TODO: Test for prevent adding Wrapper component to type system...

	public class GameObjectEntityTests : ECSTestsFixture
    {
        [Test]
        [Ignore("not implemented")]
        public void ComponentArrayWithParentClass() { }


        [Test]
        public void TransformAccessArrayTests()
        {

        }

        [Test]
        public void GameObjectEntityNotAdded()
        {
            var go = new GameObject("test", typeof(GameObjectEntity));
            var entity = GameObjectEntity.AddToEntityManager(m_Manager, go);
            Assert.Throws<ArgumentException>(() => { m_Manager.HasComponent<GameObjectEntity>(entity); });
        }

        unsafe struct MyEntity
        {
            public Light              light;
            public Rigidbody          rigidbody;

            public EcsTestData*       testData;
            public EcsTestData2*      testData2;
        }

        [Test]
        [Ignore("TODO")]
        public void ComponentEnumeratorInvalidChecks()
        {
            //* Check for string in MyEntity and other illegal constructs...
        }

        [Test]
        [Ignore("TODO")]
        public void AddComponentDuringForeachProtection()
        {
            //* Check for string in MyEntity and other illegal constructs...
        }
        [Test]
        unsafe public void ComponentEnumerator()
        {
            var go = new GameObject("test", typeof(Rigidbody), typeof(Light));
            var entity = GameObjectEntity.AddToEntityManager(m_Manager, go);

            m_Manager.AddComponentData(entity, new EcsTestData(5));
            m_Manager.AddComponentData(entity, new EcsTestData2(6));

            int iterations = 0;
            foreach (var e in EmptySystem.GetEntities<MyEntity>() )
            {
                Assert.AreEqual(5, e.testData->value);
                Assert.AreEqual(6, e.testData2->value0);
                Assert.AreEqual(go.GetComponent<Light>(), e.light);
                Assert.AreEqual(go.GetComponent<Rigidbody>(), e.rigidbody);
                iterations++;
            }
            Assert.AreEqual(1, iterations);

            Object.DestroyImmediate(go);
        }
    }
}
