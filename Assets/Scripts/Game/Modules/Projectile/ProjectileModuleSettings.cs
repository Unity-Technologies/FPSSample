using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileModuleSettings", menuName = "SampleGame/Projectile/ProjectileSystemSettings")]
public class ProjectileModuleSettings : ScriptableObject
{
//    public WeakAssetReference projectilePrefab;
    public ReplicatedEntityFactor projectileFactory;
}