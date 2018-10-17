using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Entities.Tests
{
	public class CreateAndDestroyTests : ECSTestsFixture
	{
        [Test]
        unsafe public void CreateAndDestroyOne()
        {
            var entity = CreateEntityWithDefaultData(10);
            m_Manager.DestroyEntity(entity);
            AssertDoesNotExist(entity);
        }

        [Test]
        unsafe public void EmptyEntityIsNull()
        {
            CreateEntityWithDefaultData(10);
            Assert.IsFalse(m_Manager.Exists(new Entity()));
        }

        [Test]
        unsafe public void CreateAndDestroyTwo()
        {
            var entity0 = CreateEntityWithDefaultData(10);
            var entity1 = CreateEntityWithDefaultData(11);

            m_Manager.DestroyEntity(entity0);

            AssertDoesNotExist(entity0);
            AssertComponentData(entity1, 11);

            m_Manager.DestroyEntity(entity1);
            AssertDoesNotExist(entity0);
            AssertDoesNotExist(entity1);
        }

	    [Test]
	    unsafe public void CreateZeroEntities()
	    {
	        var array = new NativeArray<Entity>(0, Allocator.Temp);
	        m_Manager.CreateEntity(m_Manager.CreateArchetype(typeof(EcsTestData)), array);
	        array.Dispose();
	    }

	    [Test]
	    unsafe public void InstantiateZeroEntities()
	    {
	        var array = new NativeArray<Entity>(0, Allocator.Temp);

	        var srcEntity = m_Manager.CreateEntity(typeof(EcsTestData));
	        m_Manager.Instantiate(srcEntity , array);
	        array.Dispose();
	    }


        [Test]
        unsafe public void CreateAndDestroyThree()
        {
            var entity0 = CreateEntityWithDefaultData(10);
            var entity1 = CreateEntityWithDefaultData(11);

            m_Manager.DestroyEntity(entity0);

            var entity2 = CreateEntityWithDefaultData(12);


            AssertDoesNotExist(entity0);

            AssertComponentData(entity1, 11);
            AssertComponentData(entity2, 12);
        }

        [Test]
        unsafe public void CreateAndDestroyStressTest()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);

            m_Manager.CreateEntity(archetype, entities);

            for (int i = 0; i < entities.Length; i++)
                AssertComponentData(entities[i], 0);

            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        unsafe public void CreateAndDestroyShuffleStressTest()
		{
            Entity[] entities = new Entity[10000];
            for (int i = 0; i < entities.Length;i++)
            {
                entities[i] = CreateEntityWithDefaultData(i);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (i % 2 == 0)
                    m_Manager.DestroyEntity(entities[i]);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (i % 2 == 0)
                {
                    AssertDoesNotExist(entities[i]);
                }
                else
                {
                    AssertComponentData(entities[i], i);
                }
            }

            for (int i = 0; i < entities.Length; i++)
            {
                if (i % 2 == 1)
                    m_Manager.DestroyEntity(entities[i]);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                AssertDoesNotExist(entities[i]);
            }
        }


        [Test]
        unsafe public void InstantiateStressTest()
        {
            var entities = new NativeArray<Entity>(10000, Allocator.Persistent);
            var srcEntity = CreateEntityWithDefaultData(5);

            m_Manager.Instantiate(srcEntity, entities);

            for (int i = 0; i < entities.Length; i++)
                AssertComponentData(entities[i], 5);

            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

		[Test]
		public void AddRemoveComponent()
		{
			var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var entity = m_Manager.CreateEntity(archetype);
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
			Assert.IsFalse(m_Manager.HasComponent<EcsTestData3>(entity));

			m_Manager.AddComponentData<EcsTestData3>(entity, new EcsTestData3(3));
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entity));
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData3>(entity));

            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value0);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value1);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value2);

			m_Manager.RemoveComponent<EcsTestData2>(entity);
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
			Assert.IsFalse(m_Manager.HasComponent<EcsTestData2>(entity));
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData3>(entity));

            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value0);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value1);
            Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData3>(entity).value2);

			m_Manager.DestroyEntity(entity);
		}

	    [Test]
	    public void AddComponentSetsValueOfComponentToDefault()
	    {
	        var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var dummyEntity = m_Manager.CreateEntity(archetype);
	        m_Manager.Debug.PoisonUnusedDataInAllChunks(archetype, 0xCD);

	        var entity = m_Manager.CreateEntity();
	        m_Manager.AddComponent(entity, ComponentType.Create<EcsTestData>());
	        Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData>(entity).value);

	        m_Manager.DestroyEntity(dummyEntity);
	        m_Manager.DestroyEntity(entity);
	    }

	    [Test]
		public void ReadOnlyAndNonReadOnlyArchetypeAreEqual()
		{
			var arch = m_Manager.CreateArchetype(ComponentType.ReadOnly(typeof(EcsTestData)));
			var arch2 = m_Manager.CreateArchetype(typeof(EcsTestData));
			Assert.AreEqual(arch, arch2);
		}

		[Test]
		public void SubtractiveArchetypeReactToAddRemoveComponent()
		{
			var subtractiveArch = m_Manager.CreateComponentGroup(ComponentType.Subtractive(typeof(EcsTestData)), typeof(EcsTestData2));

			var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var entity = m_Manager.CreateEntity(archetype);
			Assert.AreEqual(0, subtractiveArch.GetComponentDataArray<EcsTestData2>().Length);

			m_Manager.RemoveComponent<EcsTestData>(entity);
			Assert.AreEqual(1, subtractiveArch.GetComponentDataArray<EcsTestData2>().Length);

			m_Manager.AddComponentData<EcsTestData>(entity, new EcsTestData());
			Assert.AreEqual(0, subtractiveArch.GetComponentDataArray<EcsTestData2>().Length);
		}


	    [Test]
	    public void ChunkCountsAreCorrect()
	    {
	        var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var entity = m_Manager.CreateEntity(archetype);

	        Assert.AreEqual(1, archetype.ChunkCount);

	        m_Manager.AddComponent(entity, typeof(EcsTestData2));
	        Assert.AreEqual(0, archetype.ChunkCount);

	        unsafe {
	            Assert.IsTrue(archetype.Archetype->ChunkList.IsEmpty);
	            Assert.AreEqual(0, archetype.Archetype->EntityCount);

	            var archetype2 = m_Manager.Entities->GetArchetype(entity);
	            Assert.AreEqual(1, archetype2->ChunkCount);
	            Assert.AreEqual(1, archetype2->EntityCount);
	        }
	    }
	}
}
