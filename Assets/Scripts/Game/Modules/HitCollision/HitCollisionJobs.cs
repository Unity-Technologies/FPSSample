using System.Threading;
using CollisionLib;
using Primitives;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;
using UnityEngine.Profiling;


[BurstCompile(CompileSynchronously = true)]
public struct BroadPhaseSphereCastJob : IJob
{
    public NativeList<Entity> result;


    public ray ray;
    public float rayDist;
    public float rayRadius;
    public Entity include;
    public Entity exclude;
    public uint flagMask;
    
    
    [ReadOnly]
    public NativeArray<Entity> ColliderEntities;

    [ReadOnly]
    public NativeArray<HitCollisionData> ColliderData;

    [ReadOnly]
    public NativeArray<uint> flags;

    [ReadOnly]
    public NativeArray<sphere> bounds;

    public BroadPhaseSphereCastJob(NativeArray<Entity> colliderEntities, 
        NativeArray<HitCollisionData> colliderData, NativeArray<uint> flags, NativeArray<sphere> bounds, Entity exclude,
        Entity include, uint flagMask, ray ray, float distance, float radius)
    {
        this.ColliderEntities = colliderEntities;
        this.ColliderData = colliderData;
        this.flags = flags;
        this.ray = ray;
        this.include = include;
        this.exclude = exclude;
        this.flagMask = flagMask;
        this.bounds = bounds;
        rayDist = distance;
        rayRadius = radius;
        
        result = new NativeList<Entity>(colliderEntities.Length,Allocator.TempJob);
    }

    public void Dispose()
    {
        result.Dispose();
    }
    
    public void Execute()
    {
        for(int i=0;i<bounds.Length;i++)
        {
            var relevant = (include != Entity.Null && include == ColliderData[i].hitCollisionOwner) ||
                           ((flags[i] & flagMask) != 0 &&
                            !(exclude != Entity.Null && exclude == ColliderData[i].hitCollisionOwner));

            if (!relevant)
                continue;
                
            var boundsHit = coll.RayCast(bounds[i], ray, rayDist, rayRadius);
            if(boundsHit)
                result.Add(ColliderEntities[i]);
        }
    }
}
  
//
////[BurstCompile(CompileSynchronously = true)]
//public struct BroadPhaseSphereCastMultiJob : IJob
//{
//    public NativeList<Entity> result;
//
//
//    public ray ray;
//    public float rayDist;
//    public float rayRadius;
//
//    [ReadOnly]
//    public NativeArray<Entity> entities;
//    [ReadOnly]
//    public NativeArray<sphere> bounds;
//
//    [ReadOnly] 
//    public NativeArray<int> relevantArray;
//
//
//    public BroadPhaseSphereCastMultiJob(EntityManager entityManager, NativeArray<Entity> entityArray, Entity exclude,
//        Entity include, int flagMask, ray ray, float distance, float radius, int tick)
//    {
//        entities = entityArray;
//        this.ray = ray;
//        rayDist = distance;
//        rayRadius = radius;
//        
//        bounds = new NativeArray<sphere>(entityArray.Length,Allocator.TempJob);
//        relevantArray = new NativeArray<int>(entityArray.Length,Allocator.TempJob);
//        
//        
//        for (int i = 0; i < entities.Length; i++)
//        {
//            // Get bounds for tick
//            var collData = entityManager.GetComponentData<HitCollisionData>(entities[i]);
//            var historyBuffer = entityManager.GetBuffer<HitCollisionData.History>(entities[i]);
//            var histIndex = HitCollisionData.GetHistoryIndex(ref collData, tick);
//            var boundSphere = primlib.sphere(historyBuffer[histIndex].center, collData.boundsRadius);
//            bounds[i] = boundSphere;
//            
//            var relevant = HitCollisionData.IsRelevant(entityManager, entities[i], flagMask, exclude,
//                include);
//            relevantArray[i] = relevant ? 1 : 0;
//        }
//        
//        result = new NativeList<Entity>(entityArray.Length,Allocator.TempJob);
//        
//        ownerArray.Dispose();
//    }
//
//    public void Dispose()
//    {
//        bounds.Dispose();
//        relevantArray.Dispose();
//        result.Dispose();
//    }
//    
//    public void Execute()
//    {
//        for(int i=0;i<bounds.Length;i++)
//        {
//            if (relevantArray[i] == 0)
//                continue;
//                
//            var boundsHit = coll.RayCast(bounds[i], ray, rayDist, rayRadius);
//            if(boundsHit)
//                result.Add(entities[i]);
//        }
//    }
//}


    
[BurstCompile(CompileSynchronously = true)]
public struct SphereCastSingleJob : IJob
{
    public NativeArray<HitCollisionData.CollisionResult> result;
    public Entity hitCollObject;
    
    [ReadOnly]
    NativeSlice<HitCollisionData.TransformHistory> transformBuffer;

    [ReadOnly]
    DynamicBuffer<HitCollisionData.Sphere> sphereArray;
    [ReadOnly]
    DynamicBuffer<HitCollisionData.Capsule> capsuleArray;
    [ReadOnly]
    DynamicBuffer<HitCollisionData.Box> boxArray;

    ray ray;
    float rayDist;
    float rayRadius;

    public SphereCastSingleJob(EntityManager entityManager, Entity entity, ray ray, float distance, float radius, int tick)
    {
        this.ray = ray;
        rayDist = distance;
        rayRadius = radius;

        hitCollObject = entity;
        
        var collData = entityManager.GetComponentData<HitCollisionData>(entity);
        var histIndex = collData.GetHistoryIndex(tick);

        transformBuffer = new NativeSlice<HitCollisionData.TransformHistory>(
            entityManager.GetBuffer<HitCollisionData.TransformHistory>(entity).ToNativeArray(),
            histIndex * HitCollisionData.k_maxColliderCount);

        sphereArray = entityManager.GetBuffer<HitCollisionData.Sphere>(entity);
        capsuleArray = entityManager.GetBuffer<HitCollisionData.Capsule>(entity);
        boxArray = entityManager.GetBuffer<HitCollisionData.Box>(entity);
        result = new NativeArray<HitCollisionData.CollisionResult>(1,Allocator.TempJob);
    }

    public void Dispose()
    {
        result.Dispose();
    }
    
    public void Execute()
    {
        // TODO (mogensh) : find all hits and return closest

        var rayEnd = ray.origin + ray.direction * rayDist;
        
        for (var i = 0; i < sphereArray.Length; i++)
        {
            var prim = sphereArray[i].prim;
            var sourceIndex = sphereArray[i].transformIndex;                
            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, 
                transformBuffer[sourceIndex].rot);
            var hit = coll.RayCast(prim, ray, rayDist, rayRadius);
            if (hit)
            {
                result[0] = new HitCollisionData.CollisionResult()
                {
                    info = sphereArray[i].info,
                    primCenter = prim.center,
                    hit = 1,
                    sphere = prim,
                };
                return;
            }
        }

        for (var i = 0; i < capsuleArray.Length; i++)
        {
            var prim = capsuleArray[i].prim;
            var sourceIndex = capsuleArray[i].transformIndex;                
            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);

            var rayCapsule = new capsule(ray.origin, rayEnd, rayRadius);

            var hit = InstersectionHelper.IntersectCapsuleCapsule(ref prim, ref rayCapsule);
            if (hit)
            {
                result[0] = new HitCollisionData.CollisionResult()
                {
                    info = capsuleArray[i].info,
                    primCenter = prim.p1 + (prim.p2 - prim.p1)*0.5f,
                    hit = 1,
                    capsule = prim,
                };
                return;
            }
        }

        for (var i = 0; i < boxArray.Length; i++)
        {
            var prim = boxArray[i].prim;
            var sourceIndex = boxArray[i].transformIndex;                
            
            var primWorldSpace = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);
            var rayCapsule = new capsule(ray.origin, rayEnd, rayRadius);

            var hit = coll.OverlapCapsuleBox(rayCapsule, primWorldSpace);
            
                           
            if (hit)
            {
                result[0] = new HitCollisionData.CollisionResult()
                {
                    info = boxArray[i].info,
                    primCenter = primWorldSpace.center,
                    hit = 1,
                    box = primWorldSpace,
                };
                return;  
            }
        }  
    }
}


  
[BurstCompile(CompileSynchronously = true)]
public struct BroadPhaseSphereOverlapJob : IJob
{
    public sphere sphere;

    [ReadOnly]
    public NativeArray<Entity> entities;
    [ReadOnly]
    public NativeArray<sphere> bounds;
        
    public NativeList<Entity> result;

    public void Execute()
    {
        for(int i=0;i<bounds.Length;i++)
        {
            var dist = math.distance(sphere.center, bounds[i].center);
            var hit = dist < sphere.radius + bounds[i].radius;
            if(hit)
                result.Add(entities[i]);
        }
    }
}


[BurstCompile(CompileSynchronously = true)]
struct SphereOverlapJob : IJob
{
    [ReadOnly]
    public NativeSlice<HitCollisionData.TransformHistory> transformBuffer;
    
    [ReadOnly]
    public NativeArray<HitCollisionData.Sphere> sphereArray;
    [ReadOnly]
    public NativeArray<HitCollisionData.Capsule> capsuleArray;
    [ReadOnly]
    public NativeArray<HitCollisionData.Box> boxArray;

    public sphere sphere;

    public NativeArray<HitCollisionData.CollisionResult> result;

    public void Execute()
    {
        // TODO (mogensh) : find all hits and return closest

        
        for (var i = 0; i < sphereArray.Length; i++)
        {
            var prim = sphereArray[i].prim;
            var sourceIndex = sphereArray[i].transformIndex;                
            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);

            var dist = math.distance(sphere.center, prim.center);

            var hit = dist < sphere.radius + prim.radius;
            if (hit)
            {
                result[0] = new HitCollisionData.CollisionResult()
                {
                    info = sphereArray[i].info,
                    primCenter = prim.center,
                    hit = 1,
                    sphere = prim,
                };
                return;
            }
        }

        for (var i = 0; i < capsuleArray.Length; i++)
        {
            var prim = capsuleArray[i].prim;
            var sourceIndex = capsuleArray[i].transformIndex;                
            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);
            var v = prim.p2 - prim.p1;
            var hit = coll.RayCast(sphere, new ray(prim.p1, math.normalize(v)), math.length(v), prim.radius);
            if (hit)
            {
                result[0] = new HitCollisionData.CollisionResult()
                {
                    info = capsuleArray[i].info,
                    primCenter = prim.p1 + (prim.p2 - prim.p1)*0.5f,
                    hit = 1,
                    capsule = prim,
                };
                return;
            }
        }

        for (var i = 0; i < boxArray.Length; i++)
        {
            var prim = boxArray[i].prim;
            var sourceIndex = boxArray[i].transformIndex;                
            
            var primWorldSpace = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);

            var hit = true; // TODO (mogensh) SPhere Box collision
                           
            if (hit)
            {
                result[0] = new HitCollisionData.CollisionResult()
                {
                    info = boxArray[i].info,
                    primCenter = primWorldSpace.center,
                    hit = 1,
                    box = primWorldSpace,
                };
                return;  
            }
        }  
    }
}


[BurstCompile(CompileSynchronously = true)]
struct StoreBonesJobJob : IJobParallelForTransform
{
    public NativeSlice<HitCollisionData.TransformHistory> transformBuffer;
    
    public void Execute(int i, TransformAccess transform)
    {
        transformBuffer[i] = new HitCollisionData.TransformHistory
        {
            pos = transform.position,
            rot = transform.rotation,
        };
    }
}

