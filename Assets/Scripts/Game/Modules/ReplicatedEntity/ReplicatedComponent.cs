using Unity.Entities;

public interface IEntityReferenceSerializer    
{
    void SerializeReference(ref NetworkWriter writer, string name, Entity entity);
    void DeserializeReference(ref NetworkReader reader, ref Entity entity);
}

public struct SerializeContext
{
    public EntityManager entityManager;
    public Entity entity;
    public IEntityReferenceSerializer refSerializer;
    public int tick;
}

public interface IPredictedDataBase
{
}

public interface IInterpolatedDataBase
{
}

// Interface for components that are replicated to all clients
public interface IReplicatedComponent 
{
    void Serialize(ref SerializeContext context, ref NetworkWriter writer);
    void Deserialize(ref SerializeContext context, ref NetworkReader reader);
}

// Interface for components that are replicated only to predicting clients
public interface IPredictedComponent<T> : IPredictedDataBase
{
    void Serialize(ref SerializeContext context, ref NetworkWriter writer);
    void Deserialize(ref SerializeContext context, ref NetworkReader reader);
#if UNITY_EDITOR    
    bool VerifyPrediction(ref T state);
#endif    
}

// Interface for components that are replicated to all non-predicting clients
public interface IInterpolatedComponent<T> : IInterpolatedDataBase
{
    void Serialize(ref SerializeContext context, ref NetworkWriter writer);
    void Deserialize(ref SerializeContext context, ref NetworkReader reader);
    void Interpolate(ref SerializeContext context, ref T first, ref T last, float t);
}

