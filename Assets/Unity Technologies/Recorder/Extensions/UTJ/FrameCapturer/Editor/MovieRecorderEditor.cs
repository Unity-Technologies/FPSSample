using System;
using UnityEditor;
using UnityEngine;

namespace UTJ.FrameCapturer
{
    [CustomEditor(typeof(MovieRecorder))]
    public class MovieRecorderEditor : RecorderBaseEditor
    {
        public virtual void VideoConfig()
        {
            var recorder = target as MovieRecorder;
            var so = serializedObject;

            EditorGUILayout.PropertyField(so.FindProperty("m_captureTarget"));
            if(recorder.captureTarget == MovieRecorder.CaptureTarget.RenderTexture)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty("m_targetRT"));
                EditorGUI.indentLevel--;
            }

            ResolutionControl();
            EditorGUILayout.PropertyField(so.FindProperty("m_captureEveryNthFrame"));
        }


        public virtual void AudioConfig()
        {
        }

        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            var recorder = target as MovieRecorder;
            var so = serializedObject;

            CommonConfig();

            EditorGUILayout.Space();

            if (recorder.supportVideo && !recorder.supportAudio)
            {
                VideoConfig();
            }
            else if (!recorder.supportVideo && recorder.supportAudio)
            {
                AudioConfig();
            }
            else if (recorder.supportVideo && recorder.supportAudio)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_captureVideo"));
                if (recorder.captureVideo)
                {
                    EditorGUI.indentLevel++;
                    VideoConfig();
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(so.FindProperty("m_captureAudio"));
                if (recorder.captureAudio)
                {
                    EditorGUI.indentLevel++;
                    AudioConfig();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
            FramerateControl();
            EditorGUILayout.Space();
            RecordingControl();

            so.ApplyModifiedProperties();
        }
    }
}
