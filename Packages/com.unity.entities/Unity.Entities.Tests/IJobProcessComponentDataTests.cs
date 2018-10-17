using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    public class IJobProcessComponentDataTests :ECSTestsFixture
    {
        struct Process1 : IJobProcessComponentData<EcsTestData>
        {
            public void Execute(ref EcsTestData dst)
            {
                dst.value = 7;
            }
        }

        struct Process2 : IJobProcessComponentData<EcsTestData, EcsTestData2>
        {
            public void Execute([ReadOnly]ref EcsTestData src, ref EcsTestData2 dst)
            {
                dst.value1 = src.value;
            }
        }

        struct Process3Entity  : IJobProcessComponentDataWithEntity<EcsTestData, EcsTestData2, EcsTestData3>
        {
            public void Execute(Entity entity, int index, [ReadOnly]ref EcsTestData src, ref EcsTestData2 dst1, ref EcsTestData3 dst2)
            {
                dst1.value1 = dst2.value2 = src.value + index + entity.Index;
            }
        }
        
        [Test]
        public void JobProcessSimple()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            m_Manager.SetComponentData(entity, new EcsTestData(42));

            new Process2().Run(EmptySystem);
            
            Assert.AreEqual(42, m_Manager.GetComponentData<EcsTestData2>(entity).value1);
        }
                
        [Test]
        public void JobProcessComponentGroupCorrect()
        {
            ComponentType[] expectedTypes = { ComponentType.ReadOnly<EcsTestData>(), ComponentType.Create<EcsTestData2>() };

            new Process2().Run(EmptySystem);
            var group = EmptySystem.GetComponentGroup(expectedTypes);
                        
            Assert.AreEqual(1, EmptySystem.ComponentGroups.Length);
            Assert.IsTrue(EmptySystem.ComponentGroups[0].CompareComponents(expectedTypes));
            Assert.AreEqual(group, EmptySystem.ComponentGroups[0]);
        }

        [Test]
        public void JobProcessComponentWithEntityGroupCorrect()
        {
            ComponentType[] expectedTypes = { ComponentType.ReadOnly<EcsTestData>(), ComponentType.Create<EcsTestData2>(), ComponentType.Create<EcsTestData3>() };

            new Process3Entity().Run(EmptySystem);
            var group = EmptySystem.GetComponentGroup(expectedTypes);
                        
            Assert.AreEqual(1, EmptySystem.ComponentGroups.Length);
            Assert.IsTrue(EmptySystem.ComponentGroups[0].CompareComponents(expectedTypes));
            Assert.AreEqual(group, EmptySystem.ComponentGroups[0]);
        }
        

        [DisableAutoCreation]
        class ChainedProcessComponentDataWorks : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                inputDeps = new Process1().Schedule(this, inputDeps);
                inputDeps = new Process2().Schedule(this, inputDeps);
                return inputDeps;
            }
        }
        [Test]
        public void MultipleJobProcessComponentDataCanChain()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            var system = World.GetOrCreateManager<ChainedProcessComponentDataWorks>();
            system.Update();
            Assert.AreEqual(7, m_Manager.GetComponentData<EcsTestData2>(entity).value1);
        }


        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void JobWithMissingDependency()
        {
            Assert.IsTrue(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled, "JobDebugger must be enabled for these tests");

            m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var job = new Process2().Schedule(EmptySystem);
            Assert.Throws<InvalidOperationException>(() => { new Process2().Schedule(EmptySystem); });
            
            job.Complete();
        }
#endif
        
        [RequireSubtractiveComponent(typeof(EcsTestData3))]
        [RequireComponentTag(typeof(EcsTestData4))]
        struct ProcessTagged : IJobProcessComponentData<EcsTestData, EcsTestData2>
        {
            public void Execute(ref EcsTestData src, ref EcsTestData2 dst)
            {
                dst.value1 = dst.value0 = src.value;
            }
        }
        
        void Test(bool didProcess, Entity entity)
        {
            m_Manager.SetComponentData(entity, new EcsTestData(42));

            new ProcessTagged().Schedule(EmptySystem).Complete();

            if (didProcess)
                Assert.AreEqual(42, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
            else
                Assert.AreEqual(0, m_Manager.GetComponentData<EcsTestData2>(entity).value0);
        }

        [Test]
        public void JobProcessAdditionalRequirements()
        {
            var entityIgnore0 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData3));
            Test(false, entityIgnore0);
            
            var entityIgnore1 = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            Test(false, entityIgnore1);

            var entityProcess = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2), typeof(EcsTestData4));
            Test(true, entityProcess);
        }


        

        [Test]
        [Ignore("TODO")]
        public void TestCoverageFor_ComponentSystemBase_InjectNestedIJobProcessComponentDataJobs()
        {
        }
        
        [Test]
        [Ignore("TODO")]
        public void DuplicateComponentTypeParametersThrows()
        {
        }
    }
}