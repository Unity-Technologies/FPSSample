using System;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Random = System.Random;

public class DestructiblePropPresentation : MonoBehaviour
{
    [Serializable]
    public class ShatterSettings
    {
        public Rigidbody[] rigidBodies;
        public float explosionForce = 10f;
        public float explosionRadius = 3;
        public float upwardsModifier = 0.5f;
        public ForceMode mode = ForceMode.Force;
        public Transform center;
        public float centerRnd = 0.3f;
    }

    public GameObject geometryRoot;
    public SpatialEffectTypeDefinition destructionEffect;
    public Transform destructionEffectTransform;
    public float triggerEffectTimeThreshold = 5.0f;
    public ShatterSettings shatterSettings;
    public GameObject[] collision;
    
    [NonSerialized] public bool triggered;

    void OnEnable()
    {
        // TODO (mogensh) we can't setup entity references from prefabs, so we hardcode hitcollision ref to hitcollisionowner here
        var goe = GetComponent<GameObjectEntity>();
        if (goe.EntityManager.HasComponent<HitCollisionOwnerData>(goe.Entity))
        {
            foreach (var coll in collision)
            {
                var hitColl = coll.GetComponent<HitCollision>();
                if (hitColl != null)
                    hitColl.owner = goe.Entity;
            }        
        }
    }
}

[DisableAutoCreation]
public class DestructiblePropSystemClient : BaseComponentSystem
{
    ComponentGroup Group;    
    
    public DestructiblePropSystemClient(GameWorld world) : base(world) {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(DestructablePropReplicatedData), typeof(DestructiblePropPresentation));
    }

    protected override void OnUpdate()
    {
        var presentationArray = Group.GetComponentArray<DestructiblePropPresentation>();
        var replicatedDataArray = Group.GetComponentDataArray<DestructablePropReplicatedData>();

        for (int i = 0; i < presentationArray.Length; i++)
        {
            var presentation = presentationArray[i];
            
            if (presentation.triggered)
                continue;

            var replicatedState = replicatedDataArray[i];
            if (replicatedState.destroyedTick == 0)
                continue;
    
            foreach (var renderer in presentation.geometryRoot.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
                
            presentation.triggered = true;
            
            foreach (var gameObject in presentation.collision)
            {
                gameObject.SetActive(false);
            }
            
            // Trigger effect if it just happened (otherwise late joiner will se effect when connecting)
            var time  = m_world.worldTime;
            if (time.DurationSinceTick(replicatedState.destroyedTick) < presentation.triggerEffectTimeThreshold)
            {
                for (var j = 0; j < presentation.shatterSettings.rigidBodies.Length; j++)
                {
                    var center = presentation.shatterSettings.center.position;
                    center.x += UnityEngine.Random.Range(-presentation.shatterSettings.centerRnd, presentation.shatterSettings.centerRnd);
                    center.y += UnityEngine.Random.Range(-presentation.shatterSettings.centerRnd, presentation.shatterSettings.centerRnd);
                    center.z += UnityEngine.Random.Range(-presentation.shatterSettings.centerRnd, presentation.shatterSettings.centerRnd);
                    
                    var rigidBody = presentation.shatterSettings.rigidBodies[j];
                    rigidBody.gameObject.SetActive(true);
                    rigidBody.AddExplosionForce(presentation.shatterSettings.explosionForce, center,
                        presentation.shatterSettings.explosionRadius, presentation.shatterSettings.upwardsModifier, presentation.shatterSettings.mode);
                }
    
                if (presentation.destructionEffect != null)
                {
                    World.GetExistingManager<HandleSpatialEffectRequests>().Request(presentation.destructionEffect, 
                        presentation.destructionEffectTransform.position, presentation.destructionEffectTransform.rotation);
                }
            }
        }
    }
  
} 