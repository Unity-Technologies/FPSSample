using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[DisallowMultipleComponent]
[ClientOnlyComponent]
public class TeleporterClient : MonoBehaviour
{
    public SpatialEffectTypeDefinition effect;
    public Transform effectTransform;

    [NonSerialized] public TickEventHandler effectEvent = new TickEventHandler(0.5f);
}


