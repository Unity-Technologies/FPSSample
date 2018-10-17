using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Unity.Entities.Tests
{
    [TestFixture]
    public class ArchetypeChunkArrayTest : ECSTestsFixture
    {
        public Entity CreateEntity(int value, int sharedValue)
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestSharedComp), typeof(EcsIntElement));
            m_Manager.SetComponentData(entity, new EcsTestData(value));
            m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp(sharedValue));
            var buffer = m_Manager.GetBuffer<EcsIntElement>(entity);
            buffer.ResizeUninitialized(value);
            for (int i = 0; i < value; ++i)
            {
                buffer[i] = i;
            }
            return entity;
        }

        public Entity CreateEntity2(int value, int sharedValue)
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData2), typeof(EcsTestSharedComp));
            m_Manager.SetComponentData(entity, new EcsTestData2(value));
            m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp(sharedValue));
            return entity;
        }

        void CreateEntities(int count)
        {
            for (int i = 0; i != count; i++)
                CreateEntity(i, i % 7);
        }

        void CreateMixedEntities(int count)
        {
            for (int i = 0; i != count; i++)
            {
                if ((i & 1) == 0)
                    CreateEntity(i, i % 7);
                else
                    CreateEntity2(-i, i % 7);
            }
        }

        struct ChangeMixedValues : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            public ArchetypeChunkComponentType<EcsTestData> ecsTestData;
            public ArchetypeChunkComponentType<EcsTestData2> ecsTestData2;

            public void Execute(int chunkIndex)
            {
                var chunk = chunks[chunkIndex];
                var chunkCount = chunk.Count;
                var chunkEcsTestData = chunk.GetNativeArray(ecsTestData);
                var chunkEcsTestData2 = chunk.GetNativeArray(ecsTestData2);

                if (chunkEcsTestData.Length > 0)
                {
                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkEcsTestData[i] = new EcsTestData( chunkEcsTestData[i].value + 100 );
                    }
                }
                else if (chunkEcsTestData2.Length > 0)
                {
                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkEcsTestData2[i] = new EcsTestData2( chunkEcsTestData2[i].value0 - 1000 );
                    }
                }
            }
        }
        
        [Test]
        public void ACS_WriteMixed()
        {
            CreateMixedEntities(64);

            var query = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(EcsTestData2), typeof(EcsTestData)}, // any
                None = Array.Empty<ComponentType>(), // none
                All = Array.Empty<ComponentType>(), // all
            };
            var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);

            Assert.AreEqual(14,chunks.Length);

            var ecsTestData = m_Manager.GetArchetypeChunkComponentType<EcsTestData>(false);
            var ecsTestData2 = m_Manager.GetArchetypeChunkComponentType<EcsTestData2>(false);
            var changeValuesJobs = new ChangeMixedValues
            {
                chunks = chunks,
                ecsTestData = ecsTestData,
                ecsTestData2 = ecsTestData2,
            };

            var collectValuesJobHandle = changeValuesJobs.Schedule(chunks.Length, 64);
            collectValuesJobHandle.Complete();

            ulong foundValues = 0;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var chunkCount = chunk.Count;

                Assert.AreEqual(4,math.ceil_pow2(chunkCount-1));

                var chunkEcsTestData = chunk.GetNativeArray(ecsTestData);
                var chunkEcsTestData2 = chunk.GetNativeArray(ecsTestData2);
                if (chunkEcsTestData.Length > 0)
                {
                    for (int i = 0; i < chunkCount; i++)
                    {
                        foundValues |= (ulong)1 << (chunkEcsTestData[i].value-100);
                    }
                }
                else if (chunkEcsTestData2.Length > 0)
                {
                    for (int i = 0; i < chunkCount; i++)
                    {
                        foundValues |= (ulong)1 << (-chunkEcsTestData2[i].value0-1000);
                    }
                }
            }

            foundValues++;
            Assert.AreEqual(0,foundValues);
            
            chunks.Dispose();
        }
        
        struct ChangeMixedValuesSharedFilter : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ArchetypeChunk> chunks;
            public ArchetypeChunkComponentType<EcsTestData> ecsTestData;
            public ArchetypeChunkComponentType<EcsTestData2> ecsTestData2;
            [ReadOnly] public ArchetypeChunkSharedComponentType<EcsTestSharedComp> ecsTestSharedData;
            public int sharedFilterIndex;

            public void Execute(int chunkIndex)
            {
                var chunk = chunks[chunkIndex];
                var chunkCount = chunk.Count;
                var chunkEcsTestData = chunk.GetNativeArray(ecsTestData);
                var chunkEcsTestData2 = chunk.GetNativeArray(ecsTestData2);
                var chunkEcsSharedDataIndex = chunk.GetSharedComponentIndex(ecsTestSharedData);

                if (chunkEcsSharedDataIndex != sharedFilterIndex)
                    return;
                
                if (chunkEcsTestData.Length > 0)
                {
                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkEcsTestData[i] = new EcsTestData( chunkEcsTestData[i].value + 100 );
                    }
                }
                else if (chunkEcsTestData2.Length > 0)
                {
                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkEcsTestData2[i] = new EcsTestData2( chunkEcsTestData2[i].value0 - 1000 );
                    }
                }
            }
        }
        
        [Test]
        public void ACS_WriteMixedFilterShared()
        {
            CreateMixedEntities(64);
            
            Assert.AreEqual(1,m_Manager.GlobalSystemVersion);
            
            // Only update shared value == 1
            var unique = new List<EcsTestSharedComp>(0);
            m_Manager.GetAllUniqueSharedComponentData(unique);
            var sharedFilterValue = 1;
            var sharedFilterIndex = -1;
            for (int i = 0; i < unique.Count; i++)
            {
                if (unique[i].value == sharedFilterValue)
                {
                    sharedFilterIndex = i;
                    break;
                }
            }

            var query = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {typeof(EcsTestData2), typeof(EcsTestData)}, // any
                None = Array.Empty<ComponentType>(), // none
                All = new ComponentType[] {typeof(EcsTestSharedComp)}, // all
            };
            var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);

            Assert.AreEqual(14,chunks.Length);

            var ecsTestData = m_Manager.GetArchetypeChunkComponentType<EcsTestData>(false);
            var ecsTestData2 = m_Manager.GetArchetypeChunkComponentType<EcsTestData2>(false);
            var ecsTestSharedData = m_Manager.GetArchetypeChunkSharedComponentType<EcsTestSharedComp>();
            var changeValuesJobs = new ChangeMixedValuesSharedFilter
            {
                chunks = chunks,
                ecsTestData = ecsTestData,
                ecsTestData2 = ecsTestData2,
                ecsTestSharedData = ecsTestSharedData,
                sharedFilterIndex = sharedFilterIndex
            };

            var collectValuesJobHandle = changeValuesJobs.Schedule(chunks.Length, 64);
            collectValuesJobHandle.Complete();

            ulong foundValues = 0;
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var chunkCount = chunk.Count;

                Assert.AreEqual(4,math.ceil_pow2(chunkCount-1));
                
                var chunkEcsSharedDataIndex = chunk.GetSharedComponentIndex(ecsTestSharedData);

                var chunkEcsTestData = chunk.GetNativeArray(ecsTestData);
                var chunkEcsTestData2 = chunk.GetNativeArray(ecsTestData2);
                if (chunkEcsTestData.Length > 0)
                {
                    var chunkEcsTestDataVersion = chunk.GetComponentVersion(ecsTestData);

                    Assert.AreEqual(1, chunkEcsTestDataVersion);
                    
                    for (int i = 0; i < chunkCount; i++)
                    {
                        if (chunkEcsSharedDataIndex == sharedFilterIndex)
                        {
                          foundValues |= (ulong)1 << (chunkEcsTestData[i].value-100);
                        }
                        else
                        {
                          foundValues |= (ulong)1 << (chunkEcsTestData[i].value);
                        }
                    }
                }
                else if (chunkEcsTestData2.Length > 0)
                {
                    var chunkEcsTestData2Version = chunk.GetComponentVersion(ecsTestData2);
                    
                    Assert.AreEqual(1, chunkEcsTestData2Version);
                    
                    for (int i = 0; i < chunkCount; i++)
                    {
                        if (chunkEcsSharedDataIndex == sharedFilterIndex)
                        {
                          foundValues |= (ulong)1 << (-chunkEcsTestData2[i].value0-1000);
                        }
                        else
                        {
                          foundValues |= (ulong)1 << (-chunkEcsTestData2[i].value0);
                        }
                    }
                }
            }

            foundValues++;
            Assert.AreEqual(0,foundValues);
            
            chunks.Dispose();
        }

        [Test]
        public void ACS_Buffers()
        {
            CreateEntities(128);

            var query = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {}, // any
                None = Array.Empty<ComponentType>(), // none
                All = new ComponentType[] {typeof(EcsIntElement)}, // all
            };
            var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);

            var entities = m_Manager.GetArchetypeChunkEntityType();
            var intElements = m_Manager.GetArchetypeChunkBufferType<EcsIntElement>(false);
            var totalBuffersFound = 0;

            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];
                var accessor = chunk.GetBufferAccessor(intElements);

                for (int k = 0; k < accessor.Length; ++k)
                {
                    var buffer = accessor[i];

                    for (int n = 0; n < buffer.Length; ++n)
                    {
                        if (buffer[n] != n)
                            Assert.Fail("buffer element does not have the expected value");
                    }

                    ++totalBuffersFound;
                }
            }

            chunks.Dispose();
        }

        [Test]
        public void ACS_BuffersRO()
        {
            CreateEntities(128);

            var query = new EntityArchetypeQuery
            {
                Any = new ComponentType[] {}, // any
                None = Array.Empty<ComponentType>(), // none
                All = new ComponentType[] {typeof(EcsIntElement)}, // all
            };
            var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob);
            var entities = m_Manager.GetArchetypeChunkEntityType();
            var intElements = m_Manager.GetArchetypeChunkBufferType<EcsIntElement>(true);

            var chunk = chunks[0];
            var accessor = chunk.GetBufferAccessor(intElements);
            var buffer = accessor[0];

            Assert.Throws<InvalidOperationException>(() => buffer.Add(12));

            chunks.Dispose();
        }

        [Test]
        public void ACS_ChunkArchetypeTypesMatch()
        {
            var entityTypes = new ComponentType[] {typeof(EcsTestData), typeof(EcsTestSharedComp), typeof(EcsIntElement)};
            
            CreateEntities(128);
            
            var query = new EntityArchetypeQuery
            {
                Any = new ComponentType[0], // any
                None = new ComponentType[0], // none
                All = entityTypes, // all
            };
            
            using (var chunks = m_Manager.CreateArchetypeChunkArray(query, Allocator.TempJob))
            {
                foreach (var chunk in chunks)
                {
                    var archetype = chunk.Archetype;
                    var chunkTypes = archetype.ComponentTypes;
                    foreach (var type in entityTypes)
                    {
                        Assert.Contains(type, chunkTypes);
                    }

                    Assert.Contains(new ComponentType(typeof(Entity)), chunkTypes);
                }
            }
        }
    }
}
