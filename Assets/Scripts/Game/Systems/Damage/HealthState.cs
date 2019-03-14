using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[Serializable]
public struct HealthStateData : IComponentData, IReplicatedComponent      
{
    [NonSerialized] public float health;
    [NonSerialized] public float maxHealth;     
    [NonSerialized] public int deathTick;
    [NonSerialized] public Entity killedBy;

    public static IReplicatedComponentSerializerFactory CreateSerializerFactory()
    {
        return new ReplicatedComponentSerializerFactory<HealthStateData>();
    }
    
    public void Serialize(ref SerializeContext context, ref NetworkWriter writer)
    {
        writer.WriteFloat("health", health);
    }

    public void Deserialize(ref SerializeContext context, ref NetworkReader reader)
    {
        health = reader.ReadFloat();
    }

    public void SetMaxHealth(float maxHealth)
    {
        this.maxHealth = maxHealth;
        health = maxHealth;
    }
    
    public void ApplyDamage(ref DamageEvent damageEvent, int tick)
    {
        if (health <= 0)
            return;

        health -= damageEvent.damage;
        if (health <= 0)
        {
            killedBy = damageEvent.instigator;
            deathTick = tick;
            health = 0;
        }
    }
}


public class HealthState : ComponentDataProxy<HealthStateData>
{
    
}
