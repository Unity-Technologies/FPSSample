using Unity.Entities;
using UnityEngine;

public interface IPredictedData<T>
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
#if UNITY_EDITOR    
    bool VerifyPrediction(ref T state);
#endif    
}

public interface IInterpolatedData<T>
{
    void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer);
    void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick);
    void Interpolate(ref T first, ref T last, float t);
}

public interface IPredictedDataHandler: INetworkSerializable
{
    void Rollback();
}

public interface IInterpolatedDataHandler : INetworkSerializable
{
    void Interpolate(GameTime time);
}

public class PredictedStructBehavior<T> : MonoBehaviour, IPredictedDataHandler
    where T : struct, IPredictedData<T>    
{
    public T State;

    private T m_lastServerState;
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
        State.Serialize(ref writer, refSerializer);
        writer.ClearFieldSection();
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        m_lastServerState.Deserialize(ref reader, refSerializer, tick);
        
#if UNITY_EDITOR
        StateHistory.SetState(this, tick, ref m_lastServerState);
#endif
    }

    public void Rollback()
    {
        State = m_lastServerState;
    }
}
                             
public abstract class InterpolatedStructBehavior<T> : MonoBehaviour, IInterpolatedDataHandler  
    where T : struct, IInterpolatedData<T>    
{
    public T State;
    private TickStateSparseBuffer<T> stateHistory = new TickStateSparseBuffer<T>(32);
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        State.Serialize(ref writer, refSerializer);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        var state = new T();
        state.Deserialize(ref reader, refSerializer, tick);
        stateHistory.Add(tick, state);
    }
    
    public void Interpolate(GameTime interpTime)
    {
        int lowIndex = 0, highIndex = 0;
        float interpVal = 0;
        var interpValid = stateHistory.GetStates(interpTime.tick, interpTime.TickDurationAsFraction, ref lowIndex, ref highIndex, ref interpVal);

        if (interpValid)
        {
            var prevState = stateHistory[lowIndex];
            var nextState = stateHistory[highIndex];
            State.Interpolate(ref prevState, ref nextState, interpVal);
        }
        else
        {
            State = stateHistory.Last();
        }
    }
}


[RequireComponent(typeof(GameObjectEntity))]
public class PredictedComponentDataBehavior<T> : MonoBehaviour, IPredictedDataHandler
    where T : struct, IPredictedData<T>, IComponentData    
{
    protected Entity entity;
    protected EntityManager entityManager;
    T m_lastServerState;
    
    private void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        GameDebug.Assert(gameObjectEntity != null,"Missing GameObjectEntity component");
        GameDebug.Assert(gameObjectEntity.Entity != Entity.Null,"GameObjectEntity Entity is null!");
        entity = gameObjectEntity.Entity;
        entityManager = gameObjectEntity.EntityManager;

        var state = new T();
        entityManager.AddComponentData(entity, state);
    }
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);
        var state = entityManager.GetComponentData<T>(entity);
        state.Serialize(ref writer, refSerializer);
        writer.ClearFieldSection();
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        m_lastServerState.Deserialize(ref reader, refSerializer, tick);
        entityManager.SetComponentData<T>(entity, m_lastServerState);
    }

    public void Rollback()
    {
        entityManager.SetComponentData<T>(entity, m_lastServerState);
    }
}

[RequireComponent(typeof(GameObjectEntity))]
public abstract class InterpolatedComponentDataBehavior<T> : MonoBehaviour, IInterpolatedDataHandler  
    where T : struct, IInterpolatedData<T>, IComponentData
{
    Entity entity;
    EntityManager entityManager;
    TickStateSparseBuffer<T> stateHistory = new TickStateSparseBuffer<T>(32);

    private void OnEnable()
    {
        var gameObjectEntity = GetComponent<GameObjectEntity>();
        GameDebug.Assert(gameObjectEntity != null,"Missing GameObjectEntity component");
        GameDebug.Assert(gameObjectEntity.Entity != Entity.Null,"GameObjectEntity Entity is null!");
        entity = gameObjectEntity.Entity;
        entityManager = gameObjectEntity.EntityManager;
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
        int lowIndex = 0, highIndex = 0;
        float interpVal = 0;
        var interpValid = stateHistory.GetStates(interpTime.tick, interpTime.TickDurationAsFraction, ref lowIndex, ref highIndex, ref interpVal);

        T state = new T();
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
        entityManager.SetComponentData(entity, state);
    }
}



class SerializedComponentDataHandler<T> : INetworkSerializable  
    where T : struct, INetworkSerializable, IComponentData    
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

class PredictedEntityHandler<T> : IPredictedDataHandler  
    where T : struct, IPredictedData<T>, IComponentData    
{
    protected EntityManager entityManager;
    protected Entity entity;
    private T m_lastServerState;

    public PredictedEntityHandler(EntityManager entityManager, Entity entity)
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
        m_lastServerState.Deserialize(ref reader, refSerializer, tick);
    }

    public void Rollback()
    {
//        GameDebug.Log("Rollback:" + m_lastServerState); 
        entityManager.SetComponentData(entity, m_lastServerState);
    }
}


class InterpolatedEntityHandler<T> : IInterpolatedDataHandler  
    where T : struct, IInterpolatedData<T>, IComponentData    
{
    protected EntityManager entityManager;
    protected Entity entity;
    TickStateSparseBuffer<T> stateHistory = new TickStateSparseBuffer<T>(32);

    public InterpolatedEntityHandler(EntityManager entityManager, Entity entity)
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
