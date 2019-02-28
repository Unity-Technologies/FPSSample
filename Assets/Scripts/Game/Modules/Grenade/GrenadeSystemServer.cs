using System;
using Unity.Mathematics;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;

public struct GrenadeSpawnRequest : IComponentData
{
    public WeakAssetReference assetGuid;
    public Vector3 position;
    public Vector3 velocity;
    public Entity owner;
    public int teamId;
    
    public static void Create(EntityCommandBuffer commandBuffer, WeakAssetReference assetGuid, Vector3 position, Vector3 velocity, Entity owner, int teamId)
    {
        var data = new GrenadeSpawnRequest();
        data.assetGuid = assetGuid;
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
        var grenadeEntity = m_resourceManager.CreateEntity(request.assetGuid);
        
        var internalState = EntityManager.GetComponentData<Grenade.InternalState>(grenadeEntity);       
        internalState.startTick = m_world.worldTime.tick;
        internalState.owner = request.owner;
        internalState.teamId = request.teamId;
        internalState.velocity = request.velocity;
        internalState.position = request.position;
        EntityManager.SetComponentData(grenadeEntity,internalState);
        
        PostUpdateCommands.DestroyEntity(entity);
    }
}


[DisableAutoCreation]
public class StartGrenadeMovement : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public StartGrenadeMovement(GameWorld world) : base(world)
    {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Grenade.Settings),typeof(Grenade.InternalState));
    }

    protected override void OnUpdate()  
    {
        var time = m_world.worldTime;

        // Update movements  
        var entityArray = Group.GetEntityArray();      
        var settingsArray = Group.GetComponentDataArray<Grenade.Settings>();
        var internalStateArray = Group.GetComponentDataArray<Grenade.InternalState>();
        for (var i = 0; i < internalStateArray.Length; i++)
        {
            var internalState = internalStateArray[i];

            if (internalState.active == 0 || math.length(internalState.velocity) < 0.5f)
                continue;

            var entity = entityArray[i]; 
            var settings = settingsArray[i];
            
            // Crate movement query
            var startPos = internalState.position;
            var newVelocity = internalState.velocity - new float3(0,1,0) * settings.gravity * time.tickDuration;
            var deltaPos = newVelocity * time.tickDuration;

            internalState.position = startPos + deltaPos;
            internalState.velocity = newVelocity;
            
            
            var collisionMask = ~(1U << internalState.teamId);
           
            // Setup new collision query
            var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
            internalState.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
            {
                hitCollisionTestTick = time.tick,
                origin = startPos,
                direction = math.normalize(newVelocity),
                distance = math.length(deltaPos) + settings.collisionRadius,
                radius = settings.proximityTriggerDist,
                mask = collisionMask, 
                ExcludeOwner = time.DurationSinceTick(internalState.startTick) < 0.2f ? internalState.owner : Entity.Null,
            });
            
            EntityManager.SetComponentData(entity,internalState);
        }
    }
}

[DisableAutoCreation]
public class FinalizeGrenadeMovement : BaseComponentSystem
{
    ComponentGroup Group;   
    
    public FinalizeGrenadeMovement(GameWorld world) : base(world) {}

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(Grenade.Settings),typeof(Grenade.InternalState), 
            typeof(Grenade.InterpolatedState));
    }

    protected override void OnUpdate()
    {
        Profiler.BeginSample("FinalizeGrenadeMovement");
        
        var time = m_world.worldTime;
        var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();

        var grenadeEntityArray = Group.GetEntityArray();
        var settingsArray = Group.GetComponentDataArray<Grenade.Settings>();
        var internalStateArray = Group.GetComponentDataArray<Grenade.InternalState>();
        var interpolatedStateArray = Group.GetComponentDataArray<Grenade.InterpolatedState>();

        for (var i = 0; i < internalStateArray.Length; i++)
        {
            var internalState = internalStateArray[i];
            var entity = grenadeEntityArray[i];
            
            if (internalState.active == 0)
            {
                // Keep grenades around for a short duration so shortlived grenades gets a chance to get replicated 
                // and explode effect played
                
                if(m_world.worldTime.DurationSinceTick(internalState.explodeTick) > 1.0f)
                    m_world.RequestDespawn(PostUpdateCommands, entity);
                
                continue;
            }

            var settings = settingsArray[i];
            var interpolatedState = interpolatedStateArray[i];
            var hitCollisionOwner = Entity.Null;            
            if (internalState.rayQueryId != -1)
            {
                RaySphereQueryReciever.Query query;
                RaySphereQueryReciever.QueryResult queryResult;
                queryReciever.GetResult(internalState.rayQueryId, out query, out queryResult);
                internalState.rayQueryId = -1;
                    
                // If grenade hit something that was no hitCollision it is environment and grenade should bounce
                if (queryResult.hit == 1 && queryResult.hitCollisionOwner == Entity.Null)
                {
                    var moveDir = math.normalize(internalState.velocity);
                    var moveVel = math.length(internalState.velocity);

                    internalState.position = queryResult.hitPoint + queryResult.hitNormal * settings.collisionRadius;

                    moveDir = Vector3.Reflect(moveDir, queryResult.hitNormal);
                    internalState.velocity = moveDir * moveVel * settings.bounciness;

                    if(moveVel > 1.0f)
                        interpolatedState.bouncetick = m_world.worldTime.tick;
                }

                if (queryResult.hitCollisionOwner != Entity.Null)
                {
                    internalState.position = queryResult.hitPoint;
                }

                hitCollisionOwner = queryResult.hitCollisionOwner;
            }
            
            // Should we explode ?
            var timeout = time.DurationSinceTick(internalState.startTick) > settings.maxLifetime;
            if (timeout || hitCollisionOwner != Entity.Null)
            {
                internalState.active = 0;
                internalState.explodeTick = time.tick; 
                interpolatedState.exploded = 1;

                if (settings.splashDamage.radius > 0)
                {
                    var collisionMask = ~(1 << internalState.teamId);

                    SplashDamageRequest.Create(PostUpdateCommands, time.tick, internalState.owner, internalState.position,
                        collisionMask, settings.splashDamage);
                }
            }
            
            interpolatedState.position = internalState.position;
            
            DebugDraw.Sphere(interpolatedState.position,settings.collisionRadius,Color.red);
            
            
            EntityManager.SetComponentData(entity,internalState);
            EntityManager.SetComponentData(entity,interpolatedState);
        }
        
        Profiler.EndSample();
    }
    
}