using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class ReplicatedEntityCollection : IEntityReferenceSerializer
{
    public struct ReplicatedData
    {
        public Entity entity;
        public INetworkSerializable[] serializableArray;
        public int lastServerUpdate;
    }

    public ReplicatedEntityCollection(GameWorld world)
    {
        m_world = world;
    }
    
    private List<INetworkSerializable> networkSerializables = new List<INetworkSerializable>(16);
    public void Register(int entityId, Entity entity, INetworkSerializable[] serializableArray)
    {
        // Grow to make sure there is room for entity            
        if (entityId >= m_replicatedData.Count)
        {
            var count = entityId - m_replicatedData.Count + 1;
            var emptyData = new ReplicatedData();
            for (var i = 0; i < count; i++)
            {
                m_replicatedData.Add(emptyData);
            }
        }

        GameDebug.Assert(m_replicatedData[entityId].entity == Entity.Null,"ReplicatedData has entity set:{0}", m_replicatedData[entityId].entity);
        
        // Sort serializables by type name so server and client agree on order
        networkSerializables.Clear();
        networkSerializables.AddRange(serializableArray);
        networkSerializables.Sort((p1,p2)=>p1.GetType().Name.CompareTo(p2.GetType().Name));
        
        var data = new ReplicatedData
        {
            entity = entity,
            serializableArray = networkSerializables.ToArray()
        };

        m_replicatedData[entityId] = data;
    }
    
    public Entity Unregister(int entityId)
    {
        var entity = m_replicatedData[entityId].entity;
        GameDebug.Assert(entity != Entity.Null,"Unregister. ReplicatedData has has entity set");

        m_replicatedData[entityId] = new ReplicatedData();
        return entity;
    }

    public void ProcessEntityUpdate(int serverTick, int id, ref NetworkReader reader)
    {
        var data = m_replicatedData[id];
        
        GameDebug.Assert(data.lastServerUpdate < serverTick, "Failed to apply snapshot. Wrong tick order. entityId:{0} snapshot tick:{1} last server tick:{2}", id, serverTick, data.lastServerUpdate);
        data.lastServerUpdate = serverTick;

        GameDebug.Assert(data.serializableArray != null, "Failed to apply snapshot. Serializablearray is null");

        foreach (var entry in data.serializableArray)
            entry.Deserialize(ref reader, this, serverTick);

        m_replicatedData[id] = data;
    }

    public void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer)
    {
        var data = m_replicatedData[entityId];

        GameDebug.Assert(data.serializableArray != null, "Failed to generate snapshot. Serializablearray is null");
        
        foreach (var entry in data.serializableArray)
            entry.Serialize(ref writer, this);
    }
    
    public string GenerateName(int entityId)
    {
        var data = m_replicatedData[entityId];
        
        bool first = true;
        string name = "";
        foreach (var entry in data.serializableArray)
        {
            if (!first)
                name += "_";
            if (entry is Component)
                name += (entry as Component).GetType();
            else
                name += "?";
            first = false;
        }
        return name;
    }
    
    public void SerializeReference(ref NetworkWriter writer, string name, Entity entity)
    {
        if (entity == Entity.Null || !m_world.GetEntityManager().Exists(entity))
        {
            writer.WriteInt32(name, -1);
            return;
        }

        if (m_world.GetEntityManager().HasComponent<ReplicatedEntity>(entity))
        {
            var replicatedEntity = m_world.GetEntityManager().GetComponentObject<ReplicatedEntity>(entity);
            writer.WriteInt32(name, replicatedEntity.id);
            return;
        }

        if (m_world.GetEntityManager().HasComponent<ReplicatedDataEntity>(entity))
        {
            var replicatedDataEntity = m_world.GetEntityManager().GetComponentData<ReplicatedDataEntity>(entity);
            writer.WriteInt32(name, replicatedDataEntity.id);
            return;
        }

        GameDebug.LogError("Failed to serialize reference named:" + name + " to entity:" + entity);
    }

    public void DeserializeReference(ref NetworkReader reader, ref Entity entity)
    {
        var replicatedId = reader.ReadInt32();
        if (replicatedId < 0)
        {
            entity = Entity.Null;
            return;
        }

        entity = m_replicatedData[replicatedId].entity;    
    }

    
    List<ReplicatedData> m_replicatedData = new List<ReplicatedData>(512);
    private readonly GameWorld m_world;
}
