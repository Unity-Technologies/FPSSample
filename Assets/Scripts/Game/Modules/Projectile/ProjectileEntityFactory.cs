using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileEntityFactory",menuName = "FPS Sample/Projectile/ProjectileEntityFactory")]
public class ProjectileEntityFactory : ReplicatedEntityFactor
{
    public override Entity Create(EntityManager entityManager)
    {
        var entity = entityManager.CreateEntity(typeof(ReplicatedDataEntity), typeof(ProjectileData) );

        // Add uninitialized replicated entity
        var repData = new ReplicatedDataEntity
        {
            id = -1,
            typeId = typeId,
        };
        entityManager.SetComponentData(entity, repData);

//        GameDebug.Log("ProjectileEntityFactory.Crate entity:" + entity + " typeId:" + repData.typeId + " id:" + repData.id);

        return entity;
    }
    
    public override INetworkSerializable[] CreateSerializables(EntityManager entityManager, Entity entity)
    {
        var serializableArray = new INetworkSerializable[1];
        serializableArray[0] = new SerializedComponentDataHandler<ProjectileData>(entityManager, entity); 
        return serializableArray;
    }
}
