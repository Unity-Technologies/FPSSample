using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class EffectModuleClient      
{
    public EffectModuleClient(GameWorld world, BundledResourceManager resourceSystem)
    {
        m_GameWorld = world;
        m_resourceSystem = resourceSystem;

        m_HandleSpatialEffectRequests = m_GameWorld.GetECSWorld().CreateManager<HandleSpatialEffectRequests>(m_GameWorld);
        m_HandleHitscanEffectRequests = m_GameWorld.GetECSWorld().CreateManager<HandleHitscanEffectRequests>(m_GameWorld);
        m_VFXSystem = m_GameWorld.GetECSWorld().CreateManager<VFXSystem>();
    }

    public void Shutdown()
    {
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleSpatialEffectRequests);
        m_GameWorld.GetECSWorld().DestroyManager(m_HandleHitscanEffectRequests);
        m_GameWorld.GetECSWorld().DestroyManager(m_VFXSystem);
    }

    public void ClientUpdate()
    {
        m_HandleSpatialEffectRequests.Update();
        m_HandleHitscanEffectRequests.Update();
        m_VFXSystem.Update();
    }

    
    readonly GameWorld m_GameWorld;
    readonly BundledResourceManager m_resourceSystem;

    HandleSpatialEffectRequests m_HandleSpatialEffectRequests;
    HandleHitscanEffectRequests m_HandleHitscanEffectRequests;
    VFXSystem m_VFXSystem;
}
