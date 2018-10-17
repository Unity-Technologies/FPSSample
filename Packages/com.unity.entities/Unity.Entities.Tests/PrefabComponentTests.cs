using System;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Tests
{
	public class PrefabComponentTests : ECSTestsFixture
	{
		[Test]
		public void PFB_DontFindPrefabInComponentGroup()
		{
		    var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Prefab));

		    var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
		    
			var entity0 = m_Manager.CreateEntity(archetype0);
			var entity1 = m_Manager.CreateEntity(archetype1);
		    
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(1, arr.Length);

			m_Manager.DestroyEntity(entity0);
			m_Manager.DestroyEntity(entity1);
		}
	    
	    [Test]
	    public void PFB_DontFindPrefabInChunkIterator()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Prefab));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);
	        
	        var query = new EntityArchetypeQuery
	        {
	            Any = Array.Empty<ComponentType>(),
	            None = Array.Empty<ComponentType>(),
	            All = new ComponentType[] {typeof(EcsTestData)}
	        };
	        var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);
	        var count = ArchetypeChunkArray.CalculateEntityCount(chunks);
	        chunks.Dispose();

	        Assert.AreEqual(1, count);

	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	    }
	    
		[Test]
		public void PFB_FindPrefabIfRequestedInComponentGroup()
		{
		    var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Prefab));

		    var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(Prefab));
		    
			var entity0 = m_Manager.CreateEntity(archetype0);
			var entity1 = m_Manager.CreateEntity(archetype1);
			var entity2 = m_Manager.CreateEntity(archetype1);
		    
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(2, arr.Length);

			m_Manager.DestroyEntity(entity0);
			m_Manager.DestroyEntity(entity1);
			m_Manager.DestroyEntity(entity2);
		}
	    
	    [Test]
	    public void PFB_FindPrefabIfRequestedInChunkIterator()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Prefab));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);
	        var entity2 = m_Manager.CreateEntity(archetype1);
	        
	        var query = new EntityArchetypeQuery
	        {
	            Any = Array.Empty<ComponentType>(),
	            None = Array.Empty<ComponentType>(),
	            All = new ComponentType[] {typeof(EcsTestData), typeof(Prefab)}
	        };
	        var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);
	        var count = ArchetypeChunkArray.CalculateEntityCount(chunks);
	        chunks.Dispose();

	        Assert.AreEqual(2, count);

	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	        m_Manager.DestroyEntity(entity2);
	    }
	    
	    [Test]
	    public void PFB_GetAllIncludesPrefab()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Prefab));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);
	        var entity2 = m_Manager.CreateEntity(archetype1);

	        var entities = m_Manager.GetAllEntities();
	        Assert.AreEqual(3,entities.Length);
	        entities.Dispose();
	        
	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	        m_Manager.DestroyEntity(entity2);
	    }
	    
	    [Test]
	    public void PFB_InstantiatedWithoutPrefab()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Prefab));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.Instantiate(entity0);

	        Assert.AreEqual(true, m_Manager.HasComponent<Prefab>(entity0));
	        Assert.AreEqual(false, m_Manager.HasComponent<Prefab>(entity1));
	        
	        var query = new EntityArchetypeQuery
	        {
	            Any = Array.Empty<ComponentType>(),
	            None = Array.Empty<ComponentType>(),
	            All = new ComponentType[] {typeof(EcsTestData)}
	        };
	        var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);
	        var count = ArchetypeChunkArray.CalculateEntityCount(chunks);
	        chunks.Dispose();

	        Assert.AreEqual(1, count);
	        
	        m_Manager.DestroyEntity(entity0);
	        m_Manager.DestroyEntity(entity1);
	    }
	}
}
