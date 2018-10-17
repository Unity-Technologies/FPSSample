using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(Bloom))]
    public sealed class BloomEditor : PostProcessEffectEditor<Bloom>
    {
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_Threshold;
        SerializedParameterOverride m_SoftKnee;
        SerializedParameterOverride m_Clamp;
        SerializedParameterOverride m_Diffusion;
        SerializedParameterOverride m_AnamorphicRatio;
        SerializedParameterOverride m_Color;
        SerializedParameterOverride m_FastMode;

        SerializedParameterOverride m_DirtTexture;
        SerializedParameterOverride m_DirtIntensity;

        public override void OnEnable()
        {
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_Threshold = FindParameterOverride(x => x.threshold);
            m_SoftKnee = FindParameterOverride(x => x.softKnee);
            m_Clamp = FindParameterOverride(x => x.clamp);
            m_Diffusion = FindParameterOverride(x => x.diffusion);
            m_AnamorphicRatio = FindParameterOverride(x => x.anamorphicRatio);
            m_Color = FindParameterOverride(x => x.color);
            m_FastMode = FindParameterOverride(x => x.fastMode);

            m_DirtTexture = FindParameterOverride(x => x.dirtTexture);
            m_DirtIntensity = FindParameterOverride(x => x.dirtIntensity);
        }

        public override void OnInspectorGUI()
        {
            EditorUtilities.DrawHeaderLabel("Bloom");

            PropertyField(m_Intensity);
            PropertyField(m_Threshold);
            PropertyField(m_SoftKnee);
            PropertyField(m_Clamp);
            PropertyField(m_Diffusion);
            PropertyField(m_AnamorphicRatio);
            PropertyField(m_Color);
            PropertyField(m_FastMode);

            if (m_FastMode.overrideState.boolValue && !m_FastMode.value.boolValue && EditorUtilities.isTargetingConsolesOrMobiles)
                EditorGUILayout.HelpBox("For performance reasons it is recommended to use Fast Mode on mobile and console platforms.", MessageType.Warning);

            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Dirtiness");

            PropertyField(m_DirtTexture);
            PropertyField(m_DirtIntensity);

            if (RuntimeUtilities.isVREnabled)
            {
                if ((m_DirtIntensity.overrideState.boolValue && m_DirtIntensity.value.floatValue > 0f)
                 || (m_DirtTexture.overrideState.boolValue && m_DirtTexture.value.objectReferenceValue != null))
                    EditorGUILayout.HelpBox("Using a dirt texture in VR is not recommended.", MessageType.Warning);
            }
        }
    }
}
