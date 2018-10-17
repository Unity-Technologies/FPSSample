using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorCamModuleServer 
{
    public SpectatorCamModuleServer(GameWorld world, BundledResourceManager resourceManager)
    {
        m_world = world;
        m_HandleSpectatorCamRequests =  world.GetECSWorld().CreateManager<HandleSpectatorCamRequests>(world, resourceManager);
    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_HandleSpectatorCamRequests);  
    }

    public void HandleSpawnRequests()
    {
        m_HandleSpectatorCamRequests.Update();
    }
    
    GameWorld m_world;
    HandleSpectatorCamRequests m_HandleSpectatorCamRequests;
}
