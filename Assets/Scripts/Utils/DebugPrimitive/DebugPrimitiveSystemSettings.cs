using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DebugPrimitiveSystemSettings", menuName = "FPS Sample/DebugPrimitive/DebugPrimitiveSystemSettings")]
public class DebugPrimitiveSystemSettings : ScriptableObject
{
    public CapsulePrimitive capsulePrefab;
    public SpherePrimitive spherePrefab;
    public LinePrimitive linePrefab;
}
