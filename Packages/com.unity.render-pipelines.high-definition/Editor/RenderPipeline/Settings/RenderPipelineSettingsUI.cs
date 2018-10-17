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

        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SupportedSettings = CED.FoldoutGroup(
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
                EditorGUILayout.PropertyField(d.increaseSssSampleCount, _.GetContent("Increase SSS Sample Count|This allows for better SSS quality. Warning: high performance cost, do not enable on consoles."));
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportVolumetrics, _.GetContent("Volumetrics|Enable memory and shader variant for volumetric."));
            using (new EditorGUI.DisabledScope(!d.supportVolumetrics.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.increaseResolutionOfVolumetrics, _.GetContent("Increase resolution of volumetrics|Increase the resolution of volumetric lighting buffers. Warning: high performance cost, do not enable on consoles."));
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportLightLayers, _.GetContent("LightLayers|Enable light layers. In deferred this imply an extra render target in memory and extra cost."));
            EditorGUILayout.PropertyField(d.supportOnlyForward, _.GetContent("Forward Rendering Only|Remove all the memory and shader variant of GBuffer. The renderer cannot be switch to deferred anymore."));
            
            // MSAA is an option that is only available in full forward but Camera can be set in Full Forward only. Thus MSAA have no dependency currently
            EditorGUILayout.PropertyField(d.supportMSAA, _.GetContent("Support Multi Sampling Anti-Aliasing|This feature doesn't work currently."));
            using (new EditorGUI.DisabledScope(!d.supportMSAA.boolValue))
            {
                ++EditorGUI.indentLevel;
                EditorGUILayout.PropertyField(d.MSAASampleCount, _.GetContent("MSAA Sample Count|Allow to select the level of MSAA."));
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.PropertyField(d.supportDecals, _.GetContent("Decals|Enable memory and variant for decals buffer and cluster decals"));
            EditorGUILayout.PropertyField(d.supportMotionVectors, _.GetContent("Motion Vectors|Motion vector are use for Motion Blur, TAA, temporal re-projection of various effect like SSR."));
            EditorGUILayout.PropertyField(d.supportRuntimeDebugDisplay, _.GetContent("Runtime debug display|Remove all debug display shader variant only in the player. Allow faster build."));
            EditorGUILayout.PropertyField(d.supportDitheringCrossFade, _.GetContent("Dithering cross fade|Remove all dithering cross fade shader variant only in the player. Allow faster build."));
            EditorGUILayout.Space();
        }
    }
}
