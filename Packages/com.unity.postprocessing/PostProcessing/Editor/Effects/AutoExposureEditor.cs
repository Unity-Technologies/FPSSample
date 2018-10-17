using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [PostProcessEditor(typeof(AutoExposure))]
    public sealed class AutoExposureEditor : PostProcessEffectEditor<AutoExposure>
    {
        SerializedParameterOverride m_Filtering;
        
        SerializedParameterOverride m_MinLuminance;
        SerializedParameterOverride m_MaxLuminance;
        SerializedParameterOverride m_KeyValue;

        SerializedParameterOverride m_EyeAdaptation;
        SerializedParameterOverride m_SpeedUp;
        SerializedParameterOverride m_SpeedDown;

        public override void OnEnable()
        {
            m_Filtering = FindParameterOverride(x => x.filtering);
            
            m_MinLuminance = FindParameterOverride(x => x.minLuminance);
            m_MaxLuminance = FindParameterOverride(x => x.maxLuminance);
            m_KeyValue = FindParameterOverride(x => x.keyValue);
            
            m_EyeAdaptation = FindParameterOverride(x => x.eyeAdaptation);
            m_SpeedUp = FindParameterOverride(x => x.speedUp);
            m_SpeedDown = FindParameterOverride(x => x.speedDown);
        }

        public override void OnInspectorGUI()
        {
            if (!SystemInfo.supportsComputeShaders)
                EditorGUILayout.HelpBox("Auto exposure requires compute shader support.", MessageType.Warning);

            EditorUtilities.DrawHeaderLabel("Exposure");

            PropertyField(m_Filtering);
            PropertyField(m_MinLuminance);
            PropertyField(m_MaxLuminance);

            // Clamp min/max adaptation values
            float minLum = m_MinLuminance.value.floatValue;
            float maxLum = m_MaxLuminance.value.floatValue;
            m_MinLuminance.value.floatValue = Mathf.Min(minLum, maxLum);
            m_MaxLuminance.value.floatValue = Mathf.Max(minLum, maxLum);

            PropertyField(m_KeyValue);
            
            EditorGUILayout.Space();
            EditorUtilities.DrawHeaderLabel("Adaptation");

            PropertyField(m_EyeAdaptation);

            if (m_EyeAdaptation.value.intValue == (int)EyeAdaptation.Progressive)
            {
                PropertyField(m_SpeedUp);
                PropertyField(m_SpeedDown);
            }
        }
    }
}
