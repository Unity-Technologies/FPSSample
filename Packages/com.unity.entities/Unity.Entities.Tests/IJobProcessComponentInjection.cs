using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Entities.Tests
{
    public class IJobProcessComponentInjection : ECSTestsFixture
    {
        [DisableAutoCreation]
        class TestSystem : JobComponentSystem
        {
            private struct Process1 : IJobProcessComponentData<EcsTestData>
            {
                public void Execute(ref EcsTestData value)
                {
                    value.value = 7;
                }
            }

            public struct Process2 : IJobProcessComponentData<EcsTestData, EcsTestData2>
            {
                public void Execute(ref EcsTestData src, ref EcsTestData2 dst)
                {
                    dst.value1 = src.value;
                }
            }
            
            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                inputDeps = new Process1().Schedule(this, inputDeps);
                inputDeps = new Process2().Schedule(this, inputDeps);
                return inputDeps;
            }
        }
        
        [Test]
        public void NestedIJobProcessComponentDataAreInjectedDuringOnCreateManager()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
            var system = World.GetOrCreateManager<TestSystem>();
            Assert.AreEqual(2, system.ComponentGroups.Length);
        }
    }
}