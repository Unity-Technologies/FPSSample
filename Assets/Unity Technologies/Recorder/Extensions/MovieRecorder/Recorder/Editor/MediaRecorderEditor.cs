#if UNITY_2017_3_OR_NEWER

using System;
using System.Collections.Generic;
using UnityEditor.Recorder.Input;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder
{
    [CustomEditor(typeof(MediaRecorderSettings))]
    public class MediaRecorderEditor : RecorderEditor
    {
        SerializedProperty m_OutputFormat;
#if UNITY_2018_1_OR_NEWER
        SerializedProperty m_EncodingBitRateMode;
#endif
        //SerializedProperty m_FlipVertical;
        //RTInputSelector m_RTInputSelector;

        [MenuItem("Window/Recorder/Video")]
        static void ShowRecorderWindow()
        {
            RecorderWindow.ShowAndPreselectCategory("Video");
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (target == null)
                return;

            var pf = new PropertyFinder<MediaRecorderSettings>(serializedObject);
            m_OutputFormat = pf.Find(w => w.m_OutputFormat);
#if UNITY_2018_1_OR_NEWER
            m_EncodingBitRateMode = pf.Find(w => w.m_VideoBitRateMode);
#endif
        }

#if UNITY_2018_1_OR_NEWER
        protected override void OnEncodingGui()
        {
            AddProperty(m_EncodingBitRateMode, () => EditorGUILayout.PropertyField(m_EncodingBitRateMode, new GUIContent("Bitrate Mode")));
        }
#else
        protected override void OnEncodingGroupGui()
        {
            // hiding this group by not calling parent class's implementation.  
        }
#endif

        protected override void OnOutputGui()
        {
            AddProperty(m_OutputFormat, () => EditorGUILayout.PropertyField(m_OutputFormat, new GUIContent("Output format")));

            base.OnOutputGui();
        }

        protected override EFieldDisplayState GetFieldDisplayState(SerializedProperty property)
        {
            if (property.name == "m_FlipVertical" || property.name == "m_CaptureEveryNthFrame" )
                return EFieldDisplayState.Hidden;
            if (property.name == "m_FrameRateMode" )
                return EFieldDisplayState.Disabled;

            if (property.name == "m_AllowTransparency")
            {
                return (target as MediaRecorderSettings).m_OutputFormat == MediaRecorderOutputFormat.MP4 ? EFieldDisplayState.Disabled : EFieldDisplayState.Enabled;
            }

            return base.GetFieldDisplayState(property);
        }


    }
}

#endif
