using System;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(ReplicatedEntity))]
public class ReplicatedAbility : MonoBehaviour, INetworkSerializable
{
    public AbilityUI uiPrefab;   
    
    [NonSerialized] public IPredictedDataHandler[] predictedHandlers;
    [NonSerialized] public IInterpolatedDataHandler[] interpolatedHandlers;
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        if (predictedHandlers != null)
        {
            writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyPredicting);

            for(var i=0;i<predictedHandlers.Length;i++)
                predictedHandlers[i].Serialize(ref writer, refSerializer);
        
            writer.ClearFieldSection();
        }

        if (interpolatedHandlers != null)
        {
            writer.SetFieldSection(NetworkWriter.FieldSectionType.OnlyNotPredicting);
        
            for(var i=0;i<interpolatedHandlers.Length;i++)
                interpolatedHandlers[i].Serialize(ref writer, refSerializer);

            writer.ClearFieldSection();
        }
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        if (predictedHandlers != null)
        {
            for (var i = 0; i < predictedHandlers.Length; i++)
                predictedHandlers[i].Deserialize(ref reader, refSerializer, tick);
        }

        if (interpolatedHandlers != null)
        {
            for (var i = 0; i < interpolatedHandlers.Length; i++)
                interpolatedHandlers[i].Deserialize(ref reader, refSerializer, tick);
        }
    }

    public void Interpolate(GameTime time)
    {
        if (interpolatedHandlers != null)
            for(var i=0;i<interpolatedHandlers.Length;i++)
                interpolatedHandlers[i].Interpolate(time);
    }

    public void Rollback()
    {
        if (predictedHandlers != null)
            for(var i=0;i<predictedHandlers.Length;i++)
                predictedHandlers[i].Rollback();
    }
}



[DisableAutoCreation]
public class ReplicatedAbilityRollback : ComponentSystem 
{
    ComponentGroup Group;

    public ReplicatedAbilityRollback(GameWorld world)
    {}

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(ServerEntity), typeof(ReplicatedAbility));
    }

    protected override void OnUpdate()
    {
        var replicatedAbilites = Group.GetComponentArray<ReplicatedAbility>();
        for (var i = 0; i < replicatedAbilites.Length; i++)
        {
            replicatedAbilites[i].Rollback();
        }
    }
}

[DisableAutoCreation]
public class ReplicatedAbilityInterpolate : ComponentSystem 
{
    ComponentGroup Group;

    public ReplicatedAbilityInterpolate(GameWorld world)
    {
        m_world = world;
    }

    protected override void OnCreateManager(int capacity)
    {
        base.OnCreateManager(capacity);
        Group = GetComponentGroup(typeof(ReplicatedAbility));
    }

    protected override void OnUpdate()
    {
        var entityArray = Group.GetEntityArray();
        var replicatedAbilityArray = Group.GetComponentArray<ReplicatedAbility>();
        for (var i = 0; i < replicatedAbilityArray.Length; i++)
        {
            if (EntityManager.HasComponent<ServerEntity>(entityArray[i]))
                continue;
        
            replicatedAbilityArray[i].Interpolate(m_world.worldTime);
        }
    }

    readonly GameWorld m_world;
}


