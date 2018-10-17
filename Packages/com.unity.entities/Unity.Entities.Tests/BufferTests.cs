using NUnit.Framework;
using Unity.Collections;
using System;
using Unity.Entities;
using Unity.Jobs;
using UnityEditor.Experimental.UIElements;

namespace Unity.Entities.Tests
{
	public class BufferTests : ECSTestsFixture
	{
        [InternalBufferCapacity(1024*1024)]
        public struct OverSizedCapacity : IBufferElementData
        {
            public int Value;
        }

		[Test]
		public void BufferTypeClassificationWorks()
		{
            var t  = TypeManager.GetTypeInfo<EcsIntElement>();
            Assert.AreEqual(TypeManager.TypeCategory.BufferData, t.Category);
            Assert.AreEqual(8, t.BufferCapacity);
		}

		[Test]
		public void BufferComponentTypeCreationWorks()
		{
            var bt = ComponentType.Create<EcsIntElement>();
            Assert.AreEqual(ComponentType.AccessMode.ReadWrite, bt.AccessModeType);
            Assert.AreEqual(8, bt.BufferCapacity);
		}

		[Test]
		public void CreateEntityWithIntThrows()
		{
			Assert.Throws<System.ArgumentException>(() => { m_Manager.CreateEntity(typeof(int));});
		}

		[Test]
		public void AddComponentWithIntThrows()
		{
			var entity = m_Manager.CreateEntity();
			Assert.Throws<System.ArgumentException>(() => { m_Manager.AddComponent(entity, ComponentType.Create<int>()); });
		}

		[Test]
		// Invalid because chunk size is too small to hold a single entity
		public void CreateEntityWithInvalidInternalCapacity()
		{
            var arrayType = ComponentType.Create<OverSizedCapacity>();
			Assert.Throws<ArgumentException>(() => m_Manager.CreateEntity(arrayType));
		}

		[Test]
		public void HasComponent()
		{
            var arrayType = ComponentType.Create<EcsIntElement>();
			var entity = m_Manager.CreateEntity(arrayType);
            Assert.IsTrue(m_Manager.HasComponent(entity, arrayType));
		}

		[Test]
		public void InitialCapacityWorks()
		{
            var arrayType = ComponentType.Create<EcsIntElement>();
			var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(8, buffer.Capacity);
		}

		[Test]
		public void InitialCapacityWorks2()
		{
			var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(8, buffer.Capacity);
		}

		[Test]
		public void AddWorks()
		{
            var arrayType = ComponentType.Create<EcsIntElement>();
			var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 189; ++i)
                buffer.Add(i);

            Assert.AreEqual(189, buffer.Length);
            for (int i = 0; i < 189; ++i)
            {
                Assert.AreEqual(i, buffer[i].Value);
            }
		}

		[Test]
		public void AddRangeWorks()
		{
            var arrayType = ComponentType.Create<EcsIntElement>();
			var entity = m_Manager.CreateEntity(arrayType);
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 7; ++i)
                buffer.Add(i);

            Assert.AreEqual(7, buffer.Length);

            var blah = new NativeArray<EcsIntElement>(1024, Allocator.Temp);

            for (int i = 0; i < blah.Length; ++i)
            {
                blah[i] = i;
            }

            buffer.AddRange(blah);
            blah.Dispose();

            Assert.AreEqual(1024 + 7, buffer.Length);

            for (int i = 0; i < 7; ++i)
                Assert.AreEqual(i, buffer[i].Value);
            for (int i = 0; i < 1024; ++i)
                Assert.AreEqual(i, buffer[7 + i].Value);
		}

		[Test]
		public void RemoveAtWorks()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveAt(7);

            CheckBufferContents(buffer, new int[] { 0, 1, 2, 3, 4, 5, 6, 8 });
        }

        private static void CheckBufferContents(DynamicBuffer<EcsIntElement> buffer, int[] refs)
        {
            Assert.AreEqual(refs.Length, buffer.Length);

            for (int i = 0; i < refs.Length; ++i)
            {
                Assert.AreEqual(refs[i], buffer[i].Value);
            }
        }

        [Test]
		public void RemoveAtWorksFromStart()
		{
			var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveAt(0);

            CheckBufferContents(buffer, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 });
		}

		[Test]
		public void RemoveAtWorksFromEnd()
		{
			var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveAt(8);
            buffer.RemoveAt(7);

            CheckBufferContents(buffer, new int[] { 0, 1, 2, 3, 4, 5, 6 });
		}

		[Test]
		public void RemoveRangeWorksFromEnd()
		{
			var entity = m_Manager.CreateEntity(typeof(EcsIntElement));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            for (int i = 0; i < 9; ++i)
                buffer.Add(i);

            buffer.RemoveRange(5, 4);

            CheckBufferContents(buffer, new int[] { 0, 1, 2, 3, 4 });
		}

		[Test]
		public void InitialCapacityWorksWithAddComponment()
		{
			var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent(entity, ComponentType.Create<EcsIntElement>());
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            Assert.AreEqual(8, buffer.Capacity);
		}

		[Test]
		public void RemoveComponent()
		{
            var arrayType = ComponentType.Create<EcsIntElement>();
			var entity = m_Manager.CreateEntity(arrayType);
            Assert.IsTrue(m_Manager.HasComponent(entity, arrayType));
            m_Manager.RemoveComponent(entity, arrayType);
            Assert.IsFalse(m_Manager.HasComponent(entity, arrayType));
		}

		[Test]
		public void MutateBufferData()
		{
			var entity = m_Manager.CreateEntity();
            m_Manager.AddBuffer<EcsIntElement>(entity);

			var array = m_Manager.GetBuffer<EcsIntElement>(entity);
			Assert.AreEqual(0, array.Length);

            using (var array2 = new NativeArray<EcsIntElement>(6, Allocator.Temp))
            {
                array.CopyFrom(array2);

                Assert.AreEqual(6, array.Length);

                array[3] = 5;
                Assert.AreEqual(5, array[3].Value);
                Assert.AreNotEqual(5, array2[3].Value); // no aliasing
            }
		}

        [Test]
        public void BufferComponentGroupIteration()
        {
            /*var entity64 =*/
            m_Manager.CreateEntity(typeof(EcsIntElement));
            /*var entity10 =*/
            m_Manager.CreateEntity(typeof(EcsIntElement));

            var group = m_Manager.CreateComponentGroup(typeof(EcsIntElement));

            var buffers = group.GetBufferArray<EcsIntElement>();

            Assert.AreEqual(2, buffers.Length);
            Assert.AreEqual(0, buffers[0].Length);
            Assert.AreEqual(8, buffers[0].Capacity);
            Assert.AreEqual(0, buffers[1].Length);
            Assert.AreEqual(8, buffers[1].Capacity);

            buffers[0].Add(12);
            buffers[0].Add(13);

            Assert.AreEqual(2, buffers[0].Length);
	        Assert.AreEqual(12, buffers[0][0].Value);
            Assert.AreEqual(13, buffers[0][1].Value);

            Assert.AreEqual(0, buffers[1].Length);
        }

        [Test]
		public void BufferFromEntityWorks()
		{
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
            m_Manager.GetBuffer<EcsIntElement>(entityInt).CopyFrom(new EcsIntElement[] { 1, 2, 3 });

			var intLookup = EmptySystem.GetBufferFromEntity<EcsIntElement>();
			Assert.IsTrue(intLookup.Exists(entityInt));
			Assert.IsFalse(intLookup.Exists(new Entity()));

			Assert.AreEqual(2, intLookup[entityInt][1].Value);
		}

        [Test]
        public void OutOfBoundsAccessThrows()
        {
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var intArray = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            intArray.Add(12);
            m_Manager.DestroyEntity(entityInt);

            Assert.Throws<InvalidOperationException>(() =>
            {
                intArray.Add(123);
            });
        }

        [Test]
        public void UseAfterStructuralChangeThrows()
        {
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var intArray = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            m_Manager.DestroyEntity(entityInt);

            Assert.Throws<InvalidOperationException>(() =>
            {
                intArray.Add(123);
            });
        }

        [Test]
        public void UseAfterStructuralChangeThrows2()
        {
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var buffer = m_Manager.GetBufferFromEntity<EcsIntElement>();
            var array = buffer[entityInt];
            m_Manager.DestroyEntity(entityInt);

            Assert.Throws<InvalidOperationException>(() =>
            {
                array.Add(123);
            });
        }

	    [Test]
	    public void UseAfterStructuralChangeThrows3()
	    {
	        var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
	        buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
	        m_Manager.AddComponentData(entityInt, new EcsTestData() { value = 20 });
	        Assert.Throws<InvalidOperationException>(() => { buffer.Add(4); });
	    }


        [Test]
        public void WritingReadOnlyThrows()
        {
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var buffer = m_Manager.GetBufferFromEntity<EcsIntElement>(true);
            var array = buffer[entityInt];
            Assert.Throws<InvalidOperationException>(() =>
            {
                array.Add(123);
            });
        }

        [Test]
        public void ReinterpretWorks()
        {
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var intBuffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            var floatBuffer = intBuffer.Reinterpret<float>();

            intBuffer.Add(0x3f800000);
            floatBuffer.Add(-1.0f);

            Assert.AreEqual(2, intBuffer.Length);
            Assert.AreEqual(2, floatBuffer.Length);

            Assert.AreEqual(0x3f800000, intBuffer[0].Value);
            Assert.AreEqual(1.0f, floatBuffer[0]);
            Assert.AreEqual(0xbf800000u, (uint)intBuffer[1].Value);
            Assert.AreEqual(-1.0f, floatBuffer[1]);
        }

        [Test]
        public void ReinterpretWrongSizeThrows()
        {
			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var buffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.Reinterpret<ushort>();
            });
        }

        [DisableAutoCreation]
        public class InjectionTestSystem : JobComponentSystem
        {
            public struct Data
            {
                public readonly int Length;
                public BufferArray<EcsIntElement> Buffers;
            }

            [Inject] Data m_Data;

            public struct MyJob : IJobParallelFor
            {
                public BufferArray<EcsIntElement> Buffers;

                public void Execute(int i)
                {
                    Buffers[i].Add(i * 3);
                }
            }

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                new MyJob { Buffers = m_Data.Buffers }.Schedule(m_Data.Length, 32, inputDeps).Complete();
                return default(JobHandle);
            }
        }

        [Test]
        public void Injection()
        {
            var system = World.Active.GetOrCreateManager<InjectionTestSystem>();

            using (var entities = new NativeArray<Entity>(320, Allocator.Temp))
            {
                var arch = m_Manager.CreateArchetype(typeof(EcsIntElement));
                m_Manager.CreateEntity(arch, entities);

                system.Update();
                system.Update();

                for (var i = 0; i < entities.Length; ++i)
                {
                    var buf = m_Manager.GetBuffer<EcsIntElement>(entities[i]);
                    Assert.AreEqual(2, buf.Length);
                }
            }
        }

        [Test]
        public void TrimExcessWorks()
        {

			var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
			var intBuffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);

            Assert.AreEqual(0, intBuffer.Length);
            Assert.AreEqual(8, intBuffer.Capacity);

            intBuffer.CopyFrom(new EcsIntElement[] { 0, 1, 2, 3 });

            intBuffer.TrimExcess();

            Assert.AreEqual(4, intBuffer.Length);
            Assert.AreEqual(8, intBuffer.Capacity);

            for (int i = 4; i < 10; ++i)
            {
                intBuffer.Add(i);
            }

            Assert.AreEqual(10, intBuffer.Length);
            Assert.AreEqual(16, intBuffer.Capacity);

            intBuffer.TrimExcess();

            Assert.AreEqual(10, intBuffer.Length);
            Assert.AreEqual(10, intBuffer.Capacity);

            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(i, intBuffer[i].Value);
            }
        }

	    [Test]
	    public void BufferSurvivesArchetypeChange()
	    {
	        var entityInt = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(entityInt);
	        buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });

	        m_Manager.AddComponentData(entityInt, new EcsTestData() { value = 20 });

	        CheckBufferContents(m_Manager.GetBuffer<EcsIntElement>(entityInt), new int[] { 1, 2, 3 });
	    }

	    [Test]
	    public unsafe void InstantiateCreatesCopyOverflow()
	    {
	        var original = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }); // greater than 8

	        var clone = m_Manager.Instantiate(original);

	        buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        var buffer2 = m_Manager.GetBuffer<EcsIntElement>(clone);

	        Assert.AreNotEqual((UIntPtr)buffer.GetBasePointer(), (UIntPtr)buffer2.GetBasePointer());
	        Assert.AreEqual(buffer.Length, buffer2.Length);
	        for (int i = 0; i < buffer.Length; ++i)
	        {
	            Assert.AreEqual(buffer[i].Value, buffer2[i].Value);
	        }
	    }

	    [Test]
	    public unsafe void InstantiateCreatesCopyInternal()
	    {
	        var original = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 }); // smaller than 8

	        var clone = m_Manager.Instantiate(original);

	        buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        var buffer2 = m_Manager.GetBuffer<EcsIntElement>(clone);

	        Assert.AreNotEqual((UIntPtr)buffer.GetBasePointer(), (UIntPtr)buffer2.GetBasePointer());
	        Assert.AreEqual(buffer.Length, buffer2.Length);
	        for (int i = 0; i < buffer.Length; ++i)
	        {
	            Assert.AreEqual(buffer[i].Value, buffer2[i].Value);
	        }
	    }

	    internal struct ElementWithoutCapacity : IBufferElementData
	    {
	        public float Value;
	    }

	    [Test]
	    public void NoCapacitySpecifiedWorks()
	    {
	        var original = m_Manager.CreateEntity(typeof(ElementWithoutCapacity));
	        var buffer = m_Manager.GetBuffer<ElementWithoutCapacity>(original);
	        Assert.AreEqual(buffer.Capacity, 32);
	    }

	    [Test]
	    public void ArrayInvalidationWorks()
	    {
	        var original = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        buffer.Add(1);
	        var array = buffer.ToNativeArray();
	        Assert.AreEqual(1, array[0].Value);
	        Assert.AreEqual(1, array.Length);
	        buffer.Add(2);
	        Assert.Throws<InvalidOperationException>(() =>
	        {
	            int value = array[0].Value;
	        });
	    }

	    [Test]
	    public void ArrayInvalidationHappensForAllInstances()
	    {
	        var e0 = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var e1 = m_Manager.CreateEntity(typeof(EcsIntElement));

	        var b0 = m_Manager.GetBuffer<EcsIntElement>(e0);
	        var b1 = m_Manager.GetBuffer<EcsIntElement>(e1);

	        b0.Add(1);
	        b1.Add(1);

	        var a0 = b0.ToNativeArray();
	        var a1 = b1.ToNativeArray();

	        b0.Add(1);

	        Assert.Throws<InvalidOperationException>(() =>
	        {
	            int value = a0[0].Value;
	        });

	        Assert.Throws<InvalidOperationException>(() =>
	        {
	            int value = a1[0].Value;
	        });
	    }

	    [Test]
	    public void ArraysAreNotInvalidateByWrites()
	    {
	        var original = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        buffer.Add(1);
	        var array = buffer.ToNativeArray();
	        Assert.AreEqual(1, array[0].Value);
	        Assert.AreEqual(1, array.Length);
	        buffer[0] = 2;
	        Assert.AreEqual(2, array[0].Value);
	    }

	    struct ArrayConsumingJob : IJob
	    {
	        public NativeArray<EcsIntElement> Array;

	        public void Execute()
	        {
	        }
	    }

	    [Test]
	    public void BufferInvalidationNotPossibleWhenArraysAreGivenToJobs()
	    {
	        var original = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        buffer.Add(1);
	        var handle = new ArrayConsumingJob {Array = buffer.ToNativeArray()}.Schedule();
	        Assert.Throws<InvalidOperationException>(() => buffer.Add(2));
	        Assert.Throws<InvalidOperationException>(() => m_Manager.DestroyEntity(original));
	        handle.Complete();
	    }

	    struct BufferConsumingJob : IJob
	    {
	        public DynamicBuffer<EcsIntElement> Buffer;

	        public void Execute()
	        {
	        }
	    }

	    [Test]
	    public void BufferInvalidationNotPossibleWhenBuffersAreGivenToJobs()
	    {
	        var original = m_Manager.CreateEntity(typeof(EcsIntElement));
	        var buffer = m_Manager.GetBuffer<EcsIntElement>(original);
	        buffer.Add(1);
	        var handle = new BufferConsumingJob {Buffer = buffer}.Schedule();
	        Assert.Throws<InvalidOperationException>(() => buffer.Add(2));
	        Assert.Throws<InvalidOperationException>(() => m_Manager.DestroyEntity(original));
	        handle.Complete();
	    }
	}
}
