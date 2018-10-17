using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    public static class VolumeFactory
    {
        [MenuItem("GameObject/3D Object/Post-process Volume")]
        static void CreateVolume()
        {
            var gameObject = new GameObject("Post-process Volume");
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            collider.isTrigger = true;
            gameObject.AddComponent<PostProcessVolume>();

            Selection.objects = new [] { gameObject };
            EditorApplication.ExecuteMenuItem("GameObject/Move To View");
        }
    }
}
