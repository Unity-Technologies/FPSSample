using UnityEngine.Events;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDRenderPipelineUI, SerializedHDRenderPipelineAsset>;

    class HDRenderPipelineUI : BaseUI<SerializedHDRenderPipelineAsset>
    {
        static readonly GUIContent defaultFrameSettingsContent = CoreEditorUtils.GetContent("Default Frame Settings For");
        static readonly GUIContent renderPipelineResourcesContent = CoreEditorUtils.GetContent("Render Pipeline Resources|Set of resources that need to be loaded when creating stand alone");
        static readonly GUIContent renderPipelineEditorResourcesContent = CoreEditorUtils.GetContent("Render Pipeline Editor Resources|Set of resources that need to be loaded for working in editor");
        static readonly GUIContent diffusionProfileSettingsContent = CoreEditorUtils.GetContent("Diffusion Profile Settings");
        //static readonly GUIContent enableShaderVariantStrippingContent = CoreEditorUtils.GetContent("Enable Shader Variant Stripping");
        static readonly GUIContent enableSRPBatcher = CoreEditorUtils.GetContent("Enable SRP Batcher (experimental)");
        static readonly GUIContent enableVariantStrippingLog = CoreEditorUtils.GetContent("Enable Variant stripping logging");

        internal enum SelectedFrameSettings { Camera, BakedOrCustomReflection, RealtimeReflection };
        internal static SelectedFrameSettings selectedFrameSettings = SelectedFrameSettings.Camera;

        static HDRenderPipelineUI()
        {
            Inspector = CED.Group(
                SectionPrimarySettings,
                CED.space,
                CED.Select(
                    (s, d, o) => s.renderPipelineSettings,
                    (s, d, o) => d.renderPipelineSettings,
                    RenderPipelineSettingsUI.SupportedSettings
                    ),
                FrameSettingsSection,
                CED.Select(
                    (s, d, o) => s.renderPipelineSettings,
                    (s, d, o) => d.renderPipelineSettings,
                    RenderPipelineSettingsUI.Inspector
                    )
            );
        }

#pragma warning disable 618 //CED
        public static readonly CED.IDrawer Inspector;
#pragma warning restore 618

#pragma warning disable 618 //CED
        public static readonly CED.IDrawer SectionPrimarySettings = CED.Action(Drawer_SectionPrimarySettings);
#pragma warning restore 618

#pragma warning disable 618 //CED
        public static readonly CED.IDrawer FrameSettingsSection = CED.Group(
#pragma warning restore 618
            CED.Action((s,d,o) => {
                EditorGUILayout.BeginVertical("box");
                Drawer_TitleDefaultFrameSettings(s, d, o);
                }),
            CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedCamera,
#pragma warning disable 618
                FadeOption.None,
#pragma warning restore 618
                CED.Select(
                    (s, d, o) => s.defaultFrameSettings,
                    (s, d, o) => d.defaultFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedBakedOrCustomReflection,
#pragma warning disable 618
                FadeOption.None,
#pragma warning restore 618
                CED.Select(
                    (s, d, o) => s.defaultCubeReflectionFrameSettings,
                    (s, d, o) => d.defaultBakedOrCustomReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedRealtimeReflection,
#pragma warning disable 618
                FadeOption.None,
#pragma warning restore 618
                CED.Select(
                    (s, d, o) => s.defaultPlanarReflectionFrameSettings,
                    (s, d, o) => d.defaultRealtimeReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Action((s, d, o) => EditorGUILayout.EndVertical())
            );

        public FrameSettingsUI defaultFrameSettings = new FrameSettingsUI();
        public FrameSettingsUI defaultCubeReflectionFrameSettings = new FrameSettingsUI();
        public FrameSettingsUI defaultPlanarReflectionFrameSettings = new FrameSettingsUI();
        public RenderPipelineSettingsUI renderPipelineSettings = new RenderPipelineSettingsUI();
        
        public AnimBool isSectionExpandedCamera { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedBakedOrCustomReflection { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedRealtimeReflection { get { return m_AnimBools[2]; } }

        public HDRenderPipelineUI()
            : base(3)
        {
            isSectionExpandedCamera.value = true;
        }

        public override void Reset(SerializedHDRenderPipelineAsset data, UnityAction repaint)
        {
            renderPipelineSettings.Reset(data.renderPipelineSettings, repaint);
            defaultFrameSettings.Reset(data.defaultFrameSettings, repaint);
            defaultCubeReflectionFrameSettings.Reset(data.defaultBakedOrCustomReflectionFrameSettings, repaint);
            defaultPlanarReflectionFrameSettings.Reset(data.defaultRealtimeReflectionFrameSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            renderPipelineSettings.Update();
            defaultFrameSettings.Update();
            defaultCubeReflectionFrameSettings.Update();
            defaultPlanarReflectionFrameSettings.Update();
            base.Update();
        }

        static public void Init(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            s.isSectionExpandedCamera.value = false;
            s.isSectionExpandedBakedOrCustomReflection.value = false;
            s.isSectionExpandedRealtimeReflection.value = false;
            switch (selectedFrameSettings)
            {
                case SelectedFrameSettings.Camera:
                    s.isSectionExpandedCamera.value = true;
                    break;
                case SelectedFrameSettings.BakedOrCustomReflection:
                    s.isSectionExpandedBakedOrCustomReflection.value = true;
                    break;
                case SelectedFrameSettings.RealtimeReflection:
                    s.isSectionExpandedRealtimeReflection.value = true;
                    break;
            }
        }

        static void Drawer_TitleDefaultFrameSettings(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(defaultFrameSettingsContent, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
            if(EditorGUI.EndChangeCheck())
            {
                Init(s, d, o);
            }
            GUILayout.EndHorizontal();
        }

        static void Drawer_SectionPrimarySettings(HDRenderPipelineUI s, SerializedHDRenderPipelineAsset d, Editor o)
        {
            EditorGUILayout.PropertyField(d.renderPipelineResources, renderPipelineResourcesContent);

            HDRenderPipelineAsset hdrpAsset = d.serializedObject.targetObject as HDRenderPipelineAsset;
            hdrpAsset.renderPipelineEditorResources = EditorGUILayout.ObjectField(renderPipelineEditorResourcesContent, hdrpAsset.renderPipelineEditorResources, typeof(HDRenderPipelineEditorResources), allowSceneObjects: false) as HDRenderPipelineEditorResources;

            EditorGUILayout.PropertyField(d.diffusionProfileSettings, diffusionProfileSettingsContent);
            // EditorGUILayout.PropertyField(d.allowShaderVariantStripping, enableShaderVariantStrippingContent);
            EditorGUILayout.PropertyField(d.enableSRPBatcher, enableSRPBatcher);
            EditorGUILayout.PropertyField(d.enableVariantStrippingLog, enableVariantStrippingLog);
        }
    }
}
