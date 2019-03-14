using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Flags]
public enum HitCollisionFlags
{
    TeamA = 1 << 0,
    TeamB = 1 << 1,
}


[Serializable]
public struct HitCollisionOwnerData : IComponentData
{
    [EnumBitField(typeof(HitCollisionFlags))] 
    public uint colliderFlags;

    public int collisionEnabled;
}


[DisallowMultipleComponent]
public class HitCollisionOwner : ComponentDataProxy<HitCollisionOwnerData>
{
    private void OnEnable()
    {
        // Make sure damage event buffer is created
        // TODO (mogensh) create DamageEvent buffer using monobehavior wrapper (when it is available) 
        var goe = GetComponent<GameObjectEntity>();
        if (goe != null && goe.EntityManager != null)
        {
            goe.EntityManager.AddBuffer<DamageEvent>(goe.Entity);
        }
        
    }
}
