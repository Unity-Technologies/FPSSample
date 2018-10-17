using UnityEngine;
using Object = UnityEngine.Object;







public class ProjectileModuleClient 
{
    [ConfigVar(Name = "projectile.logclientinfo", DefaultValue = "0", Description = "Show projectilesystem info")]
    public static ConfigVar logInfo;
    
    [ConfigVar(Name = "projectile.drawclientdebug", DefaultValue = "0", Description = "Show projectilesystem debug")]
    public static ConfigVar drawDebug;
    
    
    public ProjectileModuleClient(GameWorld world, BundledResourceManager resourceSystem)
    {
        m_world = world;
        
        if (world.SceneRoot != null)
        {
            m_SystemRoot = new GameObject("ProjectileSystem");
            m_SystemRoot.transform.SetParent(world.SceneRoot.transform);
        }
        
        m_settings = Resources.Load<ProjectileModuleSettings>("ProjectileModuleSettings");

        m_clientProjectileFactory = new ClientProjectileFactory(m_world, m_world.GetEntityManager(), m_SystemRoot, resourceSystem);
        
        m_handleRequests = m_world.GetECSWorld().CreateManager<HandleClientProjectileRequests>(m_world, resourceSystem, m_SystemRoot, m_clientProjectileFactory);
        m_handleProjectileSpawn = m_world.GetECSWorld().CreateManager<HandleProjectileSpawn>(m_world, m_SystemRoot, resourceSystem, m_clientProjectileFactory);
        m_removeMispredictedProjectiles = m_world.GetECSWorld().CreateManager<RemoveMispredictedProjectiles>(m_world);
        m_despawnClientProjectiles = m_world.GetECSWorld().CreateManager<DespawnClientProjectiles>(m_world, m_clientProjectileFactory);
        m_CreateProjectileMovementQueries = m_world.GetECSWorld().CreateManager<CreateProjectileMovementCollisionQueries>(m_world);
        m_HandleProjectileMovementQueries = m_world.GetECSWorld().CreateManager<HandleProjectileMovementCollisionQuery>(m_world);
        m_updateClientProjectilesPredicted = m_world.GetECSWorld().CreateManager<UpdateClientProjectilesPredicted>(m_world);
        m_updateClientProjectilesNonPredicted = m_world.GetECSWorld().CreateManager<UpdateClientProjectilesNonPredicted>(m_world);
    }

    public void Shutdown()
    {
        m_world.GetECSWorld().DestroyManager(m_handleRequests);
        m_world.GetECSWorld().DestroyManager(m_handleProjectileSpawn);
        m_world.GetECSWorld().DestroyManager(m_removeMispredictedProjectiles);
        m_world.GetECSWorld().DestroyManager(m_despawnClientProjectiles);
        m_world.GetECSWorld().DestroyManager(m_CreateProjectileMovementQueries);
        m_world.GetECSWorld().DestroyManager(m_HandleProjectileMovementQueries);
        m_world.GetECSWorld().DestroyManager(m_updateClientProjectilesPredicted);
        m_world.GetECSWorld().DestroyManager(m_updateClientProjectilesNonPredicted);

    
        if(m_SystemRoot != null)
            Object.Destroy(m_SystemRoot);
        
        Resources.UnloadAsset(m_settings);
    }
        
    public void StartPredictedMovement()
    {
        m_CreateProjectileMovementQueries.Update();
    }

    
    public void FinalizePredictedMovement()
    {
        m_HandleProjectileMovementQueries.Update();
    }

    public void HandleProjectileSpawn()
    {
        m_handleProjectileSpawn.Update();
        m_removeMispredictedProjectiles.Update();
    }

    public void HandleProjectileDespawn()
    {
        m_despawnClientProjectiles.Update();
    }

    
    public void HandleProjectileRequests()
    {
        m_handleRequests.Update();
    }
    
    public void UpdateClientProjectilesNonPredicted()
    {
        m_updateClientProjectilesNonPredicted.Update();
    }

    public void UpdateClientProjectilesPredicted()
    {
        m_updateClientProjectilesPredicted.Update();
    }

    readonly GameWorld m_world;
    readonly GameObject m_SystemRoot;
    readonly ProjectileModuleSettings m_settings;

    readonly ClientProjectileFactory m_clientProjectileFactory;
    
    readonly HandleClientProjectileRequests m_handleRequests;
    readonly CreateProjectileMovementCollisionQueries m_CreateProjectileMovementQueries;
    readonly HandleProjectileMovementCollisionQuery m_HandleProjectileMovementQueries;

    readonly HandleProjectileSpawn m_handleProjectileSpawn;
    readonly RemoveMispredictedProjectiles m_removeMispredictedProjectiles;
    readonly DespawnClientProjectiles m_despawnClientProjectiles;
    readonly UpdateClientProjectilesNonPredicted m_updateClientProjectilesNonPredicted;
    readonly UpdateClientProjectilesPredicted m_updateClientProjectilesPredicted;
}
