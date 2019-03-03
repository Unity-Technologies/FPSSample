using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

public class HitCollisionModule 
{
    public static int HitCollisionLayer;
    public static int DisabledHitCollisionLayer;

    [ConfigVar(Name ="hitcollision.showdebug", DefaultValue = "0", Description = "Show debug")]
    public static ConfigVar ShowDebug;

    public static int PrimDebugChannel {
        get { return m_primDebugChannel; }
    }
    
    public HitCollisionModule(GameWorld world, int bufferSize, int primDebugChannel)
    {
        m_world = world;

        if (m_world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("HitColliderSystem");
            m_SystemRoot.transform.SetParent(m_world.SceneRoot.transform);
        }

        m_primDebugChannel = primDebugChannel;
        
        m_RaySphereQueryReciever = m_world.GetECSWorld().CreateManager<RaySphereQueryReciever>(m_world);
        m_HandleSplashDamageRequest = m_world.GetECSWorld().CreateManager<HandleSplashDamageRequests>(m_world);
        m_StoreColliderStates = m_world.GetECSWorld().CreateManager<StoreColliderStates>(m_world);
        m_HandleHitCollisionSpawning = m_world.GetECSWorld().CreateManager<HandleHitCollisionSpawning>(m_world,m_SystemRoot,bufferSize);
        m_HandleHitCollisionDespawning = m_world.GetECSWorld().CreateManager<HandleHitCollisionDespawning>(m_world);
        
        HitCollisionLayer = LayerMask.NameToLayer("hitcollision_enabled");
        DisabledHitCollisionLayer = LayerMask.NameToLayer("hitcollision_disabled");
    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_HandleSplashDamageRequest);
        m_world.GetECSWorld().DestroyManager(m_StoreColliderStates);
        m_world.GetECSWorld().DestroyManager(m_HandleHitCollisionSpawning);
        m_world.GetECSWorld().DestroyManager(m_HandleHitCollisionDespawning);
        m_world.GetECSWorld().DestroyManager(m_RaySphereQueryReciever);

        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }

    public void HandleSpawning()
    {
        m_HandleHitCollisionSpawning.Update();
    }

    public void HandleDespawn()
    {
        m_HandleHitCollisionDespawning.Update();
    }

    public void HandleSplashDamage() 
    {
        m_HandleSplashDamageRequest.Update();
    }

    public void StoreColliderState()
    {
        m_StoreColliderStates.Update();
    }

    readonly GameWorld m_world;
    readonly HandleSplashDamageRequests m_HandleSplashDamageRequest;
    readonly StoreColliderStates m_StoreColliderStates;
    readonly HandleHitCollisionSpawning m_HandleHitCollisionSpawning;
    readonly HandleHitCollisionDespawning m_HandleHitCollisionDespawning;
    readonly RaySphereQueryReciever m_RaySphereQueryReciever;                                              
        
    GameObject m_SystemRoot;
    static int m_primDebugChannel;
        
}
