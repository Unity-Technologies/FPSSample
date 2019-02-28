using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "CharacterTypeDefinition", menuName = "FPS Sample/Character/TypeDefinition")]
public class CharacterTypeDefinition : ScriptableObject
{
    public WeakAssetReference prefabServer;
    public WeakAssetReference prefabClient;
    public WeakAssetReference prefab1P;
}
