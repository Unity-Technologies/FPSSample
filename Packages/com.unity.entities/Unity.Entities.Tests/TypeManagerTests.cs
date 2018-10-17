using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    public class TypeManagerTests : ECSTestsFixture
	{
        struct TestType1 : IComponentData
		{
#pragma warning disable 0169 // "never used" warning
			int empty;
#pragma warning restore 0169
		}
		struct TestType2 : IComponentData
		{
#pragma warning disable 0169 // "never used" warning
			int empty;
#pragma warning restore 0169
		}
		[Test]
		public void CreateArchetypes()
		{
            var archetype1 = m_Manager.CreateArchetype(ComponentType.Create<TestType1>(), ComponentType.Create<TestType2>());
            var archetype1Same = m_Manager.CreateArchetype(ComponentType.Create<TestType1>(), ComponentType.Create<TestType2>());
            Assert.AreEqual(archetype1, archetype1Same);

            var archetype2 = m_Manager.CreateArchetype(ComponentType.Create<TestType1>());
            var archetype2Same = m_Manager.CreateArchetype(ComponentType.Create<TestType1>());
            Assert.AreEqual(archetype2Same, archetype2Same);

            Assert.AreNotEqual(archetype1, archetype2);
		}

        [InternalBufferCapacity(99)]
        public struct IntElement : IBufferElementData
        {
            public int Value;
        }

		[Test]
		public void BufferTypeClassificationWorks()
		{
            var t  = TypeManager.GetTypeInfo<IntElement>();
            Assert.AreEqual(TypeManager.TypeCategory.BufferData, t.Category);
            Assert.AreEqual(99, t.BufferCapacity);
            Assert.AreEqual(UnsafeUtility.SizeOf<BufferHeader>() + 99 * sizeof(int), t.SizeInChunk);
		}

        [Test]
        public void TestTypeManager()
        {
            var entity = ComponentType.Create<Entity>();
            var testData = ComponentType.Create<EcsTestData>();

            Assert.AreEqual(entity, ComponentType.Create<Entity>());
            Assert.AreEqual(entity, new ComponentType(typeof(Entity)));
            Assert.AreEqual(testData, ComponentType.Create<EcsTestData>());
            Assert.AreEqual(testData, new ComponentType(typeof(EcsTestData)));
            Assert.AreNotEqual(ComponentType.Create<Entity>(), ComponentType.Create<EcsTestData>());
            Assert.AreNotEqual(entity, ComponentType.ReadOnly<EcsTestData>());

            Assert.AreEqual(typeof(Entity), entity.GetManagedType());
        }

		struct NonBlittableComponentData : IComponentData
		{
#pragma warning disable 0169 // "never used" warning
			string empty;
#pragma warning restore 0169
		}

	    class ClassComponentData : IComponentData
	    {
	    }

	    interface InterfaceComponentData : IComponentData
	    {

	    }

	    struct NonBlittableBuffer: IBufferElementData
	    {
#pragma warning disable 0169 // "never used" warning
	        string empty;
#pragma warning restore 0169
	    }

	    class ClassBuffer: IBufferElementData
	    {
	    }

	    interface InterfaceBuffer : IBufferElementData
	    {

	    }

	    class ClassShared : ISharedComponentData
	    {
	    }

	    interface InterfaceShared : ISharedComponentData
	    {

	    }

	    [Test]
	    public void ComponentDataConstraints()
	    {
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<NonBlittableComponentData>(); });
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<ClassComponentData>(); });
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<InterfaceComponentData>(); });
	    }

	    [Test]
	    public void BufferConstraints()
	    {
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<NonBlittableBuffer>(); });
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<ClassBuffer>(); });
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<InterfaceBuffer>(); });
	    }


	    [Test]
	    public void SharedComponentConstraints()
	    {
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<ClassShared>(); });
	        Assert.Throws<System.ArgumentException>(() => { ComponentType.Create<InterfaceShared>(); });
	    }
    }
}
