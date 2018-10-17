using NUnit.Framework;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    public class ChangeVersionTests : ECSTestsFixture
    {
        [DisableAutoCreation]
        class BumpVersionSystemInJob : ComponentSystem
        {
            struct MyStruct
            {
                public readonly int Length;
                public ComponentDataArray<EcsTestData> Data;
                public ComponentDataArray<EcsTestData2> Data2;
            }

            [Inject]
            MyStruct DataStruct;

            struct UpdateData : IJob
            {
                public int Length;
                public ComponentDataArray<EcsTestData> Data;
                public ComponentDataArray<EcsTestData2> Data2;
                
                public void Execute()
                {
                    for (int i = 0; i < Length; ++i)
                    {
                        var d2 = Data2[i];
                        d2.value0 = 10;
                        Data2[i] = d2;
                    }
                }
            }

            protected override void OnUpdate()
            {
                var updateDataJob = new UpdateData
                {
                    Length = DataStruct.Length,
                    Data = DataStruct.Data,
                    Data2 = DataStruct.Data2
                };
                var updateDataJobHandle = updateDataJob.Schedule();
                updateDataJobHandle.Complete();
            }
        }
        
        [DisableAutoCreation]
        class BumpVersionSystem : ComponentSystem
        {
            struct MyStruct
            {
                public readonly int Length;
                public ComponentDataArray<EcsTestData> Data;
                public ComponentDataArray<EcsTestData2> Data2;
            }

            [Inject]
            MyStruct DataStruct;

            protected override void OnUpdate()
            {
                for (int i = 0; i < DataStruct.Length; ++i) {
                    var d2 = DataStruct.Data2[i];
                    d2.value0 = 10;
                    DataStruct.Data2[i] = d2;
                }
            }
        }

        [Test]
        public void CHG_IncrementedOnInjectionInJob()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            var bumpSystem = World.CreateManager<BumpVersionSystemInJob>();

            var oldGlobalVersion = m_Manager.GlobalSystemVersion;

            bumpSystem.Update();

            var value0 = m_Manager.GetComponentData<EcsTestData2>(entity0).value0;
            Assert.AreEqual(10, value0);

            Assert.That(m_Manager.GlobalSystemVersion > oldGlobalVersion);
            
            unsafe {
                // a system ran, the version should match the global
                var chunk0 = m_Manager.Entities->GetComponentChunk(entity0);
                var td2index0 = ChunkDataUtility.GetIndexInTypeArray(chunk0->Archetype, TypeManager.GetTypeIndex<EcsTestData2>());
                Assert.AreEqual(m_Manager.GlobalSystemVersion, chunk0->ChangeVersion[td2index0]);
            }
        }
        
        [Test]
        public void CHG_IncrementedOnInjection()
        {
            var entity0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            var bumpSystem = World.CreateManager<BumpVersionSystem>();

            var oldGlobalVersion = m_Manager.GlobalSystemVersion;

            bumpSystem.Update();

            var value0 = m_Manager.GetComponentData<EcsTestData2>(entity0).value0;
            Assert.AreEqual(10, value0);

            Assert.That(m_Manager.GlobalSystemVersion > oldGlobalVersion);
            
            unsafe {
                // a system ran, the version should match the global
                var chunk0 = m_Manager.Entities->GetComponentChunk(entity0);
                var td2index0 = ChunkDataUtility.GetIndexInTypeArray(chunk0->Archetype, TypeManager.GetTypeIndex<EcsTestData2>());
                Assert.AreEqual(m_Manager.GlobalSystemVersion, chunk0->ChangeVersion[td2index0]);
            }
        }
    }
}
