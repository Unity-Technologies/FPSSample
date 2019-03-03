using UnityEditor.AnimatedValues;
using UnityEngine.Events;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<RenderPipelineSettingsUI, SerializedRenderPipelineSettings>;

    class RenderPipelineSettingsUI : BaseUI<SerializedRenderPipelineSettings>
    {
        static RenderPipelineSettingsUI()
        {
            Inspector = CED.Group(
                    CED.Select(
                        (s, d, o) => s.lightLoopSettings,
                        (s, d, o) => d.lightLoopSettings,
                        GlobalLightLoopSettingsUI.Inspector
                        ),
                    CED.Select(
                        (s, d, o) => s.hdShadowInitParams,
                        (s, d, o) => d.hdShadowInitParams,
                        HDShadowInitParametersUI.Inspector
                    ),
                    CED.Select(
                        (s, d, o) => s.decalSettings,
                        (s, d, o) => d.decalSettings,
                        GlobalDecalSettingsUI.Inspector
                        )
                    );
        }

#pragma warning disable 618 //CED
        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SupportedSettings = CED.FoldoutGroup(
#pragma warning restore 618
            "Render Pipeline Supported Features",
            (s, d, o) => s.isSectionExpandedSupportedSettings,
            FoldoutOption.None,
            CED.Action(Drawer_SectionPrimarySettings)
            );

        GlobalLightLoopSettingsUI lightLoopSettings = new GlobalLightLoopSettingsUI();
        GlobalDecalSettingsUI decalSettings = new GlobalDecalSettingsUI();
        HDShadowInitParametersUI hdShadowInitParams = new HDShadowInitParametersUI();


        public AnimBool isSectionExpandedSupportedSettings { get { return m_AnimBools[0]; } }

        public RenderPipelineSettingsUI()
            : base(1)
        {
            isSectionExpandedSupportedSettings.value = true;
        }

        public override void Reset(SerializedRenderPipelineSettings data, UnityAction repaint)
        {
            lightLoopSettings.Reset(data.lightLoopSettings, repaint);
            hdShadowInitParams.Reset(data.hdShadowInitParams, repaint);
            decalSettings.Reset(data.decalSettings, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            lightLoopSettings.Update();
            hdShadowInitParams.Update();
            decalSettings.Update();
            base.Update();
        }

        static void Drawer_SectionPrimarySettings(RenderPipelineSettingsUI s, SerializedRenderPipelineSettings d, Editor o)
        {
            EditorGUILayout.PropertyField(d.supportShadowMask, _.GetContent("Shadow Mask|Enable memory (Extra Gbuffer in deferred) and shader variant for shadow mask."));
            EditorGUILayout.PropertyField(d.supportSSR, _.GetContent("SSR|Enable memory use by SSR effect."));
            EditorGUILayout.PropertyField(d.supportSSAO, _.GetContent("SSAO|Enable memory use by SSAO effect."));

            EditorGUILayout.PropertyField(d.supportSubsurfaceScattering, _.GetContent("Subsurface Scattering"));
            using (new EditorGUI.DisabledScope(!d.supportSubsurfaceScattering.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.increaseSssSampleCount, _.GetContent("High quality |This allows for better SSS quality. Warning: high performance cost, do not enable on consoles."));
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportVolumetrics, _.GetContent("Volumetrics|Enable memory and shader variant for volumetric."));
            using (new EditorGUI.DisabledScope(!d.supportVolumetrics.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.increaseResolutionOfVolumetrics, _.GetContent("High quality |Increase the resolution of volumetric lighting buffers. Warning: high performance cost, do not enable on consoles."));
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportLightLayers, _.GetContent("LightLayers|Enable light layers. In deferred this imply an extra render target in memory and extra cost."));
            
            EditorGUILayout.PropertyField(d.supportedLitShaderMode, _.GetContent("Supported Lit Shader Mode|Remove all the memory and shader variant of GBuffer of non used mode. The renderer cannot be switch to non selected path anymore."));

            // MSAA is an option that is only available in full forward but Camera can be set in Full Forward only. Thus MSAA have no dependency currently
            //Note: do not use SerializedProperty.enumValueIndex here as this enum not start at 0 as it is used as flags.
            bool msaaAllowed = d.supportedLitShaderMode.intValue == (int)UnityEngine.Experimental.Rendering.HDPipeline.RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly || d.supportedLitShaderMode.intValue == (int)UnityEngine.Experimental.Rendering.HDPipeline.RenderPipelineSettings.SupportedLitShaderMode.Both;
            using (new EditorGUI.DisabledScope(!msaaAllowed))
            {
                ++EditorGUI.indentLevel;
                d.supportMSAA.boolValue = EditorGUILayout.Toggle(_.GetContent("Support Multi Sampling Anti-Aliasing|This feature only work when only ForwardOnly LitShaderMode is supported."), d.supportMSAA.boolValue && msaaAllowed);
                using (new EditorGUI.DisabledScope(!d.supportMSAA.boolValue))
                {
                    ++EditorGUI.indentLevel;
                    EditorGUILayout.PropertyField(d.MSAASampleCount, _.GetContent("MSAA Sample Count|Allow to select the level of MSAA."));
                    --EditorGUI.indentLevel;
                }
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportDecals, _.GetContent("Decals|Enable memory and variant for decals buffer and cluster decals"));
            EditorGUILayout.PropertyField(d.supportMotionVectors, _.GetContent("Motion Vectors|Motion vector are use for Motion Blur, TAA, temporal re-projection of various effect like SSR."));
            EditorGUILayout.PropertyField(d.supportRuntimeDebugDisplay, _.GetContent("Runtime debug display|Remove all debug display shader variant only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportDitheringCrossFade, _.GetContent("Dithering cross fade|Remove all dithering cross fade shader variant only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportDistortion, _.GetContent("Distortion|Remove all distortion shader variants only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportTransparentBackface, _.GetContent("Transparent Backface|Remove all Transparent backface shader variants only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportTransparentDepthPrepass, _.GetContent("Transparent Depth Prepass|Remove all Transparent Depth Prepass shader variants only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportTransparentDepthPostpass, _.GetContent("Transparent Depth Postpass|Remove all Transparent Depth Postpass shader variants only in the player. Allow faster build."));


            // Only display the support ray tracing feature if the platform supports it
#if REALTIME_RAYTRACING_SUPPORT
            if(UnityEngine.SystemInfo.supportsRayTracing)
            {
                EditorGUILayout.PropertyField(d.supportRayTracing, _.GetContent("Support Realtime Raytracing."));
            }
            else
#endif
            {
                d.supportRayTracing.boolValue = false;
            }
            
            EditorGUILayout.Space();
        }
    }
}
