using System;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

[Serializable]
public struct ProjectileSettings    
{
    public float velocity;
    public float impactDamage;
    public float impactImpulse;
    public float collisionRadius;
    public SplashDamageSettings splashDamage;
}

[CreateAssetMenu(fileName = "ProjectileTypeDefinition", menuName = "FPS Sample/Projectile/ProjectileTypeDefinition")]
public class ProjectileTypeDefinition : ScriptableObjectRegistryEntry
{
    public ProjectileSettings properties;
        
    // Clientprojectile settings.  
    public int clientProjectileBufferSize = 20;
    public WeakAssetReference clientProjectilePrefab;
}

#if UNITY_EDITOR
[CustomEditor(typeof(ProjectileTypeDefinition))]
public class
    ProjectileTypeDefinitionEditor : ScriptableObjectRegistryEntryEditor<ProjectileRegistry, ProjectileTypeDefinition>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif

