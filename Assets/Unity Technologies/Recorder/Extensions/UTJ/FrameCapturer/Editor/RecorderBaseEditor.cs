using UnityEditor;
using UnityEngine;

namespace UTJ.FrameCapturer
{
    public class RecorderBaseEditor : Editor
    {
        public virtual void CommonConfig()
        {
            var so = serializedObject;
            EditorGUILayout.PropertyField(so.FindProperty("m_outputDir"), true);
            EditorGUILayout.PropertyField(so.FindProperty("m_encoderConfigs"), true);
        }

        public virtual void ResolutionControl()
        {
            var recorder = target as RecorderBase;
            var so = serializedObject;

            EditorGUILayout.PropertyField(so.FindProperty("m_resolution"));
            EditorGUI.indentLevel++;
            if (recorder.resolutionUnit == RecorderBase.ResolutionUnit.Percent)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_resolutionPercent"));
            }
            else if (recorder.resolutionUnit == RecorderBase.ResolutionUnit.Pixels)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_resolutionWidth"));
            }
            EditorGUI.indentLevel--;
        }

        public virtual void FramerateControl()
        {
            var recorder = target as RecorderBase;
            var so = serializedObject;

            EditorGUILayout.PropertyField(so.FindProperty("m_framerateMode"));
            if (recorder.framerateMode == MovieRecorder.FrameRateMode.Constant)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(so.FindProperty("m_targetFramerate"));
                EditorGUILayout.PropertyField(so.FindProperty("m_fixDeltaTime"));
                if (recorder.fixDeltaTime)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(so.FindProperty("m_waitDeltaTime"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
        }

        public virtual void RecordingControl()
        {
            var recorder = target as RecorderBase;
            var so = serializedObject;

            // capture control
            EditorGUILayout.PropertyField(so.FindProperty("m_captureControl"));
            EditorGUI.indentLevel++;
            if (recorder.captureControl == RecorderBase.CaptureControl.FrameRange)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_startFrame"));
                EditorGUILayout.PropertyField(so.FindProperty("m_endFrame"));
            }
            else if (recorder.captureControl == RecorderBase.CaptureControl.TimeRange)
            {
                EditorGUILayout.PropertyField(so.FindProperty("m_startTime"));
                EditorGUILayout.PropertyField(so.FindProperty("m_endTime"));
            }

            if (recorder.captureControl == RecorderBase.CaptureControl.FrameRange ||
                recorder.captureControl == RecorderBase.CaptureControl.TimeRange)
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Play"))
                    {
                        EditorApplication.isPlaying = true;
                    }
                }
                else if (recorder.isRecording)
                {
                    if (GUILayout.Button("Abort"))
                    {
                        recorder.EndRecording();
                    }
                }
            }
            else if (recorder.captureControl == RecorderBase.CaptureControl.Manual)
            {
                EditorGUILayout.Space();
                if (!recorder.isRecording)
                {
                    if (GUILayout.Button("Start Recording"))
                    {
                        if (!EditorApplication.isPlaying)
                        {
                            so.FindProperty("m_recordOnStart").boolValue = true;
                            EditorApplication.isPlaying = true;
                        }
                        else
                        {
                            recorder.BeginRecording();
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("End Recording"))
                    {
                        recorder.EndRecording();
                    }
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}
