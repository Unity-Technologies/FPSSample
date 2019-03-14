using System;
using Unity.Entities;

[Serializable]
public struct CharacterReplicatedData : IComponentData, IReplicatedComponent
{
    [NonSerialized] public int heroTypeIndex;
    [NonSerialized] public Entity abilityCollection;

    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<CharacterReplicatedData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteInt16("heroType",(short)heroTypeIndex);
        context.refSerializer.SerializeReference(ref writer, "behaviorController",abilityCollection);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        heroTypeIndex = reader.ReadInt16();
        context.refSerializer.DeserializeReference(ref reader, ref abilityCollection);
    }
    
    public Entity FindAbilityWithComponent(EntityManager entityManager, Type abilityType)
    {
        var buffer = entityManager.GetBuffer<EntityGroupChildren>(abilityCollection);
        for (int j = 0; j < buffer.Length; j++)
        {
            var childEntity = buffer[j].entity;
            if (!entityManager.HasComponent<CharBehaviour>(childEntity))
                continue;
            if (entityManager.HasComponent(childEntity, abilityType))
                return childEntity;
        }  
        
        return Entity.Null;
    }
}

public class CharacterReplicated : ComponentDataProxy<CharacterReplicatedData>
{
}