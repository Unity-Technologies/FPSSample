using NUnit.Framework;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Entities;

namespace Unity.Entities.Tests
{
	public class IterationTests : ECSTestsFixture
	{
		[Test]
		public void CreateComponentGroup()
		{
			var archetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var group = m_Manager.CreateComponentGroup(typeof(EcsTestData), typeof(EcsTestData2));
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(0, arr.Length);

			var entity = m_Manager.CreateEntity(archetype);
            m_Manager.SetComponentData(entity, new EcsTestData(42));
			arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(1, arr.Length);
			Assert.AreEqual(42, arr[0].value);

			m_Manager.DestroyEntity(entity);
		}

	    struct TempComponentNeverInstantiated : IComponentData
	    {
	        private int m_Internal;
	    }
	    
		[Test]
		public void IterateEmptyArchetype()
		{
			var group = m_Manager.CreateComponentGroup(typeof(TempComponentNeverInstantiated));
			var arr = group.GetComponentDataArray<TempComponentNeverInstantiated>();
			Assert.AreEqual(0, arr.Length);

			var archetype = m_Manager.CreateArchetype(typeof(TempComponentNeverInstantiated));
			arr = group.GetComponentDataArray<TempComponentNeverInstantiated>();
			Assert.AreEqual(0, arr.Length);

			Entity ent = m_Manager.CreateEntity(archetype);
			arr = group.GetComponentDataArray<TempComponentNeverInstantiated>();
			Assert.AreEqual(1, arr.Length);
			m_Manager.DestroyEntity(ent);
			arr = group.GetComponentDataArray<TempComponentNeverInstantiated>();
			Assert.AreEqual(0, arr.Length);
		}
		[Test]
		public void IterateChunkedComponentGroup()
		{
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype2 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(0, arr.Length);

            Entity[] entities = new Entity[10000];
            for (int i = 0; i < entities.Length/2;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype1);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }
            for (int i = entities.Length/2; i < entities.Length;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype2);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }

			arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(entities.Length, arr.Length);
			HashSet<int> values = new HashSet<int>();
            for (int i = 0; i < arr.Length;i++)
			{
				int val = arr[i].value;
				Assert.IsFalse(values.Contains(i));
				Assert.IsTrue(val >= 0);
				Assert.IsTrue(val < entities.Length);
				values.Add(i);
			}

            for (int i = 0; i < entities.Length;i++)
				m_Manager.DestroyEntity(entities[i]);
		}
		[Test]
		public void IterateChunkedComponentGroupBackwards()
		{
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype2 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(0, arr.Length);

            Entity[] entities = new Entity[10000];
            for (int i = 0; i < entities.Length/2;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype1);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }
            for (int i = entities.Length/2; i < entities.Length;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype2);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }

			arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(entities.Length, arr.Length);
			HashSet<int> values = new HashSet<int>();
            for (int i = arr.Length-1; i >= 0;i--)
			{
				int val = arr[i].value;
				Assert.IsFalse(values.Contains(i));
				Assert.IsTrue(val >= 0);
				Assert.IsTrue(val < entities.Length);
				values.Add(i);
			}

            for (int i = 0; i < entities.Length;i++)
				m_Manager.DestroyEntity(entities[i]);
		}



		[Test]
		public void IterateChunkedComponentGroupAfterDestroy()
		{
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype2 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
			var arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(0, arr.Length);

            Entity[] entities = new Entity[10000];
            for (int i = 0; i < entities.Length/2;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype1);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }
            for (int i = entities.Length/2; i < entities.Length;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype2);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }
            for (int i = 0; i < entities.Length;i++)
			{
				if (i%2 != 0)
				{
					m_Manager.DestroyEntity(entities[i]);
				}
			}

			arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(entities.Length/2, arr.Length);
			HashSet<int> values = new HashSet<int>();
            for (int i = 0; i < arr.Length;i++)
			{
				int val = arr[i].value;
				Assert.IsFalse(values.Contains(i));
				Assert.IsTrue(val >= 0);
				Assert.IsTrue(val%2 == 0);
				Assert.IsTrue(val < entities.Length);
				values.Add(i);
			}

            for (int i = entities.Length/2; i < entities.Length;i++)
            {
				if (i%2 == 0)
					m_Manager.RemoveComponent<EcsTestData>(entities[i]);
            }
			arr = group.GetComponentDataArray<EcsTestData>();
			Assert.AreEqual(entities.Length/4, arr.Length);
			values = new HashSet<int>();
            for (int i = 0; i < arr.Length;i++)
			{
				int val = arr[i].value;
				Assert.IsFalse(values.Contains(i));
				Assert.IsTrue(val >= 0);
				Assert.IsTrue(val%2 == 0);
				Assert.IsTrue(val < entities.Length/2);
				values.Add(i);
			}

            for (int i = 0; i < entities.Length;i++)
			{
				if (i%2 == 0)
					m_Manager.DestroyEntity(entities[i]);
			}
		}


		[Test]
		public void IterateEntityArray()
		{
			var archetype1 = m_Manager.CreateArchetype(typeof(EcsTestData));
			var archetype2 = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

			var group = m_Manager.CreateComponentGroup(typeof(EcsTestData));
			var arr = group.GetEntityArray();
			Assert.AreEqual(0, arr.Length);

            Entity[] entities = new Entity[10000];
            for (int i = 0; i < entities.Length/2;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype1);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }
            for (int i = entities.Length/2; i < entities.Length;i++)
            {
				entities[i] = m_Manager.CreateEntity(archetype2);
				m_Manager.SetComponentData(entities[i], new EcsTestData(i));
            }

			arr = group.GetEntityArray();
			Assert.AreEqual(entities.Length, arr.Length);
			var values = new HashSet<Entity>();
            for (int i = 0; i < arr.Length;i++)
			{
				Entity val = arr[i];
				Assert.IsFalse(values.Contains(val));
				values.Add(val);
			}

            for (int i = 0; i < entities.Length;i++)
				m_Manager.DestroyEntity(entities[i]);
		}

        [Test]
        public void ComponentDataArrayCopy()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));

            var entities = new NativeArray<Entity>(20000, Allocator.Persistent);
            m_Manager.Instantiate(entity, entities);

            var ecsArray = m_Manager.CreateComponentGroup(typeof(EcsTestData)).GetComponentDataArray<EcsTestData>();

            for (int i = 0; i < ecsArray.Length; i++)
                ecsArray[i] = new EcsTestData(i);

            var copied = new NativeArray<EcsTestData>(entities.Length - 11 + 1, Allocator.Persistent);
            ecsArray.CopyTo(copied, 11);

            for (int i = 0; i < copied.Length; i++)
            {
                if (copied[i].value != i)
                    Assert.AreEqual(i + 11, copied[i].value);
            }

            copied.Dispose();
            entities.Dispose();
        }
		
	}
}