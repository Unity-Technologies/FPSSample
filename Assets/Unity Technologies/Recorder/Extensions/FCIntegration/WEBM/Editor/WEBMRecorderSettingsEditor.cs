using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

namespace UTJ.FrameCapturer.Recorders
{
    [CustomEditor(typeof(WEBMRecorderSettings))]
    public class WEBMRecorderSettingsEditor : RecorderEditorBase
    {
        SerializedProperty m_VideoEncoder;
        SerializedProperty m_VideoBitRateMode;
        SerializedProperty m_VideoBitRate;
        SerializedProperty m_VideoMaxTasks;
        SerializedProperty m_AutoSelectBR;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            var pf = new PropertyFinder<WEBMRecorderSettings>(serializedObject);
            var encoding = pf.Find(w => w.m_WebmEncoderSettings);
            var settings = target as WEBMRecorderSettings;
            m_VideoBitRateMode = encoding.FindPropertyRelative(() => settings.m_WebmEncoderSettings.videoBitrateMode);
            m_VideoBitRate = encoding.FindPropertyRelative(() => settings.m_WebmEncoderSettings.videoTargetBitrate);            
            m_VideoMaxTasks = encoding.FindPropertyRelative(() => settings.m_WebmEncoderSettings.videoMaxTasks);   
            m_VideoEncoder = encoding.FindPropertyRelative(() => settings.m_WebmEncoderSettings.videoEncoder);   
            m_AutoSelectBR = pf.Find(w => w.m_AutoSelectBR);
        }

        protected override void OnEncodingGui()
        {
            EditorGUILayout.PropertyField( m_VideoEncoder, new GUIContent("Encoder"), true);            
            EditorGUILayout.PropertyField( m_VideoBitRateMode, new GUIContent("Bitrate mode"), true);   
            EditorGUILayout.PropertyField( m_AutoSelectBR, new GUIContent("Autoselect bitrate"), true);
                using (new EditorGUI.DisabledScope(m_AutoSelectBR.boolValue))                        
            EditorGUILayout.PropertyField( m_VideoBitRate, new GUIContent("Bitrate (bps)"), true);            
            EditorGUILayout.PropertyField( m_VideoMaxTasks, new GUIContent("Max tasks"), true);    
        }

        protected override EFieldDisplayState GetFieldDisplayState( SerializedProperty property)
        {
            if( property.name == "m_CaptureEveryNthFrame" )
                return EFieldDisplayState.Hidden;

            if (property.name == "m_AllowTransparency"  )
                return EFieldDisplayState.Hidden;

            return base.GetFieldDisplayState(property);
        }
    }
}
