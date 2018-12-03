using UnityEngine;

public class RagdollModule 
{
    public RagdollModule(GameWorld world)
    {
        m_world = world;

        if (world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("RagdollSystem");
            m_SystemRoot.transform.SetParent(world.SceneRoot.transform);
        }
        
        m_updateRagdolls = m_world.GetECSWorld().CreateManager<UpdateRagdolls>(m_world);
        m_handleRagdollSpawn = m_world.GetECSWorld().CreateManager<HandleRagdollSpawn>(m_world, m_SystemRoot);
        m_handleRagdollDespawn = m_world.GetECSWorld().CreateManager<HandleRagdollDespawn>(m_world);
    }

    public void Shutdown()
    {

        m_world.GetECSWorld().DestroyManager(m_updateRagdolls);
        m_world.GetECSWorld().DestroyManager(m_handleRagdollSpawn);
        m_world.GetECSWorld().DestroyManager(m_handleRagdollDespawn);
        
        if(m_SystemRoot != null)
            GameObject.Destroy(m_SystemRoot);
    }


    public void HandleSpawning()
    {
        m_handleRagdollSpawn.Update();
    }

    public void HandleDespawning()
    {
        m_handleRagdollDespawn.Update();
    }
    
    public void LateUpdate()
    {
        m_updateRagdolls.Update();
    }
    
    protected GameWorld m_world;
    protected GameObject m_SystemRoot;

    readonly UpdateRagdolls m_updateRagdolls;
    readonly HandleRagdollSpawn m_handleRagdollSpawn;
    readonly HandleRagdollDespawn m_handleRagdollDespawn;
}
