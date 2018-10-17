using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    public class ComponentOrderVersionTests : ECSTestsFixture
    {
        int oddTestValue = 34;
        int evenTestValue = 17;

        void AddEvenOddTestData()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var evenShared = new SharedData1(evenTestValue);
            var oddShared = new SharedData1(oddTestValue);
            for (int i = 0; i < 100; i++)
            {
                Entity e = m_Manager.CreateEntity(archetype);
                var testData = m_Manager.GetComponentData<EcsTestData>(e);
                testData.value = i;
                m_Manager.SetComponentData(e, testData);
                if ((i & 0x01) == 0)
                {
                    m_Manager.AddSharedComponentData(e, evenShared);
                }
                else
                {
                    m_Manager.AddSharedComponentData(e, oddShared);
                }
            }
        }

        void ActionEvenOdd(Action<int, ComponentGroup> even, Action<int, ComponentGroup> odd)
        {
            var uniqueTypes = new List<SharedData1>(10);
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));
            group.CompleteDependency();

            m_Manager.GetAllUniqueSharedComponentData(uniqueTypes);

            for (int sharedIndex = 0; sharedIndex != uniqueTypes.Count; sharedIndex++)
            {
                var sharedData = uniqueTypes[sharedIndex];
                group.SetFilter(sharedData);
                var version = m_Manager.GetSharedComponentOrderVersion(sharedData);

                if (sharedData.value == evenTestValue)
                {
                    even(version, group);
                }

                if (sharedData.value == oddTestValue)
                {
                    odd(version, group);
                }
            }

            group.Dispose();
        }

        [Test]
        public void SharedComponentNoChangeVersionUnchanged()
        {
            AddEvenOddTestData();
            ActionEvenOdd((version, group) => { Assert.AreEqual(version, 1); },
                (version, group) => { Assert.AreEqual(version, 1); });
        }

        void TestSourceEvenValues(int version, ComponentGroup group)
        {
            var testData = group.GetComponentDataArray<EcsTestData>();

            Assert.AreEqual(50, testData.Length);

            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(i * 2, testData[i].value);
            }
        }

        void TestSourceOddValues(int version, ComponentGroup group)
        {
            var testData = group.GetComponentDataArray<EcsTestData>();

            Assert.AreEqual(50, testData.Length);

            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(1 + (i * 2), testData[i].value);
            }
        }

        [Test]
        public void SharedComponentNoChangeValuesUnchanged()
        {
            AddEvenOddTestData();
            ActionEvenOdd(TestSourceEvenValues, TestSourceOddValues);
        }

        void ChangeGroupOrder(int version, ComponentGroup group)
        {
            var entityData = group.GetEntityArray();
            var entities = new NativeArray<Entity>(50, Allocator.Temp);
            entityData.CopyTo(new NativeSlice<Entity>(entities));

            for (int i = 0; i < 50; i++)
            {
                var e = entities[i];
                if ((i & 0x01) == 0)
                {
                    var testData2 = new EcsTestData2(i);
                    m_Manager.AddComponentData(e, testData2);
                }
            }

            entities.Dispose();
        }

        [Test]
        public void SharedComponentChangeOddGroupOrderOnlyOddVersionChanged()
        {
            AddEvenOddTestData();

            ActionEvenOdd((version, group) => { }, ChangeGroupOrder);
            ActionEvenOdd((version, group) => { Assert.AreEqual(version, 1); },
                (version, group) => { Assert.Greater(version, 1); });
        }

        [Test]
        public void SharedComponentChangeOddGroupOrderEvenValuesUnchanged()
        {
            AddEvenOddTestData();

            ActionEvenOdd((version, group) => { }, ChangeGroupOrder);
            ActionEvenOdd(TestSourceEvenValues, (version, group) => { });
        }

        void DestroyAllButOneEntityInGroup(int version, ComponentGroup group)
        {
            var entityData = group.GetEntityArray();
            var entities = new NativeArray<Entity>(50, Allocator.Temp);
            entityData.CopyTo(new NativeSlice<Entity>(entities));

            for (int i = 0; i < 49; i++)
            {
                var e = entities[i];
                m_Manager.DestroyEntity(e);
            }

            entities.Dispose();
        }

        [Test]
        public void SharedComponentDestroyAllButOneEntityInOddGroupOnlyOddVersionChanged()
        {
            AddEvenOddTestData();

            ActionEvenOdd((version, group) => { }, DestroyAllButOneEntityInGroup);
            ActionEvenOdd((version, group) => { Assert.AreEqual(version, 1); },
                (version, group) => { Assert.Greater(version, 1); });
        }

        [Test]
        public void SharedComponentDestroyAllButOneEntityInOddGroupEvenValuesUnchanged()
        {
            AddEvenOddTestData();

            ActionEvenOdd((version, group) => { }, DestroyAllButOneEntityInGroup);
            ActionEvenOdd(TestSourceEvenValues, (version, group) => { });
        }
        
        [Test]
        public void CreateEntity()
        {
            m_Manager.CreateEntity(typeof(EcsTestData));
            Assert.AreEqual(1, m_Manager.GetComponentOrderVersion<EcsTestData>());
            Assert.AreEqual(0, m_Manager.GetComponentOrderVersion<EcsTestData2>());
        }
        
        [Test]
        public void DestroyEntity()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.DestroyEntity(entity);
            
            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<EcsTestData>());
            Assert.AreEqual(0, m_Manager.GetComponentOrderVersion<EcsTestData2>());
        }

        [Test]
        public void AddComponent()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.AddComponentData(entity, new EcsTestData2());
            
            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<EcsTestData>());
            Assert.AreEqual(1, m_Manager.GetComponentOrderVersion<EcsTestData2>());
        }
        
        [Test]
        public void RemoveComponent()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            m_Manager.RemoveComponent<EcsTestData2>(entity);
            
            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<EcsTestData>());
            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<EcsTestData2>());
        }
        
        [Test]
        public void ChangedOnlyAffectedArchetype()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData3));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            m_Manager.RemoveComponent<EcsTestData2>(entity1);
            
            Assert.AreEqual(3, m_Manager.GetComponentOrderVersion<EcsTestData>());
            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<EcsTestData2>());
            Assert.AreEqual(1, m_Manager.GetComponentOrderVersion<EcsTestData3>());
        } 
        
        [Test]
        public void SetSharedComponent()
        {
            var entity = m_Manager.CreateEntity(typeof(SharedData1), typeof(SharedData2));
            m_Manager.SetSharedComponentData(entity, new SharedData1(1));

            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<SharedData2>());
            Assert.AreEqual(2, m_Manager.GetComponentOrderVersion<SharedData1>());
            
            Assert.AreEqual(1, m_Manager.GetSharedComponentOrderVersion(new SharedData1(1)));
        }
        
        [Test]
        public void DestroySharedComponentEntity()
        {
            var sharedData = new SharedData1(1);
            
            var destroyEntity = m_Manager.CreateEntity(typeof(SharedData1));
            m_Manager.SetSharedComponentData(destroyEntity, sharedData );
            /*var dontDestroyEntity = */ m_Manager.Instantiate(destroyEntity);

            Assert.AreEqual(2, m_Manager.GetSharedComponentOrderVersion(sharedData));

            m_Manager.DestroyEntity(destroyEntity);

            Assert.AreEqual(3, m_Manager.GetSharedComponentOrderVersion(sharedData));
        }
        
        [Test]
        public void DestroySharedComponentDataSetsOrderVersionToZero()
        {
            var sharedData = new SharedData1(1);
            var entity = m_Manager.CreateEntity();
            m_Manager.AddSharedComponentData(entity, sharedData);
            
            m_Manager.DestroyEntity(entity);

            Assert.AreEqual(0, m_Manager.GetSharedComponentOrderVersion(sharedData));
        }
    }
}
