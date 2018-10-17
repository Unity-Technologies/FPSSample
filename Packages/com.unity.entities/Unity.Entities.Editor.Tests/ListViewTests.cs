using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Entities.Tests;
using UnityEditor.IMGUI.Controls;

namespace Unity.Entities.Editor.Tests
{
    public class ListViewTests : ECSTestsFixture
    {
        private static void SetEntitySelection(Entity s)
        {
        }

        private World GetWorldSelection()
        {
            return World.Active;
        }

        private static void SetComponentGroupSelection(EntityListQuery query)
        {
        }

        private static void SetSystemSelection(ScriptBehaviourManager system, World world)
        {
        }
        
        private EntityListQuery AllQuery => new EntityListQuery(new EntityArchetypeQuery(){All = new ComponentType[0], Any = new ComponentType[0], None = new ComponentType[0]});

        private World World2;

        public override void Setup()
        {
            base.Setup();
            
            World2 = new World("Test World 2");

            World2.GetOrCreateManager<EntityManager>();
            World2.GetOrCreateManager<EmptySystem>();
            
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.AllWorlds.ToArray());
        }

        public override void TearDown()
        {
            World2.Dispose();
            World2 = null;
            
            base.TearDown();
            
            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
        }

        [Test]
        public void EntityListView_ShowNothingWithoutWorld()
        {
            m_Manager.CreateEntity();
            var emptySystem = World.Active.GetOrCreateManager<EmptySystem>();
            ScriptBehaviourManager currentSystem = null;

            using (var listView = new EntityListView(new TreeViewState(), null, SetEntitySelection, () => null,
                () => currentSystem))
            {
                currentSystem = World.Active.GetExistingManager<EntityManager>();
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);

                currentSystem = emptySystem;
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);

                currentSystem = null;
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);

                currentSystem = null;
                listView.SelectedEntityQuery = AllQuery;
                Assert.IsFalse(listView.ShowingSomething);
            }
        }

        [Test]
        public void EntityListView_ShowEntitiesFromWorld()
        {
            m_Manager.CreateEntity();
            var emptySystem = World.Active.GetOrCreateManager<EmptySystem>();
            var selectedWorld = World.Active;
            ScriptBehaviourManager currentSystem = null;

            using (var listView = new EntityListView(new TreeViewState(), null, SetEntitySelection, () => selectedWorld,
                () => currentSystem))
            {
                currentSystem = World.Active.GetExistingManager<EntityManager>();
                listView.SelectedEntityQuery = AllQuery;
                Assert.IsTrue(listView.ShowingSomething);
                Assert.AreEqual(1, listView.GetRows().Count);
                
                currentSystem = World.Active.GetExistingManager<EntityManager>();
                listView.SelectedEntityQuery = null;
                Assert.IsTrue(listView.ShowingSomething);
                Assert.AreEqual(1, listView.GetRows().Count);

                currentSystem = emptySystem;
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);
            }
        }

        [Test]
        public void ComponentGroupListView_CanSetNullSystem()
        {
            var listView = new ComponentGroupListView(new TreeViewState(), EmptySystem, SetComponentGroupSelection, GetWorldSelection);

            Assert.DoesNotThrow(() => listView.SelectedSystem = null);
        }

        [Test]
        public void ComponentGroupListView_SortOrderExpected()
        {
            var typeList = new List<ComponentType>();
            var subtractive = ComponentType.Subtractive<EcsTestData>();
            var readWrite = ComponentType.Create<EcsTestData2>();
            var readOnly = ComponentType.ReadOnly<EcsTestData3>();
            
            typeList.Add(subtractive);
            typeList.Add(readOnly);
            typeList.Add(readWrite);
            typeList.Sort(ComponentGroupGUI.CompareTypes);
            
            Assert.AreEqual(readOnly, typeList[0]);
            Assert.AreEqual(readWrite, typeList[1]);
            Assert.AreEqual(subtractive, typeList[2]);
        }

        [Test]
        public void SystemListView_CanCreateWithNullWorld()
        {
            SystemListView listView;
            var states = new List<TreeViewState>();
            var stateNames = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                listView = SystemListView.CreateList(states, stateNames, SetSystemSelection, () => null);
                listView.Reload();
            });
        }

        [Test]
        public void SystemListView_ShowExactlyWorldSystems()
        {
            var listView = new SystemListView(
                new TreeViewState(),
                new MultiColumnHeader(SystemListView.GetHeaderState()),
                (manager, world) => { },
                () => World.Active);
            var managerItems = listView.GetRows().Where(x => listView.managersById.ContainsKey(x.id)).Select(x => listView.managersById[x.id]);
            Assert.AreEqual(World.Active.BehaviourManagers.Count(), managerItems.Intersect(World.Active.BehaviourManagers).Count());
        }

        [Test]
        public void SystemListView_NullWorldShowsAllSystems()
        {
            var listView = new SystemListView(
                new TreeViewState(),
                new MultiColumnHeader(SystemListView.GetHeaderState()),
                (manager, world) => { },
                () => null);
            var managerItems = listView.GetRows().Where(x => listView.managersById.ContainsKey(x.id)).Select(x => listView.managersById[x.id]);
            var allManagers = new List<ScriptBehaviourManager>();
            foreach (var world in World.AllWorlds)
                allManagers.AddRange(world.BehaviourManagers);
            Assert.AreEqual(allManagers.Count, allManagers.Intersect(managerItems).Count());
        }
        
    }
}
