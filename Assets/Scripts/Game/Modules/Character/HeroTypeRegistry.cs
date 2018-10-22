using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS Sample/Hero/HeroTypeRegistry", fileName = "HeroTypeRegistry")]
public class HeroTypeRegistry : RegistryBase  
{
    public HeroTypeAsset[] entries;
    
#if UNITY_EDITOR    
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        foreach (var entry in entries)
        {
            foreach (var ability in entry.abilities)
            {
                guids.Add(ability.guid);
            }
        }
    }
#endif
}