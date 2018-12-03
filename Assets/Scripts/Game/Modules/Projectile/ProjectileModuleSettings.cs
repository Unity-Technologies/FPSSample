using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileModuleSettings", menuName = "FPS Sample/Projectile/ProjectileSystemSettings")]
public class ProjectileModuleSettings : ScriptableObject
{
//    public WeakAssetReference projectilePrefab;
    public ReplicatedEntityFactory projectileFactory;
}