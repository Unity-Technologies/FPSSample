using System;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

namespace UTJ.FrameCapturer.Recorders
{
    [CustomEditor(typeof(MP4RecorderSettings))]
    public class Mp4RecorderSettingsEditor : RecorderEditorBase
    {
        SerializedProperty m_VideoBitRateMode;
        SerializedProperty m_VideoBitRate;
        SerializedProperty m_VideoMaxTasks;
        SerializedProperty m_AutoSelectBR;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;
            
            var pf = new PropertyFinder<MP4RecorderSettings>(serializedObject);
            var encoding = pf.Find(w => w.m_MP4EncoderSettings);
            var settings = target as MP4RecorderSettings;
            m_VideoBitRateMode = encoding.FindPropertyRelative(() => settings.m_MP4EncoderSettings.videoBitrateMode);
            m_VideoBitRate = encoding.FindPropertyRelative(() => settings.m_MP4EncoderSettings.videoTargetBitrate);            
            m_VideoMaxTasks = encoding.FindPropertyRelative(() => settings.m_MP4EncoderSettings.videoMaxTasks);
            m_AutoSelectBR = pf.Find(w => w.m_AutoSelectBR);
        }

        protected override void OnEncodingGui()
        {
            EditorGUILayout.PropertyField( m_VideoBitRateMode, new GUIContent("Bitrate mode"), true);            
            EditorGUILayout.PropertyField( m_AutoSelectBR, new GUIContent("Autoselect bitrate"), true);
            using (new EditorGUI.DisabledScope(m_AutoSelectBR.boolValue))
                EditorGUILayout.PropertyField(m_VideoBitRate, new GUIContent("Bitrate (bps)"), true);            
            EditorGUILayout.PropertyField( m_VideoMaxTasks, new GUIContent("Max tasks"), true);    
        }

        protected override EFieldDisplayState GetFieldDisplayState( SerializedProperty property)
        {
            if( property.name == "m_CaptureEveryNthFrame" || property.name == "m_AllowTransparency" )
                return EFieldDisplayState.Hidden;
            return base.GetFieldDisplayState(property);
        }

    }
}
