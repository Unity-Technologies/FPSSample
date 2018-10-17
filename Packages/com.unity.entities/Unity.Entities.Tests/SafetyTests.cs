using NUnit.Framework;
using System;
using Unity.Entities;

//@TODO: We should really design systems / jobs / exceptions / errors 
//       so that an error in one system does not affect the next system.
//       Right now failure to set dependencies correctly in one system affects other code,
//       this makes the error messages significantly less useful...
//       So need to redo all tests accordingly

namespace Unity.Entities.Tests
{
    public class SafetyTests : ECSTestsFixture
	{

		[Test]
		public void RemoveEntityComponentThrows()
		{
			var entity = m_Manager.CreateEntity(typeof(EcsTestData));
			Assert.Throws<ArgumentException>(() => { m_Manager.RemoveComponent(entity, typeof(Entity)); });
			Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
		}

		[Test]
	    public void ComponentArrayChunkSliceOutOfBoundsThrowsException()
	    {
	        for (int i = 0;i<10;i++)
	            m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

	        var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
	        var testData = group.GetComponentDataArray<EcsTestData>();

	        Assert.AreEqual(0, testData.GetChunkArray(5, 0).Length);
	        Assert.AreEqual(10, testData.GetChunkArray(0, 10).Length);

	        Assert.Throws<IndexOutOfRangeException>(() => { testData.GetChunkArray(-1, 1); });
	        Assert.Throws<IndexOutOfRangeException>(() => { testData.GetChunkArray(5, 6); });
	        Assert.Throws<IndexOutOfRangeException>(() => { testData.GetChunkArray(10, 1); });
	    }
	    
	    
        [Test]
        public void ReadOnlyComponentDataArray()
        {
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData2), ComponentType.ReadOnly(typeof(EcsTestData)));

            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            
            // EcsTestData is read only
            var arr = group.GetComponentDataArray<EcsTestData>();
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(42, arr[0].value);
            Assert.Throws<System.InvalidOperationException>(() => { arr[0] = new EcsTestData(0); });
           
            // EcsTestData2 can be written to
            var arr2 = group.GetComponentDataArray<EcsTestData2>();
            Assert.AreEqual(1, arr2.Length);
            arr2[0] = new EcsTestData2(55);
            Assert.AreEqual(55, arr2[0].value0);
        }

        [Test]
        public void AccessComponentArrayAfterCreationThrowsException()
        {
            CreateEntityWithDefaultData(0);

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
            var arr = group.GetComponentDataArray<EcsTestData>();

            CreateEntityWithDefaultData(1);

            Assert.Throws<InvalidOperationException>(() => { var value = arr[0]; });
        }

        [Test]
        public void CreateEntityInvalidatesArray()
        {
            CreateEntityWithDefaultData(0);

            var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
            var arr = group.GetComponentDataArray<EcsTestData>();

            CreateEntityWithDefaultData(1);

            Assert.Throws<InvalidOperationException>(() => { var value = arr[0]; });
        }

        [Test]
        public void GetSetComponentThrowsIfNotExist()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var destroyedEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.DestroyEntity(destroyedEntity);

            Assert.Throws<System.ArgumentException>(() => { m_Manager.SetComponentData(entity, new EcsTestData2()); });
            Assert.Throws<System.ArgumentException>(() => { m_Manager.SetComponentData(destroyedEntity, new EcsTestData2()); });

            Assert.Throws<System.ArgumentException>(() => { m_Manager.GetComponentData<EcsTestData2>(entity); });
            Assert.Throws<System.ArgumentException>(() => { m_Manager.GetComponentData<EcsTestData2>(destroyedEntity); });
        }

        [Test]
        public void ComponentDataArrayFromEntityThrowsIfNotExist()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var destroyedEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.DestroyEntity(destroyedEntity);

            var data = EmptySystem.GetComponentDataFromEntity<EcsTestData2>();

            Assert.Throws<System.ArgumentException>(() => { data[entity] = new EcsTestData2(); });
            Assert.Throws<System.ArgumentException>(() => { data[destroyedEntity] = new EcsTestData2(); });

            Assert.Throws<System.ArgumentException>(() => { var p = data[entity]; });
            Assert.Throws<System.ArgumentException>(() => { var p = data[destroyedEntity]; });
        }

        [Test]
        public void AddComponentTwiceThrows()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.AddComponentData(entity, new EcsTestData(1));
            Assert.Throws<System.ArgumentException>(() => { m_Manager.AddComponentData(entity, new EcsTestData(1)); });
        }

        [Test]
        public void AddRemoveComponentOnDestroyedEntityThrows()
        {
            var destroyedEntity = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(destroyedEntity);

            Assert.Throws<System.ArgumentException>(() => { m_Manager.AddComponentData(destroyedEntity, new EcsTestData(1)); });
            Assert.Throws<System.ArgumentException>(() => { m_Manager.RemoveComponent<EcsTestData>(destroyedEntity); });
        }

        [Test]
        public void RemoveComponentOnEntityWithoutComponent()
        {
            var entity = m_Manager.CreateEntity();
            Assert.Throws<System.ArgumentException>(() => { m_Manager.RemoveComponent<EcsTestData>(entity); });
        }

        [Test]
        public void CreateDestroyEmptyEntity()
        {
            var entity = m_Manager.CreateEntity();
            Assert.IsTrue(m_Manager.Exists(entity));
            m_Manager.DestroyEntity(entity);
            Assert.IsFalse(m_Manager.Exists(entity));
        }
	    
	    [Test]
	    public void CreateEntityWithNullTypeThrows()
	    {
	        Assert.Throws<System.NullReferenceException>(() => m_Manager.CreateEntity(null));
	    }
	    
	    [Test]
	    public void CreateEntityWithOneNullTypeThrows()
	    {
	        Assert.Throws<System.ArgumentException>(() => m_Manager.CreateEntity(null, typeof(EcsTestData)));
	    }

        unsafe struct BigComponentData1 : IComponentData
        {
            public fixed int BigArray[10000];
        }

        unsafe struct BigComponentData2 : IComponentData
        {
            public fixed float BigArray[10000];
        }

	    [Test]
	    public void CreateTooBigArchetypeThrows()
	    {
	        Assert.Throws<System.ArgumentException>(() =>
	        {
                m_Manager.CreateArchetype(typeof(BigComponentData1), typeof(BigComponentData2));
	        });
	    }
    }
}
