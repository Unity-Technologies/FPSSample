#if UNITY_2017_3_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder.Input
{
    [CustomEditor(typeof(ScreenCaptureInputSettings))]
    public class ScreenCaptureInputEditor : InputEditor
    {
        SerializedProperty m_RenderSize;
        SerializedProperty m_RenderAspect;
        ResolutionSelector m_ResSelector;

        protected void OnEnable()
        {
            if (target == null)
                return;

            var pf = new PropertyFinder<ScreenCaptureInputSettings>(serializedObject);
            m_RenderSize = pf.Find(w => w.m_OutputSize);
            m_RenderAspect = pf.Find(w => w.m_AspectRatio);

            m_ResSelector = new ResolutionSelector();
        }

        public override void OnInspectorGUI()
        {
            AddProperty(m_RenderSize, () =>
            {
                m_ResSelector.OnInspectorGUI((target as ImageInputSettings).maxSupportedSize, m_RenderSize);
            });

            if (m_RenderSize.intValue > (int)EImageDimension.Window)
            {
                AddProperty(m_RenderAspect, () => EditorGUILayout.PropertyField(m_RenderAspect, new GUIContent("Aspect Ratio")));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif