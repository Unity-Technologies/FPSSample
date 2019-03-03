using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(ChromaticAberration))]
    internal sealed class ChromaticAberrationEditor : PostProcessEffectEditor<ChromaticAberration>
    {
        SerializedParameterOverride m_SpectralLut;
        SerializedParameterOverride m_Intensity;
        SerializedParameterOverride m_FastMode;

        public override void OnEnable()
        {
            m_SpectralLut = FindParameterOverride(x => x.spectralLut);
            m_Intensity = FindParameterOverride(x => x.intensity);
            m_FastMode = FindParameterOverride(x => x.fastMode);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PropertyField(m_SpectralLut);
            PropertyField(m_Intensity);
            PropertyField(m_FastMode);

            if (m_FastMode.overrideState.boolValue && !m_FastMode.value.boolValue && EditorUtilities.isTargetingConsolesOrMobiles)
                EditorGUILayout.HelpBox("For performance reasons it is recommended to use Fast Mode on mobile and console platforms.", MessageType.Warning);
        }
    }
}
