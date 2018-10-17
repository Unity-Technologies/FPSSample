using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<FrameSettingsUI, SerializedFrameSettings>;

    partial class FrameSettingsUI
    {
        internal static CED.IDrawer Inspector(bool withOverride = true, bool withXR = true)
        {
            return CED.Group(
                CED.Action((s, d, o) =>
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }),
                InspectorInnerbox(withOverride, withXR),
                CED.Action((s, d, o) => EditorGUILayout.EndVertical())
                );
        }

        //separated to add enum popup on default frame settings
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true, bool withXR = true)
        {
            return CED.Group(
                SectionRenderingPasses(withOverride),
                SectionRenderingSettings(withOverride),
                CED.FadeGroup(
                    (s, d, o, i) => new AnimBool(withXR),
                    FadeOption.None,
                    SectionXRSettings(withOverride)),
                SectionLightingSettings(withOverride),
                CED.Select(
                    (s, d, o) => s.lightLoopSettings,
                    (s, d, o) => d.lightLoopSettings,
                    LightLoopSettingsUI.SectionLightLoopSettings(withOverride)
                    )
                );
        }

        public static CED.IDrawer SectionRenderingPasses(bool withOverride)
        {
            return CED.FoldoutGroup(
                renderingPassesHeaderContent,
                (s, p, o) => s.isSectionExpandedRenderingPasses,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(200, CED.Action((s, p, o) => Drawer_SectionRenderingPasses(s, p, o, withOverride))),
                CED.space
                );
        }
        
        public static CED.IDrawer SectionRenderingSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                renderingSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedRenderingSettings,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(300, CED.Action((s, p, o) => Drawer_SectionRenderingSettings(s, p, o, withOverride))),
                CED.space
                );
        }

        public static CED.IDrawer SectionXRSettings(bool withOverride)
        {
            return CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedXRSupported,
                FadeOption.None,
                CED.FoldoutGroup(
                    xrSettingsHeaderContent,
                    (s, p, o) => s.isSectionExpandedXRSettings,
                    FoldoutOption.Indent | FoldoutOption.Boxed,
                    CED.LabelWidth(200, CED.Action((s, p, o) => Drawer_FieldStereoEnabled(s, p, o, withOverride))),
                    CED.space));
        }

        public static CED.IDrawer SectionLightingSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                lightSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedLightingSettings,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionLightingSettings(s, p, o, withOverride))),
                CED.space);
        }

        internal static FrameSettings GetDefaultFrameSettingsFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            if (owner is HDProbeEditor)
            {
                if ((owner as HDProbeEditor).GetTarget(owner.target).mode == ReflectionProbeMode.Realtime)
                {
                    return hdrpAsset.GetRealtimeReflectionFrameSettings();
                }
                else
                {
                    return hdrpAsset.GetBakedOrCustomReflectionFrameSettings();
                }
            }
            return hdrpAsset.GetFrameSettings();
        }

        static void Drawer_SectionRenderingPasses(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                RenderPipelineSettings hdrpSettings = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings;
                FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
                OverridableSettingsArea area = new OverridableSettingsArea(8);
                area.Add(p.enableTransparentPrepass, transparentPrepassContent, () => p.overridesTransparentPrepass, a => p.overridesTransparentPrepass = a, defaultValue: defaultFrameSettings.enableTransparentPrepass);
                area.Add(p.enableTransparentPostpass, transparentPostpassContent, () => p.overridesTransparentPostpass, a => p.overridesTransparentPostpass = a, defaultValue: defaultFrameSettings.enableTransparentPostpass);
                area.Add(p.enableMotionVectors, motionVectorContent, () => p.overridesMotionVectors, a => p.overridesMotionVectors = a, () => hdrpSettings.supportMotionVectors, defaultValue: defaultFrameSettings.enableMotionVectors);
                area.Add(p.enableObjectMotionVectors, objectMotionVectorsContent, () => p.overridesObjectMotionVectors, a => p.overridesObjectMotionVectors = a, () => hdrpSettings.supportMotionVectors && p.enableMotionVectors.boolValue, defaultValue: defaultFrameSettings.enableObjectMotionVectors, indent: 1);
                area.Add(p.enableDecals, decalsContent, () => p.overridesDecals, a => p.overridesDecals = a, () => hdrpSettings.supportDecals, defaultValue: defaultFrameSettings.enableDecals);
                area.Add(p.enableRoughRefraction, roughRefractionContent, () => p.overridesRoughRefraction, a => p.overridesRoughRefraction = a, defaultValue: defaultFrameSettings.enableRoughRefraction);
                area.Add(p.enableDistortion, distortionContent, () => p.overridesDistortion, a => p.overridesDistortion = a, defaultValue: defaultFrameSettings.enableDistortion);
                area.Add(p.enablePostprocess, postprocessContent, () => p.overridesPostprocess, a => p.overridesPostprocess = a, defaultValue: defaultFrameSettings.enablePostprocess);
                area.Draw(withOverride);
            }
        }

        static void Drawer_SectionRenderingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                RenderPipelineSettings hdrpSettings = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings;
                FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
                OverridableSettingsArea area = new OverridableSettingsArea(6);
                area.Add(p.enableForwardRenderingOnly, forwardRenderingOnlyContent, () => p.overridesForwardRenderingOnly, a => p.overridesForwardRenderingOnly = a, () => !GL.wireframe && !hdrpSettings.supportOnlyForward, defaultValue: defaultFrameSettings.enableForwardRenderingOnly || hdrpSettings.supportOnlyForward);
                area.Add(p.enableMSAA, msaaContent, () => p.overridesMSAA, a => p.overridesMSAA = a, () => hdrpSettings.supportMSAA && (p.enableForwardRenderingOnly.boolValue || (GL.wireframe || hdrpSettings.supportOnlyForward) && (defaultFrameSettings.enableForwardRenderingOnly || hdrpSettings.supportOnlyForward)), defaultValue: defaultFrameSettings.enableMSAA && hdrpSettings.supportMSAA && !GL.wireframe && !hdrpSettings.supportOnlyForward && p.enableForwardRenderingOnly.boolValue, indent: 1);
                area.Add(p.enableDepthPrepassWithDeferredRendering, depthPrepassWithDeferredRenderingContent, () => p.overridesDepthPrepassWithDeferredRendering, a => p.overridesDepthPrepassWithDeferredRendering = a, () => (!defaultFrameSettings.enableForwardRenderingOnly && !p.overridesForwardRenderingOnly || p.overridesForwardRenderingOnly && !p.enableForwardRenderingOnly.boolValue) && !hdrpSettings.supportOnlyForward, defaultValue: defaultFrameSettings.enableDepthPrepassWithDeferredRendering && !hdrpSettings.supportOnlyForward && !p.enableForwardRenderingOnly.boolValue);
                area.Add(p.enableAsyncCompute, asyncComputeContent, () => p.overridesAsyncCompute, a => p.overridesAsyncCompute = a, () => SystemInfo.supportsAsyncCompute, defaultValue: defaultFrameSettings.enableAsyncCompute);
                area.Add(p.enableOpaqueObjects, opaqueObjectsContent, () => p.overridesOpaqueObjects, a => p.overridesOpaqueObjects = a, defaultValue: defaultFrameSettings.enableOpaqueObjects);
                area.Add(p.enableTransparentObjects, transparentObjectsContent, () => p.overridesTransparentObjects, a => p.overridesTransparentObjects = a, defaultValue: defaultFrameSettings.enableTransparentObjects);
                area.Add(p.enableRealtimePlanarReflection, realtimePlanarReflectionContent, () => p.overridesRealtimePlanarReflection, a => p.overridesRealtimePlanarReflection = a, defaultValue: defaultFrameSettings.enableRealtimePlanarReflection);
                area.Draw(withOverride);
            }
        }
        
        static void Drawer_SectionLightingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                RenderPipelineSettings hdrpSettings = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings;
                FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
                OverridableSettingsArea area = new OverridableSettingsArea(10);
                area.Add(p.enableShadow, shadowContent, () => p.overridesShadow, a => p.overridesShadow = a, defaultValue: defaultFrameSettings.enableShadow);
                area.Add(p.enableContactShadow, contactShadowContent, () => p.overridesContactShadow, a => p.overridesContactShadow = a, defaultValue: defaultFrameSettings.enableContactShadows);
                area.Add(p.enableShadowMask, shadowMaskContent, () => p.overridesShadowMask, a => p.overridesShadowMask = a, () => hdrpSettings.supportShadowMask, defaultValue: defaultFrameSettings.enableShadowMask);
                area.Add(p.enableSSR, ssrContent, () => p.overridesSSR, a => p.overridesSSR = a, () => hdrpSettings.supportSSR, defaultValue: defaultFrameSettings.enableSSR);
                area.Add(p.enableSSAO, ssaoContent, () => p.overridesSSAO, a => p.overridesSSAO = a, () => hdrpSettings.supportSSAO, defaultValue: defaultFrameSettings.enableSSAO);
                area.Add(p.enableSubsurfaceScattering, subsurfaceScatteringContent, () => p.overridesSubsurfaceScattering, a => p.overridesSubsurfaceScattering = a, () => hdrpSettings.supportSubsurfaceScattering, defaultValue: defaultFrameSettings.enableSubsurfaceScattering);
                area.Add(p.enableTransmission, transmissionContent, () => p.overridesTransmission, a => p.overridesTransmission = a, defaultValue: defaultFrameSettings.enableTransmission);
                area.Add(p.enableAtmosphericScattering, atmosphericScatteringContent, () => p.overridesAtmosphericScaterring, a => p.overridesAtmosphericScaterring = a, defaultValue: defaultFrameSettings.enableAtmosphericScattering);
                area.Add(p.enableVolumetrics, volumetricContent, () => p.overridesVolumetrics, a => p.overridesVolumetrics = a, () => hdrpSettings.supportVolumetrics && p.enableAtmosphericScattering.boolValue, defaultValue: defaultFrameSettings.enableAtmosphericScattering, indent: 1);
                area.Add(p.enableReprojectionForVolumetrics, reprojectionForVolumetricsContent, () => p.overridesProjectionForVolumetrics, a => p.overridesProjectionForVolumetrics = a, () => hdrpSettings.supportVolumetrics && p.enableAtmosphericScattering.boolValue, defaultValue: defaultFrameSettings.enableVolumetrics, indent: 1);
                area.Add(p.enableLightLayers, lightLayerContent, () => p.overridesLightLayers, a => p.overridesLightLayers = a, () => hdrpSettings.supportLightLayers, defaultValue: defaultFrameSettings.enableLightLayers);
                area.Draw(withOverride);
            }
        }

        static void Drawer_FieldStereoEnabled(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            OverridableSettingsArea area = new OverridableSettingsArea(2);
            FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
            area.Add(p.enableStereo, stereoContent, () => p.overridesStereo, a => p.overridesStereo = a, defaultValue: defaultFrameSettings.enableStereo);
            //need to add support for xrGraphicConfig to show it
            area.Add(p.xrGraphicsConfig, xrGraphicConfigContent, () => p.overridesXrGraphicSettings, a => p.overridesXrGraphicSettings = a, () => XRGraphicsConfig.tryEnable, defaultValue: defaultFrameSettings.xrGraphicsConfig);
            area.Draw(withOverride);
        }
    }
}
