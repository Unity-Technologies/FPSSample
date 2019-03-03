using System;
using System.Collections.Generic;
using Primitives;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;

[Serializable]
public struct SplashDamageSettings
{
	public float radius;
	public float falloffStartRadius;
	public float damage;
	public float minDamage;
	public float impulse;
	public float minImpulse;
	public float ownerDamageFraction;
}

public struct SplashDamageRequest: IComponentData
{
	public Entity instigator;
	public int tick;
	public float3 center;
    public int collisionMask;
	public SplashDamageSettings settings;
	
	public static void Create(EntityCommandBuffer commandBuffer, int tick, Entity instigator, float3 center, 
		int collisionMask, SplashDamageSettings settings)
	{
		var request = new SplashDamageRequest
		{
			instigator = instigator,
			tick = tick,
			center = center,
			collisionMask = collisionMask,
			settings = settings
		};
		commandBuffer.CreateEntity();
		commandBuffer.AddComponent(request);
	}
}

[DisableAutoCreation]
public class HandleSplashDamageRequests : BaseComponentSystem
{
	ComponentGroup RequestGroup;   
	ComponentGroup ColliderGroup;

	List<HitCollisionData.CollisionResult> m_resultsBuffer = new List<HitCollisionData.CollisionResult>(32);
	List<Entity> m_resultsOwnerBuffer = new List<Entity>(32);
	
	
	public HandleSplashDamageRequests(GameWorld world) : base(world)
	{
		m_hitCollisionLayer = LayerMask.NameToLayer("hitcollision_enabled");
	}

	protected override void OnCreateManager()
	{
		base.OnCreateManager();
		RequestGroup = GetComponentGroup(typeof(SplashDamageRequest));
		ColliderGroup = GetComponentGroup(typeof(HitCollisionHistory));
	}


	
	protected override void OnUpdate()
	{
		var requestEntityArray = RequestGroup.GetEntityArray();
		var requestArray = RequestGroup.GetComponentDataArray<SplashDamageRequest>();

		
		
		
		
		
		var hitCollisionEntityArray = ColliderGroup.GetEntityArray();

		var requstCount = requestArray.Length;
		
		// Broad phase hit collision check
		var entityArray = new NativeArray<Entity>(hitCollisionEntityArray.Length,Allocator.TempJob);
		var boundsArray = new NativeArray<sphere>[requstCount];
		var broadPhaseResultArray = new NativeList<Entity>[requstCount];
		var broadPhaseHandleArray = new NativeArray<JobHandle>(requstCount, Allocator.Temp);

		// TODO (mogensh) find faster/easier way to copy entityarray to native
		for (int i = 0; i < hitCollisionEntityArray.Length; i++)
		{
			entityArray[i] = hitCollisionEntityArray[i];	
		}

		for (var i = 0; i < requstCount; i++)
		{
			boundsArray[i] = new NativeArray<sphere>(hitCollisionEntityArray.Length,Allocator.TempJob);
			broadPhaseResultArray[i] = new NativeList<Entity>(hitCollisionEntityArray.Length,Allocator.TempJob);
		}

		for (int i = 0; i < requstCount; i++)
		{
			GetBounds(EntityManager, entityArray, requestArray[i].tick,
				ref boundsArray[i]);
		}
		
		for (int i = 0; i < requstCount; i++)
		{
			var request = requestArray[i];
			var broadPhaseJob = new BroadPhaseSphereOverlapJob
			{
				entities = entityArray,
				bounds = boundsArray[i],
				sphere = new sphere(request.center, request.settings.radius),
				result = broadPhaseResultArray[i],
			};
			broadPhaseHandleArray[i] = broadPhaseJob.Schedule();
		}

		var broadPhaseHandle = JobHandle.CombineDependencies(broadPhaseHandleArray);
		broadPhaseHandle.Complete();
		
		
		
		
		for (var i = 0; i < requestArray.Length; i++)
		{
			var request = requestArray[i];
			
			// HitCollision damage
			{
				var requestSphere = new sphere(request.center, request.settings.radius); 
				var broadPhaseResult = broadPhaseResultArray[i];
				var forceIncluded = request.settings.ownerDamageFraction > 0 ? request.instigator : Entity.Null;

				m_resultsBuffer.Clear();
				m_resultsOwnerBuffer.Clear();

				SphereOverlapAll(EntityManager, ref broadPhaseResult, 
					request.tick, request.collisionMask, Entity.Null, forceIncluded, requestSphere, m_resultsBuffer, 
					m_resultsOwnerBuffer);

				for (int j = 0; j < m_resultsBuffer.Count; j++)
				{
					Damage(request.center, ref request.settings, request.instigator, m_resultsOwnerBuffer[j],
						m_resultsBuffer[j].primCenter);
				}
			}
			
			
			// Environment damage
			{
				var hitColliderMask = 1 << m_hitCollisionLayer;
				var count = Physics.OverlapSphereNonAlloc(request.center, request.settings.radius, g_colliderBuffer, hitColliderMask);
			
				var colliderCollections = new Dictionary<Entity, ClosestCollision>();
				GetClosestCollision(g_colliderBuffer, count, request.center, colliderCollections);

				foreach (var collision in colliderCollections.Values)
				{
					var collisionOwnerEntity = collision.hitCollision.owner;

					Damage(request.center, ref request.settings, request.instigator, collisionOwnerEntity,
						collision.closestPoint);
				}
			}


			PostUpdateCommands.DestroyEntity(requestEntityArray[i]);
		}
		
		broadPhaseHandleArray.Dispose();
		entityArray.Dispose();
		for (var i = 0; i < requstCount; i++)
		{
			boundsArray[i].Dispose();
			broadPhaseResultArray[i].Dispose();
		}
	}

	void Damage(float3 origin, ref SplashDamageSettings settings, Entity instigator, Entity hitCollisionOwnerEntity, float3 centerOfMass)
	{
				
		// TODO (mogens) dont hardcode center of mass - and dont get from Character. Should be set on hitCollOwner by some other system
		if (EntityManager.HasComponent<Character>(hitCollisionOwnerEntity))
		{
			var charPredicedState = EntityManager.GetComponentData<CharacterPredictedData>(hitCollisionOwnerEntity);
			centerOfMass = charPredicedState.position + Vector3.up * 1.2f;	     
		}

		// Calc damage
		var damageVector = centerOfMass - origin;
		var damageDirection = math.normalize(damageVector);
		var distance = math.length(damageVector);
		if (distance > settings.radius)
			return;

		var damage = settings.damage;
		var impulse = settings.impulse;
		if (distance > settings.falloffStartRadius)
		{
			var falloffFraction = (distance - settings.falloffStartRadius) / (settings.radius - settings.falloffStartRadius);
			damage -= (settings.damage - settings.minDamage) * falloffFraction;
			impulse -= (settings.impulse - settings.minImpulse) * falloffFraction;
		}

		if (instigator != Entity.Null && instigator == hitCollisionOwnerEntity)
			damage = damage * settings.ownerDamageFraction;

		//GameDebug.Log(string.Format("SplashDamage. Target:{0} Inst:{1}", collider.hitCollision, m_world.GetGameObjectFromEntity(instigator) ));
		var damageEventBuffer = EntityManager.GetBuffer<DamageEvent>(hitCollisionOwnerEntity);
		DamageEvent.AddEvent(damageEventBuffer, instigator, damage, damageDirection, impulse);
	}
	
	public static void SphereOverlapAll(EntityManager entityManager, 
		ref NativeList<Entity> hitColliHistoryEntityArray, int tick, int mask, 
		Entity forceExcluded, Entity forceIncluded, sphere sphere,
		List<HitCollisionData.CollisionResult> results, List<Entity> hitCollisionOwners)
	{
		for (var i = 0; i < hitColliHistoryEntityArray.Length; i++)
		{
			var entity = hitColliHistoryEntityArray[i];
			
			if(!HitCollisionData.IsRelevant(entityManager, hitColliHistoryEntityArray[i], mask, forceExcluded, forceIncluded))
				continue;

			var collectionResult = new HitCollisionData.CollisionResult();
            
			var hit = HitCollisionData.SphereOverlapSingle(entityManager, entity, tick, sphere, ref collectionResult);

			if (hit)
			{
				var hitCollisionData = entityManager.GetComponentData<HitCollisionData>(hitColliHistoryEntityArray[i]);

				results.Add(collectionResult);
				hitCollisionOwners.Add(hitCollisionData.hitCollisionOwner);
			}
		}
	}
	
	public static void GetBounds(EntityManager entityManager, NativeArray<Entity> hitCollHistEntityArray, int tick, 
		ref NativeArray<sphere> boundsArray)
	{
		for (int i = 0; i < hitCollHistEntityArray.Length; i++)
		{
			var entity = hitCollHistEntityArray[i];
            
			var collData = entityManager.GetComponentData<HitCollisionData>(entity);
			var historyBuffer = entityManager.GetBuffer<HitCollisionData.BoundsHistory>(entity);

			var histIndex = collData.GetHistoryIndex(tick);
			var boundSphere = primlib.sphere(historyBuffer[histIndex].pos, collData.boundsRadius);
			boundsArray[i] = boundSphere;
		}
	}
	
	public struct ClosestCollision
	{
		public HitCollision hitCollision;
		public Vector3 closestPoint;
		public float dist;
	}

	public static void GetClosestCollision(Collider[] colliders, int colliderCount, Vector3 origin, Dictionary<Entity, ClosestCollision> colliderOwners)
	{
		for (int i = 0; i < colliderCount; i++)
		{
			var collider = colliders[i];

			var hitCollision = collider.GetComponent<HitCollision>();
			if (hitCollision == null)
			{
				GameDebug.LogError("Collider:" + collider + " has no hitcollision");
				continue;
			}

			var closestPoint = collider.transform.position;
			float dist = Vector3.Distance(origin, closestPoint);

			ClosestCollision currentClosest;
			if (colliderOwners.TryGetValue(hitCollision.owner, out currentClosest))
			{
				if (dist < currentClosest.dist)
				{
					currentClosest.hitCollision = hitCollision;
					currentClosest.closestPoint = closestPoint;
					currentClosest.dist = dist;
					colliderOwners[hitCollision.owner] = currentClosest;
				}
			}
			else
			{
				currentClosest.hitCollision = hitCollision;
				currentClosest.closestPoint = closestPoint;
				currentClosest.dist = dist;
				colliderOwners.Add(hitCollision.owner, currentClosest);
			}
		}
	}


	private readonly Collider[] g_colliderBuffer = new Collider[128];
	readonly int m_hitCollisionLayer;
}
