using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "HitscanEffectTypeDefinition", menuName = "FPS Sample/Effect/HitscanEffectTypeDefinition")]
public class HitscanEffectTypeDefinition : ScriptableObjectRegistryEntry
{
    public WeakAssetReference prefab;
    public int poolSize = 16;
}

#if UNITY_EDITOR
[CustomEditor(typeof(HitscanEffectTypeDefinition))]
public class HitscanEffectTypeDefinitionEditor : ScriptableObjectRegistryEntryEditor<HitscanEffectRegistry, HitscanEffectTypeDefinition>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif


