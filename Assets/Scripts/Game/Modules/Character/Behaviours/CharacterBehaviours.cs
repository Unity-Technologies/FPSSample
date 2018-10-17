using System.Collections.Generic;
using Unity.Entities;

public class CharacterBehaviours   
{
    public static void CreateControlledEntityChangedSystems(GameWorld world,SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<AbilityCtrl_UpdateServerEntityComponent>(world));
    }

    public static void CreateHandleSpawnSystems(GameWorld world,SystemCollection systems, BundledResourceManager resourceManager)
    {        
        systems.Add(world.GetECSWorld().CreateManager<HandleAnimStateCtrlSpawn>(world));
        systems.Add(world.GetECSWorld().CreateManager<HandleCharacterSpawn>(world, resourceManager));
        systems.Add(world.GetECSWorld().CreateManager<AbilityCtrl_HandleSpawn>(world));
    }

    public static void CreateHandleDespawnSystems(GameWorld world,SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<HandleAnimStateCtrlDespawn>(world));
        systems.Add(world.GetECSWorld().CreateManager<HandleCharacterDespawn>(world));
    }
    
    public static void CreateRollbackSystems( GameWorld world, List<ScriptBehaviourManager> systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<AbilityCtrl_Rollback>(world));
        systems.Add(world.GetECSWorld().CreateManager<ReplicatedAbilityRollback>(world));
    }

    public static void CreateMovementStartSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<GroundTest>(world));
        systems.Add(world.GetECSWorld().CreateManager<Movement_Update>(world));
    }

    public static void CreateMovementResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<HandleMovementQueries>(world));
        systems.Add(world.GetECSWorld().CreateManager<Movement_HandleCollision>(world));
    }

    public static void CreateAbilityStartSystems(GameWorld world, SystemCollection systems)
    {
        // Ability request phase
        systems.Add(world.GetECSWorld().CreateManager<Sprint_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<AutoRifle_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<ProjectileLauncher_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Chaingun_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<GrenadeLauncher_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Melee_RequestActive>(world));
        
        // Resolve active abilities
        systems.Add(world.GetECSWorld().CreateManager<AbilityCtrl_Update>(world));
        
        // Update active abilities
        systems.Add(world.GetECSWorld().CreateManager<Sprint_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<AutoRifle_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<ProjectileLauncher_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Chaingun_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<GrenadeLauncher_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Melee_Update>(world));
    }

    public static void CreateAbilityResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<AutoRifle_HandleCollisionQuery>(world));
        systems.Add(world.GetECSWorld().CreateManager<Melee_HandleCollision>(world));
    }
}
