using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities.Tests;

namespace Unity.Entities.Tests
{
    public class WorldDebuggingToolsTests : ECSTestsFixture
    {

        [DisableAutoCreation]
        class RegularSystem : ComponentSystem
        {
            struct Entities
            {
                public readonly int Length;
                public ComponentDataArray<EcsTestData> tests;
            }

#pragma warning disable 0169 // "never used" warning
            [Inject] private Entities entities;
#pragma warning restore 0169
            
            protected override void OnUpdate()
            {
                throw new NotImplementedException();
            }
        }

        [DisableAutoCreation]
        class SubtractiveSystem : ComponentSystem
        {
            struct Entities
            {
                public readonly int Length;
                public ComponentDataArray<EcsTestData> tests;
                public SubtractiveComponent<EcsTestData2> noTest2;
            }

#pragma warning disable 0169 // "never used" warning
            [Inject] private Entities entities;
#pragma warning restore 0169
            
            protected override void OnUpdate()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void SystemInclusionList_MatchesComponents()
        {
            var system = World.Active.GetOrCreateManager<RegularSystem>();
            
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var matchList = new List<Tuple<ScriptBehaviourManager, List<ComponentGroup>>>();
            
            WorldDebuggingTools.MatchEntityInComponentGroups(World.Active, entity, matchList);
            
            Assert.AreEqual(1, matchList.Count);
            Assert.AreEqual(system, matchList[0].Item1);
            Assert.AreEqual(system.ComponentGroups[0], matchList[0].Item2[0]);
        }

        [Test]
        public void SystemInclusionList_IgnoresSubtractedComponents()
        {
            var system = World.Active.GetOrCreateManager<SubtractiveSystem>();
            
            var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

            var matchList = new List<Tuple<ScriptBehaviourManager, List<ComponentGroup>>>();
            
            WorldDebuggingTools.MatchEntityInComponentGroups(World.Active, entity, matchList);
            
            Assert.AreEqual(0, matchList.Count);
        }
        
    }
}
