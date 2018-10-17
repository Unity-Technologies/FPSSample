using System;
using Unity.Entities;
using UnityEngine;

public class Character1P : MonoBehaviour
{
    public Entity character;
    public Transform itemAttachBone;   
    
    public GameObject geometry;
    [NonSerialized] public bool isVisible = true;
    
    public Transform cameraTransform;
    
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if(geometry != null && geometry.activeSelf != visible)  
            geometry.SetActive(visible);
    }
}
