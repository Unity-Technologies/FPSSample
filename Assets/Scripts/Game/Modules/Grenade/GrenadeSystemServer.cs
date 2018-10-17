using System;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

public struct GrenadeSpawnRequest : IComponentData
{
    public unsafe fixed byte prefabGUID[16];     
    public Vector3 position;
    public Vector3 velocity;
    public Entity owner;
    public int teamId;
    
    public static unsafe void Create(EntityCommandBuffer commandBuffer, byte* guid, Vector3 position, Vector3 velocity, Entity owner, int teamId)
    {
        var data = new GrenadeSpawnRequest();

        for(var i=0;i<16;i++)
            data.prefabGUID[i] = guid[i];
        data.position = position;
        data.velocity = velocity;
        data.owner = owner;
        data.teamId = teamId;
            
        commandBuffer.CreateEntity();
        commandBuffer.AddComponent(data);
    }
}

public class HandleGrenadeRequest : BaseComponentDataSystem<GrenadeSpawnRequest>
{
    private readonly BundledResourceManager m_resourceManager;

    public HandleGrenadeRequest(GameWorld world, BundledResourceManager resourceManager) : base(world)
    {
        m_resourceManager = resourceManager;
    }

    protected override void Update(Entity entity, GrenadeSpawnRequest request)
    {
        var byteArray = new byte[16];
        unsafe
        {
            for (var i = 0; i < byteArray.Length; i++)
                byteArray[i] = request.prefabGUID[i];
        }
        var grenadeGuid = new Guid(byteArray);
        var guidStr = grenadeGuid.ToString().Replace("-", "");
        var prefab = (GameObject)m_resourceManager.LoadSingleAssetResource(guidStr);
        var grenade = m_world.Spawn<Grenade>(prefab, Vector3.zero, Quaternion.identity);
           
        grenade.startTick = m_world.worldTime.tick;
        grenade.owner = request.owner;
        grenade.teamId = request.teamId;
        grenade.velocity = request.velocity;
        grenade.position = request.position;
        
        PostUpdateCommands.DestroyEntity(entity);
    }
}


[DisableAutoCreation]
public class StartGrenadeMovement : BaseComponentSystem
{
    struct Grenades
    {
        public ComponentArray<Grenade> grenades;
    }

    [Inject]
    Grenades GrenadeGroup;   
    
    
    public StartGrenadeMovement(GameWorld world) : base(world)
    {}

    protected override void OnUpdate()  
    {
        var time = m_world.worldTime;

        // Update movements
        for (var i = 0; i < GrenadeGroup.grenades.Length; i++)
        {
            var grenade = GrenadeGroup.grenades[i];

            if (!grenade.active || grenade.velocity.magnitude < 0.5f)
                continue;
            
            // Move grenade
            var startPos = grenade.position;
            var newVelocity = grenade.velocity - Vector3.up * grenade.gravity * time.tickDuration;
            var deltaPos = newVelocity * time.tickDuration;
            grenade.position = startPos + deltaPos;
            grenade.velocity = newVelocity;

            var collisionMask = ~(1 << grenade.teamId);
           
            // Setup new collision query
            var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
            grenade.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
            {
                hitCollisionTestTick = time.tick,
                origin = startPos,
                direction = math.normalize(newVelocity),
                distance = math.length(deltaPos) + grenade.collisionRadius,
                sphereCastRadius = grenade.proximityTriggerDist,
                testAgainsEnvironment = 1,
                sphereCastMask = collisionMask, 
                sphereCastExcludeOwner = time.DurationSinceTick(grenade.startTick) < 0.2f ? grenade.owner : Entity.Null,
            });
        }
    }
}

[DisableAutoCreation]
public class FinalizeGrenadeMovement : BaseComponentSystem
{
    public struct Grenades
    {
        public ComponentArray<Grenade> grenades;
        public ComponentArray<GrenadePresentation> presentations;
    }

    [Inject] 
    public Grenades Group;   
    
    public FinalizeGrenadeMovement(GameWorld world) : base(world) {}

    protected override void OnUpdate()
    {
        Profiler.BeginSample("FinalizeGrenadeMovement");
        
        var time = m_world.worldTime;
        var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
        
        for (var i = 0; i < Group.grenades.Length; i++)
        {
            var grenade = Group.grenades[i];
            if (!grenade.active)
            {
                // Keep grenades around for a short duration so shortlived grenades gets a chance to get replicated 
                // and explode effect played
                if(m_world.worldTime.DurationSinceTick(grenade.explodeTick) > 1.0f)
                    m_world.RequestDespawn(grenade.gameObject, PostUpdateCommands);
                
                continue;
            }

            var presentation = Group.presentations[i];


            var hitCollisionOwner = Entity.Null;            
            if (grenade.rayQueryId != -1)
            {
                RaySphereQueryReciever.Query query;
                RaySphereQueryReciever.Result result;
                queryReciever.GetResult(grenade.rayQueryId, out query, out result);
                grenade.rayQueryId = -1;
                    
                // If grenade hit something that was no hitCollision it is environment and grenade should bounce
                if (result.hit == 1 && result.hitCollisionOwner == Entity.Null)
                {
                    float3 moveDir = grenade.velocity.normalized;
                    var moveVel = grenade.velocity.magnitude;

                    grenade.position = result.hitPoint + result.hitNormal * grenade.collisionRadius;

                    moveDir = Vector3.Reflect(moveDir, result.hitNormal);
                    grenade.velocity = moveDir * moveVel * grenade.bounciness;

                    if(grenade.velocity.magnitude > 1.0f)
                        presentation.state.bouncetick = m_world.worldTime.tick;
                }

                if (result.hitCollisionOwner != Entity.Null)
                {
                    grenade.position = result.hitPoint;
                }

                hitCollisionOwner = result.hitCollisionOwner;
            }
            
            // Should we explode ?
            var timeout = time.DurationSinceTick(grenade.startTick) > grenade.maxLifetime;
            if (timeout || hitCollisionOwner != Entity.Null)
            {
                grenade.active = false;
                grenade.explodeTick = time.tick; 
                presentation.state.exploded = true;

                if (grenade.splashDamage.radius > 0)
                {
                    var collisionMask = ~(1 << grenade.teamId);

                    SplashDamageRequest.Create(PostUpdateCommands, time.tick, grenade.owner, grenade.position,
                        collisionMask, grenade.splashDamage);
                }
            }
            
            // Update presentation
            presentation.state.position = grenade.position;
        }
        
        Profiler.EndSample();
    }
    
}