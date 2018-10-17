using UnityEngine;
using UnityEngine.Recorder;
using UnityEngine.Recorder.Input;

namespace UnityEditor.Recorder.Input
{
    [CustomEditor(typeof(CBRenderTextureInputSettings))]
    public class CBRenderTextureInputSettingsEditor : InputEditor
    {
        static EImageSource m_SupportedSources = EImageSource.MainCamera | EImageSource.ActiveCameras | EImageSource.TaggedCamera;
        string[] m_MaskedSourceNames;
        ResolutionSelector m_ResSelector;

        SerializedProperty m_Source;
        SerializedProperty m_CameraTag;
        SerializedProperty m_RenderSize;
        SerializedProperty m_RenderAspect;
        SerializedProperty m_FlipFinalOutput;
        SerializedProperty m_Transparency;
        SerializedProperty m_CaptureUI;

        protected void OnEnable()
        {
            if (target == null)
                return;


            var pf = new PropertyFinder<CBRenderTextureInputSettings>(serializedObject);
            m_Source = pf.Find(w => w.source);
            m_CameraTag = pf.Find(w => w.m_CameraTag);

            m_RenderSize = pf.Find(w => w.m_OutputSize);
            m_RenderAspect = pf.Find(w => w.m_AspectRatio);
            m_FlipFinalOutput = pf.Find( w => w.m_FlipFinalOutput );
            m_Transparency = pf.Find(w => w.m_AllowTransparency);
            m_CaptureUI = pf.Find(w => w.m_CaptureUI);

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


            if (inputType != EImageSource.RenderTexture)
            {
                AddProperty(m_RenderSize, () =>
                {
                    m_ResSelector.OnInspectorGUI((target as ImageInputSettings).maxSupportedSize, m_RenderSize);
                });

                if (m_RenderSize.intValue > (int)EImageDimension.Window)
                {
                    AddProperty(m_RenderAspect, () => EditorGUILayout.PropertyField(m_RenderAspect, new GUIContent("Aspect Ratio")));
                }

                if(inputType == EImageSource.ActiveCameras)
                {
                    AddProperty(m_CaptureUI, () => EditorGUILayout.PropertyField(m_CaptureUI, new GUIContent("Capture UI")));
                }
            }

            AddProperty(m_Transparency, () => EditorGUILayout.PropertyField(m_Transparency, new GUIContent("Capture alpha")));

            //if (Verbose.enabled)
            {
                //using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(m_FlipFinalOutput, new GUIContent("Flip image vertically"));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
