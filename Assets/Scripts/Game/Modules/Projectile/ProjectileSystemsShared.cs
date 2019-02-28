using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Profiling;
using Unity.Mathematics;

[DisableAutoCreation]
public class CreateProjectileMovementCollisionQueries : BaseComponentSystem
{
    ComponentGroup ProjectileGroup;

    public CreateProjectileMovementCollisionQueries(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        ProjectileGroup = GetComponentGroup(typeof(UpdateProjectileFlag), typeof(ProjectileData), 
            ComponentType.Subtractive<DespawningEntity>());
    }

    protected override void OnUpdate()
    {
        var entityArray = ProjectileGroup.GetEntityArray();
        var projectileDataArray = ProjectileGroup.GetComponentDataArray<ProjectileData>();
        var time = m_world.worldTime;
        for (var i = 0; i < projectileDataArray.Length; i++)
        {
            var projectileData = projectileDataArray[i];
            if (projectileData.impactTick > 0)
                continue;

            var entity = entityArray[i];

            var collisionTestTick = time.tick - projectileData.collisionCheckTickDelay;

            var totalMoveDuration = time.DurationSinceTick(projectileData.startTick);
            var totalMoveDist = totalMoveDuration * projectileData.settings.velocity;

            var dir = Vector3.Normalize(projectileData.endPos - projectileData.startPos);
            var newPosition = (Vector3)projectileData.startPos + dir * totalMoveDist;
            var moveDist = math.distance(projectileData.position, newPosition);

            var collisionMask = ~(1U << projectileData.teamId);

            var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();
            projectileData.rayQueryId = queryReciever.RegisterQuery(new RaySphereQueryReciever.Query()
            {
                hitCollisionTestTick = collisionTestTick,
                origin = projectileData.position,
                direction = dir,
                distance = moveDist,
                radius = projectileData.settings.collisionRadius,
                mask = collisionMask,
                ExcludeOwner = projectileData.projectileOwner,
            });
            PostUpdateCommands.SetComponent(entity,projectileData);
        }
    }
}

[DisableAutoCreation]
public class HandleProjectileMovementCollisionQuery : BaseComponentSystem
{
    ComponentGroup ProjectileGroup;

    public HandleProjectileMovementCollisionQuery(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        ProjectileGroup = GetComponentGroup(typeof(UpdateProjectileFlag), typeof(ProjectileData), 
            ComponentType.Subtractive<DespawningEntity>());
    }
    
    protected override void OnUpdate()
    {
        var entityArray = ProjectileGroup.GetEntityArray();
        var projectileDataArray = ProjectileGroup.GetComponentDataArray<ProjectileData>();
        var queryReciever = World.GetExistingManager<RaySphereQueryReciever>();    
        for (var i = 0; i < projectileDataArray.Length; i++)
        {
            var projectileData = projectileDataArray[i];
            
            if (projectileData.impactTick > 0)
                continue;
            
            RaySphereQueryReciever.Query query;
            RaySphereQueryReciever.QueryResult queryResult;
            queryReciever.GetResult(projectileData.rayQueryId, out query, out queryResult);
            
            var projectileVec = projectileData.endPos - projectileData.startPos;
            var projectileDir = Vector3.Normalize(projectileVec);
            var newPosition = (Vector3)projectileData.position + projectileDir * query.distance;

            var impact = queryResult.hit == 1;
            if (impact)
            {
                projectileData.impacted = 1;
                projectileData.impactPos = queryResult.hitPoint;
                projectileData.impactNormal = queryResult.hitNormal;
                projectileData.impactTick = m_world.worldTime.tick;

                // Owner can despawn while projectile is in flight, so we need to make sure we dont send non existing instigator
                var damageInstigator = EntityManager.Exists(projectileData.projectileOwner) ? projectileData.projectileOwner : Entity.Null;

                var collisionHit = queryResult.hitCollisionOwner != Entity.Null;
                if (collisionHit)
                {
                    if (damageInstigator != Entity.Null)
                    {
                        if (EntityManager.HasComponent<DamageEvent>(queryResult.hitCollisionOwner))
                        {
                            var damageEventBuffer = EntityManager.GetBuffer<DamageEvent>(queryResult.hitCollisionOwner);
                            DamageEvent.AddEvent(damageEventBuffer, damageInstigator, projectileData.settings.impactDamage, projectileDir, projectileData.settings.impactImpulse);
                        }
                    }
                }

                if (projectileData.settings.splashDamage.radius > 0)
                {
                    if (damageInstigator != Entity.Null)
                    {
                        var collisionMask = ~(1 << projectileData.teamId);
                        SplashDamageRequest.Create(PostUpdateCommands, query.hitCollisionTestTick, damageInstigator, queryResult.hitPoint, collisionMask, projectileData.settings.splashDamage);
                    }
                }

                newPosition = queryResult.hitPoint;
            }

            if (ProjectileModuleServer.drawDebug.IntValue == 1)
            {
                var color = impact ? Color.red : Color.green;
                Debug.DrawLine(projectileData.position, newPosition, color, 2);
                DebugDraw.Sphere(newPosition, 0.1f, color, impact ? 2 : 0);
            }

            projectileData.position = newPosition;
            PostUpdateCommands.SetComponent(entityArray[i],projectileData);
        }
    }
}


[DisableAutoCreation]
public class DespawnProjectiles : BaseComponentSystem
{
    ComponentGroup ProjectileGroup;

    public DespawnProjectiles(GameWorld world) : base(world) { }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        ProjectileGroup = GetComponentGroup(typeof(ProjectileData));
    }
    
    protected override void OnUpdate()
    {
        var time = m_world.worldTime;
        var entityArray = ProjectileGroup.GetEntityArray();
        var projectileDataArray = ProjectileGroup.GetComponentDataArray<ProjectileData>();
        for (var i = 0; i < projectileDataArray.Length; i++)
        {
            var projectileData = projectileDataArray[i];
            
            if (projectileData.impactTick > 0)
            {
                if (m_world.worldTime.DurationSinceTick(projectileData.impactTick) > 1.0f)
                {
                    PostUpdateCommands.AddComponent(entityArray[i],new DespawningEntity());
                }
                continue;
            }

            var age = time.DurationSinceTick(projectileData.startTick);
            var toOld = age > projectileData.maxAge;
            if (toOld)
            {
                PostUpdateCommands.AddComponent(entityArray[i],new DespawningEntity());
            }
        }
    }
}



