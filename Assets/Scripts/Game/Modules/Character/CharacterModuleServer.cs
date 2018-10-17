using Unity.Entities;
using UnityEngine;

[DisableAutoCreation]
public class HandleDamage : BaseComponentSystem<HealthState,HitCollisionOwner>
{
    public HandleDamage(GameWorld gameWorld) : base(gameWorld)
    {}

    protected override void Update(Entity entity, HealthState healthState, HitCollisionOwner hitCollisionOwner)
    {
        if (healthState.health <= 0)
            return;

        var isDamaged = false;
        var impulseVec = Vector3.zero;
        var damage = 0.0f;
        var damageVec = Vector3.zero;


        // Apply hitcollider damage events
        var damageEvents = hitCollisionOwner.damageEvents;
        for (var eventIndex=0;eventIndex < damageEvents.Count; eventIndex++)
        {
            isDamaged = true;

            var damageEvent = damageEvents[eventIndex];

            //GameDebug.Log(string.Format("ApplyDamage. Target:{0} Instigator:{1} Dam:{2}", healthState.name, m_world.GetGameObjectFromEntity(damageEvent.instigator), damageEvent.damage ));
            healthState.ApplyDamage(ref damageEvent, m_world.worldTime.tick);

            impulseVec += damageEvent.direction * damageEvent.impulse;
            damageVec += damageEvent.direction * damageEvent.damage;
            damage += damageEvent.damage;

            //damageHistory.ApplyDamage(ref damageEvent, m_world.worldTime.tick);

            if (damageEvents[eventIndex].instigator != Entity.Null && EntityManager.Exists(damageEvent.instigator) && EntityManager.HasComponent<DamageHistory>(damageEvent.instigator))
            {
                var instigatorDamageHistory = EntityManager.GetComponentObject<DamageHistory>(damageEvent.instigator);
                if (m_world.worldTime.tick > instigatorDamageHistory.inflictedDamage.tick)
                {
                    instigatorDamageHistory.inflictedDamage.tick = m_world.worldTime.tick;
                    instigatorDamageHistory.inflictedDamage.lethal = false;
                }
                instigatorDamageHistory.inflictedDamage.lethal |= healthState.health <= 0;
            }

            hitCollisionOwner.collisionEnabled = healthState.health > 0;
        }
        hitCollisionOwner.damageEvents.Clear();

        if (isDamaged)
        {
            var charPredictedState = EntityManager.GetComponentObject<CharacterPredictedState>(entity);
            var damageImpulse = impulseVec.magnitude;
            var damageDir = damageImpulse > 0 ? impulseVec.normalized : damageVec.normalized;
            charPredictedState.State.damageTick = m_world.worldTime.tick;
            charPredictedState.State.damageDirection = damageDir;
            charPredictedState.State.damageImpulse = damageImpulse;


            if (healthState.health <= 0)
            {
                var ragdollState =  EntityManager.GetComponentObject<RagdollState>(entity);
                ragdollState.ragdollActive = true;
                ragdollState.impulse = impulseVec;
            }
        }
     
    }
}



public class CharacterModuleServer : CharacterModuleShared
{
    public CharacterModuleServer(GameWorld world, BundledResourceManager resourceSystem): base(world)
    {
        // Handle spawn requests
        m_HandleCharacterSpawnRequests = m_world.GetECSWorld().CreateManager<HandleCharacterSpawnRequests>(m_world, resourceSystem, true);
        m_HandleCharacterDespawnRequests = m_world.GetECSWorld().CreateManager<HandleCharacterDespawnRequests>(m_world);

        // Handle controlled entity changed
        CharacterBehaviours.CreateControlledEntityChangedSystems(m_world, m_ControlledEntityChangedSystems);
        m_ControlledEntityChangedSystems.Add(m_world.GetECSWorld().CreateManager<PlayerCharacterControlSystem>(m_world));

        // Handle spawn
        CharacterBehaviours.CreateHandleSpawnSystems(m_world, m_HandleSpawnSystems, resourceSystem);

        // Handle despawn
        CharacterBehaviours.CreateHandleDespawnSystems(m_world, m_HandleSpawnSystems);
        
        // Movement
        m_MovementStartSystems.Add(m_world.GetECSWorld().CreateManager<UpdateTeleportation>(m_world));
        CharacterBehaviours.CreateMovementStartSystems(m_world,m_MovementStartSystems);
        CharacterBehaviours.CreateMovementResolveSystems(m_world,m_MovementResolveSystems);

        // Ability 
        CharacterBehaviours.CreateAbilityStartSystems(m_world,m_AbilityStartSystems);
        CharacterBehaviours.CreateAbilityResolveSystems(m_world,m_AbilityResolveSystems);
        
        
        
        m_UpdateCharPresentationState = m_world.GetECSWorld().CreateManager<UpdateCharPresentationState>(m_world);
        m_ApplyPresentationStateToCharacters = m_world.GetECSWorld().CreateManager<ApplyPresentationStateToCharacters>(m_world);
        m_ApplyPresentationStateToItems = m_world.GetECSWorld().CreateManager<ApplyPresentationStateToItems>(m_world);

        
        m_HandleDamage = m_world.GetECSWorld().CreateManager<HandleDamage>(m_world);
        m_characterItemLateUpdate = m_world.GetECSWorld().CreateManager<CharacterItemLateUpdate>(m_world);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        
        m_world.GetECSWorld().DestroyManager(m_HandleCharacterDespawnRequests);
        
        m_world.GetECSWorld().DestroyManager(m_UpdateCharPresentationState);

        m_world.GetECSWorld().DestroyManager(m_HandleDamage);
        m_world.GetECSWorld().DestroyManager(m_characterItemLateUpdate);
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationStateToCharacters);
        m_world.GetECSWorld().DestroyManager(m_ApplyPresentationStateToItems);
        
    }

    public void HandleSpawnRequests()
    {
        m_HandleCharacterDespawnRequests.Update();
        m_HandleCharacterSpawnRequests.Update();
    }

    public void HandleDamage()
    {
        m_HandleDamage.Update();
    }
    
    public void PresentationUpdate()
    {
        m_UpdateCharPresentationState.Update();
        m_ApplyPresentationStateToCharacters.Update();
        m_ApplyPresentationStateToItems.Update();
    }

    public void AttachmentUpdate()
    {
        m_characterItemLateUpdate.Update();    
    }
    
    public void CleanupPlayer(PlayerState player)
    {
        if (player.controlledEntity != Entity.Null)
        {
            CharacterDespawnRequest.Create(m_world, player.controlledEntity);
        }
    }

   

    readonly HandleCharacterSpawnRequests m_HandleCharacterSpawnRequests;
    readonly HandleCharacterDespawnRequests m_HandleCharacterDespawnRequests;

    readonly UpdateCharPresentationState m_UpdateCharPresentationState;
    readonly ApplyPresentationStateToCharacters m_ApplyPresentationStateToCharacters;
    readonly ApplyPresentationStateToItems m_ApplyPresentationStateToItems;
    
    readonly HandleDamage m_HandleDamage;
    
    readonly CharacterItemLateUpdate m_characterItemLateUpdate;
}
