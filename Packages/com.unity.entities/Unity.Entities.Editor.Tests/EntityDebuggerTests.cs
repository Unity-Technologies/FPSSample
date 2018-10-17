using System;
using System.Linq;
using NUnit.Framework;
using Unity.Entities.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.LowLevel;

namespace Unity.Entities.Editor.Tests
{
    public class EntityDebuggerTests : ECSTestsFixture
    {

        private EntityDebugger m_Window;
        private ComponentSystem m_System;
        private ComponentGroup m_ComponentGroup;
        private Entity m_Entity;
        
        [DisableAutoCreation]
        class SingleGroupSystem : ComponentSystem
        {
            
#pragma warning disable 0169 // "never used" warning
            struct Group
            {
                private readonly int Length;
                private ComponentDataArray<EcsTestData> testDatas;
            }

            [Inject] private Group entities;
#pragma warning restore 0169
            
            protected override void OnUpdate()
            {
                throw new NotImplementedException();
            }
        }

        private static void CloseAllDebuggers()
        {
            var windows = Resources.FindObjectsOfTypeAll<EntityDebugger>();
            foreach (var window in windows)
                window.Close();
        }

        private const string World2Name = "Test World 2";
        private World World2;

        public override void Setup()
        {
            base.Setup();

            CloseAllDebuggers();
            
            m_Window = EditorWindow.GetWindow<EntityDebugger>();

            m_System = World.Active.GetOrCreateManager<SingleGroupSystem>();
            
            World2 = new World(World2Name);

            World2.GetOrCreateManager<EntityManager>();
            World2.GetOrCreateManager<EmptySystem>();
            
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);

            m_ComponentGroup = m_System.ComponentGroups[0];

            m_Entity = m_Manager.CreateEntity(typeof(EcsTestData));
        }

        public override void TearDown()
        {
            CloseAllDebuggers();
            
            World2.Dispose();
            World2 = null;
            
            base.TearDown();
            
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
        }

        [Test]
        public void WorldPopup_RestorePreviousSelection()
        {
            World world = null;
            var popup = new WorldPopup(() => null, x => world = x);
            popup.TryRestorePreviousSelection(false, WorldPopup.kNoWorldName);
            Assert.AreEqual(World.AllWorlds[0], world);
            popup.TryRestorePreviousSelection(false, World2Name);
            Assert.AreEqual(World2, world);
        }

        [Test]
        public void EntityDebugger_SetSystemSelection()
        {
            m_Window.SetSystemSelection(m_Manager, World.Active, true, true);
            
            Assert.AreEqual(World.Active, m_Window.SystemSelectionWorld);
            
            Assert.Throws<ArgumentNullException>(() => m_Window.SetSystemSelection(m_Manager, null, true, true));
        }

        [Test]
        public void EntityDebugger_SetAllSelections()
        {
            var entityListQuery = new EntityListQuery(m_ComponentGroup);
            EntityDebugger.SetAllSelections(World.Active, m_System, entityListQuery, m_Entity);
            
            Assert.AreEqual(World.Active, m_Window.WorldSelection);
            Assert.AreEqual(m_System, m_Window.SystemSelection);
            Assert.AreEqual(m_ComponentGroup, m_Window.EntityListQuerySelection.Group);
            Assert.AreEqual(m_Entity, m_Window.EntitySelection);
        }

        [Test]
        public void EntityDebugger_RememberSelections()
        {
            var entityListQuery = new EntityListQuery(m_ComponentGroup);
            EntityDebugger.SetAllSelections(World.Active, m_System, entityListQuery, m_Entity);
            
            m_Window.SetWorldSelection(null, true);
            
            m_Window.SetWorldSelection(World.Active, true);
            
            Assert.AreEqual(World.Active, m_Window.WorldSelection);
            Assert.AreEqual(m_System, m_Window.SystemSelection);
            Assert.AreEqual(m_ComponentGroup, m_Window.EntityListQuerySelection.Group);
            Assert.AreEqual(m_Entity, m_Window.EntitySelection);
        }

        [Test]
        public void EntityDebugger_SetAllEntitiesFilter()
        {
            var query = new EntityArchetypeQuery()
            {
                All = new ComponentType[] {ComponentType.Create<EcsTestData>() },
                Any = new ComponentType[0],
                None = new ComponentType[0]
            };
            var listQuery = new EntityListQuery(query);
            
            m_Window.SetWorldSelection(World.Active, true);
            m_Window.SetSystemSelection(null, null, true, true);
            m_Window.SetAllEntitiesFilter(listQuery);
            Assert.AreEqual(query, m_Window.EntityListQuerySelection.Query);
            
            m_Window.SetEntityListSelection(null, true, true);
            m_Window.SetSystemSelection(World.Active.GetExistingManager<EntityManager>(), World.Active, true, true);
            m_Window.SetAllEntitiesFilter(listQuery);
            Assert.AreEqual(query, m_Window.EntityListQuerySelection.Query);
            
            m_Window.SetSystemSelection(m_System, World.Active, true, true);
            m_Window.SetAllEntitiesFilter(listQuery);
            Assert.AreNotEqual(listQuery, m_Window.EntityListQuerySelection);
        }

        [Test]
        public void EntityDebugger_StylesIntact()
        {
            Assert.IsNotNull(EntityDebuggerStyles.ComponentRequired);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentSubtractive);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadOnly);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadWrite);
            
            Assert.IsNotNull(EntityDebuggerStyles.ComponentRequired.normal.background);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentSubtractive.normal.background);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadOnly.normal.background);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadWrite.normal.background);
        }
        
    }
}
