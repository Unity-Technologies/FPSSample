using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    public class SizeTests : ECSTestsFixture
    {
        [Test]
        public void SIZ_TagComponentDoesNotChangeCapacity()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData));
            var entity1 = m_Manager.CreateEntity(typeof(EcsTestData),typeof(EcsTestTag));
            
            unsafe {
                // a system ran, the version should match the global
                var chunk0 = m_Manager.Entities->GetComponentChunk(entity0);
                var chunk1 = m_Manager.Entities->GetComponentChunk(entity1);
                var archetype0 = chunk0->Archetype;
                var archetype1 = chunk1->Archetype;

                var td2index0 = ChunkDataUtility.GetIndexInTypeArray(chunk0->Archetype, TypeManager.GetTypeIndex<EcsTestData2>());

                Assert.AreEqual(archetype0->ChunkCapacity, archetype1->ChunkCapacity);
            }
        }
        
        [Test]
        public void SIZ_TagComponentZeroSize()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestTag));
            
            unsafe {
                // a system ran, the version should match the global
                var chunk0 = m_Manager.Entities->GetComponentChunk(entity0);
                var archetype0 = chunk0->Archetype;
                var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(chunk0->Archetype, TypeManager.GetTypeIndex<EcsTestTag>());

                Assert.AreEqual(0, archetype0->SizeOfs[indexInTypeArray]);
            }
        }
        
        [Test]
        public void SIZ_TagCannotGetComponentData()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestTag));

            Assert.Throws<ArgumentException>(() =>
            {
               var data = m_Manager.GetComponentData<EcsTestTag>(entity0);
            });
        }
        
        [Test]
        public void SIZ_TagCannotSetComponentData()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestTag));

            Assert.Throws<ArgumentException>(() =>
            {
               m_Manager.SetComponentData(entity0, default(EcsTestTag));
            });
        }

        [Test]
        public void SIZ_TagCannotGetComponentDataArray()
        {
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestTag));
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestTag));

            Assert.Throws<ArgumentException>(() =>
            {
               var array = group.GetComponentDataArray<EcsTestTag>();
            });
        }
        
        [Test]
        public void SIZ_TagCannotGetComponentDataArrayFromEntity()
        {
            var group = m_Manager.CreateComponentGroup(typeof(EcsTestTag));
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestTag));

            Assert.Throws<ArgumentException>(() =>
            {
                var array = m_Manager.GetComponentDataFromEntity<EcsTestTag>();
            });
        }
        
        [Test]
         public void SIZ_TagCannotGetNativeArrayFromArchetypeChunk()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestTag));
            var chunks = m_Manager.CreateArchetypeChunkArray(
                new EntityArchetypeQuery
                {
                    Any = Array.Empty<ComponentType>(),
                    None = Array.Empty<ComponentType>(),
                    All = new ComponentType[] {typeof(EcsTestTag)},
                }, Allocator.TempJob);
            
            var tagType = m_Manager.GetArchetypeChunkComponentType<EcsTestTag>(false);

            Assert.AreEqual(1, ArchetypeChunkArray.CalculateEntityCount(chunks));
            
            for (int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                Assert.IsTrue(chunk.Has(tagType));
                Assert.Throws<ArgumentException>(() =>
                {
                    var tags = chunk.GetNativeArray(tagType);
                });
            }
            
            chunks.Dispose();
        }
    }
}
