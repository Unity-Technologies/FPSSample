using System;
using UnityEditor;
using UnityEngine;

namespace UTJ.FrameCapturer
{
    [CustomEditor(typeof(GBufferRecorder))]
    public class ImageSequenceRecorderEditor : RecorderBaseEditor
    {
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();

            var recorder = target as GBufferRecorder;
            var so = serializedObject;

            CommonConfig();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Capture Components");
            EditorGUI.indentLevel++;
            {
                EditorGUI.BeginChangeCheck();
                var fbc = recorder.fbComponents;

                fbc.frameBuffer = EditorGUILayout.Toggle("Frame Buffer", fbc.frameBuffer);
                if(fbc.frameBuffer)
                {
                    EditorGUI.indentLevel++;
                    fbc.fbColor = EditorGUILayout.Toggle("Color", fbc.fbColor);
                    fbc.fbAlpha = EditorGUILayout.Toggle("Alpha", fbc.fbAlpha);
                    EditorGUI.indentLevel--;
                }

                fbc.GBuffer = EditorGUILayout.Toggle("GBuffer", fbc.GBuffer);
                if (fbc.GBuffer)
                {
                    EditorGUI.indentLevel++;
                    fbc.gbAlbedo    = EditorGUILayout.Toggle("Albedo", fbc.gbAlbedo);
                    fbc.gbOcclusion = EditorGUILayout.Toggle("Occlusion", fbc.gbOcclusion);
                    fbc.gbSpecular  = EditorGUILayout.Toggle("Specular", fbc.gbSpecular);
                    fbc.gbSmoothness= EditorGUILayout.Toggle("Smoothness", fbc.gbSmoothness);
                    fbc.gbNormal    = EditorGUILayout.Toggle("Normal", fbc.gbNormal);
                    fbc.gbEmission  = EditorGUILayout.Toggle("Emission", fbc.gbEmission);
                    fbc.gbDepth     = EditorGUILayout.Toggle("Depth", fbc.gbDepth);
                    fbc.gbVelocity  = EditorGUILayout.Toggle("Velocity", fbc.gbVelocity);
                    EditorGUI.indentLevel--;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    recorder.fbComponents = fbc;
                    EditorUtility.SetDirty(recorder);
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();

            ResolutionControl();
            FramerateControl();

            EditorGUILayout.Space();

            RecordingControl();

            so.ApplyModifiedProperties();
        }

    }
}
