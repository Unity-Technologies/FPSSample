//#define WRITE_TO_DISK
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Unity.Entities.Serialization;

namespace Unity.Entities.Tests
{
    public class SharedComponentSerializeTests : ECSTestsFixture
    {
        [Test]
        public void SharedComponentSerialize()
        {
            for (int i = 0; i != 20; i++)
            {
                var entity = m_Manager.CreateEntity();
                m_Manager.AddSharedComponentData(entity, new TestShared(i));
                m_Manager.AddComponentData(entity, new EcsTestData(i));
            }

            var writer = new TestBinaryWriter();

            GameObject sharedComponents;
            SerializeUtilityHybrid.Serialize(m_Manager, writer, out sharedComponents);

            var reader = new TestBinaryReader(writer);

            var world = new World("temp");
            SerializeUtilityHybrid.Deserialize (world.GetOrCreateManager<EntityManager>(), reader, sharedComponents);

            var newWorldEntities = world.GetOrCreateManager<EntityManager>();

            {
                var entities = newWorldEntities.GetAllEntities();

                Assert.AreEqual(20, entities.Length);

                for (int i = 0; i != 20; i++)
                {
                    Assert.AreEqual(i, newWorldEntities.GetComponentData<EcsTestData>(entities[i]).value);
                    Assert.AreEqual(i, newWorldEntities.GetSharedComponentData<TestShared>(entities[i]).Value);
                }
                for (int i = 0; i != 20; i++)
                    newWorldEntities.DestroyEntity(entities[i]);

                entities.Dispose();
            }

            Assert.IsTrue(newWorldEntities.Debug.IsSharedComponentManagerEmpty());

            world.Dispose();
            reader.Dispose();

            Object.DestroyImmediate(sharedComponents);
        }
    }
}
