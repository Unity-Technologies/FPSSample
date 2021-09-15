using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct PresentationOwnerData : IComponentData
{
    public int variation;
    public int currentVariation;
    public Entity currentVariationEntity;
    
    public PresentationOwnerData(int variation)
    {
        this.variation = variation;
        currentVariation = -1;
        currentVariationEntity = Entity.Null;
    }
}

public class PresentationOwner : ComponentDataProxy<PresentationOwnerData>
{}


[DisableAutoCreation]
public class UpdatePresentationOwners : BaseComponentSystem
{
    ComponentGroup Group;
    readonly PresentationRegistry m_presentationRegistry;
    readonly BundledResourceManager m_resourceManager;
    
    public UpdatePresentationOwners(GameWorld world, BundledResourceManager resourceManager) : base(world)
    {
        m_presentationRegistry = resourceManager.GetResourceRegistry<PresentationRegistry>();
        m_resourceManager = resourceManager;
    }

    protected override void OnCreateManager()
    {
        base.OnCreateManager();
        Group = GetComponentGroup(typeof(PresentationOwnerData));
    }


    List<Entity> m_entityBuffer = new List<Entity>(16);
    List<PresentationOwnerData> m_typeDataBuffer = new List<PresentationOwnerData>(16);
    protected override void OnUpdate()
    {
        // Add entities that needs change to buffer (as we cant destroy/create while iterating)
        var gameEntityTypeArray = Group.GetComponentDataArray<PresentationOwnerData>();
        var entityArray = Group.GetEntityArray();
        m_entityBuffer.Clear();
        m_typeDataBuffer.Clear();
        for (int i = 0; i < gameEntityTypeArray.Length; i++)
        {
            var typeData = gameEntityTypeArray[i];

            if (typeData.variation == typeData.currentVariation)
                continue;

            m_entityBuffer.Add(entityArray[i]);
            m_typeDataBuffer.Add(typeData);
        }

        for (int i = 0; i < m_entityBuffer.Count; i++)
        {
            var entity = m_entityBuffer[i];
            var typeData = m_typeDataBuffer[i];

            var replicatedData = EntityManager.GetComponentData<ReplicatedEntityData>(entity);

            WeakAssetReference presentationGuid;
            var found = m_presentationRegistry.GetPresentation(replicatedData.assetGuid, out presentationGuid);

            if (!found)
            {
                continue;
            }
            
            
            
//            var registryEntry = m_assetRegistry.GetEntry(replicatedData.assetGuid);
//
//            if (registryEntry == null)
//                continue;
//            
//            if (registryEntry.factory == null)
//                continue;
            
            //var presentation = registryEntry.factory.CreateVariation(EntityManager, m_resourceManager, m_world, entity, 0);

            var presentation = m_resourceManager.CreateEntity(presentationGuid);
            GameDebug.Assert(presentation != Entity.Null, "failed to create presentation");
                
            
            typeData.currentVariation = typeData.variation;
            typeData.currentVariationEntity = presentation;
            EntityManager.SetComponentData(entity,typeData);

            var presentationEntity = EntityManager.GetComponentObject<PresentationEntity>(presentation);
            presentationEntity.ownerEntity = entity;

        }
    }
} 



[DisableAutoCreation]
public class HandlePresentationOwnerDesawn : DeinitializeComponentDataSystem<PresentationOwnerData>
{
    public HandlePresentationOwnerDesawn(GameWorld world) : base(world)
    {
    }

    protected override void Deinitialize(Entity entity, PresentationOwnerData component)
    {
        if (component.currentVariationEntity != Entity.Null)
        {
            // TODO (mogensh) for now we know presentation is a gameobject. We should support entity with entitygroup

            var gameObject = EntityManager.GetComponentObject<Transform>(component.currentVariationEntity).gameObject;
                
            m_world.RequestDespawn(gameObject);
                
            component.currentVariation = -1;
            EntityManager.SetComponentData(entity,component);
        }
    }

}
