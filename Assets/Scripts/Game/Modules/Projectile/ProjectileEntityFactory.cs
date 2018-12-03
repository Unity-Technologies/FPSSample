using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ProjectileEntityFactory",menuName = "FPS Sample/Projectile/ProjectileEntityFactory")]
public class ProjectileEntityFactory : ReplicatedEntityFactory
{
    public override Entity Create(EntityManager entityManager, int predictingPlayerId)
    {
        var entity = entityManager.CreateEntity(typeof(ReplicatedDataEntity), typeof(ProjectileData) );

        // Add uninitialized replicated entity
        var repData = new ReplicatedDataEntity
        {
            id = -1,
            typeId = registryId,
            predictingPlayerId = predictingPlayerId,
        };
        entityManager.SetComponentData(entity, repData);

//        GameDebug.Log("ProjectileEntityFactory.Crate entity:" + entity + " typeId:" + repData.typeId + " id:" + repData.id);

        return entity;
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(ProjectileEntityFactory))]
public class ProjectileEntityFactoryEditor : ReplicatedFactoryEntryEditor<ReplicatedEntityFactory>
{
}

public class ReplicatedFactoryEntryEditor<T> : Editor
    where T : ReplicatedEntityFactory
{
    public override void OnInspectorGUI()
    {
        var registry = ReplicatedEntityRegistry.GetReplicatedEntityRegistry();
        if (registry == null)
        {
            EditorGUILayout.HelpBox("Make sure you have a ReplicatedEntityRegistry in project", MessageType.Error);
            return;
        }


        var factory = target as T;
        
        var registryIndex = registry != null ? registry.GetId(factory) : -1;
        
        GUILayout.Label("Factory registry index:" + factory.registryId);
        GUILayout.Label("Registry index:" + registryIndex);

        if (registryIndex != factory.registryId)
            EditorGUILayout.HelpBox("Factory index does not match client registry index", MessageType.Error);

        if (registryIndex != -1 || factory.registryId != -1)
        {
            if (GUILayout.Button("Unregister"))
            {
                if (registryIndex != -1)
                    registry.ClearAtId(registryIndex);
                factory.registryId = -1;
                EditorUtility.SetDirty(target);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("NOT REGISTERED!", MessageType.Error);
            
            if (GUILayout.Button("Register"))
            {
                registryIndex = registry.FindFreeId();
                registry.SetFactory(registryIndex, factory);
                factory.registryId = registryIndex;
                EditorUtility.SetDirty(target);
            }
        }
    }
}

#endif