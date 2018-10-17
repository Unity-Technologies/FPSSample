#if UNITY_2018_1_OR_NEWER

using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder.Input
{
    [CustomEditor(typeof(Camera360InputSettings))]
    public class Camera360InputEditor : InputEditor
    {
        static EImageSource m_SupportedSources = EImageSource.MainCamera | EImageSource.TaggedCamera;
        string[] m_MaskedSourceNames;

        SerializedProperty m_Source;
        SerializedProperty m_CameraTag;
        SerializedProperty m_FlipFinalOutput;
        SerializedProperty m_StereoSeparation;
        SerializedProperty m_CubeMapSz;
        SerializedProperty m_OutputWidth;
        SerializedProperty m_OutputHeight;
        SerializedProperty m_RenderStereo;

        protected void OnEnable()
        {
            if (target == null)
                return;

            var pf = new PropertyFinder<Camera360InputSettings>(serializedObject);
            m_Source = pf.Find(w => w.source);
            m_CameraTag = pf.Find(w => w.m_CameraTag);

            m_StereoSeparation = pf.Find(w => w.m_StereoSeparation);
            m_FlipFinalOutput = pf.Find( w => w.m_FlipFinalOutput );
            m_CubeMapSz = pf.Find( w => w.m_MapSize );
            m_OutputWidth = pf.Find(w => w.m_OutputWidth);
            m_OutputHeight = pf.Find(w => w.m_OutputHeight);
            m_RenderStereo = pf.Find(w => w.m_RenderStereo);
        }

        public override void OnInspectorGUI()
        {
            AddProperty(m_Source, () =>
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    if (m_MaskedSourceNames == null)
                        m_MaskedSourceNames = EnumHelper.MaskOutEnumNames<EImageSource>((int)m_SupportedSources);
                    var index = EnumHelper.GetMaskedIndexFromEnumValue<EImageSource>(m_Source.intValue, (int)m_SupportedSources);
                    index = EditorGUILayout.Popup("Source", index, m_MaskedSourceNames);

                    if (check.changed)
                        m_Source.intValue = EnumHelper.GetEnumValueFromMaskedIndex<EImageSource>(index, (int)m_SupportedSources);
                }
            });

            var inputType = (EImageSource)m_Source.intValue;
            if ((EImageSource)m_Source.intValue == EImageSource.TaggedCamera )
            {
                ++EditorGUI.indentLevel;
                AddProperty(m_CameraTag, () => EditorGUILayout.PropertyField(m_CameraTag, new GUIContent("Tag")));
                --EditorGUI.indentLevel;
            }

            AddProperty(m_OutputWidth, () =>
            {
                AddProperty(m_OutputWidth, () => EditorGUILayout.PropertyField(m_OutputWidth, new GUIContent("Output width")));
            });

            AddProperty(m_OutputHeight, () =>
            {
                AddProperty(m_OutputWidth, () => EditorGUILayout.PropertyField(m_OutputHeight, new GUIContent("Output height")));
            });

            AddProperty(m_CubeMapSz, () =>
            {
                AddProperty(m_CubeMapSz, () => EditorGUILayout.PropertyField(m_CubeMapSz, new GUIContent("Cube map width")));
            });

            AddProperty(m_RenderStereo, () =>
            {
                AddProperty(m_RenderStereo, () => EditorGUILayout.PropertyField(m_RenderStereo, new GUIContent("Render in Stereo")));
            });

            AddProperty(m_StereoSeparation, () =>
            {
                ++EditorGUI.indentLevel;
                using (new EditorGUI.DisabledScope(!m_RenderStereo.boolValue))
                {
                    AddProperty(m_StereoSeparation, () => EditorGUILayout.PropertyField(m_StereoSeparation, new GUIContent("Stereo Separation")));
                }
                --EditorGUI.indentLevel;
            });

            if (Verbose.enabled)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(m_FlipFinalOutput, new GUIContent("Flip output"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif