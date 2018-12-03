using System.Collections.Generic;
using Primitives;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Jobs;

[DisableAutoCreation]
public class HandleHitCollisionSpawning : InitializeComponentGroupSystem<HitCollisionHistory, HandleHitCollisionSpawning.Initialized>
{
    public struct Initialized : IComponentData {}
    public HandleHitCollisionSpawning(GameWorld world, GameObject systemRoot, int bufferSize) : base(world)
    {
        m_systemRoot = systemRoot;
        m_bufferSize = bufferSize;
    }

    protected override void Initialize(ref ComponentGroup group)
    {
        // We copy to list of incoming hitcollisions as it is not allowed to add entities while iterating componentarray 
        var hitCollHistoryArray = group.GetComponentArray<HitCollisionHistory>().ToArray();

        for (var iHitColl = 0; iHitColl < hitCollHistoryArray.Length; iHitColl++)
        {
            var hitCollHistory = hitCollHistoryArray[iHitColl];
            var hitCollHistoryEntity = hitCollHistory.gameObject.GetComponent<GameObjectEntity>().Entity;
            var externalSetup = hitCollHistory.settings.collisionSetup != null;
            var colliderSetup = externalSetup ? hitCollHistory.settings.collisionSetup.transform : hitCollHistory.transform;
            var hitCollisionOwner =
                EntityManager.GetComponentObject<HitCollisionOwner>(hitCollHistory.hitCollisionOwner);
            
            GameDebug.Assert(hitCollHistory.hitCollisionOwner != null,"HitCollisionHistory requires HitCollisionOwner component");
            
            // Find and disable all all colliders on collisionOwner
            var sourceColliders = new List<Collider>();
            RecursiveGetCollidersInChildren(colliderSetup.transform, sourceColliders);
            foreach (var collider in sourceColliders)
                collider.enabled = false;
    
            // Create collider collection
            var name = string.Format("HitColl_{0}", hitCollHistory.name);
            hitCollHistory.collidersRoot = m_world.Spawn(name);
            if(m_systemRoot != null)
                hitCollHistory.transform.SetParent(m_systemRoot.transform, false);
            
            hitCollHistory.colliders = new HitCollisionHistory.ColliderData[sourceColliders.Count];
    
            var colliderParents = new List<Transform>(16);
            var capsuleColliders = new List<CapsuleCollider>(16);
            var capsuleColliderParents = new List<Transform>(16);
            var sphereColliders = new List<SphereCollider>(16);
            var sphereColliderParents = new List<Transform>(16);
            var boxColliders = new List<BoxCollider>(16);
            var boxColliderParents = new List<Transform>(16);
            
            for (var i = 0; i < sourceColliders.Count; i++)
            {
                var sourceCollider = sourceColliders[i];
                var colliderParentBone = sourceCollider.transform.parent;
                if (externalSetup)
                {
                    var skeleton = EntityManager.GetComponentObject<Skeleton>(hitCollHistoryEntity);
                    var ownerBoneIndex = skeleton.GetBoneIndex(colliderParentBone.name.GetHashCode());
                    colliderParentBone = skeleton.bones[ownerBoneIndex];
                }
    
                var collider = GameObject.Instantiate(sourceCollider);
    
                var hitColl = collider.gameObject.AddComponent<HitCollision>();
                hitColl.owner = hitCollisionOwner;
                collider.enabled = true;
                collider.gameObject.layer = HitCollisionModule.DisabledHitCollisionLayer;
                collider.transform.SetParent(hitCollHistory.collidersRoot.transform);
    
                hitCollHistory.colliders[i].localPosition = sourceCollider.transform.localPosition;
                hitCollHistory.colliders[i].localRotation = sourceCollider.transform.localRotation;
                hitCollHistory.colliders[i].collider = collider;
                hitCollHistory.colliders[i].collider.isTrigger = false;
    
                colliderParents.Add(colliderParentBone);
                
                var capsuleCollider = sourceCollider as CapsuleCollider;
                if (capsuleCollider != null)
                {
                    capsuleColliderParents.Add(colliderParentBone);
                    capsuleColliders.Add(capsuleCollider);
                }
                else
                {
                    var boxCollider = sourceCollider as BoxCollider;
                    if (boxCollider != null)
                    {
                        boxColliders.Add(boxCollider);
                        boxColliderParents.Add(colliderParentBone);
                    }
                    else
                    {
                        var sphereCollider = sourceCollider as SphereCollider;
                        if(sphereCollider != null)
                        {
                            sphereColliders.Add(sphereCollider);
                            sphereColliderParents.Add(colliderParentBone);
                        }
                    }
                }
            }
            hitCollHistory.colliderParents = new TransformAccessArray(colliderParents.ToArray());
    
            hitCollHistory.buffer = new HitCollisionHistory.State[ m_bufferSize];
            hitCollHistory.boundsCenterBuffer = new float3[m_bufferSize];
            for (var i = 0; i < hitCollHistory.buffer.Length; i++)
            {
                hitCollHistory.buffer[i].bonePositions = new Vector3[hitCollHistory.colliders.Length];
                hitCollHistory.buffer[i].boneRotations = new Quaternion[hitCollHistory.colliders.Length];
            }
        }
    }

    void RecursiveGetCollidersInChildren(Transform parent, List<Collider> colliders)
    {
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var child = parent.transform.GetChild(i);
            var collider = child.GetComponent<Collider>();
            if (collider != null) 
                colliders.Add(collider);
            
            RecursiveGetCollidersInChildren(child, colliders);
        }
    }

    GameObject m_systemRoot;
    int m_bufferSize;
}


[DisableAutoCreation]
public class HandleHitCollisionDespawning : DeinitializeComponentGroupSystem<HitCollisionHistory>
{
    public HandleHitCollisionDespawning(GameWorld world) : base(world)
    {}

    protected override void Deinitialize(ref ComponentGroup group)
    {
        var hitCollHistoryArray = group.GetComponentArray<HitCollisionHistory>().ToArray();

        for (var i = 0; i < hitCollHistoryArray.Length; i++)
        {
            var hitCollHistory = hitCollHistoryArray[i];
            if(hitCollHistory.colliderParents.isCreated)
                hitCollHistory.colliderParents.Dispose();
    
            if (hitCollHistory.colliders == null)
                return;

            for (var j = 0; j < hitCollHistory.colliders.Length; j++)
            {
                var go = hitCollHistory.colliders[j].collider.gameObject;
                GameObject.Destroy(go);
            }
            GameObject.Destroy(hitCollHistory.collidersRoot);
            hitCollHistory.colliders = null;
        }            
    }
}

[DisableAutoCreation]
public class StoreColliderStates : BaseComponentSystem<HitCollisionHistory>
{
    public StoreColliderStates(GameWorld world) : base(world) {}

    protected override void Update(Entity entity, HitCollisionHistory hitColliderHist)
    {
        var sampleTick = m_world.worldTime.tick;
        
        if (hitColliderHist.colliders == null)
            return;
        
        var boundsCenter = hitColliderHist.transform.position + Vector3.up*hitColliderHist.settings.boundsHeight;  
        
        // To make sure all ticks have valid data we store state of all ticks up to sampleTick that has not been stored (in editor server might run multiple game frames for each time samplestate is called) 
        var lastStoredTick = hitColliderHist.lastTick;
        var endTick = sampleTick;
        var startTick = lastStoredTick != -1 ? lastStoredTick + 1 : sampleTick;
        for (var tick = startTick; tick <= endTick; tick++)
        {
            hitColliderHist.lastIndex = (hitColliderHist.lastIndex + 1) % hitColliderHist.buffer.Length;
            hitColliderHist.lastTick = tick;
            if (hitColliderHist.bufferSize < hitColliderHist.buffer.Length)
                hitColliderHist.bufferSize++;

            hitColliderHist.boundsCenterBuffer[hitColliderHist.lastIndex] = boundsCenter;
            hitColliderHist.buffer[hitColliderHist.lastIndex].tick = tick;
            for (var colliderIndex = 0; colliderIndex < hitColliderHist.colliders.Length; colliderIndex++)
            {
                hitColliderHist.buffer[hitColliderHist.lastIndex].bonePositions[colliderIndex] = hitColliderHist.colliderParents[colliderIndex].transform.position;
                hitColliderHist.buffer[hitColliderHist.lastIndex].boneRotations[colliderIndex] = hitColliderHist.colliderParents[colliderIndex].transform.rotation;
            }

            if (HitCollisionModule.ShowDebug.IntValue == 1)
            {
                hitColliderHist.EnableCollisionForIndex(hitColliderHist.lastIndex);    
            }
        }
    }
}
