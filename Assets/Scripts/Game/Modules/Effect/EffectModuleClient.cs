using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class EffectModuleClient      
{
    public EffectModuleClient(GameWorld world, BundledResourceManager resourceSystem)
    {
        m_GameWorld = world;
        m_resourceSystem = resourceSystem;

        if (world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("EffectSystem");
            m_SystemRoot.transform.SetParent(world.SceneRoot.transform);
        }
        
        m_HandleSpatialEffectRequests = m_GameWorld.GetECSWorld().CreateManager<HandleSpatialEffectRequests>(m_GameWorld, m_SystemRoot, m_resourceSystem);
        m_HandleHitscanEffectRequests = m_GameWorld.GetECSWorld().CreateManager<HandleHitscanEffectRequests>(m_GameWorld, m_SystemRoot, m_resourceSystem);
    }

    public void Shutdown()
    {
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleSpatialEffectRequests);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleHitscanEffectRequests);
       
        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }

    public void ClientUpdate()
    {
        m_HandleSpatialEffectRequests.Update();
        m_HandleHitscanEffectRequests.Update();
    }

    
    readonly GameWorld m_GameWorld;
    readonly GameObject m_SystemRoot;
    readonly BundledResourceManager m_resourceSystem;

    HandleSpatialEffectRequests m_HandleSpatialEffectRequests;
    HandleHitscanEffectRequests m_HandleHitscanEffectRequests;
}
