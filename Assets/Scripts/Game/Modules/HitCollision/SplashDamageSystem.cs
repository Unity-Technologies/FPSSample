using System;
using System.Collections.Generic;
using Primitives;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;

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
	struct Requests
	{
		public EntityArray entities;
		public ComponentDataArray<SplashDamageRequest> requests;
	}

	struct HitColliders
	{
		public ComponentArray<HitCollisionHistory> hitCollisionArray;
	}

	[Inject] 
	Requests RequestGroup;   

	[Inject] 
	HitColliders ColliderGroup;

	public HandleSplashDamageRequests(GameWorld world) : base(world)
	{
		m_hitCollisionLayer = LayerMask.NameToLayer("hitcollision_enabled");
	}

	protected override void OnUpdate()
	{
		for (var i = 0; i < RequestGroup.requests.Length; i++)
		{
			var request = RequestGroup.requests[i];

            var forceIncluded = request.settings.ownerDamageFraction > 0 ? request.instigator : Entity.Null;
			HitCollisionHistory.PrepareColliders(ref ColliderGroup.hitCollisionArray, request.tick, request.collisionMask, 
	            Entity.Null, forceIncluded, primlib.sphere(request.center, request.settings.radius));

			var hitColliderMask = 1 << m_hitCollisionLayer;
			var count = Physics.OverlapSphereNonAlloc(request.center, request.settings.radius, g_colliderBuffer, hitColliderMask);

			
			var colliderCollections = new Dictionary<HitCollisionOwner, ClosestCollision>();
			GetClosestCollision(g_colliderBuffer, count, request.center, colliderCollections);

			foreach (var collision in colliderCollections.Values)
			{
				var collisionOwner = collision.hitCollision.owner;

				var ownerGameObjectEntity = collisionOwner.GetComponent<GameObjectEntity>();
				var ownerEntity = ownerGameObjectEntity != null ? ownerGameObjectEntity.Entity : Entity.Null;
				
				var centerOfMass = collisionOwner.transform.position + Vector3.up * 1.2f;	// TODO dont hardcode COM    

				// Calc damage
				var damageVector = centerOfMass - (Vector3)request.center;
				var damageDirection = damageVector.normalized;
				var distance = damageVector.magnitude;
				if (distance > request.settings.radius)
					continue;

				var damage = request.settings.damage;
				var impulse = request.settings.impulse;
				if (distance > request.settings.falloffStartRadius)
				{
					var falloffFraction = (distance - request.settings.falloffStartRadius) / (request.settings.radius - request.settings.falloffStartRadius);
					damage -= (request.settings.damage - request.settings.minDamage) * falloffFraction;
					impulse -= (request.settings.impulse - request.settings.minImpulse) * falloffFraction;
				}

				if (request.instigator != Entity.Null && request.instigator == ownerEntity)
					damage = damage * request.settings.ownerDamageFraction;

				//GameDebug.Log(string.Format("SplashDamage. Target:{0} Inst:{1}", collider.hitCollision, m_world.GetGameObjectFromEntity(instigator) ));
				collisionOwner.damageEvents.Add(new DamageEvent(request.instigator, damage, damageDirection, impulse));
			}
				

			PostUpdateCommands.DestroyEntity(RequestGroup.entities[i]);
		}
	}
	
	public struct ClosestCollision
	{
		public HitCollision hitCollision;
		public Vector3 closestPoint;
		public float dist;
	}

	public static void GetClosestCollision(Collider[] colliders, int colliderCount, Vector3 origin, Dictionary<HitCollisionOwner, ClosestCollision> colliderOwners)
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
