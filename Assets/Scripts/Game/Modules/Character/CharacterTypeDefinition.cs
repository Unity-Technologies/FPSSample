using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "CharacterTypeDefinition", menuName = "FPS Sample/Character/TypeDefinition")]
public class CharacterTypeDefinition : ScriptableObjectRegistryEntry
{
    public WeakAssetReference prefabServer;
    public WeakAssetReference prefabClient;
    public WeakAssetReference prefab1P;
}


#if UNITY_EDITOR
[CustomEditor(typeof(CharacterTypeDefinition))]
public class CharacterTypeDefinitionEditor : ScriptableObjectRegistryEntryEditor<CharacterTypeRegistry, CharacterTypeDefinition>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        DrawDefaultInspector();
    }
}
#endif