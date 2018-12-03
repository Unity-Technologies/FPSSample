using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "SpatialEffectTypeDefinition", menuName = "FPS Sample/Effect/SpatialEffectTypeDefinition")]
public class SpatialEffectTypeDefinition : ScriptableObjectRegistryEntry
{
    public WeakAssetReference prefab;
    public int poolSize = 16;
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpatialEffectTypeDefinition))]
public class SpatialEffectTypeDefinitionEditor : ScriptableObjectRegistryEntryEditor<SpatialEffectRegistry, SpatialEffectTypeDefinition>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif
