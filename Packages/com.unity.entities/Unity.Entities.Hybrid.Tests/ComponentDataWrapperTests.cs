using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Tests
{
    public class ComponentDataWrapperTests
    {
        interface IIntegerContainer
        {
            int Integer { get; set; }
        }

        struct MockData : IComponentData
        {
            public int Value;
        }

        class MockWrapper : ComponentDataWrapper<MockData>, IIntegerContainer
        {
            public int Integer { get => Value.Value; set => Value = new MockData { Value = value }; }
        }

        struct MockSharedData : ISharedComponentData
        {
            public int Value;
        }

        class MockSharedWrapper : SharedComponentDataWrapper<MockSharedData>, IIntegerContainer
        {
            public int Integer { get => Value.Value; set => Value = new MockSharedData { Value = value }; }
        }

        GameObject m_GameObject;

        [TearDown]
        public void TearDown()
        {
            if (m_GameObject != null)
                GameObject.DestroyImmediate(m_GameObject);
        }

        [TestCase(typeof(MockWrapper))]
        [TestCase(typeof(MockSharedWrapper))]
        public void ComponentDataWrapper_SetValue_SynchronizesWithEntityManager(Type wrapperType)
        {
            m_GameObject = new GameObject(TestContext.CurrentContext.Test.Name, wrapperType);
            var wrapper = m_GameObject.GetComponent<ComponentDataWrapperBase>();
            // manually call OnEnable so that wrapper's component will exist in entity manager
            // represents loading GameObjectEntity when wrappers already exist (e.g., loading a scene, domain reload)
            var gameObjectEntity = wrapper.GetComponent<GameObjectEntity>();
            gameObjectEntity.OnEnable();
            Assert.That(
                wrapper.CanSynchronizeWithEntityManager(out var entityManager, out var entity), Is.True,
                "EntityManager is not in correct state in arrangement for synchronization to occur"
            );
            var integerWrapper = wrapper as IIntegerContainer;
            Assert.That(
                integerWrapper.Integer, Is.EqualTo(0),
                $"{wrapper.GetComponentType()} did not initialize with default value in arrangement"
            );

            integerWrapper.Integer = 1;
            Assert.That(integerWrapper.Integer, Is.EqualTo(1), $"Setting value on {wrapperType} failed");

            var so = new SerializedObject(wrapper);
            Assert.That(integerWrapper.Integer, Is.EqualTo(1), $"Value was reset after deserializing {wrapperType}");
        }
    }
}