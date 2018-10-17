using System;
using NUnit.Framework;
using Unity.Entities.Tests;
using UnityEditor;
using UnityEngine;

namespace Unity.Entities.Editor.Tests
{
    public class ComponentTypeFilterUITests : ECSTestsFixture
    {

        public void SetFilterDummy(EntityListQuery query)
        {
            
        }

        private World WorldSelectionGetter()
        {
            return World.Active;
        }

        [Test]
        public void ComponentTypeFilterUI_GetTypesIgnoresNullWorld()
        {
            var filterUI = new ComponentTypeFilterUI(SetFilterDummy, () => null);
            Assert.DoesNotThrow(filterUI.GetTypes);
        }
        
        [Test]
        public void ComponentTypeFilterUI_ComparisonToTypeManagerCorrect()
        {
            TypeManager.GetTypeIndex(typeof(EcsTestData));
            var filterUI = new ComponentTypeFilterUI(SetFilterDummy, WorldSelectionGetter);
            Assert.IsFalse(filterUI.TypeListValid());
            filterUI.GetTypes();
            Assert.IsTrue(filterUI.TypeListValid());
        }

        [Test]
        public void ComponentTypeFilterUI_ComponentGroupCaches()
        {
            var filterUI = new ComponentTypeFilterUI(SetFilterDummy, WorldSelectionGetter);
            var types = new ComponentType[]
                {ComponentType.Create<EcsTestData>(), ComponentType.ReadOnly<EcsTestData2>()};
            Assert.IsNull(filterUI.GetExistingGroup(types));
            var group = filterUI.GetComponentGroup(types);
            Assert.AreEqual(group, filterUI.GetExistingGroup(types));
        }
    }
}
