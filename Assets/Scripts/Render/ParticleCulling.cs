using UnityEngine;

public class ParticleCulling : MonoBehaviour
{
    public float cullingRadius = 1;
    public ParticleSystem target;

    private ParticleSystemRenderer render;
    private CullingGroup m_CullingGroup;

    void Start()
    {
        m_CullingGroup = new CullingGroup();
        m_CullingGroup.targetCamera = Camera.main; 
        m_CullingGroup.SetBoundingSpheres(new BoundingSphere[] { new BoundingSphere(transform.position, cullingRadius) });
        m_CullingGroup.SetBoundingSphereCount(1);
        m_CullingGroup.onStateChanged += OnStateChanged;
        render = GetComponent<ParticleSystemRenderer>();
    }
    private void Update()
    {
        m_CullingGroup.targetCamera = Camera.main;
    }
    void OnStateChanged(CullingGroupEvent sphere)
    {
        if (sphere.isVisible)
        {
            // We could simulate forward a little here to hide that the system was not updated off-screen.
            target.Play(true);
            render.enabled = true;
            //Debug.Log("ParticlesEnabled");

        }
        else
        {
            target.Pause();
            render.enabled = false;
            //Debug.Log("ParticlesDisabled");

        }
    }

    void OnDestroy()
    {
        if (m_CullingGroup != null)
            m_CullingGroup.Dispose();
    }

    void OnDrawGizmos()
    {
        // Draw gizmos to show the culling sphere.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, cullingRadius);
    }
}