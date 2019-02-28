using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif




[CreateAssetMenu(fileName = "GrenadeEntityFactory",menuName = "FPS Sample/Grenade/GrenadeEntityFactory")]
public class GrenadeEntityFactory : ReplicatedEntityFactory
{
    public Grenade.Settings settings;

    public override Entity Create(EntityManager entityManager, BundledResourceManager resourceManager, 
        GameWorld world)
    {
        var entity = entityManager.CreateEntity(typeof(PresentationOwnerData), typeof(ReplicatedEntityData), 
            typeof(Grenade.Settings), typeof(Grenade.InternalState), typeof(Grenade.InterpolatedState) );

        var repData = new ReplicatedEntityData( guid);
        var presentationOwner = new PresentationOwnerData(0);
        
        var internalState = new Grenade.InternalState
        {
            active = 1,
            rayQueryId = -1,
        };

        entityManager.SetComponentData(entity, repData);
        entityManager.SetComponentData(entity, presentationOwner);
        entityManager.SetComponentData(entity, settings);
        entityManager.SetComponentData(entity, internalState);
        
        return entity;
    }
 
}

#if UNITY_EDITOR
[CustomEditor(typeof(GrenadeEntityFactory))]
public class GrenadeEntityFactoryEditor : ReplicatedEntityFactoryEditor<GrenadeEntityFactory>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif


