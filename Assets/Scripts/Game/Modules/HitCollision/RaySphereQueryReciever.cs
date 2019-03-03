using System.Collections.Generic;
using CollisionLib;
using Primitives;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

[DisableAutoCreation]
public class RaySphereQueryReciever : BaseComponentSystem
{
	public struct Query
	{
		public float3 origin;
		public float3 direction;
		public float radius;
		public float distance;

		public int hitCollisionTestTick;
		
		public Entity ExcludeOwner;
		public uint mask;
	}

	public struct QueryResult
	{
		public int hit;
		public Entity hitCollisionOwner;
		public float3 hitPoint;
		public float3 hitNormal;
	}

	public class QueryBatch
	{
		public List<int> queryIds = new List<int>();

		public List<DynamicBuffer<HitCollisionData.BoundsHistory>> boundsHistory 
			= new List<DynamicBuffer<HitCollisionData.BoundsHistory>>();
		
		public void Prepare(int count)
		{
			if (queryIds.Capacity < count)
				queryIds.Capacity = count;
			queryIds.Clear();
			if (boundsHistory.Capacity < count)
				boundsHistory.Capacity = count;
			boundsHistory.Clear();
		}
	}

	public class QueryData
	{
		public Query query;
		public QueryResult result;


		// Broad phase test
		public BroadPhaseSphereCastJob broadTestJob;
		public NativeArray<sphere> broadPhaseBounds;
		
		// Narrow phase test
		public List<SphereCastSingleJob> narrowTestJobs = new List<SphereCastSingleJob>(128);
	}
	
	[ConfigVar(Name = "collision.raysphere.debug", DefaultValue = "0", Description = "Show collision query debug", Flags = ConfigVar.Flags.None)]
	public static ConfigVar showDebug;
    
	[ConfigVar(Name = "collision.raysphere.debugduration", DefaultValue = "2", Description = "Show collision query debug", Flags = ConfigVar.Flags.None)]
	public static ConfigVar debugDuration;
	
	private const int c_bufferSize = 64;

	private List<QueryData> m_queries = new List<QueryData>(128);
	private Queue<int> m_freeQueryIds = new Queue<int>(128);
	private List<int> m_incommingQueryIds = new List<int>(128);

//	readonly RaycastHit[] raycastHitBuffer = new RaycastHit[128];
	readonly int m_defaultLayer;
	readonly int m_detailLayer;
	readonly int m_teamAreaALayer;
	readonly int m_teamAreaBLayer;
	readonly int m_environmentMask;
	readonly int m_hitCollisionLayer;

	ComponentGroup m_colliderGroup;

	QueryBatch m_batch = new QueryBatch();
	
	public RaySphereQueryReciever(GameWorld world) : base(world) 
	{
		m_defaultLayer = LayerMask.NameToLayer("Default");
		m_detailLayer = LayerMask.NameToLayer("collision_detail");
		m_teamAreaALayer = LayerMask.NameToLayer("TeamAreaA");
		m_teamAreaBLayer = LayerMask.NameToLayer("TeamAreaB");
		m_hitCollisionLayer = LayerMask.NameToLayer("hitcollision_enabled");
		m_environmentMask = 1 << m_defaultLayer | 1 << m_detailLayer | 1 << m_teamAreaALayer | 1 << m_teamAreaBLayer 
		                    | 1 << m_hitCollisionLayer;
	}
	
	protected override void OnCreateManager()
	{
		m_colliderGroup = GetComponentGroup(typeof(HitCollisionHistory), typeof(HitCollisionData));
		base.OnCreateManager();
	}

	public int RegisterQuery(Query query)
	{
		QueryData queryData;
		int queryId;
		if (m_freeQueryIds.Count > 0)
		{
			queryId = m_freeQueryIds.Dequeue();
			queryData = m_queries[queryId];
		}
		else
		{
			queryData = new QueryData();
			queryId = m_queries.Count;
			m_queries.Add(queryData);
		}
		
		m_incommingQueryIds.Add(queryId);

//		GameDebug.Assert(queryData.state == QueryData.State.Idle);

		queryData.query = query;
		queryData.result = new QueryResult();
		
		return queryId;
	}

	public void GetResult(int requestId, out Query query, out QueryResult result)
	{
		Profiler.BeginSample("RaySphereQueryReciever.GetResult");		

		//		GameDebug.Log("Get result id:" + requestId);

		// Update all incomming queries
		if (m_incommingQueryIds.Count > 0)
		{
			m_batch.Prepare(m_incommingQueryIds.Count);
			m_batch.queryIds.AddRange(m_incommingQueryIds);
			UpdateBatch(m_batch);
			m_incommingQueryIds.Clear();
		}
		
		var queryData = m_queries[requestId];

		query = queryData.query;
		result = queryData.result;

		m_freeQueryIds.Enqueue(requestId);		
		Profiler.EndSample();
	}


	void UpdateBatch(QueryBatch queryBatch)
	{
		Profiler.BeginSample("UpdateBatch");
		var queryCount = queryBatch.queryIds.Count;
	
		Profiler.BeginSample("Get hitcollision entities");
		var hitCollEntityArray = m_colliderGroup.GetEntityArray();
		var hitCollDataArray = m_colliderGroup.GetComponentDataArray<HitCollisionData>();
		var hitColliders = new NativeList<Entity>(hitCollEntityArray.Length,Allocator.TempJob);
		var hitColliderData = new NativeList<HitCollisionData>(hitCollEntityArray.Length,Allocator.TempJob);
		var hitColliderFlags = new NativeList<uint>(hitCollEntityArray.Length,Allocator.TempJob);
		for (int i = 0; i < hitCollEntityArray.Length; i++)
		{
			var hitCollisionOwner = 
				EntityManager.GetComponentData<HitCollisionOwnerData>(hitCollDataArray[i].hitCollisionOwner);
	
			if (hitCollisionOwner.collisionEnabled == 0)
				continue;
			
			queryBatch.boundsHistory.Add(EntityManager.GetBuffer<HitCollisionData.BoundsHistory>(hitCollEntityArray[i]));
			hitColliderData.Add(hitCollDataArray[i]);
			hitColliders.Add(hitCollEntityArray[i]);
			
			hitColliderFlags.Add(hitCollisionOwner.colliderFlags);
		}
		Profiler.EndSample();
		

		// Environment test
		Profiler.BeginSample("Environment test");
		var envTestCommands = new NativeArray<RaycastCommand>(queryCount,Allocator.TempJob); 
		var envTestResults = new NativeArray<RaycastHit>(queryCount, Allocator.TempJob);
		for (int nQuery = 0; nQuery < queryCount; nQuery++)
		{
			var queryId = queryBatch.queryIds[nQuery];
			var queryData = m_queries[queryId];

			// Start environment test
			var query = queryData.query;
			envTestCommands[nQuery] = new RaycastCommand(query.origin, query.direction, query.distance, m_environmentMask);
		}
		var envTestHandle = RaycastCommand.ScheduleBatch(envTestCommands,envTestResults,10);
		envTestHandle.Complete();
		Profiler.EndSample();

		Profiler.BeginSample("Handle environment test");
		for (int nQuery = 0; nQuery < queryCount; nQuery++)
		{
			var queryId = queryBatch.queryIds[nQuery];
			var queryData = m_queries[queryId];

			var result = envTestResults[nQuery];
			var impact = result.collider != null;

			// query distance is adjusted so followup tests only are done before environment hit point 
			if (impact)
			{
				queryData.query.distance = result.distance;

				// Set environment as default hit. Will be overwritten if HitCollision is hit				
				queryData.result.hit = 1;
				queryData.result.hitPoint = result.point;
				queryData.result.hitNormal = result.normal;
				if (result.collider.gameObject.layer == m_hitCollisionLayer)
				{
					var hitCollision = result.collider.GetComponent<HitCollision>();
					if (hitCollision != null)
					{
						queryData.result.hitCollisionOwner = hitCollision.owner;
					}
				}
			}
		}
		Profiler.EndSample();
		
		// Start broadphase tests
		Profiler.BeginSample("Broadphase test");
		var broadphaseHandels = new NativeArray<JobHandle>(queryCount, Allocator.Temp);
		for (int nQuery = 0; nQuery < queryCount; nQuery++)
		{
			var queryId = queryBatch.queryIds[nQuery];
			var queryData = m_queries[queryId];
			var query = queryData.query; 
			
			queryData.broadPhaseBounds = new NativeArray<sphere>(hitColliderData.Length,Allocator.TempJob);
			for (int i = 0; i < hitColliderData.Length; i++)
			{
				// Get bounds for tick
				var histIndex = hitColliderData[i].GetHistoryIndex(query.hitCollisionTestTick);
				var boundSphere = primlib.sphere(queryBatch.boundsHistory[i][histIndex].pos, 
					hitColliderData[i].boundsRadius);
				queryData.broadPhaseBounds[i] = boundSphere;
			}
			
			queryData.broadTestJob = new BroadPhaseSphereCastJob(hitColliders, hitColliderData, 
				hitColliderFlags, queryData.broadPhaseBounds, query.ExcludeOwner, Entity.Null, query.mask, 
				new ray(query.origin, query.direction), query.distance, query.radius);
			
			broadphaseHandels[nQuery] = queryData.broadTestJob.Schedule();
		}
		var broadphaseHandle = JobHandle.CombineDependencies(broadphaseHandels);
		broadphaseHandels.Dispose();
		broadphaseHandle.Complete();	
		Profiler.EndSample();
			

		// Start narrow tests
		Profiler.BeginSample("Narrow test");
		
		// TODO (mogensh) find out how to combine jobs without "write to same native" issue
		//var handles = new NativeArray<JobHandle>(queryCount, Allocator.TempJob);
		for (int nQuery = 0; nQuery < queryCount; nQuery++)
		{
			var queryId = queryBatch.queryIds[nQuery];
			var queryData = m_queries[queryId];

			var query = queryData.query;
			var broadPhaseResult = queryData.broadTestJob.result;
						
			// Start narrow tests
			queryData.narrowTestJobs.Clear();
	
			//var narrowTestHandles = new NativeArray<JobHandle>(broadPhaseResult.Length, Allocator.Temp);
			for (var i = 0; i < broadPhaseResult.Length; i++)
			{
				var entity = broadPhaseResult[i];
				var ray = new ray(query.origin, query.direction);
				queryData.narrowTestJobs.Add(new SphereCastSingleJob(EntityManager, entity, ray, 
					query.distance, query.radius, query.hitCollisionTestTick));
							
//				narrowTestHandles[i] = queryData.narrowTestJobs[i].Schedule();
				var handle = queryData.narrowTestJobs[i].Schedule();
				handle.Complete();
			}
			
			//handles[nQuery] = JobHandle.CombineDependencies(narrowTestHandles); 
//			narrowTestHandles.Dispose();
			
		}
//		var handle = JobHandle.CombineDependencies(handles); 
//		handles.Dispose();
//		handle.Complete();	
		Profiler.EndSample();
		
		// Find closest
		Profiler.BeginSample("Find closest");
		for (int nQuery = 0; nQuery < queryBatch.queryIds.Count; nQuery++)
		{
			var queryId = queryBatch.queryIds[nQuery];
			var queryData = m_queries[queryId];

			var query = queryData.query; 
						
			var closestIndex = -1;
			var closestDist = float.MaxValue;
						
			for (int i = 0; i < queryData.narrowTestJobs.Count; i++)
			{
				var result = queryData.narrowTestJobs[i].result[0];
				var hit = result.hit == 1;
				if (hit)
				{
					var dist = math.distance(query.origin, result.primCenter);
					if (dist < closestDist)
					{
						closestDist = dist;
						closestIndex = i;
					}
				}
			}
	
			if (closestIndex != -1)
			{
				var result = queryData.narrowTestJobs[closestIndex].result[0];
				queryData.result.hit = 1;
				queryData.result.hitPoint = result.primCenter;
				queryData.result.hitNormal = - queryData.query.direction; // TODO (mogensh) find correct normal
	
				var hitCollisionData = EntityManager.GetComponentData<HitCollisionData>(
					queryData.narrowTestJobs[closestIndex].hitCollObject);
	
				queryData.result.hitCollisionOwner = hitCollisionData.hitCollisionOwner;
			}
	
						
			// TODO (mogensh) keep native arrays for next query
			queryData.broadTestJob.Dispose();
						
	
			for (int i = 0; i < queryData.narrowTestJobs.Count; i++)
			{
				queryData.narrowTestJobs[i].Dispose();
			}
		}
		Profiler.EndSample();



		for (int nQuery = 0; nQuery < queryBatch.queryIds.Count; nQuery++)
		{
			var queryId = queryBatch.queryIds[nQuery];
			var queryData = m_queries[queryId];
			queryData.broadPhaseBounds.Dispose();
		}

		envTestCommands.Dispose();
		envTestResults.Dispose();
		
		hitColliders.Dispose();
		hitColliderData.Dispose();
		hitColliderFlags.Dispose();
		
		Profiler.EndSample();
	}

	protected override void OnUpdate()
	{

	
	}
}
