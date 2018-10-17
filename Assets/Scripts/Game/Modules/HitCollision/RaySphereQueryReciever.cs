using static CollisionLib.coll;
using Unity.Collections;
using Unity.Entities;
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
		public float distance;

		public int testAgainsEnvironment;

		public int hitCollisionTestTick;
		public float sphereCastRadius;
		public Entity sphereCastExcludeOwner;
		public int sphereCastMask;
	}

	public struct Result
	{
		public int hit;
		public Entity hitCollisionOwner;
		public Collider collider;
		public float3 hitPoint;
		public float3 hitNormal;
	}
	
	[ConfigVar(Name = "collision.raysphere.debug", DefaultValue = "0", Description = "Show collision query debug", Flags = ConfigVar.Flags.None)]
	public static ConfigVar showDebug;
    
	[ConfigVar(Name = "collision.raysphere.debugduration", DefaultValue = "2", Description = "Show collision query debug", Flags = ConfigVar.Flags.None)]
	public static ConfigVar debugDuration;
	
	private const int c_bufferSize = 64;
	
	Query[] m_queries = new Query[c_bufferSize];
	Result[] m_results = new Result[c_bufferSize];
	bool[] m_resultReady = new bool[c_bufferSize];
	
	int m_nextRequestId;
	int m_lastHandledRequestId = -1;

	readonly RaycastHit[] raycastHitBuffer = new RaycastHit[128];
	readonly int m_defaultLayer;
	readonly int m_detailLayer;
	readonly int m_teamAreaALayer;
	readonly int m_teamAreaBLayer;
	readonly int m_environmentMask;
	readonly int m_hitCollisionLayer;

	ComponentGroup m_colliderGroup;
	
	public RaySphereQueryReciever(GameWorld world) : base(world) 
	{
		m_defaultLayer = LayerMask.NameToLayer("Default");
		m_detailLayer = LayerMask.NameToLayer("collision_detail");
		m_teamAreaALayer = LayerMask.NameToLayer("TeamAreaA");
		m_teamAreaBLayer = LayerMask.NameToLayer("TeamAreaB");
		m_hitCollisionLayer = LayerMask.NameToLayer("hitcollision_enabled");
		m_environmentMask = 1 << m_defaultLayer | 1 << m_detailLayer | 1 << m_teamAreaALayer | 1 << m_teamAreaBLayer;
	}
	
	protected override void OnCreateManager(int capacity)
	{
		m_colliderGroup = GetComponentGroup(typeof(HitCollisionHistory));
		base.OnCreateManager(capacity);
	}

	public int RegisterQuery(Query query)
	{
		var requestId = m_nextRequestId;
		var index = requestId % c_bufferSize;
		
		GameDebug.Assert(m_resultReady[index] == false,
			"Attempting to write to id:" + requestId + " that has not been read yet");
		
		m_queries[index] = query;
		m_nextRequestId++;
		
		var requestCount = m_nextRequestId - m_lastHandledRequestId - 1;
		if (requestCount >= 10)
			HandleRequests();

//		GameDebug.Log("Add Request id:" + requestId);
		
		return requestId;
	}

	public void GetResult(int requestId, out Query query, out Result result)
	{
//		GameDebug.Log("Get result id:" + requestId);

		var index = requestId % c_bufferSize;
		
		// Result not ready so we need to handle requests
		if (!m_resultReady[index])
		{
			HandleRequests();
		}

		query = m_queries[index];
		result = m_results[index];
		
		GameDebug.Assert(m_resultReady[index] == true,"Result for id:" + requestId + " not ready");
		
		m_resultReady[index] = false;
	}

	void HandleRequests()
	{
		var startId = m_lastHandledRequestId + 1;
		var endId = m_nextRequestId - 1;
		var count = endId - startId + 1;
		
		var queries = new Query[count];
		var results = new Result[count];

		var bufferIndex = 0;
		for (var id = startId; id <= endId; id++)
		{
			var index = id % c_bufferSize;
			queries[bufferIndex] = m_queries[index];
			bufferIndex++;
		}

		var hitCollisionArray = m_colliderGroup.GetComponentArray<HitCollisionHistory>();
		HandleRequests(count, queries, results, hitCollisionArray, m_environmentMask, m_hitCollisionLayer, raycastHitBuffer);
		
		bufferIndex = 0;
		for (var id = startId; id <= endId; id++)
		{
			var index = id % c_bufferSize;
			m_results[index] = results[bufferIndex];
			
			if (m_results[index].collider != null && m_results[index].collider.transform.parent != null)
			{
				var collider = m_results[index].collider;
				var hitCollision = collider.GetComponent<HitCollision>();
				if (hitCollision != null)
				{
					m_results[index].hitCollisionOwner = hitCollision.owner.GetComponent<GameObjectEntity>().Entity;
				}
			}
				
			if (showDebug.IntValue > 0)
				Debug.DrawLine(m_queries[index].origin, m_queries[index].origin + m_queries[index].direction * m_queries[index].distance,
					m_results[index].hitCollisionOwner != Entity.Null ? Color.red : Color.green, debugDuration.FloatValue);
			if (showDebug.IntValue > 0 && m_results[index].hitCollisionOwner != Entity.Null)
			{
				DebugDraw.Sphere(m_results[index].hitPoint, 1.0f, Color.red, debugDuration.FloatValue);
				Debug.DrawLine(m_results[index].hitPoint,
					EntityManager.GetComponentObject<Transform>(m_results[index].hitCollisionOwner).position, Color.red,
					debugDuration.FloatValue);
			}
			
			m_resultReady[index] = true;
			bufferIndex++;
		}
		m_lastHandledRequestId = endId;
	}


	static void HandleRequests(int queryCount, Query[] queries, Result[] results, ComponentArray<HitCollisionHistory> hitCollisionArray,
		int environmentMask, int hitCollisionLayer, RaycastHit[] raycastHitBuffer)
	{
//		GameDebug.Log("HandleRequests id:" + startId + " to " + endId + "  index:" + startId%c_bufferSize + " to " + endId%c_bufferSize);
		
		// First perform collision test against environment.New endpoint (if environment collision is found) is 
		// used when testing agains hitcollision. If no hitcollision found damage will be applied to environment
		var rayTestCount = 0;
		for (var i = 0; i < queryCount; i++)
		{
			var query = queries[i];
			if (query.testAgainsEnvironment == 0)
				continue;
			rayTestCount++;
		}

		Profiler.BeginSample("-create ray commands");

		var rayCommands =
			new NativeArray<RaycastCommand>(rayTestCount,Allocator.TempJob); 
		var rayResults = new NativeArray<RaycastHit>(rayTestCount, Allocator.TempJob);

		var rayTestIndex = 0;
		for (var i = 0; i < queryCount; i++)
		{
			var query = queries[i];
			if (query.testAgainsEnvironment == 0)
				continue;

			rayCommands[rayTestIndex] =
				new RaycastCommand(query.origin, query.direction, query.distance, environmentMask);

			rayTestIndex++;
		}

		Profiler.EndSample();

		Profiler.BeginSample("-excute ray commands");
		var handle =
			RaycastCommand.ScheduleBatch(rayCommands, rayResults,10); 
		handle.Complete();
		Profiler.EndSample();

		// Test collision with hitCollision
		rayTestIndex = 0;
		for (var i = 0; i < queryCount; i++)
		{
			var query = queries[i];

			// Handle raytest result
			var environmentHit = false;
			var environmentPoint = Vector3.zero;
			var environmentNormal = Vector3.zero;
			Collider environmentCollider = null;
			if (query.testAgainsEnvironment == 1)
			{
				environmentCollider = rayResults[rayTestIndex].collider;
				var impact = environmentCollider != null;
				if (impact)
				{
					environmentHit = true;
					environmentPoint = rayResults[rayTestIndex].point;
					environmentNormal = rayResults[rayTestIndex].normal;
					
					// query distance is adjusted so followup tests only are done before environment hit point 
					query.distance = rayResults[rayTestIndex].distance;
													   }

				if (showDebug.IntValue > 0)
					Debug.DrawLine(query.origin, query.origin + query.direction * query.distance,
						impact ? Color.red : Color.green, debugDuration.FloatValue);

				rayTestIndex++;
			}

			var result = new Result();
			
			if (query.sphereCastRadius == 0)
			{
				HitCollisionHistory.PrepareColliders(ref hitCollisionArray, query.hitCollisionTestTick,query.sphereCastMask,
					query.sphereCastExcludeOwner, Entity.Null, ray(query.origin, query.direction), query.distance);

				// HitCollision test
				var count = Physics.RaycastNonAlloc(query.origin, query.direction, raycastHitBuffer, query.distance,
					1 << hitCollisionLayer);
				if (count > 0)
				{
					var closestIndex = GetClosestHit(raycastHitBuffer, count, query.origin);
					result.hit = 1;
					result.collider = raycastHitBuffer[closestIndex].collider;
					result.hitPoint = raycastHitBuffer[closestIndex].point;
					result.hitNormal = raycastHitBuffer[closestIndex].normal;
				}
				else
				{
					result.hitCollisionOwner = Entity.Null;
				}
			}
			else
			{
				HitCollisionHistory.PrepareColliders(ref hitCollisionArray, query.hitCollisionTestTick, query.sphereCastMask,
					query.sphereCastExcludeOwner, Entity.Null, ray(query.origin, query.direction), query.distance,
					query.sphereCastRadius);

				var count = Physics.SphereCastNonAlloc(query.origin, query.sphereCastRadius, query.direction,
					raycastHitBuffer, query.distance, 1 << hitCollisionLayer);

				if (count > 0)
				{
					var closestIndex = GetClosestHit(raycastHitBuffer, count, query.origin);

					result.hit = 1;
					result.collider = raycastHitBuffer[closestIndex].collider;
					result.hitPoint = raycastHitBuffer[closestIndex].point;
					result.hitNormal = raycastHitBuffer[closestIndex].normal;
				}
				else
				{
					result.hitCollisionOwner = Entity.Null;
				}
			}

			// If no hitCollision found we use environment hit results
			if (result.hit == 0 && environmentHit)
			{
				result.hit = 1;
				result.hitPoint = environmentPoint;
				result.hitNormal = environmentNormal;
				result.collider = environmentCollider;
			}
			
			// Flag result as ready
			results[i] = result;
		}

		rayCommands.Dispose();
		rayResults.Dispose();
	}

	static int GetClosestHit(RaycastHit[] hits, int hitCount, float3 position)
	{
		if (hitCount == 1)
			return 0;
	
		var minDist = float.MaxValue;
		var minIndex = -1;
		for (var i = 0; i < hitCount; i++)
		{
			var dist = math.distance(hits[i].point, position);
			if (dist >= minDist) 
				continue;
		
			minDist = dist;
			minIndex = i;
		}
		return minIndex;
	}

	protected override void OnUpdate()
	{
	}
}

