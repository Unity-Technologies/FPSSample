using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Entities.Tests
{
    public class EntityTransactionTests : ECSTestsFixture
    {
        ComponentGroup m_Group;

        public EntityTransactionTests()
        {
            Assert.IsTrue(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled, "JobDebugger must be enabled for these tests");
        }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            m_Group = m_Manager.CreateComponentGroup(typeof(EcsTestData));

            // Archetypes can't be created on a job
            m_Manager.CreateArchetype(typeof(EcsTestData));
        }

        struct CreateEntityAddToListJob : IJob
        {
            public ExclusiveEntityTransaction entities;
            public NativeList<Entity> createdEntities;

            public void Execute()
            {
                var entity = entities.CreateEntity(ComponentType.Create<EcsTestData>());
                entities.SetComponentData(entity, new EcsTestData(42));
                Assert.AreEqual(42, entities.GetComponentData<EcsTestData>(entity).value);

                createdEntities.Add(entity);
            }
        }

        struct CreateEntityJob : IJob
        {
            public ExclusiveEntityTransaction entities;

            public void Execute()
            {
                var entity = entities.CreateEntity(ComponentType.Create<EcsTestData>());
                entities.SetComponentData(entity, new EcsTestData(42));
                Assert.AreEqual(42, entities.GetComponentData<EcsTestData>(entity).value);
            }
        }

        [Test]
        public void CreateEntitiesChainedJob()
        {
            var job = new CreateEntityAddToListJob();
            job.entities = m_Manager.BeginExclusiveEntityTransaction();
            job.createdEntities = new NativeList<Entity>(0, Allocator.TempJob);

            m_Manager.ExclusiveEntityTransactionDependency = job.Schedule(m_Manager.ExclusiveEntityTransactionDependency);
            m_Manager.ExclusiveEntityTransactionDependency = job.Schedule(m_Manager.ExclusiveEntityTransactionDependency);

            m_Manager.EndExclusiveEntityTransaction();

            Assert.AreEqual(2, m_Group.CalculateLength());
            Assert.AreEqual(42, m_Group.GetComponentDataArray<EcsTestData>()[0].value);
            Assert.AreEqual(42, m_Group.GetComponentDataArray<EcsTestData>()[1].value);

            Assert.IsTrue(m_Manager.Exists(job.createdEntities[0]));
            Assert.IsTrue(m_Manager.Exists(job.createdEntities[1]));

            job.createdEntities.Dispose();
        }


        [Test]
        public void CommitAfterNotRegisteredTransactionJobLogsError()
        {
            var job = new CreateEntityJob();
            job.entities = m_Manager.BeginExclusiveEntityTransaction();

            /*var jobHandle =*/ job.Schedule(m_Manager.ExclusiveEntityTransactionDependency);

            // Commit transaction expects an error not exception otherwise errors might occurr after a system has completed...
            LogAssert.Expect(LogType.Error, new Regex("ExclusiveEntityTransaction job has not been registered"));
            m_Manager.EndExclusiveEntityTransaction();
        }

        [Test]
        public void EntityManagerAccessDuringTransactionThrows()
        {
            var job = new CreateEntityAddToListJob();
            job.entities = m_Manager.BeginExclusiveEntityTransaction();

            Assert.Throws<InvalidOperationException>(() => { m_Manager.CreateEntity(typeof(EcsTestData)); });
            
            //@TODO:
            //Assert.Throws<InvalidOperationException>(() => { m_Manager.Exists(new Entity()); });
        }

        [Test]
        public void AccessTransactionAfterEndTransactionThrows()
        {
            var transaction = m_Manager.BeginExclusiveEntityTransaction();
            m_Manager.EndExclusiveEntityTransaction();

            Assert.Throws<InvalidOperationException>(() => { transaction.CreateEntity(typeof(EcsTestData)); });
        }

        [Test]
        public void AccessExistingEntityFromTransactionWorks()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.SetComponentData(entity, new EcsTestData(42));
            
            var transaction = m_Manager.BeginExclusiveEntityTransaction();
            Assert.AreEqual(42, transaction.GetComponentData<EcsTestData>(entity).value);
        }

        [Test]
        public void MissingJobCreationDependency()
        {
            var job = new CreateEntityJob();
            job.entities = m_Manager.BeginExclusiveEntityTransaction();

            var jobHandle = job.Schedule();
            Assert.Throws<InvalidOperationException>(() => { job.Schedule(); });

            jobHandle.Complete();
        }

        [Test]
        public void CreationJobAndMainThreadNotAllowedInParallel()
        {
            var job = new CreateEntityJob();
            job.entities = m_Manager.BeginExclusiveEntityTransaction();

            var jobHandle = job.Schedule();

            Assert.Throws<InvalidOperationException>(() => { job.entities.CreateEntity(typeof(EcsTestData)); });

            jobHandle.Complete();
        }

        [Test]
        public void CreatingEntitiesBeyondCapacityInTransactionWorks()
        {
            var arch = m_Manager.CreateArchetype(typeof(EcsTestData));

            var transaction = m_Manager.BeginExclusiveEntityTransaction();
            var entities = new NativeArray<Entity>(1000, Allocator.Persistent);
            transaction.CreateEntity(arch, entities);
            entities.Dispose();
        }
    }
}
