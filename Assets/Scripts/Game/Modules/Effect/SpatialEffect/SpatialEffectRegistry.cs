using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "FPS Sample/Effect/SpatialEffectRegistry", fileName = "SpatialEffectRegistry")]
public class SpatialEffectRegistry : Registry<SpatialEffectTypeDefinition>
{
}
