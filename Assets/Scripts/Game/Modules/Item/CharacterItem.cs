using System;
using UnityEngine;
using Unity.Entities;

public class CharacterItem : MonoBehaviour, INetworkSerializable
{
    public GameObject geomery;
    
    [NonSerialized] public Entity character;
    [NonSerialized] public bool visible;
    
    public void SetVisible(bool visible)
    {
        this.visible = visible;
        if(geomery != null && geomery.activeSelf != visible)  
            geomery.SetActive(visible);
    }
    
    public void Serialize(ref NetworkWriter writer, IEntityReferenceSerializer refSerializer)
    {
        refSerializer.SerializeReference(ref writer, "character", character);
    }

    public void Deserialize(ref NetworkReader reader, IEntityReferenceSerializer refSerializer, int tick)
    {
        refSerializer.DeserializeReference(ref reader, ref character);
    }
}
