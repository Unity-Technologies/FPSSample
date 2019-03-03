using System;
using UnityEngine;
using Unity.Entities;
using UnityEditor;


[InternalBufferCapacity(16)]
public struct DamageEvent : IBufferElementData
{
    public static void AddEvent(DynamicBuffer<DamageEvent> damageBuffer, Entity instigator, float damage, Vector3 direction, float impulse)
    {
        DamageEvent e;
        e.instigator = instigator;
        e.damage = damage;
        e.direction = direction;
        e.impulse = impulse;
        
        if (damageBuffer.Length == damageBuffer.Capacity)
        {
            // TODO (mogensh) handle buffer full by merging smallest
            GameDebug.LogError("DamageEvent buffer full. Damage skipped");
            return;
        }
        
        damageBuffer.Add(e);
    }

    public Entity instigator;
    public float damage;
    public Vector3 direction;
    public float impulse;       
}
