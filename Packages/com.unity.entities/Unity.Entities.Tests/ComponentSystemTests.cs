using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Entities.Tests
{
    public class ComponentSystemTests : ECSTestsFixture
    {
        [DisableAutoCreation]
        class TestSystem : ComponentSystem
        {
            public bool Created = false;
            
            protected override void OnUpdate()
            {
            }

            protected override void OnCreateManager(int capacity)
            {
                Created = true;
            }
            
            protected override void OnDestroyManager()
            {
                Created = false;        
            }
        }
        
        [DisableAutoCreation]
        class DerivedTestSystem : TestSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        
        [DisableAutoCreation]
        class ThrowExceptionSystem : TestSystem
        {
            protected override void OnCreateManager(int capacity)
            {
                throw new System.Exception();
            }
            protected override void OnUpdate()
            {
            }
        }
        
        [DisableAutoCreation]
        class ScheduleJobAndDestroyArray : JobComponentSystem
        {
            NativeArray<int> test = new NativeArray<int>(10, Allocator.Persistent);

            struct Job : IJob
            {
                public NativeArray<int> test;

                public void Execute() { }
            }

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                return new Job(){ test = test }.Schedule(inputDeps);
            }

            protected override void OnDestroyManager()
            {
                // We expect this to not throw an exception since the jobs scheduled
                // by this system should be synced before the system is destroyed
                test.Dispose();
            }
        }
        
        [Test]
        public void Create()
        {
            var system = World.CreateManager<TestSystem>();
            Assert.AreEqual(system, World.GetExistingManager<TestSystem>());
            Assert.IsTrue(system.Created);
        }

        [Test]
        public void CreateAndDestroy()
        {
            var system = World.CreateManager<TestSystem>();
            World.DestroyManager(system);
            Assert.AreEqual(null, World.GetExistingManager<TestSystem>());
            Assert.IsFalse(system.Created);
        }

        [Test]
        public void GetOrCreateManagerReturnsSameSystem()
        {
            var system = World.GetOrCreateManager<TestSystem>();
            Assert.AreEqual(system, World.GetOrCreateManager<TestSystem>());
        }

        [Test]
        public void InheritedSystem()
        {
            var system = World.CreateManager<DerivedTestSystem>();
            Assert.AreEqual(system, World.GetExistingManager<DerivedTestSystem>());
            Assert.AreEqual(system, World.GetExistingManager<TestSystem>());

            World.DestroyManager(system);

            Assert.AreEqual(null, World.GetExistingManager<DerivedTestSystem>());
            Assert.AreEqual(null, World.GetExistingManager<TestSystem>());

            Assert.IsFalse(system.Created);
        }
        
        [Test]
        public void OnCreateThrowRemovesSystem()
        {
            Assert.Throws<Exception>(() => { World.CreateManager<ThrowExceptionSystem>(); });
            Assert.AreEqual(null, World.GetExistingManager<ThrowExceptionSystem>());
        }
        
        [Test]
        public void DestroySystemWhileJobUsingArrayIsRunningWorks()
        {
            var system = World.CreateManager<ScheduleJobAndDestroyArray>();
            system.Update();
            World.DestroyManager(system);
        }
        
        [Test]
        public void DisposeSystemComponentGroupThrows()
        {
            var system = World.CreateManager<EmptySystem>();
            var group = system.GetComponentGroup(typeof(EcsTestData));
            Assert.Throws<ArgumentException>(() => group.Dispose());
        }
        
        [Test]
        public void DestroyManagerTwiceThrows()
        {
            var system = World.CreateManager<TestSystem>();
            World.DestroyManager(system);
            Assert.Throws<ArgumentException>(() => World.DestroyManager(system) );
        }

        [Test]
        public void CreateTwoSystemsOfSameType()
        {
            var systemA = World.CreateManager<TestSystem>();
            var systemB = World.CreateManager<TestSystem>();
            // CreateManager makes a new manager
            Assert.AreNotEqual(systemA, systemB);
            // Return first system
            Assert.AreEqual(systemA, World.GetOrCreateManager<TestSystem>());
        }
        
        [Test]
        public void CreateTwoSystemsAfterDestroyReturnSecond()
        {
            var systemA = World.CreateManager<TestSystem>();
            var systemB = World.CreateManager<TestSystem>();
            World.DestroyManager(systemA);
            
            Assert.AreEqual(systemB, World.GetExistingManager<TestSystem>());;
        }
        
        [Test]
        public void CreateTwoSystemsAfterDestroyReturnFirst()
        {
            var systemA = World.CreateManager<TestSystem>();
            var systemB = World.CreateManager<TestSystem>();
            World.DestroyManager(systemB);
            
            Assert.AreEqual(systemA, World.GetExistingManager<TestSystem>());;
        }
        
        [Test]
        public void GetComponentGroup()
        {
            ComponentType[] ro_rw = { ComponentType.ReadOnly<EcsTestData>(), typeof(EcsTestData2) };
            ComponentType[] rw_rw = { typeof(EcsTestData), typeof(EcsTestData2) };
            ComponentType[] rw = { typeof(EcsTestData) };
            
            var ro_rw0_system = EmptySystem.GetComponentGroup(ro_rw);
            var rw_rw_system = EmptySystem.GetComponentGroup(rw_rw);
            var rw_system = EmptySystem.GetComponentGroup(rw);

            Assert.AreEqual(ro_rw0_system, EmptySystem.GetComponentGroup(ro_rw));
            Assert.AreEqual(rw_rw_system, EmptySystem.GetComponentGroup(rw_rw));
            Assert.AreEqual(rw_system, EmptySystem.GetComponentGroup(rw));
            
            Assert.AreEqual(3, EmptySystem.ComponentGroups.Length);
        }
        
        //@TODO: Behaviour is a slightly dodgy... Should probably just ignore and return same as single typeof(EcsTestData)
        [Test]
        public void GetComponentGroupWithEntityThrows()
        {
            ComponentType[] e = { typeof(Entity), typeof(EcsTestData) };
            EmptySystem.GetComponentGroup(e);
            Assert.Throws<ArgumentException>(() => EmptySystem.GetComponentGroup(e));
        }
        
        //@TODO: Behaviour is a slightly dodgy... Optimally would always return the same ComponentGroup
        [Test]
        public void GetComponentGroupWithDuplicates()
        {
            // Currently duplicates will create two seperate groups doing the same thing...
            ComponentType[] dup_1 = { typeof(EcsTestData2) };
            ComponentType[] dup_2 = { typeof(EcsTestData2), typeof(EcsTestData2) };
            
            var dup1_system = EmptySystem.GetComponentGroup(dup_1);
            var dup2_system = EmptySystem.GetComponentGroup(dup_2);

            Assert.AreEqual(dup1_system, EmptySystem.GetComponentGroup(dup_1));
            Assert.AreEqual(dup2_system, EmptySystem.GetComponentGroup(dup_2));
            
            Assert.AreEqual(2, EmptySystem.ComponentGroups.Length);
        }
        
    }

    public class Issue101 : ECSTestsFixture
    {
        [BurstCompile(CompileSynchronously = true)]
        struct Issue101Job : IJob
        {
            [WriteOnly] public NativeHashMap<ulong, byte>.Concurrent hashMap;
            [WriteOnly] public NativeQueue<ulong>.Concurrent keys;
            public int Index;

            public void Execute()
            {
                byte hash = (byte) Index;
                hashMap.TryAdd(hash, hash);
                keys.Enqueue(hash);
            }
        }

        [Ignore("Passed off to compute team")]
        [Test]
        public void TestIssue101()
        {
            var hashMap = new NativeHashMap<ulong, byte>(100, Allocator.TempJob);
            var keys = new NativeQueue<ulong>(Allocator.TempJob);

            try
            {
                var job = new Issue101Job()
                {
                    hashMap = hashMap,
                    keys = keys,
                    Index = 1,
                };

                job.Schedule(default(JobHandle)).Complete();
            }
            finally
            {
                keys.Dispose();
                hashMap.Dispose();
            }
        }
    }
}