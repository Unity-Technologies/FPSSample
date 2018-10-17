using System;
using UnityEngine;
using Unity.Entities;

public class Grenade : MonoBehaviour
{
    public bool active = true;
    public float maxLifetime = 4;               
    public SplashDamageSettings splashDamage;
    public float proximityTriggerDist = 1.0f;
    public float gravity = 0.3f;
    public float bounciness = 0.9f;
    public float collisionRadius = 0.2f;
    
    [NonSerialized] public int rayQueryId = -1;
    [NonSerialized] public Vector3 position;
    [NonSerialized] public Vector3 velocity;
    [NonSerialized] public Entity owner;
    [NonSerialized] public int teamId;
    [NonSerialized] public int startTick;
    [NonSerialized] public int explodeTick;
}
