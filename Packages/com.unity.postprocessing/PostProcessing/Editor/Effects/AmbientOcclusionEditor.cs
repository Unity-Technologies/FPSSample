using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(AmbientOcclusion))]
    public sealed class AmbientOcclusionEditor : PostProcessEffectEditor<AmbientOcclusion>
    {
        SerializedParameterOverride m_Mode;
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Color;
        SerializedParameterOverride m_AmbientOnly;
        SerializedParameterOverride m_ThicknessModifier;
        SerializedParameterOverride m_DirectLightingStrength;
        SerializedParameterOverride m_Quality;
        SerializedParameterOverride m_Radius;

        public override void OnEnable()
        {
            m_Mode = FindParameterOverride(x => x.mode);
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Color = FindParameterOverride(x => x.color);
            m_AmbientOnly = FindParameterOverride(x => x.ambientOnly);
            m_ThicknessModifier = FindParameterOverride(x => x.thicknessModifier);
            m_DirectLightingStrength = FindParameterOverride(x => x.directLightingStrength);
            m_Quality = FindParameterOverride(x => x.quality);
            m_Radius = FindParameterOverride(x => x.radius);
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode);
            int aoMode = m_Mode.value.intValue;

            if (RuntimeUtilities.scriptableRenderPipelineActive && aoMode == (int)AmbientOcclusionMode.ScalableAmbientObscurance)
            {
                EditorGUILayout.HelpBox("Scalable ambient obscurance doesn't work with scriptable render pipelines.", MessageType.Warning);
                return;
            }

#if !UNITY_2017_1_OR_NEWER
            if (aoMode == (int)AmbientOcclusionMode.MultiScaleVolumetricObscurance)
            {
                EditorGUILayout.HelpBox("Multi-scale volumetric obscurance requires Unity 2017.1 or more.", MessageType.Warning);
                return;
            }
#endif

            PropertyField(m_Intensity);

            if (aoMode == (int)AmbientOcclusionMode.ScalableAmbientObscurance)
            {
                PropertyField(m_Radius);
                PropertyField(m_Quality);
            }
            else if (aoMode == (int)AmbientOcclusionMode.MultiScaleVolumetricObscurance)
            {
                if (!SystemInfo.supportsComputeShaders)
                    EditorGUILayout.HelpBox("Multi-scale volumetric obscurance requires compute shader support.", MessageType.Warning);

                PropertyField(m_ThicknessModifier);

                if (RuntimeUtilities.scriptableRenderPipelineActive)
                    PropertyField(m_DirectLightingStrength);
            }

            PropertyField(m_Color);
            PropertyField(m_AmbientOnly);

            if (m_AmbientOnly.overrideState.boolValue && m_AmbientOnly.value.boolValue && !RuntimeUtilities.scriptableRenderPipelineActive)
                EditorGUILayout.HelpBox("Ambient-only only works with cameras rendering in Deferred + HDR", MessageType.Info);
        }
    }
}
