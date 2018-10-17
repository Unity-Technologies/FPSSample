using System.Collections.Generic;
using Unity.Entities;

public class ItemModule
{
    List<ScriptBehaviourManager> m_systems = new List<ScriptBehaviourManager>();
    GameWorld m_world;
    
    public ItemModule(GameWorld world)
    {
        m_world = world;
        m_systems.Add(world.GetECSWorld().CreateManager<RobotWeaponClientProjectileSpawnHandler>(world));
        m_systems.Add(world.GetECSWorld().CreateManager<TerraformerWeaponClientProjectileSpawnHandler>(world));
        m_systems.Add(world.GetECSWorld().CreateManager<UpdateTerraformerWeaponA>(world));
        m_systems.Add(world.GetECSWorld().CreateManager<UpdateItemActionTimelineTrigger>(world));
        m_systems.Add(world.GetECSWorld().CreateManager<System_RobotWeaponA>(world));
    }

    public void Shutdown()
    {
        foreach (var system in m_systems)
            m_world.GetECSWorld().DestroyManager(system);
    }

    public void Update()
    {        
        foreach (var system in m_systems)
            system.Update();
    }
}
