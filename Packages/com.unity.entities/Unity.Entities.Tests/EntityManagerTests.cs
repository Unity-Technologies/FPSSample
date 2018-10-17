using Unity.Collections;
using NUnit.Framework;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    interface IEcsFooInterface
    {
        int value { get; set; }

    }
    public struct EcsFooTest : IComponentData, IEcsFooInterface
    {
        public int value { get; set; }

        public EcsFooTest(int inValue) { value = inValue; }
    }

    interface IEcsBarInterface
    {
        int value { get; set; }

    }
    public struct EcsBarTest : IComponentData, IEcsBarInterface
    {
        public int value { get; set; }

        public EcsBarTest(int inValue) { value = inValue; }
    }

    public class EntityManagerTests : ECSTestsFixture
    {
        [Test]
        public void IncreaseEntityCapacity()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));
            var count = 1024;
            var array = new NativeArray<Entity>(count, Allocator.Temp);
            m_Manager.CreateEntity (archetype, array);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(i, array[i].Index);
            }
            array.Dispose();
        }

        [Test]
        public void FoundComponentInterface()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData),typeof(EcsFooTest));
            var count = 1024;
            var array = new NativeArray<Entity>(count, Allocator.Temp);
            m_Manager.CreateEntity (archetype, array);

            var fooTypes = m_Manager.GetAssignableComponentTypes(typeof(IEcsFooInterface));
            Assert.AreEqual(1,fooTypes.Count);
            Assert.AreEqual(typeof(EcsFooTest),fooTypes[0]);

            var barTypes = m_Manager.GetAssignableComponentTypes(typeof(IEcsBarInterface));
            Assert.AreEqual(0,barTypes.Count);

            array.Dispose();
        }

        [Test]
        public void VersionIsConsistent()
        {
            Assert.AreEqual(0, m_Manager.Version);
            
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            Assert.AreEqual(1, m_Manager.Version);
            
            m_Manager.AddComponentData(entity, new EcsTestData2(0));
            Assert.AreEqual(2, m_Manager.Version);
            
            m_Manager.SetComponentData(entity, new EcsTestData2(5));
            Assert.AreEqual(2, m_Manager.Version); // Shouldn't change when just setting data
            
            m_Manager.RemoveComponent<EcsTestData2>(entity);
            Assert.AreEqual(3, m_Manager.Version);
            
            m_Manager.DestroyEntity(entity);
            Assert.AreEqual(4, m_Manager.Version);
        }

        [Test]
        [Ignore("NOT IMPLEMENTED")]
        public void UsingComponentGroupOrArchetypeorEntityFromDifferentEntityManagerGivesExceptions()
        {
        }
    }
}
