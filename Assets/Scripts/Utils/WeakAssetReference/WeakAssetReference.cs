using System;
using Unity.Mathematics;
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
public struct WeakAssetReference
{
    public int val0;
    public int val1;
    public int val2;
    public int val3;

    public WeakAssetReference(string guid)
    {
        var g = new Guid(guid);
        byte[] gb = g.ToByteArray();
        val0 = BitConverter.ToInt32(gb,0);
        val1 = BitConverter.ToInt32(gb,4);
        val2 = BitConverter.ToInt32(gb,8);
        val3 = BitConverter.ToInt32(gb,12);
    }

    public WeakAssetReference(int val0, int val1, int val2, int val3)
    {
        this.val0 = val0;
        this.val1 = val1;
        this.val2 = val2;
        this.val3 = val3;
    }

    public static bool operator ==(WeakAssetReference x, WeakAssetReference y)
    {
        return x.val0 == y.val0 && x.val1 == y.val1 && x.val2 == y.val2 && x.val3 == y.val3;
    }
    public static bool operator !=(WeakAssetReference x, WeakAssetReference y) 
    {
        return !(x == y);
    }

    public bool IsSet()
    {
        return val0 != 0 || val1 != 0 || val2 != 0 || val3 != 0;
    }
    
    public Guid GetGuid()
    {
        byte[] gb = new byte[16];

        byte[] buf;
        buf = BitConverter.GetBytes(val0);
        Array.Copy(buf, 0, gb, 0, 4);
        buf = BitConverter.GetBytes(val1);
        Array.Copy(buf, 0, gb, 4, 4);
        buf = BitConverter.GetBytes(val2);
        Array.Copy(buf, 0, gb, 8, 4);
        buf = BitConverter.GetBytes(val3);
        Array.Copy(buf, 0, gb, 12, 4);
        
        return new Guid(gb);
    }

    public string GetGuidStr()
    {
        return GetGuid().ToString("N");
    }
    
    
#if UNITY_EDITOR
    public T LoadAsset<T>() where T : UnityEngine.Object
    {
        var path = AssetDatabase.GUIDToAssetPath(GetGuidStr());
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
