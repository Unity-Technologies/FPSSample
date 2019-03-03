using System;
using System.Collections.Generic;
using System.Net;
using Unity.Entities;
using UnityEngine;

public abstract class CharBehaviorFactory : ScriptableObject
{
    public abstract Entity Create(EntityManager entityManager, List<Entity> entities);

    public Entity CreateCharBehavior(EntityManager entityManager)
    {
        var entity = entityManager.CreateEntity();

        entityManager.AddComponentData(entity, new CharBehaviour());
        entityManager.AddComponentData(entity, new AbilityControl());

        return entity;
    }
}

public struct CharBehaviour : IComponentData
{
    public Entity character;
}

public struct AbilityControl : IPredictedComponent<AbilityControl>, IComponentData
{
    public enum State
    {
        Idle,
        RequestActive,
        Active,
        Cooldown,
    }
    
    public State behaviorState;        // State is set by behavior
    public int active;                // set by controller        
    public int requestDeactivate;     // set by controller

    public static IPredictedComponentSerializerFactory CreateSerializerFactory()
    {
        return new PredictedComponentSerializerFactory<AbilityControl>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteByte("state", (byte)behaviorState);
        writer.WriteBoolean("active", active == 1);
        writer.WriteBoolean("requestDeactivate", requestDeactivate == 1);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        behaviorState = (State)reader.ReadByte();
        active = reader.ReadBoolean() ? 1 : 0;
        requestDeactivate = reader.ReadBoolean() ? 1 : 0;
    }

#if UNITY_EDITOR
    public bool VerifyPrediction(ref AbilityControl behaviorControl)
    {
        return behaviorState == behaviorControl.behaviorState
               && active == behaviorControl.active;
    }
#endif    
}
