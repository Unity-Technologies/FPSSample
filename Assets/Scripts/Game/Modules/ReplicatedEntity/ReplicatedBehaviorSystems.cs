

using Unity.Entities;

[DisableAutoCreation]
public class PredictedComponentDataBehaviorRollback<T,S> : ComponentSystem 
    where T : PredictedComponentDataBehavior<S>
    where S : struct, IPredictedData<S>, IComponentData
{
    public struct GroupType
    {
        public ComponentDataArray<ServerEntity> serverBehaviors;
        public ComponentArray<T> behaviors;
    }

    [Inject]
    public GroupType Group;

    public PredictedComponentDataBehaviorRollback(GameWorld gameWorld) { m_world = gameWorld; }

    protected override void OnUpdate()
    {
        for (var i = 0; i < Group.behaviors.Length; i++)
        {
            Group.behaviors[i].Rollback();
        }
    }
    
    readonly GameWorld m_world;
}

[DisableAutoCreation]
public class PredictedStructBehaviorRollback<T,S> : ComponentSystem 
    where T : PredictedStructBehavior<S>
    where S : struct, IPredictedData<S>
{
    public struct GroupType
    {
        public ComponentDataArray<ServerEntity> serverBehaviors;
        public ComponentArray<T> behaviors;
    }

    [Inject]
    public GroupType Group;

    public PredictedStructBehaviorRollback(GameWorld gameWorld) { m_world = gameWorld; }

    protected override void OnUpdate()
    {
        for (var i = 0; i < Group.behaviors.Length; i++)
        {
            Group.behaviors[i].Rollback();
        }
    }
    
    readonly GameWorld m_world;
}


[DisableAutoCreation]
public class InterpolatedStructBehaviorInterpolate<T,S> : ComponentSystem  
    where T : InterpolatedStructBehavior<S>
    where S : struct, IInterpolatedData<S> 
{
    public struct GroupType
    {
        public EntityArray entities;
        public ComponentArray<T> behaviors;
    }

    [Inject]
    public GroupType Group;

    public InterpolatedStructBehaviorInterpolate(GameWorld gameWorld) { m_world = gameWorld; }

    protected override void OnUpdate()
    {
        var time = m_world.worldTime;
        for (var i = 0; i < Group.behaviors.Length; i++)
        {
            if(EntityManager.HasComponent<ServerEntity>(Group.entities[i]))    // TODO use substractive component
                continue;
            Group.behaviors[i].Interpolate(time);
        }
    }
    
    readonly GameWorld m_world;
}


[DisableAutoCreation]
public class InterpolatedComponentDataBehaviorInterpolate<T,S> : ComponentSystem  
    where T : InterpolatedComponentDataBehavior<S>
    where S : struct, IInterpolatedData<S>, IComponentData
{
    public struct GroupType
    {
        public EntityArray entities;
        public ComponentArray<T> behaviors;
    }

    [Inject]
    public GroupType Group;

    public InterpolatedComponentDataBehaviorInterpolate(GameWorld gameWorld) { m_world = gameWorld; }

    protected override void OnUpdate()
    {
        var time = m_world.worldTime;
        for (var i = 0; i < Group.behaviors.Length; i++)
        {
            if(EntityManager.HasComponent<ServerEntity>(Group.entities[i]))      // TODO use subtractive component
                continue;
            Group.behaviors[i].Interpolate(time);
        }
    }
    
    readonly GameWorld m_world;
}
