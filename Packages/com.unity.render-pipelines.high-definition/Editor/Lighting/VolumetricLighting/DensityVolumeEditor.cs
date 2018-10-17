using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DensityVolume))]
    class DensityVolumeEditor : Editor
    {
        static GUIContent s_AlbedoLabel        = new GUIContent("Single Scattering Albedo", "Hue and saturation control the color of the fog (the wavelength of in-scattered light). Value controls scattering (0 = max absorption & no scattering, 1 = no absorption & max scattering).");
        static GUIContent s_MeanFreePathLabel  = new GUIContent("Mean Free Path", "Controls the density, which determines how far you can seen through the fog. It's the distance in meters at which 50% of background light is lost in the fog (due to absorption and out-scattering).");
        static GUIContent s_VolumeTextureLabel = new GUIContent("Density Mask Texture");
        static GUIContent s_TextureScrollLabel = new GUIContent("Texture Scroll Speed");
        static GUIContent s_TextureTileLabel   = new GUIContent("Texture Tiling Amount");
        static GUIContent s_PositiveFadeLabel  = new GUIContent("Positive Fade", "Controls the [0, 1] distance from the +X/+Y/+Z face at which a linear fade ends. 0 means no fade, 1 means the fade ends at the opposite face.");
        static GUIContent s_NegativeFadeLabel  = new GUIContent("Negative Fade", "Controls the [0, 1] distance from the -X/-Y/-Z face at which a linear fade ends. 0 means no fade, 1 means the fade ends at the opposite face.");
        static GUIContent s_InvertFadeLabel    = new GUIContent("Invert Fade", "Inverts fade values in such a way that (0 -> 1), (0.5 -> 0.5) and (1 -> 0).");

        SerializedProperty densityParams;
        SerializedProperty albedo;
        SerializedProperty meanFreePath;

        SerializedProperty volumeTexture;
        SerializedProperty textureScroll;
        SerializedProperty textureTile;

        SerializedProperty positiveFade;
        SerializedProperty negativeFade;
        SerializedProperty invertFade;

        void OnEnable()
        {
            densityParams = serializedObject.FindProperty("parameters");

            albedo        = densityParams.FindPropertyRelative("albedo");
            meanFreePath  = densityParams.FindPropertyRelative("meanFreePath");

            volumeTexture = densityParams.FindPropertyRelative("volumeMask");
            textureScroll = densityParams.FindPropertyRelative("textureScrollingSpeed");
            textureTile   = densityParams.FindPropertyRelative("textureTiling");

            positiveFade  = densityParams.FindPropertyRelative("positiveFade");
            negativeFade  = densityParams.FindPropertyRelative("negativeFade");
            invertFade    = densityParams.FindPropertyRelative("invertFade");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(albedo, s_AlbedoLabel);
                EditorGUILayout.PropertyField(meanFreePath, s_MeanFreePathLabel);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(positiveFade,  s_PositiveFadeLabel);
                EditorGUILayout.PropertyField(negativeFade,  s_NegativeFadeLabel);
                EditorGUILayout.PropertyField(invertFade,    s_InvertFadeLabel);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(volumeTexture, s_VolumeTextureLabel);
                EditorGUILayout.PropertyField(textureScroll, s_TextureScrollLabel);
                EditorGUILayout.PropertyField(textureTile,   s_TextureTileLabel);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 posFade = new Vector3();
                posFade.x = Mathf.Clamp01(positiveFade.vector3Value.x);
                posFade.y = Mathf.Clamp01(positiveFade.vector3Value.y);
                posFade.z = Mathf.Clamp01(positiveFade.vector3Value.z);

                Vector3 negFade = new Vector3();
                negFade.x = Mathf.Clamp01(negativeFade.vector3Value.x);
                negFade.y = Mathf.Clamp01(negativeFade.vector3Value.y);
                negFade.z = Mathf.Clamp01(negativeFade.vector3Value.z);

                positiveFade.vector3Value = posFade;
                negativeFade.vector3Value = negFade;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
