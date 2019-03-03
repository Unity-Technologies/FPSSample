using System;
using System.Collections.Generic;
using CollisionLib;
using Primitives;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;


[DisallowMultipleComponent]
public class HitCollisionHistory : MonoBehaviour
{

    [Serializable]
    public class Settings
    {
        public GameObject collisionSetup;
        public float boundsRadius = 2.0f;
        public float boundsHeight = 1.0f;
    }

    public Settings settings;
    

    [NonSerialized] public TransformAccessArray collisiderParents;


#if UNITY_EDITOR    
    private void OnDisable()
    {
        if(collisiderParents.isCreated)
            collisiderParents.Dispose();
    }
#endif
}



public struct HitCollisionData : IComponentData
{
    public const int k_maxColliderCount = 16;
    public const int k_historyCount = 16;

    public enum HitCollType
    {
        Undefined,
        Body,
        Head,
    }

    
    public struct HitCollInfo
    {
        public HitCollType type;
    }

    public struct CollisionResult
    {
        public int hit;
        public HitCollInfo info;
        public float3 primCenter;

        public box box;
        public capsule capsule;
        public sphere sphere;
    }

    
    
    [InternalBufferCapacity(k_maxColliderCount)]
    public struct Box : IBufferElementData
    {
        public HitCollInfo info;
        public int transformIndex;
        public box prim;
    }
    
    [InternalBufferCapacity(k_maxColliderCount)]
    public struct Capsule: IBufferElementData
    {
        public HitCollInfo info;
        public int transformIndex;
        public capsule prim;
    }

    [InternalBufferCapacity(k_maxColliderCount)]
    public struct Sphere: IBufferElementData
    {
        public HitCollInfo info;
        public int transformIndex;
        public sphere prim;
    }
    
    [InternalBufferCapacity(k_historyCount)]
    public struct BoundsHistory: IBufferElementData
    {
        public float3 pos;
    }
    
    
    [InternalBufferCapacity(k_maxColliderCount*k_historyCount)]
    public struct TransformHistory: IBufferElementData
    {
        public float3 pos;
        public quaternion rot;
    }

    public Entity hitCollisionOwner;

    public float boundsRadius;
    public float boundsHeightOffset;
    public int lastTick;
    public int lastIndex;
    public int historyCount;


    public static void Setup(EntityManager entityManager, Entity entity, List<Transform> parents, 
        float boundsRadius, float boundsHeightOffset, List<CapsuleCollider> capsuleColliders, 
        List<Transform> capsuleColliderParents, List<SphereCollider> sphereColliders, 
        List<Transform> sphereColliderParents, List<BoxCollider> boxColliders, List<Transform> boxColliderParents)
    {
        var coll = new HitCollisionData();
        if (entityManager.HasComponent<HitCollisionData>(entity))
            coll = entityManager.GetComponentData<HitCollisionData>(entity);
        coll.lastTick = -1;
        coll.lastIndex = -1;
        coll.boundsRadius = boundsRadius;
        coll.boundsHeightOffset = boundsHeightOffset;
        if(entityManager.HasComponent<HitCollisionData>(entity))        
            entityManager.SetComponentData(entity,coll);
        else
            entityManager.AddComponentData(entity,coll);

        // Setup history
        entityManager.AddBuffer<TransformHistory>(entity);
        var historyBuffer = entityManager.GetBuffer<TransformHistory>(entity);
        for (int i = 0; i < k_historyCount*k_maxColliderCount; i++)
        {
            historyBuffer.Add(new TransformHistory());     
        }
        
        entityManager.AddBuffer<BoundsHistory>(entity);
        var boundsBuffer = entityManager.GetBuffer<BoundsHistory>(entity);
        for (int i = 0; i < k_historyCount; i++)
        {
            boundsBuffer.Add(new BoundsHistory());     
        }
        

        // Primitives
        entityManager.AddBuffer<Capsule>(entity);
        var capsuleBuffer = entityManager.GetBuffer<Capsule>(entity);
        for (var i = 0; i < capsuleColliders.Count; i++)
        {
            var collider = capsuleColliders[i];
            var localPos = collider.center;
            var axis = collider.direction == 0 ? Vector3.right :
                collider.direction == 1 ? Vector3.up : Vector3.forward;

            var offset = 0.5f*axis*(collider.height - 2*collider.radius);
            var prim = new capsule()
            {
                p1 = localPos - offset,
                p2 = localPos + offset,
                radius = collider.radius,
            };

            var capsule = new Capsule();
            capsule.prim = primlib.transform(prim, collider.transform.localPosition,
                collider.transform.localRotation);

            var parent = capsuleColliderParents[i];
            capsule.transformIndex = parents.IndexOf(parent);
            capsule.info = new HitCollInfo
            {
                type = HitCollType.Body
            };
            capsuleBuffer.Add(capsule);
        }
        
        entityManager.AddBuffer<Box>(entity);
        var boxBuffer = entityManager.GetBuffer<Box>(entity);
        for (var i = 0; i < boxColliders.Count; i++)
        {
            var collider = boxColliders[i];
            var prim = new box
            {
                center = collider.center,
                size = collider.size,
                rotation = Quaternion.identity
            };

            var box = new Box();
            box.prim = primlib.transform(prim, collider.transform.localPosition,
                collider.transform.localRotation);
            
            var parent = boxColliderParents[i];
            box.transformIndex = parents.IndexOf(parent);
            box.info = new HitCollInfo
            {
                type = HitCollType.Body
            };
            boxBuffer.Add(box);
        }

        entityManager.AddBuffer<Sphere>(entity);
        var sphereBuffer = entityManager.GetBuffer<Sphere>(entity);
        for (var i = 0; i < sphereColliders.Count; i++)
        {
            var collider = sphereColliders[i];
            var prim = new sphere
            {
                center = collider.center,
                radius = collider.radius,
            };

            var sphere = new Sphere();
            sphere.prim = primlib.transform(prim, collider.transform.localPosition,
                collider.transform.localRotation);
            
            var parent = sphereColliderParents[i];
            sphere.transformIndex = parents.IndexOf(parent);
            sphere.info = new HitCollInfo
            {
                type = HitCollType.Body
            };
            sphereBuffer.Add(sphere);
        }
    }
    
    public int GetHistoryIndex(int tick)
    {
        // If we exceed buffersize we should always use last value (if player latency to high no rollback is performed)
        var roolbackTicks = lastTick - tick;
        if (roolbackTicks >= historyCount || tick > lastTick)
            roolbackTicks = 0;

        var index = lastIndex - roolbackTicks;
        while (index < 0)
            index += k_historyCount;
        return index;
    }
    

    public static bool IsRelevant(EntityManager entityManager, Entity hitCollisionEntity, int flagMask, 
        Entity forceExcluded, Entity forceIncluded)
    {

        var hitCollisionData = entityManager.GetComponentData<HitCollisionData>(hitCollisionEntity);
        
        if (hitCollisionData.hitCollisionOwner == Entity.Null)
        {
            GameDebug.Assert(false,"HitCollisionHistory:" + hitCollisionData + " has a null hitCollisionOwner");
            return false;
        }

        Profiler.BeginSample("IsRelevant");

        var hitCollisionOwner = entityManager.GetComponentData<HitCollisionOwnerData>(hitCollisionData.hitCollisionOwner);
        var valid = (forceIncluded != Entity.Null && forceIncluded == hitCollisionData.hitCollisionOwner) ||
                    (hitCollisionOwner.collisionEnabled == 1&&
                     (hitCollisionOwner.colliderFlags & flagMask) != 0 &&
                     !(forceExcluded != Entity.Null && forceExcluded == hitCollisionData.hitCollisionOwner));
        
        Profiler.EndSample();
                                                           
        return valid;
    }
 
    public static void StoreBones(EntityManager entityManager, Entity entity, TransformAccessArray boneTransformArray, int sampleTick)
    {
        var collData = entityManager.GetComponentData<HitCollisionData>(entity);
        
        var historyBuffer = entityManager.GetBuffer<TransformHistory>(entity);
        var boundsBuffer = entityManager.GetBuffer<BoundsHistory>(entity);
        
       
        
        // To make sure all ticks have valid data we store state of all ticks up to sampleTick that has not been stored (in editor server might run multiple game frames for each time samplestate is called) 
        var lastStoredTick = collData.lastTick;
        var endTick = sampleTick;
        var startTick = lastStoredTick != -1 ? lastStoredTick + 1 : sampleTick;
        for (var tick = startTick; tick <= endTick; tick++)
        {
            collData.lastIndex = (collData.lastIndex + 1) % k_historyCount;
            collData.lastTick = tick;
            if (collData.historyCount < k_historyCount)
                collData.historyCount++;

            var slice = new NativeSlice<TransformHistory>(historyBuffer.ToNativeArray(),collData.lastIndex*k_maxColliderCount);
            
            var job = new StoreBonesJobJob
            {
                transformBuffer = slice,
            };
            var handle = job.Schedule(boneTransformArray);
            handle.Complete();

            boundsBuffer[collData.lastIndex] = new BoundsHistory
            {
                pos = historyBuffer[0].pos + new float3(0, 1, 0) * collData.boundsHeightOffset,
            };
        }

        entityManager.SetComponentData(entity, collData);
    }
    
    public static void DebugDrawTick(EntityManager entityManager, Entity entity, int tick, Color primColor, Color boundsColor)
    {
//        var collData = entityManager.GetComponentData<HitCollisionData>(entity);
//
//        var histIndex = collData.GetHistoryIndex(tick);
//        
//        var historyBuffer = entityManager.GetBuffer<History>(entity);
//        DebugDraw.Prim(new sphere(historyBuffer[histIndex].center, collData.boundsRadius), boundsColor, 0);
//
//        var transformBuffer = entityManager.GetBuffer<TransformHistory>(historyBuffer[histIndex].transformHistory);
//        
//        var sphereArray = entityManager.GetBuffer<Sphere>(entity);
//        for (var i = 0; i < sphereArray.Length; i++)
//        {
//            var prim = sphereArray[i].prim;
//            var sourceIndex = sphereArray[i].transformIndex;                
//            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);
//            DebugDraw.Prim(prim, primColor, 0);
//        }
//
//        var capsuleArray = entityManager.GetBuffer<Capsule>(entity);
//        for (var i = 0; i < capsuleArray.Length; i++)
//        {
//            var prim = capsuleArray[i].prim;
//            var sourceIndex = capsuleArray[i].transformIndex;                
//            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);
//
//            DebugDraw.Prim(prim, primColor, 0);
//        }
//    
//        var boxArray = entityManager.GetBuffer<Box>(entity);
//        for (var i = 0; i < boxArray.Length; i++)
//        {
//            var prim = boxArray[i].prim;
//            var sourceIndex = boxArray[i].transformIndex;                
//            prim = primlib.transform(prim, transformBuffer[sourceIndex].pos, transformBuffer[sourceIndex].rot);
//
//            DebugDraw.Prim(prim, primColor, 0);
//        }
    }


    public static bool SphereOverlapSingle(EntityManager entityManager, Entity entity, int tick, sphere sphere, 
        ref CollisionResult result)
    {
        var collData = entityManager.GetComponentData<HitCollisionData>(entity);

        var histIndex = collData.GetHistoryIndex(tick);
        var transformBuffer = entityManager.GetBuffer<TransformHistory>(entity);
        
        var sphereArray = entityManager.GetBuffer<Sphere>(entity);
        var capsuleArray = entityManager.GetBuffer<Capsule>(entity);
        var boxArray = entityManager.GetBuffer<Box>(entity);
        var resultArray = new NativeArray<CollisionResult>(1,Allocator.TempJob);

        var job = new SphereOverlapJob
        {
            transformBuffer = new NativeSlice<TransformHistory>(transformBuffer.ToNativeArray(),histIndex*k_maxColliderCount),
            sphereArray = sphereArray.ToNativeArray(),
            capsuleArray = capsuleArray.ToNativeArray(),
            boxArray = boxArray.ToNativeArray(),
            sphere = sphere,
            result = resultArray,
        };
        
        var handle = job.Schedule();
        handle.Complete();
        result = resultArray[0];
        
        if(math.length(result.box.size) > 0)
            DebugDraw.Prim(result.box, Color.red, 1);
        if(result.capsule.radius > 0)
            DebugDraw.Prim(result.capsule, Color.red, 1);
        if(result.sphere.radius > 0)
            DebugDraw.Prim(result.sphere, Color.red, 1);
        
        
        resultArray.Dispose();
        
        return result.hit == 1;
    }
    




}
