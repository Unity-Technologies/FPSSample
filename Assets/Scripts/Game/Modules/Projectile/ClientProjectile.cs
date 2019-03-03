using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.VFX;

[ClientOnlyComponent]
public class ClientProjectile : MonoBehaviour
{
    // Settings
    public GameObject shellRoot;
    public GameObject trailRoot;
    public SoundDef thrustSound;
    public float rotationSpeed = 500;
    public float offsetScaleDuration = 0.5f;
    public SoundSystem.SoundHandle m_ThrustSoundHandle;
    public SpatialEffectTypeDefinition impactEffect;
    
    // State
    public bool IsVisible { get { return m_isVisible == 1; } }
    [NonSerialized] public Entity projectile;
    [NonSerialized] public bool impacted;
    [NonSerialized] public float roll;
    [NonSerialized] public Vector3 startOffset;
    [NonSerialized] public float offsetScale;
    
    [NonSerialized] public int poolIndex;
    [NonSerialized] public int bufferIndex;

    public void Reset()
    {
        projectile = Entity.Null;
        impacted = false;
    }
    
    public void SetVisible(bool isVisible)
    {
        var newVal = isVisible ? 1 : 0;
        if (m_isVisible != -1 && newVal == m_isVisible)
            return;
        m_isVisible = newVal;
        
        if(shellRoot != null)
            shellRoot.SetActive(isVisible);

        if(trailRoot != null)
        {
            if (isVisible)
                StartAllEffects(trailRoot);
            else
                StopAllEffects(trailRoot);
        }

        if (thrustSound && isVisible)
        {
            m_ThrustSoundHandle = Game.SoundSystem.Play(thrustSound, gameObject.transform);
        }
        else if (m_ThrustSoundHandle.IsValid() && !isVisible)
        {
            Game.SoundSystem.Stop(m_ThrustSoundHandle);
        }

        var lights = GetComponentsInChildren<Light>();
        foreach (var light in lights)
            light.enabled = isVisible;
    }

    public void SetMuzzlePosition(EntityManager entityManager, float3 muzzlePos)
    {
        if(ProjectileModuleClient.logInfo.IntValue > 1)
            GameDebug.Log("SetMuzzlePosition clientprojectile:" + name + " projectile:" + projectile);
        
        var projectileData = entityManager.GetComponentData<ProjectileData>(projectile);

        var dir = Vector3.Normalize(projectileData.endPos - projectileData.startPos);
        var deltaPos = muzzlePos - projectileData.startPos;
        var q = Quaternion.LookRotation(dir);
        var invQ = Quaternion.Inverse(q);

        startOffset = invQ * deltaPos;
        offsetScale = 1;
    }
    
    void StopAllEffects(GameObject root)
    {
        VisualEffect[] effects = root.GetComponentsInChildren<VisualEffect>();
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i].Stop();
        }

        Light[] lights = root.GetComponentsInChildren<Light>();
        foreach (var light in lights)
            light.enabled = false;
    }

    void StartAllEffects(GameObject root)
    {
        //        Game.Log("StartAllEffects:" + root.name);

        if (root == null)
            return;

        VisualEffect[] effects = root.GetComponentsInChildren<VisualEffect>();
        for (int i = 0; i < effects.Length; i++)
        {
            effects[i].Play();
        }
    }

    int m_isVisible = -1;
}
