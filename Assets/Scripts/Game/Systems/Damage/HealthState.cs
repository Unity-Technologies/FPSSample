using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class HealthState : MonoBehaviour, INetworkSerializable      
{
    [NonSerialized] public float health = 100;
    [NonSerialized] public float maxHealth = 100;     
    [NonSerialized] public int deathTick;
    [NonSerialized] public Entity killedBy;


    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        writer.WriteFloat("health", health);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
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
