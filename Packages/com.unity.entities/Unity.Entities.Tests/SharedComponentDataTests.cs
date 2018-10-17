using System;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    struct SharedData1 : ISharedComponentData
    {
        public int value;

        public SharedData1(int val) { value = val; }
    }

    struct SharedData2 : ISharedComponentData
    {
        public int value;

        public SharedData2(int val) { value = val; }
    }

    public class SharedComponentDataTests : ECSTestsFixture
    {
        //@TODO: No tests for invalid shared components / destroyed shared component data
        //@TODO: No tests for if we leak shared data when last entity is destroyed...
        //@TODO: No tests for invalid shared component type?

        [Test]
        public void SetSharedComponent()
        {
            var archetype = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData), typeof(SharedData2));

            var group1 = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));
            var group2 = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData2));
            var group12 = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData2), typeof(SharedData1));
            
            var group1_filter_0 = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));
            group1_filter_0.SetFilter(new SharedData1(0));
            var group1_filter_20 = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));
            group1_filter_20.SetFilter(new SharedData1(20));
            
            Assert.AreEqual(0, group1.CalculateLength());
            Assert.AreEqual(0, group2.CalculateLength());
            Assert.AreEqual(0, group12.CalculateLength());

            Assert.AreEqual(0, group1_filter_0.CalculateLength());
            Assert.AreEqual(0, group1_filter_20.CalculateLength());

            Entity e1 = m_Manager.CreateEntity(archetype);
            m_Manager.SetComponentData(e1, new EcsTestData(117));
            Entity e2 = m_Manager.CreateEntity(archetype);
            m_Manager.SetComponentData(e2, new EcsTestData(243));

            Assert.AreEqual(2, group1_filter_0.CalculateLength());
            Assert.AreEqual(0, group1_filter_20.CalculateLength());
            Assert.AreEqual(117, group1_filter_0.GetComponentDataArray<EcsTestData>()[0].value);
            Assert.AreEqual(243, group1_filter_0.GetComponentDataArray<EcsTestData>()[1].value);

            m_Manager.SetSharedComponentData(e1, new SharedData1(20));

            Assert.AreEqual(1, group1_filter_0.CalculateLength());
            Assert.AreEqual(1, group1_filter_20.CalculateLength());
            Assert.AreEqual(117, group1_filter_20.GetComponentDataArray<EcsTestData>()[0].value);
            Assert.AreEqual(243, group1_filter_0.GetComponentDataArray<EcsTestData>()[0].value);

            m_Manager.SetSharedComponentData(e2, new SharedData1(20));

            Assert.AreEqual(0, group1_filter_0.CalculateLength());
            Assert.AreEqual(2, group1_filter_20.CalculateLength());
            Assert.AreEqual(117, group1_filter_20.GetComponentDataArray<EcsTestData>()[0].value);
            Assert.AreEqual(243, group1_filter_20.GetComponentDataArray<EcsTestData>()[1].value);

            group1.Dispose();
            group2.Dispose();
            group12.Dispose();
            group1_filter_0.Dispose();
            group1_filter_20.Dispose();
        }


        [Test]
        public void GetComponentArray()
        {
            var archetype1 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            var archetype2 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData), typeof(SharedData2));

            const int entitiesPerValue = 5000;
            for (int i = 0; i < entitiesPerValue*8; ++i)
            {
                Entity e = m_Manager.CreateEntity((i % 2 == 0) ? archetype1 : archetype2);
                m_Manager.SetComponentData(e, new EcsTestData(i));
                m_Manager.SetSharedComponentData(e, new SharedData1(i%8));
            }

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));

            for (int sharedValue = 0; sharedValue < 8; ++sharedValue)
            {
                bool[] foundEntities = new bool[entitiesPerValue];
                group.SetFilter(new SharedData1(sharedValue));
                var componentArray = group.GetComponentDataArray<EcsTestData>();
                Assert.AreEqual(entitiesPerValue, componentArray.Length);
                for (int i = 0; i < entitiesPerValue; ++i)
                {
                    int index = componentArray[i].value;
                    Assert.AreEqual(sharedValue, index % 8);
                    Assert.IsFalse(foundEntities[index/8]);
                    foundEntities[index/8] = true;
                }
            }

            group.Dispose();
        }

        [Test]
        public void GetAllUniqueSharedComponents()
        {
            var unique = new List<SharedData1>(0);
            m_Manager.GetAllUniqueSharedComponentData(unique);

            Assert.AreEqual(1, unique.Count);
            Assert.AreEqual(default(SharedData1).value, unique[0].value);

            var archetype = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            Entity e = m_Manager.CreateEntity(archetype);
            m_Manager.SetSharedComponentData(e, new SharedData1(17));

            unique.Clear();
            m_Manager.GetAllUniqueSharedComponentData(unique);

            Assert.AreEqual(2, unique.Count);
            Assert.AreEqual(default(SharedData1).value, unique[0].value);
            Assert.AreEqual(17, unique[1].value);

            m_Manager.SetSharedComponentData(e, new SharedData1(34));

            unique.Clear();
            m_Manager.GetAllUniqueSharedComponentData(unique);

            Assert.AreEqual(2, unique.Count);
            Assert.AreEqual(default(SharedData1).value, unique[0].value);
            Assert.AreEqual(34, unique[1].value);

            m_Manager.DestroyEntity(e);

            unique.Clear();
            m_Manager.GetAllUniqueSharedComponentData(unique);

            Assert.AreEqual(1, unique.Count);
            Assert.AreEqual(default(SharedData1).value, unique[0].value);
        }

        [Test]
        public void GetSharedComponentData()
        {
            var archetype = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            Entity e = m_Manager.CreateEntity(archetype);

            Assert.AreEqual(0, m_Manager.GetSharedComponentData<SharedData1>(e).value);

            m_Manager.SetSharedComponentData(e, new SharedData1(17));

            Assert.AreEqual(17, m_Manager.GetSharedComponentData<SharedData1>(e).value);
        }
        
        [Test]
        public void GetSharedComponentDataAfterArchetypeChange()
        {
            var archetype = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            Entity e = m_Manager.CreateEntity(archetype);

            Assert.AreEqual(0, m_Manager.GetSharedComponentData<SharedData1>(e).value);

            m_Manager.SetSharedComponentData(e, new SharedData1(17));
            m_Manager.AddComponentData(e, new EcsTestData2 {value0 = 1, value1 = 2});

            Assert.AreEqual(17, m_Manager.GetSharedComponentData<SharedData1>(e).value);
        }

        [Test]
        public void NonExistingSharedComponentDataThrows()
        {
            Entity e = m_Manager.CreateEntity(typeof(EcsTestData));

            Assert.Throws<ArgumentException>(() => { m_Manager.GetSharedComponentData<SharedData1>(e); });
            Assert.Throws<ArgumentException>(() => { m_Manager.SetSharedComponentData(e, new SharedData1()); });
        }

        [Test]
        public void AddSharedComponent()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            Entity e = m_Manager.CreateEntity(archetype);

            Assert.IsFalse(m_Manager.HasComponent<SharedData1>(e));
            Assert.IsFalse(m_Manager.HasComponent<SharedData2>(e));

            m_Manager.AddSharedComponentData(e, new SharedData1(17));

            Assert.IsTrue(m_Manager.HasComponent<SharedData1>(e));
            Assert.IsFalse(m_Manager.HasComponent<SharedData2>(e));
            Assert.AreEqual(17, m_Manager.GetSharedComponentData<SharedData1>(e).value);

            m_Manager.AddSharedComponentData(e, new SharedData2(34));
            Assert.IsTrue(m_Manager.HasComponent<SharedData1>(e));
            Assert.IsTrue(m_Manager.HasComponent<SharedData2>(e));
            Assert.AreEqual(17, m_Manager.GetSharedComponentData<SharedData1>(e).value);
            Assert.AreEqual(34, m_Manager.GetSharedComponentData<SharedData2>(e).value);
        }
        
        [Test]
        public void RemoveSharedComponent()
        {
            Entity e = m_Manager.CreateEntity();

            m_Manager.AddComponentData(e, new EcsTestData(42));
            m_Manager.AddSharedComponentData(e, new SharedData1(17));
            m_Manager.AddSharedComponentData(e, new SharedData2(34));

            Assert.IsTrue(m_Manager.HasComponent<SharedData1>(e));
            Assert.IsTrue(m_Manager.HasComponent<SharedData2>(e));
            Assert.AreEqual(17, m_Manager.GetSharedComponentData<SharedData1>(e).value);
            Assert.AreEqual(34, m_Manager.GetSharedComponentData<SharedData2>(e).value);

            m_Manager.RemoveComponent<SharedData1>(e);
            Assert.IsFalse(m_Manager.HasComponent<SharedData1>(e));
            Assert.AreEqual(34, m_Manager.GetSharedComponentData<SharedData2>(e).value);

            m_Manager.RemoveComponent<SharedData2>(e);
            Assert.IsFalse(m_Manager.HasComponent<SharedData2>(e));
            
            Assert.AreEqual(42, m_Manager.GetComponentData<EcsTestData>(e).value);
        }
        
        [Test]
        public void GetSharedComponentArray()
        {
            var archetype1 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            var archetype2 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData), typeof(SharedData2));

            const int entitiesPerValue = 5000;
            for (int i = 0; i < entitiesPerValue*8; ++i)
            {
                Entity e = m_Manager.CreateEntity((i % 2 == 0) ? archetype1 : archetype2);
                m_Manager.SetComponentData(e, new EcsTestData(i));
                m_Manager.SetSharedComponentData(e, new SharedData1(i%8));
            }

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));

            var foundEntities = new bool[8, entitiesPerValue];


            var sharedComponentDataArray = group.GetSharedComponentDataArray<SharedData1>();
            var componentArray = group.GetComponentDataArray<EcsTestData>();

            Assert.AreEqual(entitiesPerValue*8, sharedComponentDataArray.Length);
            Assert.AreEqual(entitiesPerValue*8, componentArray.Length);

            for (int i = 0; i < entitiesPerValue*8; ++i)
            {
                var sharedValue = sharedComponentDataArray[i].value;
                int index = componentArray[i].value;
                Assert.AreEqual(sharedValue, index % 8);
                Assert.IsFalse(foundEntities[sharedValue, index/8]);
                foundEntities[sharedValue, index/8] = true;
            }

            group.Dispose();
        }
        
        [Test]
        public void SCG_DoesNotMatchRemovedSharedComponentInComponentGroup()
        {
            var archetype0 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            var archetype1 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData), typeof(SharedData2));

            var group0 = m_Manager.CreateComponentGroup(typeof(SharedData1));
            var group1 = m_Manager.CreateComponentGroup(typeof(SharedData2));
            
            var entity0 = m_Manager.CreateEntity(archetype0);
            var entity1 = m_Manager.CreateEntity(archetype1);

            Assert.AreEqual(2, group0.CalculateLength());
            Assert.AreEqual(1, group1.CalculateLength());

            m_Manager.RemoveComponent<SharedData2>(entity1);

            Assert.AreEqual(2, group0.CalculateLength());
            Assert.AreEqual(0, group1.CalculateLength());

            group0.Dispose();
            group1.Dispose();
        }
        
        [Test]
        public void SCG_DoesNotMatchRemovedSharedComponentInChunkQuery()
        {
            var archetype0 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData));
            var archetype1 = m_Manager.CreateArchetype(typeof(SharedData1), typeof(EcsTestData), typeof(SharedData2));

            var query0 = new EntityArchetypeQuery
            {
                All = new ComponentType[] {typeof(SharedData1)},
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>()
            };
            var query1 = new EntityArchetypeQuery
            {
                All = new ComponentType[] {typeof(SharedData2)},
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>()
            };

            var entity0 = m_Manager.CreateEntity(archetype0);
            var entity1 = m_Manager.CreateEntity(archetype1);

            var preChunks0 = m_Manager.CreateArchetypeChunkArray(query0, Allocator.TempJob);
            var preChunks1 = m_Manager.CreateArchetypeChunkArray(query1, Allocator.TempJob);

            Assert.AreEqual(2, ArchetypeChunkArray.CalculateEntityCount(preChunks0));
            Assert.AreEqual(1, ArchetypeChunkArray.CalculateEntityCount(preChunks1));

            m_Manager.RemoveComponent<SharedData2>(entity1);
            
            var postChunks0 = m_Manager.CreateArchetypeChunkArray(query0, Allocator.TempJob);
            var postChunks1 = m_Manager.CreateArchetypeChunkArray(query1, Allocator.TempJob);

            Assert.AreEqual(2, ArchetypeChunkArray.CalculateEntityCount(postChunks0));
            Assert.AreEqual(0, ArchetypeChunkArray.CalculateEntityCount(postChunks1));

            preChunks0.Dispose();
            preChunks1.Dispose();
            postChunks0.Dispose();
            postChunks1.Dispose();
        }

    }
}
