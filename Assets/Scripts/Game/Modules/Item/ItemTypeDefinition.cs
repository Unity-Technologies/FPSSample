using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif



[CreateAssetMenu(fileName = "ItemTypeDefinition", menuName = "FPS Sample/Item/TypeDefinition")]
public class ItemTypeDefinition : ScriptableObject
{
    public WeakAssetReference prefabServer;
    public WeakAssetReference prefabClient;
    public WeakAssetReference prefab1P;
}

