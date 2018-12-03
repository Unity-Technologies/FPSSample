using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "FPS Sample/Hero/HeroTypeRegistry", fileName = "HeroTypeRegistry")]
public class HeroTypeRegistry : ScriptableObjectRegistry<HeroTypeAsset>
{
#if UNITY_EDITOR    
    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
    }
#endif
}

