using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Use this attribute to limit the types allowed on a weak asset reference field
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
 public class AssetTypeAttribute : Attribute
{
    public Type assetType;

    public AssetTypeAttribute(Type t)
    {
        assetType = t;
    }
}

/// <summary>
/// Weak asset reference that does not result in assets getting pulled in. Use has
/// responsibility to find another way to actually get asset loaded
/// </summary>
[System.Serializable]
public class WeakAssetReference
{
    public string guid = "";
    
#if UNITY_EDITOR
    public T LoadAsset<T>() where T : UnityEngine.Object
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif     
}

// This base is here to allow CustomPropertyDrawer to pick it up
[System.Serializable]
public class WeakBase
{
    public string guid = "";         
}

// Derive from this to create a typed weak asset reference
[System.Serializable]
public class Weak<T> : WeakBase
{
}
