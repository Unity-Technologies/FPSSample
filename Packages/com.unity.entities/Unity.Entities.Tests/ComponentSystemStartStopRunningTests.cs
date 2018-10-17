using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.TestTools;

namespace Unity.Entities.Tests
{
    public class ComponentSystemStartStopRunningTests : ECSTestsFixture
    {
        [DisableAutoCreation]
        class TestSystem : ComponentSystem
        {
            public const string OnStartRunningString =
                nameof(TestSystem) + ".OnStartRunning()";

            public const string OnStopRunningString =
                nameof(TestSystem) + ".OnStopRunning()";

            struct MyStruct
            {
                public readonly int Length;
                public readonly ComponentDataArray<EcsTestData> Data;
            }

            [Inject]
            MyStruct DataStruct;
            public NativeArray<int> StoredData;
            protected override void OnUpdate()
            {
                var index = StoredData[0] + DataStruct.Data[0].value + 1;
                StoredData.Dispose();

                StoredData = new NativeArray<int>(1, Allocator.Temp);
                StoredData[0] = index;
            }

            protected override void OnStartRunning()
            {
                UnityEngine.Debug.Log(OnStartRunningString);
                StoredData = new NativeArray<int>(1, Allocator.Temp);
                base.OnStartRunning();
            }

            protected override void OnStopRunning()
            {
                UnityEngine.Debug.Log(OnStopRunningString);
                StoredData.Dispose();
                base.OnStopRunning();
            }
        }

        TestSystem system;
        Entity runSystemEntity = Entity.Null;

        public void ShouldRunSystem(bool shouldRun)
        {
            if (runSystemEntity != Entity.Null)
            {
                m_Manager.DestroyEntity(runSystemEntity);
                runSystemEntity = Entity.Null;
            }

            if (shouldRun)
            {
                runSystemEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            }
        }

        public override void Setup()
        {
            base.Setup();
            system = World.Active.GetOrCreateManager<TestSystem>();
            ShouldRunSystem(true);
        }

        public override void TearDown()
        {
            if (runSystemEntity != Entity.Null)
            {
                m_Manager.DestroyEntity(runSystemEntity);
                runSystemEntity = Entity.Null;
            }
            if (system != null)
            {
                World.Active.DestroyManager(system);
                system = null;
            }

            base.TearDown();
        }

        [Test]
        public void TempAllocation_DisposedInOnStopRunning_IsDisposed()
        {
            system.Update();

            system.Enabled = false;

            system.Update();

            Assert.IsFalse(system.StoredData.IsCreated);
        }


        [Test]
        public void OnStartRunning_FirstUpdate_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_WhenReEnabled_CalledOnce()
        {
            system.Enabled = false;

            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Enabled = true;

            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_WithEnabledAndShouldRun_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Enabled = true;
            ShouldRunSystem(true);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_WithDisabledAndShouldRun_NotCalled()
        {
            system.Enabled = false;
            ShouldRunSystem(true);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_WithEnabledAndShouldNotRun_NotCalled()
        {
            system.Enabled = true;
            ShouldRunSystem(false);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_WithDisabledAndShouldNotRun_NotCalled()
        {
            system.Enabled = false;
            ShouldRunSystem(false);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_EnablingWhenShouldRunSystemIsTrue_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);

            ShouldRunSystem(true);
            system.Enabled = false;
            system.Update();

            system.Enabled = true;
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStartRunning_WhenShouldRunSystemBecomesTrue_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            ShouldRunSystem(true);
            system.Enabled = true;
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            ShouldRunSystem(false);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            ShouldRunSystem(true);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WithEnabledAndShouldRun_NotCalled()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            system.Enabled = true;
            ShouldRunSystem(true);
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WithDisabledAndShouldRun_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            system.Enabled = false;
            ShouldRunSystem(true);
            system.Update();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WithEnabledAndShouldNotRun_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            system.Enabled = true;
            ShouldRunSystem(false);
            system.Update();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WithDisabledAndShouldNotRun_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            system.Enabled = false;
            ShouldRunSystem(false);
            system.Update();
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WhenDisabledBeforeFirstUpdate_NotCalled()
        {
            system.Enabled = false;
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WhenDestroyingActiveManager_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            World.Active.DestroyManager(system);
            system = null;

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WhenDestroyingInactiveManager_NotCalled()
        {
            system.Enabled = false;
            system.Update();

            World.Active.DestroyManager(system);
            system = null;

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_WhenShouldRunSystemBecomesFalse_CalledOnce()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            system.Enabled = false;
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnStopRunning_DisablingWhenShouldRunSystemIsFalse_NotCalled()
        {
            LogAssert.Expect(LogType.Log, TestSystem.OnStartRunningString);
            system.Update();

            LogAssert.Expect(LogType.Log, TestSystem.OnStopRunningString);
            ShouldRunSystem(false);
            system.Update();

            system.Enabled = false;
            system.Update();

            LogAssert.NoUnexpectedReceived();
        }

    }
}
