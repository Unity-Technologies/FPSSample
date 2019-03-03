using UnityEngine;
using UnityEngine.Experimental.VFX;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "HitscanEffectTypeDefinition", menuName = "FPS Sample/Effect/HitscanEffectTypeDefinition")]
public class HitscanEffectTypeDefinition : ScriptableObject
{
    public VisualEffectAsset effect;
}



