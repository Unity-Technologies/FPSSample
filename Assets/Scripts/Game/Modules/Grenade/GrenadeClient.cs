using System;
using Unity.Entities;
using UnityEngine;

public class GrenadeClient : MonoBehaviour
{
    public GameObject geometry;
    public SpatialEffectTypeDefinition explodeEffect;
    public SoundDef bounceSound;
    

    [NonSerialized] public bool exploded;
    [NonSerialized] public int bounceTick;
}
