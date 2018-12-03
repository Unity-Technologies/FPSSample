using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Projectile/ProjectileRegistry", fileName = "ProjectileRegistry")]
public class ProjectileRegistry : ScriptableObjectRegistry<ProjectileTypeDefinition>
{

#if UNITY_EDITOR

    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        if (serverBuild)
            return;
        
        foreach (var entry in entries)
        {
            if (entry.clientProjectilePrefab != null)
                guids.Add(entry.clientProjectilePrefab.guid);
        }
    }
#endif

}
