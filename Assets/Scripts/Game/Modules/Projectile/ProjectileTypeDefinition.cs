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
public class ProjectileTypeDefinition : ScriptableObject
{
    [HideInInspector]
    public WeakAssetReference guid;  
    
    public ProjectileSettings properties;
        
    // Clientprojectile settings.  
    public int clientProjectileBufferSize = 20;
    public WeakAssetReference clientProjectilePrefab;
    
    
#if UNITY_EDITOR
            
    private void OnValidate()
    {
        UpdateAssetGuid();
    }
    
    public void SetAssetGUID(string guidStr)
    {
        var newRef = new WeakAssetReference(guidStr);
        if (!newRef.Equals(guid))
        {
            guid = newRef;
            EditorUtility.SetDirty(this);
        }
    }

    public void UpdateAssetGuid()
    {
        var path = AssetDatabase.GetAssetPath(this);
        if (path != null && path != "")
        {
            var guidStr = AssetDatabase.AssetPathToGUID(path);
            SetAssetGUID(guidStr);
        }
    }
#endif
}
