using System;
using System.Collections.Generic;
using Unity.Entities;

public interface IReplicatedComponentSerializerFactory
{
    IReplicatedSerializer CreateSerializer(EntityManager entityManager, Entity entity, 
        IEntityReferenceSerializer refSerializer);
}
    
public interface IPredictedComponentSerializerFactory
{
    IPredictedSerializer CreateSerializer(EntityManager entityManager, Entity entity,
        IEntityReferenceSerializer refSerializer);
}
    
public interface IInterpolatedComponentSerializerFactory
{
    IInterpolatedSerializer CreateSerializer(EntityManager entityManager, Entity entity,
        IEntityReferenceSerializer refSerializer);
}
    
class ReplicatedComponentSerializerFactory<T> : IReplicatedComponentSerializerFactory 
    where T : struct, IReplicatedComponent, IComponentData
{
    public IReplicatedSerializer CreateSerializer(EntityManager entityManager, Entity entity, 
        IEntityReferenceSerializer refSerializer)
    {
        return new ReplicatedComponentSerializer<T>(entityManager, entity, refSerializer);
    }
}
    
class PredictedComponentSerializerFactory<T> : IPredictedComponentSerializerFactory  
    where T : struct, IPredictedComponent<T>, IComponentData
{
    public IPredictedSerializer CreateSerializer(EntityManager entityManager, Entity entity,
        IEntityReferenceSerializer refSerializer)
    {
        return new PredictedComponentSerializer<T>(entityManager, entity, refSerializer);
    }
}
    
class InterpolatedComponentSerializerFactory<T> : IInterpolatedComponentSerializerFactory 
    where T : struct, IInterpolatedComponent<T>, IComponentData
{
    public IInterpolatedSerializer CreateSerializer(EntityManager entityManager, Entity entity,
        IEntityReferenceSerializer refSerializer)
    {
        return new InterpolatedComponentSerializer<T>(entityManager, entity, refSerializer);
    }
}



public interface IReplicatedSerializer
{
    void Serialize(ref NetworkWriter writer);
    void Deserialize(ref NetworkReader reader, int tick);
}

public interface IPredictedSerializer
{
    void Serialize(ref NetworkWriter writer);
    void Deserialize(ref NetworkReader reader, int tick);
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

public interface IInterpolatedSerializer 
{
    void Serialize(ref NetworkWriter writer);
    void Deserialize(ref NetworkReader reader, int tick);
    void Interpolate(GameTime time);
}

    class ReplicatedComponentSerializer<T> : IReplicatedSerializer 
        where T : struct, IReplicatedComponent, IComponentData    
    {
         protected SerializeContext context;
        
         public ReplicatedComponentSerializer(EntityManager entityManager, Entity entity, 
             IEntityReferenceSerializer refSerializer)
         {
             context.entityManager = entityManager;
             context.entity = entity;
             context.refSerializer = refSerializer;
         }
         
         public void Serialize(ref NetworkWriter writer)
         {
             var state = context.entityManager.GetComponentData<T>(context.entity);
             state.Serialize(ref context, ref writer);
         }
         
         public void Deserialize(ref NetworkReader reader, int tick)
         {
             var state = context.entityManager.GetComponentData<T>(context.entity);
             context.tick = tick;
             state.Deserialize(ref context, ref reader);
             context.entityManager.SetComponentData(context.entity, state);
         }
    }
    
    class PredictedComponentSerializer<T> : IPredictedSerializer 
        where T : struct, IPredictedComponent<T>, IComponentData    
    {
        SerializeContext context;
        T m_lastServerState;
    
    #if UNITY_EDITOR    
        SparseTickBuffer serverStateTicks;
        T[] serverStates;
    
    //    SparseTickBuffer predictedStateTicks;
        T[] predictedStates;
    #endif      
        
        public PredictedComponentSerializer(EntityManager entityManager, Entity entity,
            IEntityReferenceSerializer refSerializer)
        {
            context.entityManager = entityManager;
            context.entity = entity;
            context.refSerializer = refSerializer;
            
    #if UNITY_EDITOR    
            serverStateTicks = new SparseTickBuffer(ReplicatedEntityCollection.HistorySize);
            serverStates = new T[ReplicatedEntityCollection.HistorySize];
            
    //        predictedStateTicks = new SparseTickBuffer(ReplicatedEntityCollection.HistorySize);
            predictedStates = new T[ReplicatedEntityCollection.HistorySize*ReplicatedEntityCollection.PredictionSize]; 
    #endif 
        }
    
        public void Serialize(ref NetworkWriter writer)
        {
            var state = context.entityManager.GetComponentData<T>(context.entity);
            state.Serialize(ref context, ref writer);
        }
    
        public void Deserialize(ref NetworkReader reader, int tick)
        {
            context.tick = tick;
            m_lastServerState.Deserialize(ref context, ref reader);
            
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
            context.entityManager.SetComponentData(context.entity, m_lastServerState);
        }
        
    #if UNITY_EDITOR
    
        public Entity GetEntity()
        {
            return context.entity;
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
    
            var state = context.entityManager.GetComponentData<T>(context.entity);
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
    
    class InterpolatedComponentSerializer<T> : IInterpolatedSerializer  
        where T : struct, IInterpolatedComponent<T>, IComponentData    
    {
        SerializeContext context;
        TickStateSparseBuffer<T> stateHistory = new TickStateSparseBuffer<T>(32);
    
        public InterpolatedComponentSerializer(EntityManager entityManager, Entity entity,
            IEntityReferenceSerializer refSerializer)
        {
            context.entityManager = entityManager;
            context.entity = entity;
            context.refSerializer = refSerializer;
        }
    
        public void Serialize(ref NetworkWriter writer)
        {
            var state = context.entityManager.GetComponentData<T>(context.entity);
            state.Serialize(ref context, ref writer);
        }
    
        public void Deserialize(ref NetworkReader reader, int tick)
        {
            context.tick = tick;
            var state = new T();
            state.Deserialize(ref context, ref reader);
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
                    state.Interpolate(ref context, ref prevState, ref nextState, interpVal);
                }
                else
                {
                    state = stateHistory.Last();
                }
            }
            
            context.entityManager.SetComponentData(context.entity, state);
        }
    }




public class DataComponentSerializers
{



    

    
    Dictionary<Type,IReplicatedComponentSerializerFactory> m_netSerializerFactories = 
        new Dictionary<Type, IReplicatedComponentSerializerFactory>();
    
    Dictionary<Type,IPredictedComponentSerializerFactory> m_predictedSerializerFactories = 
        new Dictionary<Type, IPredictedComponentSerializerFactory>();
    
    Dictionary<Type,IInterpolatedComponentSerializerFactory> m_interpolatedSerializerFactories = 
        new Dictionary<Type, IInterpolatedComponentSerializerFactory>();

    public DataComponentSerializers()
    {
        CreateSerializerFactories();
    }
    
    public IReplicatedSerializer CreateNetSerializer(Type type, EntityManager manager, Entity entity,
        IEntityReferenceSerializer refSerializer)
    {
        IReplicatedComponentSerializerFactory factory;
        if (m_netSerializerFactories.TryGetValue(type, out factory))
        {
            return factory.CreateSerializer(manager, entity, refSerializer);
        }
        GameDebug.LogError("Failed to find INetSerializer for type:" + type.Name);
        return null;
    }
    
    public IPredictedSerializer CreatePredictedSerializer(Type type, EntityManager manager, Entity entity,
        IEntityReferenceSerializer refSerializer)
    {
        IPredictedComponentSerializerFactory factory;
        if (m_predictedSerializerFactories.TryGetValue(type, out factory))
        {
            return factory.CreateSerializer(manager, entity, refSerializer);
        }
        GameDebug.LogError("Failed to find IPredictedSerializer for type:" + type.Name);
        return null;
    }
    
    public IInterpolatedSerializer CreateInterpolatedSerializer(Type type, EntityManager manager, Entity entity,
        IEntityReferenceSerializer refSerializer)
    {
        IInterpolatedComponentSerializerFactory factory;
        if (m_interpolatedSerializerFactories.TryGetValue(type, out factory))
        {
            return factory.CreateSerializer(manager, entity, refSerializer);
        }
        GameDebug.LogError("Failed to find IInterpolatedSerializer for type:" + type.Name);
        return null;
    }


    void CreateSerializerFactories()
    {
        var componentDataType = typeof(IComponentData);
        var serializedType = typeof(IReplicatedComponent);
        var predictedType = typeof(IPredictedDataBase);
        var interpolatedType = typeof(IInterpolatedDataBase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!componentDataType.IsAssignableFrom(type))
                    continue;

                if (serializedType.IsAssignableFrom(type))
                {
//                    GameDebug.Log("Making serializer factory for type:" + type);

                    var method = type.GetMethod("CreateSerializerFactory");
                    if (method == null)
                    {
                        GameDebug.LogError("Replicated component " + type + " has no CreateSerializerFactory");
                        continue;
                    }

                    if (method.ReturnType != typeof(IReplicatedComponentSerializerFactory))
                    {
                        GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IReplicatedComponentSerializerFactory");
                        continue; 
                    }
                    
                    var result = method.Invoke(null, new object[] { });
                    m_netSerializerFactories.Add(type,(IReplicatedComponentSerializerFactory) result);
                }

                if (predictedType.IsAssignableFrom(type))
                {
//                    GameDebug.Log("Making predicted serializer factory for type:" + type);

                    var method = type.GetMethod("CreateSerializerFactory");
                    if (method == null)
                    {
                        GameDebug.LogError("Predicted component " + type + " has no CreateSerializerFactory");
                        continue;
                    }
                    
                    if (method.ReturnType != typeof(IPredictedComponentSerializerFactory))
                    {
                        GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IPredictedComponentSerializerFactory");
                        continue; 
                    }
                    
                    var result = method.Invoke(null, new object[] { });
                    m_predictedSerializerFactories.Add(type,(IPredictedComponentSerializerFactory) result);
                }

                if (interpolatedType.IsAssignableFrom(type))
                {
//                    GameDebug.Log("Making interpolated serializer factory for type:" + type);

                    var method = type.GetMethod("CreateSerializerFactory");
                    if (method == null)
                    {
                        GameDebug.LogError("Interpolated component " + type + " has no CreateSerializerFactory");
                        continue;
                    }
                    
                    if (method.ReturnType != typeof(IInterpolatedComponentSerializerFactory))
                    {
                        GameDebug.LogError("Replicated component " + type + " CreateSerializerFactory does not have return type IInterpolatedComponentSerializerFactory");
                        continue; 
                    }

                    
                    var result = method.Invoke(null, new object[] { });
                    m_interpolatedSerializerFactories.Add(type,(IInterpolatedComponentSerializerFactory) result);
                }
            }
        }
    }
}
