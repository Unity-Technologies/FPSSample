using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif




public class ReplicatedEntityCollection : IEntityReferenceSerializer 
{
    public struct ReplicatedData
    {
        public Entity entity;
        public GameObject gameObject;
        public IReplicatedSerializer[] serializableArray;
        public IPredictedSerializer[] predictedArray;
        public IInterpolatedSerializer[] interpolatedArray;
        public int lastServerUpdate;
        
#if UNITY_EDITOR     
        
        public bool VerifyPrediction(int sampleIndex, int tick)
        {
            foreach (var predictedDataHandler in predictedArray)
            {
                if (!predictedDataHandler.VerifyPrediction(sampleIndex, tick))
                    return false;
            }
            return true;
        }

        public bool HasState(int tick)
        {
            foreach (var predictedDataHandler in predictedArray)
            {
                if (predictedDataHandler.HasServerState(tick))
                    return true;
            }
            return false;
        }
#endif 
    }

    [ConfigVar(Name = "replicatedentity.showcollectioninfo", DefaultValue = "0", Description = "Show replicated system info")]
    static ConfigVar m_showInfo;

    public static bool SampleHistory;
    
    public static int HistorySize = 128;
    public static int PredictionSize = 32;

    DataComponentSerializers serializers = new DataComponentSerializers();
    
    public ReplicatedEntityCollection(GameWorld world)
    {
        m_world = world;

        
#if UNITY_EDITOR           
        historyCommands = new UserCommand[HistorySize];
        hitstoryTicks = new int[HistorySize];
        hitstoryLastServerTick = new int[HistorySize];
#endif        
    }
    
    List<IReplicatedSerializer> netSerializables = new List<IReplicatedSerializer>(32);
    List<IPredictedSerializer> netPredicted = new List<IPredictedSerializer>(32); 
    List<IInterpolatedSerializer> netInterpolated = new List<IInterpolatedSerializer>(32);
    public void Register(EntityManager entityManager, int entityId, Entity entity)
    {
        if (m_showInfo.IntValue > 0)
        {
            if (entityManager.HasComponent<Transform>(entity))
                GameDebug.Log("RepEntity REGISTER NetID:" + entityId + " Entity:" + entity + " GameObject:" + entityManager.GetComponentObject<Transform>(entity).name);
            else
                GameDebug.Log("RepEntity REGISTER NetID:" + entityId + " Entity:" + entity);
        }
            
        
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

        netSerializables.Clear();
        netPredicted.Clear();
        netInterpolated.Clear();

        var go = entityManager.HasComponent<Transform>(entity)
            ? entityManager.GetComponentObject<Transform>(entity).gameObject
            : null;
        

        FindSerializers(entityManager, entity);
        
        if (entityManager.HasComponent<EntityGroupChildren>(entity))
        {
            var buffer = entityManager.GetBuffer<EntityGroupChildren>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                var childEntity = buffer[i].entity;
                if(m_showInfo.IntValue > 0)
                    GameDebug.Log(" ReplicatedEntityChildren: " + i + " = " + childEntity);
                FindSerializers(entityManager, childEntity);
            }            
        }

        var data = new ReplicatedData
        {
            entity = entity,
            gameObject = go,
            serializableArray = netSerializables.ToArray(),
            predictedArray = netPredicted.ToArray(),
            interpolatedArray = netInterpolated.ToArray(),
        };

        m_replicatedData[entityId] = data;
    }


    void FindSerializers(EntityManager entityManager, Entity entity)
    {
        // Add entity data handlers
        if (m_showInfo.IntValue > 0)
            GameDebug.Log("  FindSerializers");
        var componentTypes = entityManager.GetComponentTypes(entity);

        // Sort to ensure order when serializing components
        var typeArray = componentTypes.ToArray();
        Array.Sort(typeArray, delegate(ComponentType type1, ComponentType type2) {
            return type1.GetManagedType().Name.CompareTo(type2.GetManagedType().Name);
        });

        var serializedComponentType = typeof(IReplicatedComponent);
        var predictedComponentType = typeof(IPredictedDataBase);
        var interpolatedComponentType = typeof(IInterpolatedDataBase);
        
        foreach (var componentType in typeArray)
        {
            var managedType = componentType.GetManagedType();

            if (!typeof(IComponentData).IsAssignableFrom(managedType))
                continue;
            
            if (serializedComponentType.IsAssignableFrom(managedType))
            {
                if (m_showInfo.IntValue > 0)
                    GameDebug.Log("   new SerializedComponentDataHandler for:" + managedType.Name);
                
                var serializer = serializers.CreateNetSerializer(managedType, entityManager, entity, this);
                if(serializer != null)    
                    netSerializables.Add(serializer);
            }
            else if (predictedComponentType.IsAssignableFrom(managedType))
            {
                var interfaceTypes = managedType.GetInterfaces();
                foreach (var it in interfaceTypes)
                {
                    if (it.IsGenericType)
                    {
                        var type = it.GenericTypeArguments[0];
                        if (m_showInfo.IntValue > 0)
                            GameDebug.Log("   new IPredictedDataHandler for:" + it.Name + " arg type:" + type);
                        
                        var serializer = serializers.CreatePredictedSerializer(managedType, entityManager, entity, this);
                        if(serializer != null)    
                            netPredicted.Add(serializer);

                        break;
                    }
                }
            }
            else if (interpolatedComponentType.IsAssignableFrom(managedType))
            {
                var interfaceTypes = managedType.GetInterfaces();
                foreach (var it in interfaceTypes)
                {
                    if (it.IsGenericType)
                    {
                        var type = it.GenericTypeArguments[0];
                        if (m_showInfo.IntValue > 0)
                            GameDebug.Log("   new IInterpolatedDataHandler for:" + it.Name + " arg type:" + type);
                        
                        var serializer = serializers.CreateInterpolatedSerializer(managedType, entityManager, entity, this);
                        if(serializer != null)    
                            netInterpolated.Add(serializer);

                        break;
                    }
                }
            }
        }
        
    }
    
    public Entity Unregister(EntityManager entityManager, int entityId)
    {
        var entity = m_replicatedData[entityId].entity;
        GameDebug.Assert(entity != Entity.Null,"Unregister. ReplicatedData has has entity set");

        if (m_showInfo.IntValue > 0)
        {
            if (entityManager.HasComponent<Transform>(entity))
                GameDebug.Log("RepEntity UNREGISTER NetID:" + entityId + " Entity:" + entity + " GameObject:" + entityManager.GetComponentObject<Transform>(entity).name);
            else
                GameDebug.Log("RepEntity UNREGISTER NetID:" + entityId + " Entity:" + entity);
        }

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
            entry.Deserialize(ref reader, serverTick);
        
        foreach (var entry in data.predictedArray)
            entry.Deserialize(ref reader, serverTick);
        
        foreach (var entry in data.interpolatedArray)
            entry.Deserialize(ref reader, serverTick);

        m_replicatedData[id] = data;
    }

    public void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer)
    {
        var data = m_replicatedData[entityId];

        GameDebug.Assert(data.serializableArray != null, "Failed to generate snapshot. Serializablearray is null");
        
        foreach (var entry in data.serializableArray)
            entry.Serialize(ref writer);
        
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
        foreach (var entry in data.predictedArray)
            entry.Serialize(ref writer);
        writer.ClearFieldSection();
        
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyNotPredicting);
        foreach (var entry in data.interpolatedArray)
            entry.Serialize(ref writer);
        writer.ClearFieldSection();
    }

    public void Rollback()
    {
        for (int i = 0; i < m_replicatedData.Count; i++)
        {
            if (m_replicatedData[i].entity == Entity.Null)
                continue;

            if (m_replicatedData[i].predictedArray == null)
                continue;

            if (!m_world.GetEntityManager().HasComponent<ServerEntity>(m_replicatedData[i].entity))
                continue;

            foreach (var predicted in m_replicatedData[i].predictedArray)
            {
                predicted.Rollback();
            }
        }
    }
    
    public void Interpolate(GameTime time)
    {
        for (int i = 0; i < m_replicatedData.Count; i++)
        {
            if (m_replicatedData[i].entity == Entity.Null)
                continue;
            
            if (m_replicatedData[i].interpolatedArray == null)
                continue;

            if (m_world.GetEntityManager().HasComponent<ServerEntity>(m_replicatedData[i].entity))
                continue;

            foreach (var interpolated in m_replicatedData[i].interpolatedArray)
            {
                interpolated.Interpolate(time);
            }
        }
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
                name += entry.GetType().ToString();
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

        if (m_world.GetEntityManager().HasComponent<ReplicatedEntityData>(entity))
        {
            var replicatedDataEntity = m_world.GetEntityManager().GetComponentData<ReplicatedEntityData>(entity);
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
    
 
    
#if UNITY_EDITOR

    public int GetSampleCount()
    {
        return historyCount;
    }

    public int GetSampleTick(int sampleIndex)
    {
        var i = (historyFirstIndex + sampleIndex) % hitstoryTicks.Length;
        return hitstoryTicks[i];
    }

    public int GetLastServerTick(int sampleIndex)
    {
        var i = (historyFirstIndex + sampleIndex) % hitstoryTicks.Length;
        return hitstoryLastServerTick[i];
    }

    public bool IsPredicted(int entityIndex)
    {
        var netId = GetNetIdFromEntityIndex(entityIndex);
        var replicatedData = m_replicatedData[netId];
        return m_world.GetEntityManager().HasComponent<ServerEntity>(replicatedData.entity);
    }

    public int GetEntityCount()
    {
        int entityCount = 0;
        for (int i = 0; i < m_replicatedData.Count; i++)
        {
            if (m_replicatedData[i].entity == Entity.Null)
                continue;
            entityCount++;
        }
        return entityCount;
    }

    public int GetNetIdFromEntityIndex(int entityIndex)
    {
        int entityCount = 0;
        for (int i = 0; i < m_replicatedData.Count; i++)
        {
            if (m_replicatedData[i].entity == Entity.Null)
                continue;

            if (entityCount == entityIndex)
                return i;
            
            entityCount++;
        }
        return -1;
    }    

    public ReplicatedData GetReplicatedDataForNetId(int netId)
    {
        return m_replicatedData[netId];
    }
    
    public void StorePredictedState(int predictedTick, int finalTick)
    {
        if (!SampleHistory)
            return;

        var predictionIndex = finalTick - predictedTick;
        var sampleIndex = GetSampleIndex();
        
        for (int i = 0; i < m_replicatedData.Count; i++)
        {
            if (m_replicatedData[i].entity == Entity.Null)
                continue;

            if (m_replicatedData[i].predictedArray == null)
                continue;

            if (!m_world.GetEntityManager().HasComponent<ServerEntity>(m_replicatedData[i].entity))
                continue;

            foreach (var predicted in m_replicatedData[i].predictedArray)
            {
                predicted.StorePredictedState(sampleIndex, predictionIndex);
            }
        }
    }

    
    public void FinalizedStateHistory(int tick, int lastServerTick, ref UserCommand command)
    {
        if (!SampleHistory)
            return;

        var sampleIndex = (historyFirstIndex + historyCount) % hitstoryTicks.Length;

        hitstoryTicks[sampleIndex] = tick;
        historyCommands[sampleIndex] = command;
        hitstoryLastServerTick[sampleIndex] = lastServerTick;

        if (historyCount < hitstoryTicks.Length)
            historyCount++;
        else
            historyFirstIndex = (historyFirstIndex + 1) % hitstoryTicks.Length;
    }

    int GetSampleIndex()
    {
        return (historyFirstIndex + historyCount) % hitstoryTicks.Length;
    }

    public int FindSampleIndexForTick(int tick)
    {
        for (int i = 0; i < hitstoryTicks.Length; i++)
        {
            if (hitstoryTicks[i] == tick)
                return i;
        }

        return -1;
    }
    
    
    UserCommand[] historyCommands;
    int[] hitstoryTicks;
    int[] hitstoryLastServerTick;
    int historyFirstIndex;
    int historyCount;

#endif
}
