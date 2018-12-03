using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public interface IPredictedDataHandler
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
    void Rollback();
    
#if UNITY_EDITOR
    Entity GetEntity();
    bool HasServerState(int tick);
    object GetServerState(int tick);
    void StorePredictedState(int sampleIndex, int predictionIndex);
    object GetPredictedState(int sampleIndex, int predictionIndex);
    bool VerifyPrediction(int sampleIndex, int tick);
#endif
}

public interface IInterpolatedDataHandler 
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
    void Interpolate(GameTime time);
}

class SerializedComponentDataHandler<T> : INetSerialized  
    where T : struct, INetSerialized, IComponentData    
{
    protected EntityManager entityManager;
    protected Entity entity;

    public SerializedComponentDataHandler(EntityManager entityManager, Entity entity)
    {
        this.entityManager = entityManager;
        this.entity = entity;
    }

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        var state = entityManager.GetComponentData<T>(entity);
        state.Serialize(ref writer, refSerializer);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        var state = entityManager.GetComponentData<T>(entity);
        state.Deserialize(ref reader, refSerializer, tick);
        entityManager.SetComponentData(entity, state);
    }
}


class NetworkPredictedDataHandler<T> : IPredictedDataHandler  
    where T : struct, INetPredicted<T>, IComponentData    
{
    protected EntityManager entityManager;
    protected Entity entity;
    private T m_lastServerState;

#if UNITY_EDITOR    
    SparseTickBuffer serverStateTicks;
    T[] serverStates;

//    SparseTickBuffer predictedStateTicks;
    T[] predictedStates;
#endif      
    
    public NetworkPredictedDataHandler(EntityManager entityManager, Entity entity)
    {
        this.entityManager = entityManager;
        this.entity = entity;
        
#if UNITY_EDITOR    
        serverStateTicks = new SparseTickBuffer(ReplicatedEntityCollection.HistorySize);
        serverStates = new T[ReplicatedEntityCollection.HistorySize];
        
//        predictedStateTicks = new SparseTickBuffer(ReplicatedEntityCollection.HistorySize);
        predictedStates = new T[ReplicatedEntityCollection.HistorySize*ReplicatedEntityCollection.PredictionSize]; 
#endif 
    }

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        var state = entityManager.GetComponentData<T>(entity);
        state.Serialize(ref writer, refSerializer);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        m_lastServerState.Deserialize(ref reader, refSerializer, tick);
        
#if UNITY_EDITOR
        if (ReplicatedEntityCollection.SampleHistory)
        {
            var index = serverStateTicks.GetIndex((uint)tick);
            if(index == -1)                
                index = serverStateTicks.Register((uint)tick);
            serverStates[index] = m_lastServerState;
        }
#endif          
    }

    public void Rollback()
    {
//        GameDebug.Log("Rollback:" + m_lastServerState); 
        entityManager.SetComponentData(entity, m_lastServerState);
    }
    
#if UNITY_EDITOR

    public Entity GetEntity()
    {
        return entity;
    }

    public object GetServerState(int tick)
    {
        var index = serverStateTicks.GetIndex((uint)tick);
        if (index == -1)
            return null;

        return serverStates[index];
    }

    public bool HasServerState(int tick)
    {
        var index = serverStateTicks.GetIndex((uint)tick);
        return index != -1;
    }

    public void StorePredictedState(int sampleIndex, int predictionIndex)
    {
        if (!ReplicatedEntityCollection.SampleHistory)
            return;

        if (predictionIndex >= ReplicatedEntityCollection.PredictionSize)
            return;

        var index = sampleIndex * ReplicatedEntityCollection.PredictionSize + predictionIndex;

        var state = entityManager.GetComponentData<T>(entity);
        predictedStates[index] = state;
    }

    public object GetPredictedState(int sampleIndex, int predictionIndex)
    {
        if (predictionIndex >= ReplicatedEntityCollection.PredictionSize)
            return null;

        var index = sampleIndex * ReplicatedEntityCollection.PredictionSize + predictionIndex;
        return predictedStates[index];
    }

    public bool VerifyPrediction(int sampleIndex, int tick)
    {
        var serverIndex = serverStateTicks.GetIndex((uint)tick);
        if (serverIndex == -1)
            return true;

        var predictedIndex = sampleIndex * ReplicatedEntityCollection.PredictionSize;
        return serverStates[serverIndex].VerifyPrediction(ref predictedStates[predictedIndex]);
    }

#endif
}


class NetworkInterpolatedDataHandler<T> : IInterpolatedDataHandler  
    where T : struct, INetInterpolated<T>, IComponentData    
{
    protected EntityManager entityManager;
    protected Entity entity;
    TickStateSparseBuffer<T> stateHistory = new TickStateSparseBuffer<T>(32);

    public NetworkInterpolatedDataHandler(EntityManager entityManager, Entity entity)
    {
        this.entityManager = entityManager;
        this.entity = entity;
    }

    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        var state = entityManager.GetComponentData<T>(entity);
        state.Serialize(ref writer, refSerializer);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        var state = new T();
        state.Deserialize(ref reader, refSerializer, tick);
        stateHistory.Add(tick, state);
    }

    public void Interpolate(GameTime interpTime)
    {

        T state = new T();

        if (stateHistory.Count > 0)
        {
            int lowIndex = 0, highIndex = 0;
            float interpVal = 0;
            var interpValid = stateHistory.GetStates(interpTime.tick, interpTime.TickDurationAsFraction, ref lowIndex, ref highIndex, ref interpVal);
            
            if (interpValid)
            {
                var prevState = stateHistory[lowIndex];
                var nextState = stateHistory[highIndex];
                state.Interpolate(ref prevState, ref nextState, interpVal);
            }
            else
            {
                state = stateHistory.Last();
            }
        }
        
        entityManager.SetComponentData(entity, state);
    }
}


public class ReplicatedEntityCollection : IEntityReferenceSerializer 
{
    [ConfigVar(Name = "replicatedentity.showcollectioninfo", DefaultValue = "0", Description = "Show replicated system info")]
    static ConfigVar m_showInfo;

    public static bool SampleHistory;
    
    public static int HistorySize = 128;
    public static int PredictionSize = 32;
    
    public struct ReplicatedData
    {
        public Entity entity;
        public GameObject gameObject;
        public INetSerialized[] serializableArray;
        public IPredictedDataHandler[] predictedArray;
        public IInterpolatedDataHandler[] interpolatedArray;
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

    public ReplicatedEntityCollection(GameWorld world)
    {
        m_world = world;
        
#if UNITY_EDITOR           
        historyCommands = new UserCommand[HistorySize];
        hitstoryTicks = new int[HistorySize];
        hitstoryLastServerTick = new int[HistorySize];
#endif        
    }
    

    
    List<INetSerialized> netSerializables = new List<INetSerialized>(32);
    List<IPredictedDataHandler> netPredicted = new List<IPredictedDataHandler>(32); 
    List<IInterpolatedDataHandler> netInterpolated = new List<IInterpolatedDataHandler>(32);
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
        // Add data handlers for monobehaviors
        // TODO (mogensh) create datahandlers for interpolate and predict. Use generic interface on monobehaviors (like IComponentData)
        if (entityManager.HasComponent<Transform>(entity))
        {
            var go = entityManager.GetComponentObject<Transform>(entity).gameObject;
            netSerializables.AddRange(go.GetComponentsInChildren<INetSerialized>());
            netPredicted.AddRange(go.GetComponentsInChildren<IPredictedDataHandler>());
            netInterpolated.AddRange(go.GetComponentsInChildren<IInterpolatedDataHandler>());

            if (m_showInfo.IntValue > 0)
            {
                GameDebug.Log("  Handle MonoBehavior components");
                GameDebug.Log("    netSerializables:" + netSerializables.Count);
                GameDebug.Log("    netPredicted:" + netPredicted.Count);
                GameDebug.Log("    netInterpolated:" + netInterpolated.Count);
            }
        }

        // Add entity data handlers
        if (m_showInfo.IntValue > 0)
            GameDebug.Log("  Handle ECS data component");
        var componentTypes = entityManager.GetComponentTypes(entity);
        foreach (var componentType in componentTypes)
        {
            var managedType = componentType.GetManagedType();

            if (!typeof(IComponentData).IsAssignableFrom(managedType))
                continue;
            
            if (typeof(INetSerialized).IsAssignableFrom(managedType))
            {
                if (m_showInfo.IntValue > 0)
                    GameDebug.Log("   new SerializedComponentDataHandler for:" + managedType.Name);
                var dataHandlerType = typeof(SerializedComponentDataHandler<>).MakeGenericType(managedType);
                var dataHandler = (INetSerialized)Activator.CreateInstance(dataHandlerType, entityManager, entity);
                netSerializables.Add(dataHandler);
            }
            else if (typeof(IPredictedDataBase).IsAssignableFrom(managedType))
            {
                var interfaceTypes = managedType.GetInterfaces();
                foreach (var it in interfaceTypes)
                {
                    if (it.IsGenericType)
                    {
                        var type = it.GenericTypeArguments[0];
                        if (m_showInfo.IntValue > 0)
                            GameDebug.Log("   new IPredictedDataHandler for:" + it.Name + " arg type:" + type);
                        var dataHandlerType = typeof(NetworkPredictedDataHandler<>).MakeGenericType(type);
                        var dataHandler = (IPredictedDataHandler)Activator.CreateInstance(dataHandlerType, entityManager, entity);
                        netPredicted.Add(dataHandler);
                        break;
                    }
                }
            }
            else if (typeof(IInterpolatedDataBase).IsAssignableFrom(managedType))
            {
                var interfaceTypes = managedType.GetInterfaces();
                foreach (var it in interfaceTypes)
                {
                    if (it.IsGenericType)
                    {
                        var type = it.GenericTypeArguments[0];
                        if (m_showInfo.IntValue > 0)
                            GameDebug.Log("   new IInterpolatedDataHandler for:" + it.Name + " arg type:" + type);
                        var dataHandlerType = typeof(NetworkInterpolatedDataHandler<>).MakeGenericType(type);
                        var dataHandler = (IInterpolatedDataHandler)Activator.CreateInstance(dataHandlerType, entityManager, entity);
                        netInterpolated.Add(dataHandler);
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
            entry.Deserialize(ref reader, this, serverTick);
        
        foreach (var entry in data.predictedArray)
            entry.Deserialize(ref reader, this, serverTick);
        
        foreach (var entry in data.interpolatedArray)
            entry.Deserialize(ref reader, this, serverTick);

        m_replicatedData[id] = data;
    }

    public void GenerateEntitySnapshot(int entityId, ref NetworkWriter writer)
    {
        var data = m_replicatedData[entityId];

        GameDebug.Assert(data.serializableArray != null, "Failed to generate snapshot. Serializablearray is null");
        
        foreach (var entry in data.serializableArray)
            entry.Serialize(ref writer, this);
        
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
        foreach (var entry in data.predictedArray)
            entry.Serialize(ref writer, this);
        writer.ClearFieldSection();
        
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyNotPredicting);
        foreach (var entry in data.interpolatedArray)
            entry.Serialize(ref writer, this);
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
