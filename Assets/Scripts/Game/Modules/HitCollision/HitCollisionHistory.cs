using System;
using System.Collections.Generic;
using CollisionLib;
using Primitives;
using static Primitives.primlib;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

[DisallowMultipleComponent]
public class HitCollisionHistory : MonoBehaviour
{
    // Collider data
    public struct ColliderData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Collider collider;
    }

    // State data
    public struct State    
    {
        public int tick;
        public Vector3[] bonePositions;
        public Quaternion[] boneRotations;
    }

    [Serializable]
    public class Settings
    {
        public GameObject collisionSetup;
        public float boundsRadius = 2.0f;
        public float boundsHeight = 1.0f;
    }

    public Settings settings;
    
    [NonSerialized] public GameObject collidersRoot; 
    [NonSerialized] public Entity hitCollisionOwner;
    [NonSerialized] public ColliderData[] colliders;
    [NonSerialized] public TransformAccessArray colliderParents;
    
    [NonSerialized] public bool collidersEnabled;
    
    [NonSerialized] public int lastTick = -1;
    [NonSerialized] public int lastIndex = -1;
    [NonSerialized] public int bufferSize;

    [NonSerialized] public int lastRollbackTick;

    [NonSerialized] public State[] buffer;
    [NonSerialized] public float3[] boundsCenterBuffer;

#if UNITY_EDITOR    
    private void OnDisable()
    {
        if(colliderParents.isCreated)
            colliderParents.Dispose();
    }
#endif    

    public int GetStateIndex(int tick)
    {
        // If we exceed buffersize we should always use last value (if player latency to high no rollback is performed)
        var roolbackTicks = lastTick - tick;
        if (roolbackTicks >= bufferSize || tick > lastTick)
            roolbackTicks = 0;

        var index = lastIndex - roolbackTicks;
        while (index < 0)
            index += buffer.Length;
        return index;
    }

    public void EnableCollisionForIndex(int index)       
    {
        GameDebug.Assert(colliders != null, "No collider hitcollisioncollection:{0}",gameObject.name);
        
        collidersEnabled = true;
        
        if (buffer[index].tick == lastRollbackTick)
        {
            //GameDebug.Log("skipping rollback");
            return;
        }

        Profiler.BeginSample("EnableCollisionForIndex");
        
        for (var i = 0; i < colliders.Length; i++)
        {
            if(colliders[i].collider.gameObject.layer != HitCollisionModule.HitCollisionLayer)
                colliders[i].collider.gameObject.layer = HitCollisionModule.HitCollisionLayer;
        }

        lastRollbackTick = buffer[index].tick;

        GameDebug.Assert(index >= 0 && index < bufferSize, "Rollback index out of bounds");

        for (var i = 0; i < colliders.Length; i++)
        {
            var bonePosition = buffer[index].bonePositions[i];
            var boneRotation = buffer[index].boneRotations[i];

            var worldPos = boneRotation * colliders[i].localPosition + bonePosition;
            var worldRot = boneRotation * colliders[i].localRotation;

            colliders[i].collider.transform.position = worldPos;
            colliders[i].collider.transform.rotation = worldRot;
            
            
            if (HitCollisionModule.ShowDebug.IntValue > 0)
            {
                DebugPrimitiveModule.ClearChannel(HitCollisionModule.PrimDebugChannel);
                
                CapsuleCollider capsuleCollider = colliders[i].collider as CapsuleCollider;
                if (capsuleCollider != null)
                {
                    var center = capsuleCollider.transform.TransformPoint(capsuleCollider.center);
                    var v = capsuleCollider.transform.rotation*Vector3.up;   
                    var L = capsuleCollider.height - capsuleCollider.radius*2;
                    var pA = center - v*L*0.5f;    
                    var pB = center + v*L*0.5f;
                    DebugPrimitiveModule.CreateCapsulePrimitive(HitCollisionModule.PrimDebugChannel, pA, pB, capsuleCollider.radius, Color.green, 0);
                }
            }
        }
        
        Profiler.EndSample();
    }
    
    public void DisableHitCollision()
    {
        if (colliders == null)   
            return;

        Profiler.BeginSample("DisableHitCollision");
        
        collidersEnabled = false;
        
        for (var i = 0; i < colliders.Length; i++)
        {
            if(colliders[i].collider.gameObject.layer != HitCollisionModule.DisabledHitCollisionLayer)
                colliders[i].collider.gameObject.layer = HitCollisionModule.DisabledHitCollisionLayer;
        }

        Profiler.EndSample();
    }

    public void DrawStates(HitCollisionHistory colliderCollection, Color color, float duration)
    {
        for (int i = 0; i < bufferSize; i++)
            DrawStateAtIndex(i, colliderCollection, color, duration);
    }

    public void DrawStateAtIndex(int stateIndex, HitCollisionHistory colliderCollection, Color color, float duration)
    {
        for (var i = 0; i < colliderCollection.colliders.Length; i++)
        {
            var bonePosition = buffer[stateIndex].bonePositions[i];
            var boneRotation = buffer[stateIndex].boneRotations[i];

            var worldPos = boneRotation * colliderCollection.colliders[i].localPosition + bonePosition;
            var worldRot = boneRotation * colliderCollection.colliders[i].localRotation;


            var capsuleCollider = colliderCollection.colliders[i].collider as CapsuleCollider;
            if (capsuleCollider != null)
                DebugDraw.Capsule(worldPos, worldRot * Vector3.up, capsuleCollider.radius, capsuleCollider.height, color, duration);
        }
    }
    
    static bool IsRelevant(EntityManager entityManager, HitCollisionHistory hitCollHistory, int flagMask, Entity forceExcluded, Entity forceIncluded)
    {

        if (hitCollHistory.hitCollisionOwner == null)
        {
            GameDebug.LogError("HitCollisionHistory:" + hitCollHistory + " has a null hitCollisionOwner");
            return false;
        }

        if (hitCollHistory.colliders == null)
            return false;

        Profiler.BeginSample("IsRelevant");

        var hitCollisionOwner = entityManager.GetComponentObject<HitCollisionOwner>(hitCollHistory.hitCollisionOwner);
        var valid = (forceIncluded != Entity.Null && forceIncluded == hitCollHistory.hitCollisionOwner) ||
                    (hitCollisionOwner.collisionEnabled &&
                     (hitCollisionOwner.colliderFlags & flagMask) != 0 &&
                     !(forceExcluded != Entity.Null && forceExcluded == hitCollHistory.hitCollisionOwner));
        
        Profiler.EndSample();
                                                           
        return valid;
    }
    
    public static void PrepareColliders(EntityManager entityManager, ref ComponentArray<HitCollisionHistory> collections, int tick, int mask, Entity forceExcluded, Entity forceIncluded, ray ray, float rayDist)
    {
        Profiler.BeginSample("HitCollisionHistory.PrepareColliders [Ray]");
        
        // Rollback
        for (var i = 0; i < collections.Length; i++)
        {
            var collection = collections[i];
            if(!IsRelevant(entityManager, collection, mask, forceExcluded, forceIncluded))
            {
                collection.DisableHitCollision();
                continue;
            }   
            
            var stateIndex = collection.GetStateIndex(tick);

            Profiler.BeginSample("-raycast");

            var sphere = primlib.sphere(collection.boundsCenterBuffer[stateIndex], collection.settings.boundsRadius);
            var boundsHit = coll.RayCast(sphere, ray, rayDist);
            
            Profiler.EndSample();

            if (boundsHit)
                collection.EnableCollisionForIndex(stateIndex);
            else
                collection.DisableHitCollision();

            if (HitCollisionModule.ShowDebug.IntValue > 0)
            {
                DebugPrimitiveModule.ClearChannel(HitCollisionModule.PrimDebugChannel);
                DebugPrimitiveModule.CreateLinePrimitive(HitCollisionModule.PrimDebugChannel, ray.origin, ray.origin + ray.direction*rayDist, Color.yellow, 5);
                DebugPrimitiveModule.CreateSpherePrimitive(HitCollisionModule.PrimDebugChannel, sphere.center, sphere.radius,
                    boundsHit ? Color.yellow : Color.gray, 5);
            }
        }

        Profiler.EndSample();
    }
    
    public static void PrepareColliders(EntityManager entityManager,ref ComponentArray<HitCollisionHistory> collections, int tick, int mask, Entity forceExcluded, Entity forceIncluded, ray ray, float rayDist, float radius)
    {
        Profiler.BeginSample("HitCollisionHistory.PrepareColliders [SphereCast]");

        for (var i = 0; i < collections.Length; i++)
        {
            var collection = collections[i];
            if(!IsRelevant(entityManager, collection, mask, forceExcluded, forceIncluded))
            {
                collection.DisableHitCollision();
                continue;
            }   

            var stateIndex = collection.GetStateIndex(tick);

            Profiler.BeginSample("-capsule test");
            var boundCenter = collection.boundsCenterBuffer[stateIndex];

            var rayEnd = ray.origin + ray.direction * rayDist;
            var closestPointOnRay = coll.ClosestPointOnLineSegment(ray.origin, rayEnd, boundCenter);
            var dist = math.distance(closestPointOnRay, boundCenter);
            var boundsHit = dist < collection.settings.boundsRadius + radius;
            
            Profiler.EndSample();

            if (boundsHit)
                collection.EnableCollisionForIndex(stateIndex);
            else
                collection.DisableHitCollision();

            if (HitCollisionModule.ShowDebug.IntValue > 0)
            {
                DebugPrimitiveModule.ClearChannel(HitCollisionModule.PrimDebugChannel);
                DebugPrimitiveModule.CreateCapsulePrimitive(HitCollisionModule.PrimDebugChannel,
                    ray.origin + ray.direction * radius, ray.origin + ray.direction * (rayDist - radius), radius, Color.yellow, 5);
                DebugPrimitiveModule.CreateSpherePrimitive(HitCollisionModule.PrimDebugChannel, boundCenter,
                    collection.settings.boundsRadius,
                    boundsHit ? Color.yellow : Color.gray, 5);
            }
        }
        
        Profiler.EndSample();
    }

    public static void PrepareColliders(EntityManager entityManager,ref ComponentArray<HitCollisionHistory> collections, int tick, int mask, Entity forceExcluded, Entity forceIncluded, sphere sphere)
    {
        Profiler.BeginSample("HitCollisionHistory.PrepareColliders [Sphere]");
        
        for (var i = 0; i < collections.Length; i++)
        {
            var collection = collections[i];
            if(!IsRelevant(entityManager, collection, mask, forceExcluded, forceIncluded))
            {
                collection.DisableHitCollision();
                continue;
            }   

            var stateIndex = collection.GetStateIndex(tick);

            var boundsCenter = collection.boundsCenterBuffer[stateIndex];
            var boundsRadius = collection.settings.boundsRadius;
            var dist = math.distance(sphere.center, boundsCenter);

            var boundsHit = dist < sphere.radius + boundsRadius;

            if (boundsHit)
                collection.EnableCollisionForIndex(stateIndex);
            else
                collection.DisableHitCollision();

            if (HitCollisionModule.ShowDebug.IntValue > 0)
            {
                DebugPrimitiveModule.ClearChannel(HitCollisionModule.PrimDebugChannel);
                DebugPrimitiveModule.CreateSpherePrimitive(HitCollisionModule.PrimDebugChannel, sphere.center, sphere.radius,
                    Color.yellow, 5);
                DebugPrimitiveModule.CreateSpherePrimitive(HitCollisionModule.PrimDebugChannel, boundsCenter,
                    boundsRadius,
                    boundsHit ? Color.yellow : Color.gray, 5);
            }
        }

        Profiler.EndSample();
    }        
}

