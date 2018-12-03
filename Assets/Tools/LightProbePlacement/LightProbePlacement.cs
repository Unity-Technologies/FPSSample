using System;
using UnityEngine;

[RequireComponent(typeof(LightProbeGroup))]
public class LightProbePlacement : MonoBehaviour
{
    [NonSerialized] public bool placementEnabled;
    public float placementHeight = 0.1f;
}
