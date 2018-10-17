using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// TODO (mogensh) this old name needs to change and so does registry building. Registry entries should get their own
// persistent id when created. Registries should be built when creating bundles. 
public class DynamicEnum : ScriptableObject   
{
    public uint registryId;


    public static ushort[] GetRegistryIdsAsShort(DynamicEnum[] definitions)
    {
        if (definitions == null)
            return null;
        
        var registryIds = new ushort[definitions.Length];
        for(var i=0;i<definitions.Length;i++)
        {
            registryIds[i] = definitions[i] != null ? (ushort)definitions[i].registryId : (ushort)0;
        }

        return registryIds;
    }
    
    
#if UNITY_EDITOR
    public int Id
    {
        get {
            var path = AssetDatabase.GetAssetPath(this);
            var guid = AssetDatabase.AssetPathToGUID(path);
            return guid.GetHashCode();
        }
    }

    public virtual void GetAssetReferences(List<string> guids, bool server)
    {
    }

#endif
}