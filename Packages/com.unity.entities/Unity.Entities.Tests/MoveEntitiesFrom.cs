using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    public class MoveEntitiesFrom : ECSTestsFixture
    {
        [Test]
        public void MoveEntitiesToSameEntityManagerThrows()
        {
            Assert.Throws<ArgumentException>(() => { m_Manager.MoveEntitiesFrom(m_Manager); });
        }

        [Test]
        public void MoveEntities()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.GetOrCreateManager<EntityManager>();

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
                creationManager.SetComponentData(entities[i], new EcsTestData(i));


            m_Manager.CheckInternalConsistency();
            creationManager.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(creationManager);

            for (int i = 0;i != entities.Length;i++)
                Assert.IsFalse(creationManager.Exists(entities[i]));

            m_Manager.CheckInternalConsistency();
            creationManager.CheckInternalConsistency();

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
            Assert.AreEqual(entities.Length, group.CalculateLength());
            Assert.AreEqual(0, creationManager.CreateComponentGroup(typeof(EcsTestData)).CalculateLength());

            // We expect that the order of the crated entities is the same as in the creation scene
            var testDataArray = group.GetComponentDataArray<EcsTestData>();
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(i, testDataArray[i].value);

            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithSharedComponentData()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.GetOrCreateManager<EntityManager>();

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(SharedData1));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
            {
                creationManager.SetComponentData(entities[i], new EcsTestData(i));
                creationManager.SetSharedComponentData(entities[i], new SharedData1(i % 5));
            }

            m_Manager.CheckInternalConsistency();
            creationManager.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(creationManager);

            m_Manager.CheckInternalConsistency();
            creationManager.CheckInternalConsistency();

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(SharedData1));
            Assert.AreEqual(entities.Length, group.CalculateLength());
            Assert.AreEqual(0, creationManager.CreateComponentGroup(typeof(EcsTestData)).CalculateLength());

            // We expect that the shared component data matches the correct entities
            var testDataArray = group.GetComponentDataArray<EcsTestData>();
            var testSharedDataArray = group.GetSharedComponentDataArray<SharedData1>();
            for (int i = 0;i != testDataArray.Length;i++)
                Assert.AreEqual(testSharedDataArray[i].value, testDataArray[i].value % 5);

            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesAppendsToExistingEntities()
        {
            int numberOfEntitiesPerManager = 10000;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
                m_Manager.SetComponentData(targetEntities[i], new EcsTestData(i));

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.GetOrCreateManager<EntityManager>();
            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestData(numberOfEntitiesPerManager + i));

            m_Manager.CheckInternalConsistency();
            sourceManager.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(sourceManager);

            m_Manager.CheckInternalConsistency();
            sourceManager.CheckInternalConsistency();

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateLength());
            Assert.AreEqual(0, sourceManager.CreateComponentGroup(typeof(EcsTestData)).CalculateLength());

            // We expect that the order of the crated entities is the same as in the creation scene
            var testDataArray = group.GetComponentDataArray<EcsTestData>();
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(i, testDataArray[i].value);

            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesPatchesEntityReferences()
        {
            int numberOfEntitiesPerManager = 10000;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestDataEntity));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
                m_Manager.SetComponentData(targetEntities[i], new EcsTestDataEntity(i, targetEntities[i]));

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.GetOrCreateManager<EntityManager>();
            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestDataEntity));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestDataEntity(numberOfEntitiesPerManager + i, sourceEntities[i]));

            m_Manager.CheckInternalConsistency();
            sourceManager.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(sourceManager);

            m_Manager.CheckInternalConsistency();
            sourceManager.CheckInternalConsistency();

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestDataEntity));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateLength());
            Assert.AreEqual(0, sourceManager.CreateComponentGroup(typeof(EcsTestDataEntity)).CalculateLength());

            var testDataArray = group.GetComponentDataArray<EcsTestDataEntity>();
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(testDataArray[i].value0, m_Manager.GetComponentData<EcsTestDataEntity>(testDataArray[i].value1).value0);

            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

        [Test]
        [Ignore("NOT IMPLEMENTED")]
        public void MoveEntitiesPatchesEntityReferencesInSharedComponentData()
        {
            int numberOfEntitiesPerManager = 10000;
            int frequency = 5;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestSharedCompEntity));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
            {
                m_Manager.SetComponentData(targetEntities[i], new EcsTestData(i));
                m_Manager.SetSharedComponentData(targetEntities[i], new EcsTestSharedCompEntity(targetEntities[i % frequency]));
            }

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.GetOrCreateManager<EntityManager>();
            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestSharedCompEntity));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
            {
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestData(numberOfEntitiesPerManager + i));
                sourceManager.SetSharedComponentData(sourceEntities[i], new EcsTestSharedCompEntity(sourceEntities[i % frequency]));
            }

            m_Manager.CheckInternalConsistency();
            sourceManager.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(sourceManager);

            m_Manager.CheckInternalConsistency();
            sourceManager.CheckInternalConsistency();

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(EcsTestSharedCompEntity));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateLength());
            Assert.AreEqual(0, sourceManager.CreateComponentGroup(typeof(EcsTestData)).CalculateLength());

            var testDataArray = group.GetComponentDataArray<EcsTestData>();
            var testSharedCompArray = group.GetSharedComponentDataArray<EcsTestSharedCompEntity>();
            for (int i = 0; i != numberOfEntitiesPerManager; i++)
                Assert.AreEqual(testDataArray[i].value % frequency, m_Manager.GetComponentData<EcsTestData>(testSharedCompArray[i].value).value);
            for (int i = numberOfEntitiesPerManager; i != numberOfEntitiesPerManager * 2; i++)
                Assert.AreEqual(numberOfEntitiesPerManager + testDataArray[i].value % frequency, m_Manager.GetComponentData<EcsTestData>(testSharedCompArray[i].value).value);

            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

        [Test]
        public void ExternalSharedComponentReferencePreventsMoveEntities()
        {
            var anotherWorld = new World("AnotherWorld");
            var anotherManager = anotherWorld.GetOrCreateManager<EntityManager>();

            var a = anotherManager.CreateEntity(typeof(EcsTestSharedComp));
            var b = anotherManager.CreateEntity(typeof(EcsTestSharedComp));
            var c = anotherManager.CreateEntity(typeof(EcsTestSharedComp));

            anotherManager.SetSharedComponentData(a, new EcsTestSharedComp(123));
            anotherManager.SetSharedComponentData(b, new EcsTestSharedComp(456));
            anotherManager.SetSharedComponentData(c, new EcsTestSharedComp(789));

            var group = anotherManager.CreateComponentGroup(typeof(EcsTestSharedComp));
            group.SetFilter(new EcsTestSharedComp(456));

            Assert.Throws<ArgumentException>(() => { m_Manager.MoveEntitiesFrom(anotherManager); });

            group.Dispose();
            anotherWorld.Dispose();
        }
    }
}
