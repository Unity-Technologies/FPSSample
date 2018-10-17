using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder.Input
{
    [CustomEditor(typeof(RenderTextureInputSettings))]
    public class RenderTextureInputSettingsEditor : InputEditor
    {
        SerializedProperty m_SourceRTxtr;

        protected void OnEnable()
        {
            if (target == null)
                return;

            var pf = new PropertyFinder<RenderTextureInputSettings>(serializedObject);
            m_SourceRTxtr = pf.Find(w => w.m_SourceRTxtr);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_SourceRTxtr, new GUIContent("Source"));
            using (new EditorGUI.DisabledScope(true))
            {
                var res = "N/A";
                if (m_SourceRTxtr.objectReferenceValue != null)
                {
                    var renderTexture = (RenderTexture)m_SourceRTxtr.objectReferenceValue;
                    res = string.Format("{0} , {1}", renderTexture.width, renderTexture.height);
                }
                EditorGUILayout.TextField("Resolution", res);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}