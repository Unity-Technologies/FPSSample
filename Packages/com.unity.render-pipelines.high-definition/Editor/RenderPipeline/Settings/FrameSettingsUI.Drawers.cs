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
#pragma warning disable 618 //CED
        internal static CED.IDrawer Inspector(bool withOverride = true)
#pragma warning restore 618
        {
            return CED.Group(
                CED.Action((s, d, o) =>
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(FrameSettingsUI.frameSettingsHeaderContent, EditorStyles.boldLabel);
                }),
                InspectorInnerbox(withOverride),
                CED.Action((s, d, o) => EditorGUILayout.EndVertical())
                );
        }

        //separated to add enum popup on default frame settings
#pragma warning disable 618 //CED
        internal static CED.IDrawer InspectorInnerbox(bool withOverride = true)
#pragma warning restore 618
        {
            return CED.Group(
                SectionRenderingPasses(withOverride),
                SectionRenderingSettings(withOverride),
                SectionLightingSettings(withOverride),
                SectionAsyncComputeSettings(withOverride),
                CED.Select(
                    (s, d, o) => s.lightLoopSettings,
                    (s, d, o) => d.lightLoopSettings,
                    LightLoopSettingsUI.SectionLightLoopSettings(withOverride)
                    )
                );
        }

#pragma warning disable 618 //CED
        public static CED.IDrawer SectionRenderingPasses(bool withOverride)
#pragma warning restore 618
        {
            return CED.FoldoutGroup(
                renderingPassesHeaderContent,
                (s, p, o) => s.isSectionExpandedRenderingPasses,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(200, CED.Action((s, p, o) => Drawer_SectionRenderingPasses(s, p, o, withOverride))),
                CED.space
                );
        }

#pragma warning disable 618 //CED
        public static CED.IDrawer SectionRenderingSettings(bool withOverride)
#pragma warning restore 618
        {
            return CED.FoldoutGroup(
                renderingSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedRenderingSettings,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionRenderingSettings(s, p, o, withOverride))),
                CED.space
                );
        }

#pragma warning disable 618 //CED
        public static CED.IDrawer SectionAsyncComputeSettings(bool withOverride)
#pragma warning restore 618
        {
            return CED.FoldoutGroup(
                asyncComputeSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedAsyncComputeSettings,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionAsyncComputeSettings(s, p, o, withOverride))),
                CED.space
                );
        }
        
#pragma warning disable 618 //CED
        public static CED.IDrawer SectionLightingSettings(bool withOverride)
#pragma warning restore 618
        {
            return CED.FoldoutGroup(
                lightSettingsHeaderContent,
                (s, p, o) => s.isSectionExpandedLightingSettings,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.LabelWidth(250, CED.Action((s, p, o) => Drawer_SectionLightingSettings(s, p, o, withOverride))),
                CED.space);
        }

        internal static HDRenderPipelineAsset GetHDRPAssetFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset;
            if (owner is HDRenderPipelineEditor)
            {
                //When drawing the inspector of a selected HDRPAsset in Project windows, access HDRP by owner drawing itself
                hdrpAsset = (owner as HDRenderPipelineEditor).target as HDRenderPipelineAsset;
            }
            else
            {
                //Else rely on GraphicsSettings are you should be in hdrp and owner could be probe or camera.
                hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            }
            return hdrpAsset;
        }

        internal static FrameSettings GetDefaultFrameSettingsFor(Editor owner)
        {
            HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
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
                HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
                RenderPipelineSettings hdrpSettings = hdrpAsset.renderPipelineSettings;
                FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
                OverridableSettingsArea area = new OverridableSettingsArea(8);
                area.Add(p.enableTransparentPrepass, transparentPrepassContent, () => p.overridesTransparentPrepass, a => p.overridesTransparentPrepass = a, defaultValue: defaultFrameSettings.enableTransparentPrepass);
                area.Add(p.enableTransparentPostpass, transparentPostpassContent, () => p.overridesTransparentPostpass, a => p.overridesTransparentPostpass = a, defaultValue: defaultFrameSettings.enableTransparentPostpass);
                area.Add(p.enableMotionVectors, motionVectorContent, () => p.overridesMotionVectors, a => p.overridesMotionVectors = a, () => hdrpSettings.supportMotionVectors, defaultValue: defaultFrameSettings.enableMotionVectors);
                area.Add(p.enableObjectMotionVectors, objectMotionVectorsContent, () => p.overridesObjectMotionVectors, a => p.overridesObjectMotionVectors = a, () => hdrpSettings.supportMotionVectors && p.enableMotionVectors.boolValue, defaultValue: defaultFrameSettings.enableObjectMotionVectors, indent: 1);
                area.Add(p.enableDecals, decalsContent, () => p.overridesDecals, a => p.overridesDecals = a, () => hdrpSettings.supportDecals, defaultValue: defaultFrameSettings.enableDecals);
                area.Add(p.enableRoughRefraction, roughRefractionContent, () => p.overridesRoughRefraction, a => p.overridesRoughRefraction = a, defaultValue: defaultFrameSettings.enableRoughRefraction);
                area.Add(p.enableDistortion, distortionContent, () => p.overridesDistortion, a => p.overridesDistortion = a, () => hdrpSettings.supportDistortion, defaultValue: defaultFrameSettings.enableDistortion);
                area.Add(p.enablePostprocess, postprocessContent, () => p.overridesPostprocess, a => p.overridesPostprocess = a, defaultValue: defaultFrameSettings.enablePostprocess);
                area.Draw(withOverride);
            }
        }

        static void Drawer_SectionRenderingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
                RenderPipelineSettings hdrpSettings = hdrpAsset.renderPipelineSettings;
                FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);
                OverridableSettingsArea area = new OverridableSettingsArea(6);
                LitShaderMode defaultShaderLitMode;
                switch(hdrpSettings.supportedLitShaderMode)
                {
                    case RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly:
                        defaultShaderLitMode = LitShaderMode.Forward;
                        break;
                    case RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly:
                        defaultShaderLitMode = LitShaderMode.Deferred;
                        break;
                    case RenderPipelineSettings.SupportedLitShaderMode.Both:
                        defaultShaderLitMode = defaultFrameSettings.shaderLitMode;
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
                }
                
                area.Add(p.litShaderMode, litShaderModeContent, () => p.overridesShaderLitMode, a => p.overridesShaderLitMode = a,
                    () => !GL.wireframe && hdrpSettings.supportedLitShaderMode == RenderPipelineSettings.SupportedLitShaderMode.Both,
                    defaultValue: defaultShaderLitMode);

                bool assetAllowMSAA = hdrpSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly && hdrpSettings.supportMSAA;
                bool frameSettingsAllowMSAA = p.litShaderMode.enumValueIndex == (int)LitShaderMode.Forward && p.overridesShaderLitMode || !p.overridesShaderLitMode && defaultShaderLitMode == LitShaderMode.Forward;
                area.Add(p.enableMSAA, msaaContent, () => p.overridesMSAA, a => p.overridesMSAA = a,
                    () => !GL.wireframe
                    && assetAllowMSAA && frameSettingsAllowMSAA,
                    defaultValue: defaultFrameSettings.enableMSAA && hdrpSettings.supportMSAA && !GL.wireframe && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.ForwardOnly) != 0 && (p.overridesShaderLitMode && p.litShaderMode.enumValueIndex == (int)LitShaderMode.Forward || !p.overridesShaderLitMode && defaultFrameSettings.shaderLitMode == (int)LitShaderMode.Forward));
                area.Add(p.enableDepthPrepassWithDeferredRendering, depthPrepassWithDeferredRenderingContent, () => p.overridesDepthPrepassWithDeferredRendering, a => p.overridesDepthPrepassWithDeferredRendering = a,
                    () => (defaultFrameSettings.shaderLitMode == LitShaderMode.Deferred && !p.overridesShaderLitMode || p.overridesShaderLitMode && p.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred) && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0,
                    defaultValue: defaultFrameSettings.enableDepthPrepassWithDeferredRendering && (hdrpSettings.supportedLitShaderMode & RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) != 0 && p.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred);
                area.Add(p.enableOpaqueObjects, opaqueObjectsContent, () => p.overridesOpaqueObjects, a => p.overridesOpaqueObjects = a, defaultValue: defaultFrameSettings.enableOpaqueObjects);
                area.Add(p.enableTransparentObjects, transparentObjectsContent, () => p.overridesTransparentObjects, a => p.overridesTransparentObjects = a, defaultValue: defaultFrameSettings.enableTransparentObjects);
                area.Add(p.enableRealtimePlanarReflection, realtimePlanarReflectionContent, () => p.overridesRealtimePlanarReflection, a => p.overridesRealtimePlanarReflection = a, defaultValue: defaultFrameSettings.enableRealtimePlanarReflection);
                area.Draw(withOverride);
            }
        }

        static void Drawer_SectionAsyncComputeSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                OverridableSettingsArea area = new OverridableSettingsArea(4);
                FrameSettings defaultFrameSettings = GetDefaultFrameSettingsFor(owner);

                area.Add(p.enableAsyncCompute, asyncComputeContent, () => p.overridesAsyncCompute, a => p.overridesAsyncCompute = a, defaultValue: defaultFrameSettings.enableAsyncCompute);
                area.Add(p.runBuildLightListAsync, lightListAsyncContent, () => p.overrideLightListInAsync, a => p.overrideLightListInAsync = a, () => p.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runLightListAsync, indent: 1);
                area.Add(p.runSSRAsync, SSRAsyncContent, () => p.overrideSSRInAsync, a => p.overrideSSRInAsync = a, () => p.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runSSRAsync, indent: 1);
                area.Add(p.runSSAOAsync, SSAOAsyncContent, () => p.overrideSSAOInAsync, a => p.overrideSSAOInAsync = a, () => p.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runSSAOAsync, indent: 1);
                area.Add(p.runContactShadowsAsync, contactShadowsAsyncContent, () => p.overrideContactShadowsInAsync, a => p.overrideContactShadowsInAsync = a, () => p.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runContactShadowsAsync, indent: 1);
                area.Add(p.runVolumeVoxelizationAsync, volumeVoxelizationAsyncContent, () => p.overrideVolumeVoxelizationInAsync, a => p.overrideVolumeVoxelizationInAsync = a, () => p.enableAsyncCompute.boolValue, defaultValue: defaultFrameSettings.runVolumeVoxelizationAsync, indent: 1);
                area.Draw(withOverride);
            }
        }

        static void Drawer_SectionLightingSettings(FrameSettingsUI s, SerializedFrameSettings p, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is HDProbeEditor) && (owner as HDProbeEditor).GetTarget(owner.target).mode != ReflectionProbeMode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                HDRenderPipelineAsset hdrpAsset = GetHDRPAssetFor(owner);
                RenderPipelineSettings hdrpSettings = hdrpAsset.renderPipelineSettings;
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
    }
}
