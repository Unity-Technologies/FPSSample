using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{
    [CustomEditor(typeof(PostProcessDebug))]
    sealed class PostProcessDebugEditor : BaseEditor<PostProcessDebug>
    {
        SerializedProperty m_PostProcessLayer;
        SerializedProperty m_LightMeterEnabled;
        SerializedProperty m_HistogramEnabled;
        SerializedProperty m_WaveformEnabled;
        SerializedProperty m_VectorscopeEnabled;
        SerializedProperty m_Overlay;

        SerializedObject m_LayerObject;

        SerializedProperty m_LightMeterShowCurves;
        SerializedProperty m_HistogramChannel;
        SerializedProperty m_WaveformExposure;
        SerializedProperty m_VectorscopeExposure;

        SerializedProperty m_LinearDepth;
        SerializedProperty m_MotionColorIntensity;
        SerializedProperty m_MotionGridSize;
        SerializedProperty m_ColorBlindness;
        SerializedProperty m_ColorBlindnessStrength;

        void OnEnable()
        {
            m_PostProcessLayer = FindProperty(x => x.postProcessLayer);
            m_LightMeterEnabled = FindProperty(x => x.lightMeter);
            m_HistogramEnabled = FindProperty(x => x.histogram);
            m_WaveformEnabled = FindProperty(x => x.waveform);
            m_VectorscopeEnabled = FindProperty(x => x.vectorscope);
            m_Overlay = FindProperty(x => x.debugOverlay);

            if (m_PostProcessLayer.objectReferenceValue != null)
                RebuildProperties();
        }

        void RebuildProperties()
        {
            if (m_PostProcessLayer.objectReferenceValue == null)
                return;

            m_LayerObject = new SerializedObject(m_Target.postProcessLayer);

            m_LightMeterShowCurves = m_LayerObject.FindProperty("debugLayer.lightMeter.showCurves");
            m_HistogramChannel = m_LayerObject.FindProperty("debugLayer.histogram.channel");
            m_WaveformExposure = m_LayerObject.FindProperty("debugLayer.waveform.exposure");
            m_VectorscopeExposure = m_LayerObject.FindProperty("debugLayer.vectorscope.exposure");

            m_LinearDepth = m_LayerObject.FindProperty("debugLayer.overlaySettings.linearDepth");
            m_MotionColorIntensity = m_LayerObject.FindProperty("debugLayer.overlaySettings.motionColorIntensity");
            m_MotionGridSize = m_LayerObject.FindProperty("debugLayer.overlaySettings.motionGridSize");
            m_ColorBlindness = m_LayerObject.FindProperty("debugLayer.overlaySettings.colorBlindnessType");
            m_ColorBlindnessStrength = m_LayerObject.FindProperty("debugLayer.overlaySettings.colorBlindnessStrength");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_PostProcessLayer);
                serializedObject.ApplyModifiedProperties(); // Needed to rebuild properties after a change
                serializedObject.Update();

                if (changed.changed)
                    RebuildProperties();
            }

            if (m_PostProcessLayer.objectReferenceValue != null)
            {
                m_LayerObject.Update();
                
                // Overlays
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(EditorUtilities.GetContent("Overlay"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Overlay);
                DoOverlayGUI(DebugOverlay.Depth, m_LinearDepth);
                DoOverlayGUI(DebugOverlay.MotionVectors, m_MotionColorIntensity, m_MotionGridSize);
                DoOverlayGUI(DebugOverlay.ColorBlindnessSimulation, m_ColorBlindness, m_ColorBlindnessStrength);

                // Special cases
                if (m_Overlay.intValue == (int)DebugOverlay.NANTracker && m_Target.postProcessLayer.stopNaNPropagation)
                    EditorGUILayout.HelpBox("Disable \"Stop NaN Propagation\" in the Post-process layer or NaNs will be overwritten!", MessageType.Warning);

                EditorGUI.indentLevel--;

                // Monitors
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(EditorUtilities.GetContent("Monitors"), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                DoMonitorGUI(EditorUtilities.GetContent("Light Meter"), m_LightMeterEnabled, m_LightMeterShowCurves);
                DoMonitorGUI(EditorUtilities.GetContent("Histogram"), m_HistogramEnabled, m_HistogramChannel);
                DoMonitorGUI(EditorUtilities.GetContent("Waveform"), m_WaveformEnabled, m_WaveformExposure);
                DoMonitorGUI(EditorUtilities.GetContent("Vectoscope"), m_VectorscopeEnabled, m_VectorscopeExposure);
                EditorGUI.indentLevel--;

                m_LayerObject.ApplyModifiedProperties();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DoMonitorGUI(GUIContent content, SerializedProperty prop, params SerializedProperty[] settings)
        {
            EditorGUILayout.PropertyField(prop, content);

            if (settings == null || settings.Length == 0)
                return;

            if (prop.boolValue)
            {
                EditorGUI.indentLevel++;
                foreach (var p in settings)
                    EditorGUILayout.PropertyField(p);
                EditorGUI.indentLevel--;
            }
        }

        void DoOverlayGUI(DebugOverlay overlay, params SerializedProperty[] settings)
        {
            if (m_Overlay.intValue != (int)overlay)
                return;

            if (settings == null || settings.Length == 0)
                return;

            foreach (var p in settings)
                EditorGUILayout.PropertyField(p);
        }
    }
}
