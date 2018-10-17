using NUnit.Framework;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.TestTools;
using UnityEngine.Experimental.LowLevel;
using UnityEngine;

namespace Unity.Entities.Tests
{
    public class UpdateOrderOptimizerTests : ECSTestsFixture
	{
        PlayerLoopSystem m_fakePlayerLoop;
        public UpdateOrderOptimizerTests()
        {
            m_fakePlayerLoop.subSystemList = new PlayerLoopSystem[3];
            m_fakePlayerLoop.subSystemList[0].type = typeof(UnityEngine.Experimental.PlayerLoop.Initialization);
            m_fakePlayerLoop.subSystemList[0].subSystemList = new PlayerLoopSystem[0];
            m_fakePlayerLoop.subSystemList[1].type = typeof(UnityEngine.Experimental.PlayerLoop.Update);
            m_fakePlayerLoop.subSystemList[1].subSystemList = new PlayerLoopSystem[2];
            m_fakePlayerLoop.subSystemList[1].subSystemList[0].type = typeof(UnityEngine.Experimental.PlayerLoop.Update.ScriptRunBehaviourUpdate);
            m_fakePlayerLoop.subSystemList[1].subSystemList[1].type = typeof(UnityEngine.Experimental.PlayerLoop.Update.ScriptRunDelayedDynamicFrameRate);
            m_fakePlayerLoop.subSystemList[2].type = typeof(UnityEngine.Experimental.PlayerLoop.PostLateUpdate);
            m_fakePlayerLoop.subSystemList[2].subSystemList = new PlayerLoopSystem[0];
        }

        [UpdateInGroup(typeof(RecursiveGroup3))]
        class RecursiveGroup1
        {}
        [UpdateInGroup(typeof(RecursiveGroup1))]
        class RecursiveGroup2
        {}
        [UpdateInGroup(typeof(RecursiveGroup2))]
        class RecursiveGroup3
        {}

        [UpdateInGroup(typeof(RecursiveGroup3))]
        [DisableAutoCreation]
        class RecursiveSystem : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }

        [UpdateAfter(typeof(SimpleCircularSystem3))]
        [DisableAutoCreation]
        class SimpleCircularSystem1 : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        [UpdateAfter(typeof(SimpleCircularSystem1))]
        [DisableAutoCreation]
        class SimpleCircularSystem2 : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        [UpdateAfter(typeof(SimpleCircularSystem2))]
        [DisableAutoCreation]
        class SimpleCircularSystem3 : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.Update))]
        [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.Initialization))]
        [DisableAutoCreation]
        class SimpleOverconstrainedSystem : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.Update))]
        [DisableAutoCreation]
        class OverconstrainedSystem1 : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        [UpdateAfter(typeof(OverconstrainedSystem1))]
        [UpdateBefore(typeof(OverconstrainedSystem3))]
        [DisableAutoCreation]
        class OverconstrainedSystem2 : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.Initialization))]
        [DisableAutoCreation]
        class OverconstrainedSystem3 : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }

	    // UpdatePlayerLoop handles nulls so that users of the API don't have to deal with setting the default loop
	    [Test]
	    public void NullWorldsDontThrow()
	    {
	        Assert.DoesNotThrow(() => ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null));
	        Assert.DoesNotThrow(() => ScriptBehaviourUpdateOrder.UpdatePlayerLoop(new World[] {World.Active, null}));
	    }

        [Test]
        public void RecursiveGroupIsError()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Found circular chain in update groups involving:"));

            var systems = new HashSet<ScriptBehaviourManager>();
            systems.Add(new RecursiveSystem());
            ScriptBehaviourUpdateOrder.InsertManagersInPlayerLoop(systems, m_fakePlayerLoop);
        }
        [Test]
        public void CircularDependencyIsError()
        {
            // The error is triggered for each system in a chain, not for each chain - so there will be three errors
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("is in a chain of circular dependencies"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("is in a chain of circular dependencies"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("is in a chain of circular dependencies"));

            var systems = new HashSet<ScriptBehaviourManager>();
            systems.Add(new SimpleCircularSystem1());
            systems.Add(new SimpleCircularSystem2());
            systems.Add(new SimpleCircularSystem3());
            ScriptBehaviourUpdateOrder.InsertManagersInPlayerLoop(systems, m_fakePlayerLoop);
        }
        [Test]
        public void OverConstrainedEngineIsError()
        {
            // The error is triggered for each system in a chain, not for each chain - so there will be three errors
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("is over constrained with engine containts"));

            var systems = new HashSet<ScriptBehaviourManager>();
            systems.Add(new SimpleOverconstrainedSystem());
            ScriptBehaviourUpdateOrder.InsertManagersInPlayerLoop(systems, m_fakePlayerLoop);
        }
        [Test]
        public void OverConstrainedEngineAndSystemIsError()
        {
            // The error is triggered for each system in a chain, not for each chain - so there will be three errors
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("is over constrained with engine and system containts"));

            var systems = new HashSet<ScriptBehaviourManager>();
            systems.Add(new OverconstrainedSystem1());
            systems.Add(new OverconstrainedSystem2());
            systems.Add(new OverconstrainedSystem3());
            ScriptBehaviourUpdateOrder.InsertManagersInPlayerLoop(systems, m_fakePlayerLoop);
        }
    }
}
