using NUnit.Framework;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Jobs;

namespace Unity.Entities.Tests
{
    public class ComponentGroupTransformAccessArrayTests : ECSTestsFixture
	{

	    TransformAccessArrayInjectionHook m_TransformAccessArrayInjectionHook = new TransformAccessArrayInjectionHook();
	    ComponentArrayInjectionHook m_ComponentArrayInjectionHook = new ComponentArrayInjectionHook();

	    [OneTimeSetUp]
	    public void Init()
	    {
	        InjectionHookSupport.RegisterHook(m_ComponentArrayInjectionHook);
	        InjectionHookSupport.RegisterHook(m_TransformAccessArrayInjectionHook);
	    }

	    [OneTimeTearDown]
	    public void Cleanup()
	    {
	        InjectionHookSupport.RegisterHook(m_TransformAccessArrayInjectionHook);
	        InjectionHookSupport.UnregisterHook(m_ComponentArrayInjectionHook);
	    }

        public ComponentGroupTransformAccessArrayTests()
        {
            Assert.IsTrue(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled, "JobDebugger must be enabled for these tests");
        }

	    public struct TransformAccessArrayTestTag : IComponentData
	    {
	    }
	    public class TransformAccessArrayTestTagComponent : ComponentDataWrapper<TransformAccessArrayTestTag> { }

	    [Test]
		public void EmptyTransformAccessArrayWorks()
	    {
	        var group = EmptySystem.GetComponentGroup(typeof(Transform), typeof(TransformAccessArrayTestTag));
	        var ta = group.GetTransformAccessArray();
			Assert.AreEqual(0, ta.length);
	    }
	    [Test]
	    public void SingleItemTransformAccessArrayWorks()
	    {
	        var go = new GameObject();
	        go.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnEnable();
	        var group = EmptySystem.GetComponentGroup(typeof(Transform), typeof(TransformAccessArrayTestTag));
	        var ta = group.GetTransformAccessArray();
	        Assert.AreEqual(1, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go);
	    }
	    [Test]
	    public void AddAndGetNewTransformAccessArrayUpdatesContent()
	    {
	        var go = new GameObject();
	        go.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnEnable();
	        var group = EmptySystem.GetComponentGroup(typeof(Transform), typeof(TransformAccessArrayTestTag));
	        var ta = group.GetTransformAccessArray();
	        Assert.AreEqual(1, ta.length);

	        var go2 = new GameObject();
	        go2.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go2.GetComponent<GameObjectEntity>().OnEnable();
	        ta = group.GetTransformAccessArray();
	        Assert.AreEqual(2, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnDisable();
	        go2.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go);
	        Object.DestroyImmediate(go2);
	    }
	    [Test]
	    // The atomic safety handle of TransformAccessArrays are not invalidated when injection changes, the array represents the transforms when you got it
	    public void AddAndUseOldTransformAccessArrayDoesNotUpdateContent()
	    {
	        var go = new GameObject();
	        go.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnEnable();
	        var group = EmptySystem.GetComponentGroup(typeof(Transform), typeof(TransformAccessArrayTestTag));
	        var ta = group.GetTransformAccessArray();
	        Assert.AreEqual(1, ta.length);

	        var go2 = new GameObject();
	        go2.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go2.GetComponent<GameObjectEntity>().OnEnable();
	        Assert.AreEqual(1, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnDisable();
	        go2.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go);
	        Object.DestroyImmediate(go2);
	    }
	    [Test]
	    public void DestroyAndGetNewTransformAccessArrayUpdatesContent()
	    {
	        var go = new GameObject();
	        go.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnEnable();
	        var go2 = new GameObject();
	        go2.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go2.GetComponent<GameObjectEntity>().OnEnable();

	        var group = EmptySystem.GetComponentGroup(typeof(Transform), typeof(TransformAccessArrayTestTag));
	        var ta = group.GetTransformAccessArray();
	        Assert.AreEqual(2, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go);

	        ta = group.GetTransformAccessArray();
	        Assert.AreEqual(1, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go2.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go2);
	    }
	    [Test]
	    // The atomic safety handle of TransformAccessArrays are not invalidated when injection changes, the array represents the transforms when you got it
	    public void DestroyAndUseOldTransformAccessArrayDoesNotUpdateContent()
	    {
	        var go = new GameObject();
	        go.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnEnable();
	        var go2 = new GameObject();
	        go2.AddComponent<TransformAccessArrayTestTagComponent>();
	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go2.GetComponent<GameObjectEntity>().OnEnable();

	        var group = EmptySystem.GetComponentGroup(typeof(Transform), typeof(TransformAccessArrayTestTag));
	        var ta = group.GetTransformAccessArray();
	        Assert.AreEqual(2, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go);

	        Assert.AreEqual(2, ta.length);

	        // Execute in edit mode is not enabled so this has to be called manually right now
	        go2.GetComponent<GameObjectEntity>().OnDisable();
	        Object.DestroyImmediate(go2);
	    }

	    [DisableAutoCreation]
	    public class GameObjectArrayWithTransformAccessSystem : ComponentSystem
	    {
	        public struct Group
	        {
	            public readonly int Length;
	            public GameObjectArray gameObjects;

	            public TransformAccessArray transforms;
	        }

	        [Inject]
	        public Group group;

	        protected override void OnUpdate()
	        {
	        }

	        public new void UpdateInjectedComponentGroups()
	        {
	            base.UpdateInjectedComponentGroups();
	        }
	    }

	    [Test]
	    public void GameObjectArrayWorksWithTransformAccessArray()
	    {
	        var hook = new GameObjectArrayInjectionHook();
	        InjectionHookSupport.RegisterHook(hook);

	        var go = new GameObject("test");
	        GameObjectEntity.AddToEntityManager(m_Manager, go);

	        var manager = World.GetOrCreateManager<GameObjectArrayWithTransformAccessSystem>();

	        manager.UpdateInjectedComponentGroups();

	        Assert.AreEqual(1, manager.group.Length);
	        Assert.AreEqual(go, manager.group.gameObjects[0]);
	        Assert.AreEqual(go, manager.group.transforms[0].gameObject);

	        Object.DestroyImmediate (go);

	        InjectionHookSupport.UnregisterHook(hook);

	        TearDown();
	    }

	    [DisableAutoCreation]
	    public class TransformWithTransformAccessSystem : ComponentSystem
	    {
	        public struct Group
	        {
	            public readonly int Length;
	            public ComponentArray<Transform> transforms;

	            public TransformAccessArray transformAccesses;
	        }

	        [Inject]
	        public Group group;

	        protected override void OnUpdate()
	        {
	        }

	        public new void UpdateInjectedComponentGroups()
	        {
	            base.UpdateInjectedComponentGroups();
	        }
	    }

	    [Test]
	    public void TransformArrayWorksWithTransformAccessArray()
	    {
	        var go = new GameObject("test");
	        GameObjectEntity.AddToEntityManager(m_Manager, go);

	        var manager = World.GetOrCreateManager<TransformWithTransformAccessSystem>();

	        manager.UpdateInjectedComponentGroups();

	        Assert.AreEqual(1, manager.group.Length);
	        Assert.AreEqual(manager.group.transforms[0].gameObject, manager.group.transformAccesses[0].gameObject);

	        Object.DestroyImmediate (go);
	        TearDown();
	    }
    }
}
