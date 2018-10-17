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

[DisallowMultipleComponent]
public class HitCollisionOwner : MonoBehaviour 
{
    [EnumBitField(typeof(HitCollisionFlags))] public int colliderFlags;
    [NonSerialized] public bool collisionEnabled = true;
    [NonSerialized] public readonly List<DamageEvent> damageEvents = new List<DamageEvent>(16);
}
