using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "PresentationRegistry",
    menuName = "FPS Sample/Presentation/PresentationRegistry")]
public class PresentationRegistry : RegistryBase
{
    [Serializable]
    public class Entry
    {
        public WeakAssetReference ownerAssetGuid;
        public UInt16 platformFlags;  
        public UInt32 type;       
        public UInt16 variation;  
        public WeakAssetReference presentation;
    }
    
    public List<Entry> m_entries = new List<Entry>();

    public bool GetPresentation(WeakAssetReference ownerGuid, out WeakAssetReference presentationGuid)
    {
        foreach (var entry in m_entries)
        {
            if (entry.ownerAssetGuid == ownerGuid)
            {
                presentationGuid = entry.presentation;
                return true;
            }
        }

        presentationGuid = new WeakAssetReference();
        return false;
    }
    
    
#if UNITY_EDITOR

    public override void PrepareForBuild()
    {
        Debug.Log("PresentationRegistry"); 
            
        m_entries.Clear();
        
        var guids = AssetDatabase.FindAssets("t:GameObject");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            var presentation = go.GetComponent<PresentationEntity>();
            if (presentation == null)
                continue;

            if (!presentation.presentationOwner.IsSet())
                continue;
            
            m_entries.Add(new Entry
            {
                ownerAssetGuid = presentation.presentationOwner,
                platformFlags = presentation.platformFlags,
                type = presentation.type,      
                variation = presentation.variation,
                presentation = new WeakAssetReference(guid)
            });
        }
    
    }

    public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
    {
        if (serverBuild)
            return;
        
        foreach (var entry in m_entries)
        {
            if(entry.presentation.IsSet())
                guids.Add(entry.presentation.GetGuidStr());
        }
    }
    
    public virtual bool Verify()
    {
        return true;
    }
    
#endif    
}
