using NUnit.Framework;
using Unity.Entities;
using Unity.Collections;

namespace Unity.Entities.Tests
{
    public class EntityRemapUtilityTests
    {
        NativeArray<EntityRemapUtility.EntityRemapInfo> m_Remapping;

        [SetUp]
        public void Setup()
        {
            m_Remapping = new NativeArray<EntityRemapUtility.EntityRemapInfo>(100, Allocator.Persistent);
        }

        [TearDown]
        public void TearDown()
        {
            m_Remapping.Dispose();
        }

        [Test]
        [Ignore("NOT IMPLEMENTED")]
        public void AddEntityRemappingThrowsForInvalidSource()
        {

        }

        [Test]
        [Ignore("NOT IMPLEMENTED")]
        public void RemapEntityThrowsForInvalidSource()
        {

        }

        [Test]
        public void RemapEntityMapsSourceToTarget()
        {
            var a = new Entity { Index = 1, Version = 2 };
            var b = new Entity { Index = 3, Version = 5 };
            EntityRemapUtility.AddEntityRemapping(ref m_Remapping, a, b);

            Assert.AreEqual(b, EntityRemapUtility.RemapEntity(ref m_Remapping, a));
        }

        [Test]
        public void RemapEntityMapsNonExistentSourceToNull()
        {
            var a = new Entity { Index = 1, Version = 2 };
            var b = new Entity { Index = 3, Version = 5 };
            var oldA = new Entity { Index = 1, Version = 1 };
            EntityRemapUtility.AddEntityRemapping(ref m_Remapping, a, b);

            Assert.AreEqual(Entity.Null, EntityRemapUtility.RemapEntity(ref m_Remapping, oldA));
        }

        [Test]
        public void RemapEntityMapsNullSourceToNull()
        {
            Assert.AreEqual(Entity.Null, EntityRemapUtility.RemapEntity(ref m_Remapping, Entity.Null));
        }

        struct EmptyStruct
        {
        }

        [Test]
        public void CalculateEntityOffsetsReturnsNullIfNoEntities()
        {
            var offsets = EntityRemapUtility.CalculateEntityOffsets(typeof(EmptyStruct));
            Assert.AreEqual(null, offsets);
        }

        [Test]
        public void CalculateEntityOffsetsReturns0IfEntity()
        {
            var offsets = EntityRemapUtility.CalculateEntityOffsets(typeof(Entity));
            Assert.AreEqual(1, offsets.Length);
            Assert.AreEqual(0, offsets[0].Offset);
        }


        struct TwoEntityStruct
        {
            // The offsets of these fields are accessed through reflection
#pragma warning disable 0169
            Entity a;
            int b;
            Entity c;
            float d;
#pragma warning restore 0169
        }

        [Test]
        public void CalculateEntityOffsetsReturnsOffsetsOfEntities()
        {
            var offsets = EntityRemapUtility.CalculateEntityOffsets(typeof(TwoEntityStruct));
            Assert.AreEqual(2, offsets.Length);
            Assert.AreEqual(0, offsets[0].Offset);
            Assert.AreEqual(12, offsets[1].Offset);
        }

        struct EmbeddedEntityStruct
        {
            // The offsets of these fields are accessed through reflection
#pragma warning disable 0169
            int a;
            TwoEntityStruct b;
#pragma warning restore 0169
        }

        [Test]
        public void CalculateEntityOffsetsReturnsOffsetsOfEmbeddedEntities()
        {
            var offsets = EntityRemapUtility.CalculateEntityOffsets(typeof(EmbeddedEntityStruct));
            Assert.AreEqual(2, offsets.Length);
            Assert.AreEqual(4, offsets[0].Offset);
            Assert.AreEqual(16, offsets[1].Offset);
        }
    }
}
