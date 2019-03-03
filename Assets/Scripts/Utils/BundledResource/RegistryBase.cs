using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class RegistryBase : ScriptableObject      
{
#if UNITY_EDITOR

    public virtual void PrepareForBuild()
    {}
    public abstract void GetSingleAssetGUIDs(List<string> guids, bool serverBuild);
    public virtual bool Verify()
    {
        return true;
    }
#endif
}

