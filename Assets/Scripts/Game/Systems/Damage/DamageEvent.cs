using System;
using UnityEngine;
using Unity.Entities;

[Serializable]
public struct DamageEvent
{
    public DamageEvent(Entity instigator, float damage, Vector3 direction, float impulse)
    {
        this.instigator = instigator;
        this.damage = damage;
        this.direction = direction;
        this.impulse = impulse;
    }

    public Entity instigator;
    public float damage;
    public Vector3 direction;
    public float impulse;       
}
