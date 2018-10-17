using System;
using NUnit.Framework;
using Unity.Collections;

namespace Unity.Entities.Tests
{
	public class DisableComponentTests : ECSTestsFixture
	{
		[Test]
		public void DIS_DontFindDisabledInComponentGroup()
		{
		    var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

		    var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
		    
			var entity0 = m_Manager.CreateEntity(archetype0);
			var entity1 = m_Manager.CreateEntity(archetype1);
		    
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(1, arr.Length);

			m_Manager.DestroyEntity(entity0);
			m_Manager.DestroyEntity(entity1);
		}
	    
	    [Test]
	    public void DIS_DontFindDisabledInChunkIterator()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

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
		public void DIS_FindDisabledIfRequestedInComponentGroup()
		{
		    var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

		    var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(Disabled));
		    
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
	    public void DIS_FindDisabledIfRequestedInChunkIterator()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

	        var entity0 = m_Manager.CreateEntity(archetype0);
	        var entity1 = m_Manager.CreateEntity(archetype1);
	        var entity2 = m_Manager.CreateEntity(archetype1);
	        
	        var query = new EntityArchetypeQuery
	        {
	            Any = Array.Empty<ComponentType>(),
	            None = Array.Empty<ComponentType>(),
	            All = new ComponentType[] {typeof(EcsTestData), typeof(Disabled)}
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
	    public void DIS_GetAllIncludesDisabled()
	    {
	        var archetype0 = m_Manager.CreateArchetype(typeof(EcsTestData));
	        var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(Disabled));

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
	}
}
