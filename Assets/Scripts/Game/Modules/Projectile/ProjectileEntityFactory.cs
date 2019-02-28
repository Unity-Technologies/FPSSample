using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ProjectileEntityFactory",menuName = "FPS Sample/Projectile/ProjectileEntityFactory")]
public class ProjectileEntityFactory : ReplicatedEntityFactory
{
    public override Entity Create(EntityManager entityManager, BundledResourceManager resourceManager, 
        GameWorld world)
    {
        var entity = entityManager.CreateEntity(typeof(ReplicatedEntityData), 
            typeof(ProjectileData) );

        var repData = new ReplicatedEntityData( guid);
        
        entityManager.SetComponentData(entity, repData);

//        GameDebug.Log("ProjectileEntityFactory.Crate entity:" + entity + " typeId:" + repData.typeId + " id:" + repData.id);

        return entity;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ProjectileEntityFactory))]
public class ProjectileEntityFactoryEditor : ReplicatedEntityFactoryEditor<ReplicatedEntityFactory>
{
}


#endif