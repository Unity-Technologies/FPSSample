using System.Collections.Generic;
using Unity.Entities;

public class CharacterBehaviours   
{
    public static void CreateHandleSpawnSystems(GameWorld world,SystemCollection systems, BundledResourceManager resourceManager, bool server)
    {        
        systems.Add(world.GetECSWorld().CreateManager<HandleCharacterSpawn>(world, resourceManager, server)); // TODO (mogensh) needs to be done first as it creates presentation
        systems.Add(world.GetECSWorld().CreateManager<HandleAnimStateCtrlSpawn>(world));
    }

    public static void CreateHandleDespawnSystems(GameWorld world,SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<HandleCharacterDespawn>(world));  // TODO (mogens) HandleCharacterDespawn dewpans char presentation and needs to be called before other HandleDespawn. How do we ensure this ?   
        systems.Add(world.GetECSWorld().CreateManager<HandleAnimStateCtrlDespawn>(world));
    }

    public static void CreateAbilityRequestSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<Movement_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<RocketJump_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Dead_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<AutoRifle_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Chaingun_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<GrenadeLauncher_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<ProjectileLauncher_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Sprint_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Melee_RequestActive>(world));
        systems.Add(world.GetECSWorld().CreateManager<Emote_RequestActive>(world));
        
        // Update main abilities
        systems.Add(world.GetECSWorld().CreateManager<DefaultBehaviourController_Update>(world));
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
        
        systems.Add(world.GetECSWorld().CreateManager<RocketJump_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Sprint_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<AutoRifle_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<ProjectileLauncher_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Chaingun_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<GrenadeLauncher_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Melee_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Emote_Update>(world));
        systems.Add(world.GetECSWorld().CreateManager<Dead_Update>(world));
    }

    public static void CreateAbilityResolveSystems(GameWorld world, SystemCollection systems)
    {
        systems.Add(world.GetECSWorld().CreateManager<AutoRifle_HandleCollisionQuery>(world));
        systems.Add(world.GetECSWorld().CreateManager<Melee_HandleCollision>(world));
    }
    
}
