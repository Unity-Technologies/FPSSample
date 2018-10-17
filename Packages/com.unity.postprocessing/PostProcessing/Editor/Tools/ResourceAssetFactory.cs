using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    static class ResourceAssetFactory
    {
#if POSTFX_DEBUG_MENUS
        [MenuItem("Tools/Post-processing/Create Resources Asset")]
#endif
        static void CreateAsset()
        {
            var asset = ScriptableObject.CreateInstance<PostProcessResources>();
            AssetDatabase.CreateAsset(asset, "Assets/PostProcessResources.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
