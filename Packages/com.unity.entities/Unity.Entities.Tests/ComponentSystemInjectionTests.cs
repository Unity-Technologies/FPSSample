using NUnit.Framework;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    public class ComponentSystemInjectionTests : ECSTestsFixture
    {
        [DisableAutoCreation]
        class TestSystem : ComponentSystem
        {
            protected override void OnUpdate()
            {
            }
        }
        
        [DisableAutoCreation]
        class AttributeInjectionSystem : ComponentSystem
        {
            [Inject] 
            public TestSystem test;

            protected override void OnUpdate()
            {
            }
        }
        
        [DisableAutoCreation]
        class ConstructorInjectionSystem : ComponentSystem
        {
            public string test;

            public ConstructorInjectionSystem(string value)
            {
                this.test = value;
            }

            protected override void OnUpdate()
            {
            }
        }

        [Test]
        public void ConstructorInjection()
        {
            var hello = "HelloWorld";
            var system = World.CreateManager<ConstructorInjectionSystem>(hello);
            Assert.AreEqual(hello, system.test);
        }
        
        [Test]
        public void AttributeInjectionCreates()
        {
            var system = World.CreateManager<AttributeInjectionSystem>();
            Assert.AreEqual(World.GetOrCreateManager<TestSystem>(), system.test);
        }
        
        [Test]
        public void AttributeInjectionAlreadyCreated()
        {
            var test = World.CreateManager<TestSystem>();
            var system = World.CreateManager<AttributeInjectionSystem>();
            Assert.AreEqual(test, system.test);
        }
    }
}