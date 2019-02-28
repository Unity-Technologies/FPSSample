using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PresentationEntity : MonoBehaviour
{

    
    [AssetType(typeof(ReplicatedEntityFactory))]
    public WeakAssetReference presentationOwner;
    
    [NonSerialized] public Entity ownerEntity;

    public UInt16 platformFlags;    // Project specific
    public UInt32 type;        // Owner type dependent
    public UInt16 variation;   // Variation, replicated on owner
}
