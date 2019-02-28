using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    public class VFXResources : ScriptableObject
    {
        public static VFXResources defaultResources
        {
            get
            {
                if (s_Instance == null)
                {
                    Initialize();
                }
                return s_Instance;
            }
        }
        private static VFXResources s_Instance;

        private const string defaultFileName = "VFXDefaultResources.asset";
        private static string defaultPath { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/"; } } // Change this to a getter once we handle package mode paths

        private static T SafeLoadAssetAtPath<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            return asset;
        }

        private static void Initialize()
        {

            string[] guids = AssetDatabase.FindAssets("t:VFXResources");


            VFXResources asset = null;

            if (guids.Length > 0)
                asset = AssetDatabase.LoadAssetAtPath<VFXResources>(AssetDatabase.GUIDToAssetPath(guids[0]));

            if (asset == null)
            {
                Debug.LogWarning("Could not find " + defaultFileName + ", creating...");
                VFXResources newAsset = CreateInstance<VFXResources>();

                newAsset.particleTexture = SafeLoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultParticle.tga");
                newAsset.noiseTexture = SafeLoadAssetAtPath<Texture2D>(defaultPath + "Textures/Noise.tga");
                newAsset.vectorField = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/vectorfield.asset");
                newAsset.signedDistanceField = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/SignedDistanceField.asset");
                newAsset.mesh = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");

                newAsset.shader = Shader.Find("Hidden/Default StaticMeshOutput");

                newAsset.animationCurve = new AnimationCurve(new Keyframe[]
                {
                    new Keyframe(0.0f, 0.0f, 0.0f, 0.0f),
                    new Keyframe(0.25f, 0.25f, 0.0f, 0.0f),
                    new Keyframe(1.0f, 0.0f, 0.0f, 0.0f),
                });

                newAsset.gradient = new Gradient();
                newAsset.gradient.colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.gray, 1.0f),
                };
                newAsset.gradient.alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.1f),
                    new GradientAlphaKey(0.8f, 0.8f),
                    new GradientAlphaKey(0.0f, 1.0f),
                };

                AssetDatabase.CreateAsset(newAsset, "Assets/" + defaultFileName);
                asset = SafeLoadAssetAtPath<VFXResources>("Assets/" + defaultFileName);
            }
            s_Instance = asset;
        }

        [Header("Default Resources")]
        public Texture2D particleTexture;
        public Texture2D noiseTexture;
        public Texture3D vectorField;
        public Texture3D signedDistanceField;
        public Mesh mesh;
        public AnimationCurve animationCurve;
        public Gradient gradient;
        public Shader shader;
    }
}
