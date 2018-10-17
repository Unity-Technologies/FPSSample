using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    public class JobComponentSystemDependencyTests : ECSTestsFixture
    {
        [DisableAutoCreation]
        public class ReadSystem1 : JobComponentSystem
        {
            public struct Inputs
            {
                [ReadOnly]
                public ComponentDataArray<EcsTestData> data;
            }

            [Inject] Inputs m_Inputs;

            struct ReadJob : IJob
            {
                [ReadOnly]
                public ComponentDataArray<EcsTestData> wat;

                public void Execute()
                {
                }
            }

            protected override JobHandle OnUpdate(JobHandle input)
            {
                var job = new ReadJob() {wat = m_Inputs.data};
                return job.Schedule(input);
            }
        }

        [DisableAutoCreation]
        public class ReadSystem2 : JobComponentSystem
        {
            public struct Inputs
            {
                [ReadOnly]
                public ComponentDataArray<EcsTestData> data;
            }

            public bool returnWrongJob = false;
            public bool ignoreInputDeps = false;

            [Inject] private Inputs m_Inputs;

            private struct ReadJob : IJob
            {
                [ReadOnly]
                public ComponentDataArray<EcsTestData> wat;

                public void Execute()
                {
                }
            }

            protected override JobHandle OnUpdate(JobHandle input)
            {
                JobHandle h;

                var job = new ReadJob() {wat = m_Inputs.data};

                if (ignoreInputDeps)
                {
                    h = job.Schedule();
                }
                else
                {
                    h = job.Schedule(input);
                }

                return returnWrongJob ? input : h;
            }
        }

        [DisableAutoCreation]
        public class ReadSystem3 : JobComponentSystem
        {
            public struct Inputs
            {
                [ReadOnly]
                public ComponentDataArray<EcsTestData> data;
            }

#pragma warning disable 0169 // "never used" warning
            [Inject] private Inputs m_Inputs;
#pragma warning restore 0169

            protected override JobHandle OnUpdate(JobHandle input)
            {
                return input;
            }
        }

        [DisableAutoCreation]
        public class WriteSystem : JobComponentSystem
        {
            public struct Inputs
            {
                public ComponentDataArray<EcsTestData> data;
            }

            [Inject] private Inputs m_Inputs;

            public bool SkipJob = false;

            private struct WriteJob : IJob
            {
                public ComponentDataArray<EcsTestData> data;

                public void Execute()
                {
                }
            }

            protected override JobHandle OnUpdate(JobHandle input)
            {
                if (!SkipJob)
                {
                    var job = new WriteJob() {data = m_Inputs.data};
                    return job.Schedule(input);
                }
                else
                {
                    return input;
                }
            }
        }

        [Test]
        public void ReturningWrongJobThrowsInCorrectSystemUpdate()
        {
            var entity = m_Manager.CreateEntity (typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            ReadSystem1 rs1 = World.GetOrCreateManager<ReadSystem1>();
            ReadSystem2 rs2 = World.GetOrCreateManager<ReadSystem2>();

            rs2.returnWrongJob = true;

            rs1.Update();
            Assert.Throws<System.InvalidOperationException>(() => { rs2.Update(); });
        }

        [Test]
        public void IgnoredInputDepsThrowsInCorrectSystemUpdate()
        {
            var entity = m_Manager.CreateEntity (typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            WriteSystem ws1 = World.GetOrCreateManager<WriteSystem>();
            ReadSystem2 rs2 = World.GetOrCreateManager<ReadSystem2>();

            rs2.ignoreInputDeps = true;

            ws1.Update();
            Assert.Throws<System.InvalidOperationException>(() => { rs2.Update(); });
        }

        [Test]
        public void NotSchedulingWriteJobIsHarmless()
        {
            var entity = m_Manager.CreateEntity (typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            WriteSystem ws1 = World.GetOrCreateManager<WriteSystem>();

            ws1.Update();
            ws1.SkipJob = true;
            ws1.Update();
        }

        [Test]
        public void NotUsingDataIsHarmless()
        {
            var entity = m_Manager.CreateEntity (typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            ReadSystem1 rs1 = World.GetOrCreateManager<ReadSystem1>();
            ReadSystem3 rs3 = World.GetOrCreateManager<ReadSystem3>();

            rs1.Update();
            rs3.Update();
        }
        
        
        [DisableAutoCreation]
        class UseEcsTestDataFromEntity: JobComponentSystem
        {
            public struct MutateEcsTestDataJob : IJob
            {
                public ComponentDataFromEntity<EcsTestData> data;

                public void Execute()
                {
                    
                }
            }

            protected override JobHandle OnUpdate(JobHandle dep)
            {
                var job = new MutateEcsTestDataJob { data = GetComponentDataFromEntity<EcsTestData>() };
                return job.Schedule(dep);
            }
        }
        
        // The writer dependency on EcsTestData is not predeclared during 
        // OnCreateManager, but we still expect the code to work correctly.
        // This should result in a sync point when adding the dependency for the first time.
        [Test]
        public void AddingDependencyTypeDuringOnUpdateSyncsDependency()
        {
            var systemA = World.CreateManager<UseEcsTestDataFromEntity>();
            var systemB = World.CreateManager<UseEcsTestDataFromEntity>();
            
            systemA.Update();
            systemB.Update();
        }
    }
}
