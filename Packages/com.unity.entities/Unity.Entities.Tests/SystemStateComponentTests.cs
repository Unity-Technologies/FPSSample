using NUnit.Framework;
using Unity.Collections;
using System;

namespace Unity.Entities.Tests
{
    [TestFixture]
    public class SystemStateComponentTests : ECSTestsFixture
    {
        void VerifyComponentCount<T>(int expectedCount)
            where T : IComponentData
        {
            var query = new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = new ComponentType[] {typeof(T)}
            };
            var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);
            Assert.AreEqual(expectedCount, ArchetypeChunkArray.CalculateEntityCount(chunks));
            chunks.Dispose();
        }

        void VerifyQueryCount(EntityArchetypeQuery query, int expectedCount)
        {
            var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);
            Assert.AreEqual(expectedCount, ArchetypeChunkArray.CalculateEntityCount(chunks));
            chunks.Dispose();
        }

        [Test]
        public void SSC_DeleteWhenEmpty()
        {
            var entity = m_Manager.CreateEntity(
                typeof(EcsTestData),
                typeof(EcsTestSharedComp),
                typeof(EcsState1)
            );

            m_Manager.SetComponentData(entity, new EcsTestData(1));
            m_Manager.SetComponentData(entity, new EcsState1(2));
            m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp(3));

            VerifyComponentCount<EcsTestData>(1);

            m_Manager.DestroyEntity(entity);
            
            VerifyComponentCount<EcsTestData>(0);
            VerifyComponentCount<EcsState1>(1);

            m_Manager.RemoveComponent<EcsState1>(entity);
            
            VerifyComponentCount<EcsState1>(0);

            Assert.IsFalse(m_Manager.Exists(entity));
        }

        [Test]
        public void SSC_DeleteWhenEmptyArray()
        {
            var entities = new Entity[512];

            for (var i = 0; i < 512; i++)
            {
                var entity = m_Manager.CreateEntity(
                    typeof(EcsTestData),
                    typeof(EcsTestSharedComp),
                    typeof(EcsState1)
                );
                entities[i] = entity;

                m_Manager.SetComponentData(entity, new EcsTestData(i));
                m_Manager.SetComponentData(entity, new EcsState1(i));
                m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp(i % 7));
            }

            VerifyComponentCount<EcsTestData>(512);
            
            for (var i = 0; i < 512; i += 2)
            {
                var entity = entities[i];
                m_Manager.DestroyEntity(entity);
            }

            VerifyComponentCount<EcsTestData>(256);
            VerifyComponentCount<EcsState1>(512);
            VerifyQueryCount(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(), // any
                None = new ComponentType[] {typeof(EcsTestData)}, // none
                All = new ComponentType[] {typeof(EcsState1)}, // all
            }, 256);
            
            for (var i = 0; i < 512; i += 2)
            {
                var entity = entities[i];
                m_Manager.RemoveComponent<EcsState1>(entity);
            }
            
            VerifyComponentCount<EcsState1>(256);

            for (var i = 0; i < 512; i += 2)
            {
                var entity = entities[i];
                Assert.IsFalse(m_Manager.Exists(entity));
            }

            for (var i = 1; i < 512; i += 2)
            {
                var entity = entities[i];
                Assert.IsTrue(m_Manager.Exists(entity));
            }
        }
        
        [Test]
        public void SSC_DeleteWhenEmptyArray2()
        {
            var entities = new Entity[512];

            for (var i = 0; i < 512; i++)
            {
                var entity = m_Manager.CreateEntity(
                    typeof(EcsTestData),
                    typeof(EcsTestSharedComp),
                    typeof(EcsState1)
                );
                entities[i] = entity;

                m_Manager.SetComponentData(entity, new EcsTestData(i));
                m_Manager.SetComponentData(entity, new EcsState1(i));
                m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp(i % 7));
            }

            VerifyComponentCount<EcsTestData>(512);
            
            for (var i = 0; i < 256; i++)
            {
                var entity = entities[i];
                m_Manager.DestroyEntity(entity);
            }
            
            VerifyComponentCount<EcsTestData>(256);
            VerifyComponentCount<EcsState1>(512);
            VerifyQueryCount(new EntityArchetypeQuery
            {
                Any = Array.Empty<ComponentType>(), // any
                None = new ComponentType[] {typeof(EcsTestData)}, // none
                All = new ComponentType[] {typeof(EcsState1)}, // all
            }, 256);

            for (var i = 0; i < 256; i++)
            {
                var entity = entities[i];
                m_Manager.RemoveComponent<EcsState1>(entity);
            }

            VerifyComponentCount<EcsState1>(256);

            for (var i = 0; i < 256; i++)
            {
                var entity = entities[i];
                Assert.IsFalse(m_Manager.Exists(entity));
            }

            for (var i = 256; i < 512; i++)
            {
                var entity = entities[i];
                Assert.IsTrue(m_Manager.Exists(entity));
            }
        }

        [Test]
        public void SSC_DoNotInstantiateSystemState()
        {
            var entity0 = m_Manager.CreateEntity(
                typeof(EcsTestData),
                typeof(EcsTestSharedComp),
                typeof(EcsState1)
            );

            var entity1 = m_Manager.Instantiate(entity0);
            
            VerifyComponentCount<EcsState1>(1);
        }
    }
}
