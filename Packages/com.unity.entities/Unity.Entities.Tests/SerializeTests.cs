using NUnit.Framework;
using Unity.Collections;
using System;
using Unity.Entities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Serialization;

namespace Unity.Entities.Tests
{
    public unsafe class TestBinaryReader : BinaryReader
    {
        NativeList<byte> content;
        int position = 0;
        public TestBinaryReader(TestBinaryWriter writer)
        {
            content = writer.content;
            writer.content = new NativeList<byte>();

        }

        public void Dispose()
        {
            content.Dispose();
        }

        public void ReadBytes(void* data, int bytes)
        {
            UnsafeUtility.MemCpy(data, (byte*)content.GetUnsafePtr() + position, bytes);
            position += bytes;
        }
    }

    public unsafe class TestBinaryWriter : BinaryWriter
    {
        internal NativeList<byte> content = new NativeList<byte>(Allocator.Temp);

        public void Dispose()
        {
            content.Dispose();
        }

        public void WriteBytes(void* data, int bytes)
        {
            int length = content.Length;
            content.ResizeUninitialized(length + bytes);
            UnsafeUtility.MemCpy((byte*)content.GetUnsafePtr() + length, data, bytes);
        }

    }


    public class SerializeTests : ECSTestsFixture
    {
        public struct TestComponentData1 : IComponentData
        {
            public int value;
            public Entity referencedEntity;
        }

        public struct TestComponentData2 : IComponentData
        {
            public int value;
            public Entity referencedEntity;
        }


        [Test]
        public void SerializeIntoExistingWorldThrows()
        {
            m_Manager.CreateEntity(typeof(EcsTestData));
            
            var writer = new TestBinaryWriter();
            int[] sharedData;
            SerializeUtility.SerializeWorld(m_Manager, writer, out sharedData);
            
            var reader = new TestBinaryReader(writer);

            Assert.Throws<ArgumentException>(()=>
                SerializeUtility.DeserializeWorld(m_Manager.BeginExclusiveEntityTransaction(), reader)
            );
            reader.Dispose();
        }

        [Test]
        public unsafe void SerializeEntities()
        {
            var dummyEntity = CreateEntityWithDefaultData(0); //To ensure entity indices are offset
            var e1 = CreateEntityWithDefaultData(1);
            var e2 = CreateEntityWithDefaultData(2);
            var e3 = CreateEntityWithDefaultData(3);
            m_Manager.AddComponentData(e1, new TestComponentData1{ value = 10, referencedEntity = e2 });
            m_Manager.AddComponentData(e2, new TestComponentData2{ value = 20, referencedEntity = e1 });
            m_Manager.AddComponentData(e3, new TestComponentData1{ value = 30, referencedEntity = Entity.Null });
            m_Manager.AddComponentData(e3, new TestComponentData2{ value = 40, referencedEntity = Entity.Null });
            m_Manager.AddBuffer<EcsIntElement>(e1);
            m_Manager.RemoveComponent<EcsTestData2>(e3);
            m_Manager.AddBuffer<EcsIntElement>(e3);

            m_Manager.GetBuffer<EcsIntElement>(e1).CopyFrom(new EcsIntElement[] { 1, 2, 3 }); // no overflow
            m_Manager.GetBuffer<EcsIntElement>(e3).CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }); // overflow into heap

            var e4 = m_Manager.CreateEntity();
            m_Manager.AddBuffer<EcsComplexEntityRefElement>(e4);
            var ebuf = m_Manager.GetBuffer<EcsComplexEntityRefElement>(e4);
            ebuf.Add(new EcsComplexEntityRefElement { Entity = e1, Dummy = 1 });
            ebuf.Add(new EcsComplexEntityRefElement { Entity = e2, Dummy = 2 });
            ebuf.Add(new EcsComplexEntityRefElement { Entity = e3, Dummy = 3 });

            m_Manager.DestroyEntity(dummyEntity);
            var writer = new TestBinaryWriter();

            int[] sharedData;
            SerializeUtility.SerializeWorld(m_Manager, writer, out sharedData);
            var reader = new TestBinaryReader(writer);

            var deserializedWorld = new World("SerializeEntities Test World 3");
            var entityManager = deserializedWorld.GetOrCreateManager<EntityManager>();

            SerializeUtility.DeserializeWorld(entityManager.BeginExclusiveEntityTransaction(), reader);
            entityManager.EndExclusiveEntityTransaction();

            try
            {
                var allEntities = entityManager.GetAllEntities(Allocator.Temp);
                var count = allEntities.Length;
                allEntities.Dispose();

                Assert.AreEqual(4, count);

                var group1 = entityManager.CreateComponentGroup(typeof(EcsTestData), typeof(EcsTestData2),
                    typeof(TestComponentData1));
                var group2 = entityManager.CreateComponentGroup(typeof(EcsTestData), typeof(EcsTestData2),
                    typeof(TestComponentData2));
                var group3 = entityManager.CreateComponentGroup(typeof(EcsTestData),
                    typeof(TestComponentData1), typeof(TestComponentData2));
                var group4 = entityManager.CreateComponentGroup(typeof(EcsComplexEntityRefElement));

                Assert.AreEqual(1, group1.CalculateLength());
                Assert.AreEqual(1, group2.CalculateLength());
                Assert.AreEqual(1, group3.CalculateLength());
                Assert.AreEqual(1, group4.CalculateLength());

                var query = new EntityArchetypeQuery
                {
                    All = Array.Empty<ComponentType>(),
                    Any = Array.Empty<ComponentType>(),
                    None = Array.Empty<ComponentType>()
                };

                var archetypes = new NativeList<EntityArchetype>(Allocator.TempJob);
                entityManager.AddMatchingArchetypes(query, archetypes);
                Assert.AreEqual(4, archetypes.Length);

                var chunks = entityManager.CreateArchetypeChunkArray(archetypes, Allocator.TempJob);
                Assert.AreEqual(4, chunks.Length);
                archetypes.Dispose();

                var entityType = entityManager.GetArchetypeChunkEntityType();
                Assert.AreEqual(1, chunks[0].GetNativeArray(entityType).Length);
                Assert.AreEqual(1, chunks[1].GetNativeArray(entityType).Length);
                Assert.AreEqual(1, chunks[2].GetNativeArray(entityType).Length);
                Assert.AreEqual(1, chunks[3].GetNativeArray(entityType).Length);
                chunks.Dispose();

                var new_e1 = group1.GetEntityArray()[0];
                var new_e2 = group2.GetEntityArray()[0];
                var new_e3 = group3.GetEntityArray()[0];
                var new_e4 = group4.GetEntityArray()[0];

                Assert.AreEqual(1, entityManager.GetComponentData<EcsTestData>(new_e1).value);
                Assert.AreEqual(-1, entityManager.GetComponentData<EcsTestData2>(new_e1).value0);
                Assert.AreEqual(-1, entityManager.GetComponentData<EcsTestData2>(new_e1).value1);
                Assert.AreEqual(10, entityManager.GetComponentData<TestComponentData1>(new_e1).value);

                Assert.AreEqual(2, entityManager.GetComponentData<EcsTestData>(new_e2).value);
                Assert.AreEqual(-2, entityManager.GetComponentData<EcsTestData2>(new_e2).value0);
                Assert.AreEqual(-2, entityManager.GetComponentData<EcsTestData2>(new_e2).value1);
                Assert.AreEqual(20, entityManager.GetComponentData<TestComponentData2>(new_e2).value);

                Assert.AreEqual(3, entityManager.GetComponentData<EcsTestData>(new_e3).value);
                Assert.AreEqual(30, entityManager.GetComponentData<TestComponentData1>(new_e3).value);
                Assert.AreEqual(40, entityManager.GetComponentData<TestComponentData2>(new_e3).value);

                Assert.IsTrue(entityManager.Exists(entityManager.GetComponentData<TestComponentData1>(new_e1).referencedEntity));
                Assert.IsTrue(entityManager.Exists(entityManager.GetComponentData<TestComponentData2>(new_e2).referencedEntity));
                Assert.AreEqual(new_e2 , entityManager.GetComponentData<TestComponentData1>(new_e1).referencedEntity);
                Assert.AreEqual(new_e1 , entityManager.GetComponentData<TestComponentData2>(new_e2).referencedEntity);

                var buf1 = entityManager.GetBuffer<EcsIntElement>(new_e1);
                Assert.AreEqual(3, buf1.Length);
                Assert.AreNotEqual((UIntPtr)m_Manager.GetBuffer<EcsIntElement>(e1).GetBasePointer(), (UIntPtr)buf1.GetBasePointer());

                for (int i = 0; i < 3; ++i)
                {
                    Assert.AreEqual(i + 1, buf1[i].Value);
                }

                var buf3 = entityManager.GetBuffer<EcsIntElement>(new_e3);
                Assert.AreEqual(10, buf3.Length);
                Assert.AreNotEqual((UIntPtr)m_Manager.GetBuffer<EcsIntElement>(e3).GetBasePointer(), (UIntPtr)buf3.GetBasePointer());

                for (int i = 0; i < 10; ++i)
                {
                    Assert.AreEqual(i + 1, buf3[i].Value);
                }

                var buf4 = entityManager.GetBuffer<EcsComplexEntityRefElement>(new_e4);
                Assert.AreEqual(3, buf4.Length);

                Assert.AreEqual(1, buf4[0].Dummy);
                Assert.AreEqual(new_e1, buf4[0].Entity);

                Assert.AreEqual(2, buf4[1].Dummy);
                Assert.AreEqual(new_e2, buf4[1].Entity);

                Assert.AreEqual(3, buf4[2].Dummy);
                Assert.AreEqual(new_e3, buf4[2].Entity);
            }
            finally
            {
                deserializedWorld.Dispose();
                reader.Dispose();
            }
        }
    }
}
