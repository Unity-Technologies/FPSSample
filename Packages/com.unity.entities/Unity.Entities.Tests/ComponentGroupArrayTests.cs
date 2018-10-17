using NUnit.Framework;
using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Entities.Tests
{
    public class ComponentGroupArrayTests : ECSTestsFixture
	{
        public ComponentGroupArrayTests()
        {
            Assert.IsTrue(Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobDebuggerEnabled, "JobDebugger must be enabled for these tests");
        }

		struct TestCopy1To2Job : IJob
		{
			public ComponentGroupArray<TestEntity> entities;
			unsafe public void Execute()
			{
				foreach (var e in entities)
					e.testData2->value0 = e.testData->value; 
			}
		}
		
		struct TestReadOnlyJob : IJob
		{
			public ComponentGroupArray<TestEntityReadOnly> entities;
			public void Execute()
			{
				foreach (var e in entities)
					;
			}
		}
		
		
	    //@TODO: Test for Entity setup with same component twice...
	    //@TODO: Test for subtractive components
	    //@TODO: Test for process ComponentGroupArray in job
	    
	    unsafe struct TestEntity
	    {
	        [ReadOnly]
	        public EcsTestData* testData;
	        public EcsTestData2* testData2;
	    }

		unsafe struct TestEntityReadOnly
		{
			[ReadOnly]
			public EcsTestData* testData;
			[ReadOnly]
			public EcsTestData2* testData2;
		}
		
	    [Test]
	    public void ComponentAccessAfterScheduledJobThrowsEntityArray()
	    {
	        m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));

	        var job = new TestCopy1To2Job();
		    job.entities = EmptySystem.GetEntities<TestEntity>();

	        var fence = job.Schedule();

	        var entityArray = EmptySystem.GetEntities<TestEntity>();
	        Assert.Throws<System.InvalidOperationException>(() => { var temp = entityArray[0]; });

	        fence.Complete();
	    }
			
	    [Test]
	    public void ComponentGroupArrayJobScheduleDetectsWriteDependency()
	    {
	        var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
	        m_Manager.SetComponentData(entity, new EcsTestData(42));

	        var job = new TestCopy1To2Job();
	        job.entities = EmptySystem.GetEntities<TestEntity>();
	        
	        var fence = job.Schedule();
			Assert.Throws<System.InvalidOperationException>(() => { job.Schedule(); });
			
	        fence.Complete();
	    }
		
		[Test]
		public void ComponentGroupArrayJobScheduleReadOnlyParallelIsAllowed()
		{
			var entity = m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
			m_Manager.SetComponentData(entity, new EcsTestData(42));

			var job = new TestReadOnlyJob();
		    job.entities = EmptySystem.GetEntities<TestEntityReadOnly>();

			var fence = job.Schedule();
			var fence2 = job.Schedule();
			
			JobHandle.CompleteAll(ref fence, ref fence2);
		}

		unsafe struct TestEntitySub2
		{
			public EcsTestData* testData;
			public SubtractiveComponent<EcsTestData2> testData2;
		}
		
		[Test]
		public void ComponentGroupArraySubtractive()
		{
			m_Manager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
			m_Manager.CreateEntity(typeof(EcsTestData));

		    var entities = EmptySystem.GetEntities<TestEntitySub2>();
			Assert.AreEqual(1, entities.Length);
		}
    }
}