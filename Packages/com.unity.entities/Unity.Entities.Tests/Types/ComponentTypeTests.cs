using NUnit.Framework;

namespace Unity.Entities.Tests.Types
{
    [TestFixture]
    public class ComponentTypeTests : ECSTestsFixture
    {
        [Test]
        public void EqualityOperator_WhenEqual_ReturnsTrue()
        {
            var t1 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);
            var t2 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);

            var result = t1 == t2;

            Assert.IsTrue(result);
        }

        [Test]
        public void EqualityOperator_WhenDifferentType_ReturnsFalse()
        {
            var t1 = new ComponentType(typeof(EmptySystem), ComponentType.AccessMode.ReadOnly);
            var t2 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);

            var result = t1 == t2;

            Assert.IsFalse(result);
        }

        [Test]
        public void EqualityOperator_WhenDifferentAccessMode_ReturnsFalse()
        {
            var t1 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadWrite);
            var t2 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);

            var result = t1 == t2;

            Assert.IsFalse(result);
        }

        [Test]
        public void InequalityOperator_WhenEqual_ReturnsFalse()
        {
            var t1 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);
            var t2 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);

            var result = t1 != t2;

            Assert.IsFalse(result);
        }

        [Test]
        public void InequalityOperator_WhenDifferentType_ReturnsTrue()
        {
            var t1 = new ComponentType(typeof(EmptySystem), ComponentType.AccessMode.ReadOnly);
            var t2 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);

            var result = t1 != t2;

            Assert.IsTrue(result);
        }

        [Test]
        public void InequalityOperator_WhenDifferentAccessMode_ReturnsTrue()
        {
            var t1 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadWrite);
            var t2 = new ComponentType(typeof(Entity), ComponentType.AccessMode.ReadOnly);

            var result = t1 != t2;

            Assert.IsTrue(result);
        }
    }
}
