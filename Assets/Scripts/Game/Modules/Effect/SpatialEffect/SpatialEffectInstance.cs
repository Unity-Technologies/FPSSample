using UnityEngine;
using System;
using UnityEngine.Profiling;

public class SpatialEffectInstance : MonoBehaviour
{
    [Serializable]
    public class ShockwaveSettings
    {
        public bool enabled;
        public float force = 7;
        public float radius = 5;
        public float upwardsModifier = 0.0f;
        public ForceMode mode = ForceMode.Impulse;
    }

    public ShockwaveSettings shockwave;
    public SoundDef sound;
    public ParticleSystem particles;
    
    public void StartEffect(Vector3 position,Quaternion rotation)
    {
        Profiler.BeginSample("SpatialEffectInstance.Start");

        transform.position = position;
        transform.rotation = rotation;

        if (sound != null)
            Game.SoundSystem.Play(sound, transform.position);
        
        if(particles != null)
            particles.Play();

        if (shockwave.enabled)
        {
            var layer = LayerMask.NameToLayer("Debris");
            var mask = 1 << layer;
            var explosionCenter = position + UnityEngine.Random.insideUnitSphere * 0.2f;
            var colliders = Physics.OverlapSphere(position,shockwave.radius,mask);
            for (var i = 0; i < colliders.Length; i++)
            {
                var rigidBody = colliders[i].gameObject.GetComponent<Rigidbody>();
                if (rigidBody != null)
                {
                    rigidBody.AddExplosionForce(shockwave.force,explosionCenter,shockwave.radius, shockwave.upwardsModifier, shockwave.mode);
                }
            }
        }


        /*
        var hdpipe = RenderPipelineManager.currentPipeline as UnityEngine.Experimental.Rendering.HDPipeline.HDRenderPipeline;
        if (hdpipe != null)
        {
            var matholder = GetComponent<DecalHolder>();
            if (matholder != null)
            {
                var ds = UnityEngine.Experimental.Rendering.HDPipeline.DecalSystem.instance;
                var go = new GameObject();
                go.transform.rotation = effectEvent.rotation;
                go.transform.position = effectEvent.position;
                go.transform.Translate(-0.5f, 0, 0, Space.Self);
                go.transform.up = go.transform.right;
                var d = go.AddComponent<UnityEngine.Experimental.Rendering.HDPipeline.DecalProjectorComponent>();
                d.m_Material = matholder.mat;
                ds.AddDecal(d);
            }
        }
        */
        
        Profiler.EndSample();
    }

    public void CancelEffect()
    {
        particles.Stop();
    }
}
