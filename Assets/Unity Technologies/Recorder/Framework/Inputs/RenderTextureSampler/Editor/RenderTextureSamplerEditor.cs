using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder.Input
{
    [CustomEditor(typeof(RenderTextureSamplerSettings))]
    public class RenderTextureSamplerEditor : InputEditor
    {
        static EImageSource m_SupportedSources = EImageSource.ActiveCameras | EImageSource.MainCamera | EImageSource.TaggedCamera;
        string[] m_MaskedSourceNames;
        SerializedProperty m_Source;
        SerializedProperty m_RenderSize;
        SerializedProperty m_FinalSize;
        SerializedProperty m_AspectRatio;
        SerializedProperty m_SuperSampling;
        SerializedProperty m_CameraTag;
        SerializedProperty m_FlipFinalOutput;
        ResolutionSelector m_ResSelector;

        protected void OnEnable()
        {
            if (target == null)
                return;

            var pf = new PropertyFinder<RenderTextureSamplerSettings>(serializedObject);
            m_Source = pf.Find(w => w.source);
            m_RenderSize = pf.Find(w => w.m_RenderSize);
            m_AspectRatio = pf.Find(w => w.m_AspectRatio);
            m_SuperSampling = pf.Find(w => w.m_SuperSampling);
            m_FinalSize = pf.Find(w => w.m_OutputSize);
            m_CameraTag = pf.Find(w => w.m_CameraTag);
            m_FlipFinalOutput = pf.Find( w => w.m_FlipFinalOutput );
            m_ResSelector = new ResolutionSelector();
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
                    index = EditorGUILayout.Popup("Object(s) of interest", index, m_MaskedSourceNames);

                    if (check.changed)
                        m_Source.intValue = EnumHelper.GetEnumValueFromMaskedIndex<EImageSource>(index, (int)m_SupportedSources);
                }
            });
            
            var inputType = (EImageSource)m_Source.intValue;

            if ((EImageSource)m_Source.intValue == EImageSource.TaggedCamera)
            {
                ++EditorGUI.indentLevel;
                AddProperty(m_CameraTag, () => EditorGUILayout.PropertyField(m_CameraTag, new GUIContent("Tag")));
                --EditorGUI.indentLevel;
            }

            AddProperty(m_AspectRatio, () => EditorGUILayout.PropertyField(m_AspectRatio, new GUIContent("Aspect Ratio")));
            AddProperty(m_SuperSampling, () => EditorGUILayout.PropertyField(m_SuperSampling, new GUIContent("Super sampling")));

            var renderSize = m_RenderSize;
            AddProperty(m_RenderSize, () =>
            {
                
                if (inputType != EImageSource.RenderTexture)
                {
                    EditorGUILayout.PropertyField(m_RenderSize, new GUIContent("Rendering resolution"));
                    if (m_FinalSize.intValue > renderSize.intValue)
                        m_FinalSize.intValue = renderSize.intValue;
                }
            });

            AddProperty(m_FinalSize, () =>
            {
                m_ResSelector.OnInspectorGUI( (target as ImageInputSettings).maxSupportedSize, m_FinalSize );
                if (m_FinalSize.intValue == (int)EImageDimension.Window)
                    m_FinalSize.intValue = (int)EImageDimension.x720p_HD;
                if (m_FinalSize.intValue > renderSize.intValue)
                    renderSize.intValue = m_FinalSize.intValue;
            });

            EditorGUILayout.PropertyField(m_FlipFinalOutput, new GUIContent("Flip image vertically"));
            if (Verbose.enabled)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Color Space", (target as RenderTextureSamplerSettings).m_ColorSpace.ToString());
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}
