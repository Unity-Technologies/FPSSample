using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Flags]
    public enum FrameSettingsOverrides
    {
        //lighting settings
        Shadow = 1 << 0,
        ContactShadow = 1 << 1,
        ShadowMask = 1 << 2,
        SSR = 1 << 3,
        SSAO = 1 << 4,
        SubsurfaceScattering = 1 << 5,
        Transmission = 1 << 6,
        AtmosphericScaterring = 1 << 7,
        Volumetrics = 1 << 8,
        ReprojectionForVolumetrics = 1 << 9,
        LightLayers = 1 << 10,
        MSAA = 1 << 11,

        //rendering pass
        TransparentPrepass = 1 << 13,
        TransparentPostpass = 1 << 14,
        MotionVectors = 1 << 15,
        ObjectMotionVectors = 1 << 16,
        Decals = 1 << 17,
        RoughRefraction = 1 << 18,
        Distortion = 1 << 19,
        Postprocess = 1 << 20,

        //rendering settings
        ForwardRenderingOnly = 1 << 21,
        DepthPrepassWithDeferredRendering = 1 << 22,
        AsyncCompute = 1 << 23,
        OpaqueObjects = 1 << 24,
        TransparentObjects = 1 << 25,
        RealtimePlanarReflection = 1 << 26,

        //stereo settings
        Stereo = 1 << 27,
        XrGraphicSettings = 1 << 28
    }

    // The settings here are per frame settings.
    // Each camera must have its own per frame settings
    [Serializable]
    [System.Diagnostics.DebuggerDisplay("FrameSettings overriding {overrides.ToString(\"X\")}")]
    public class FrameSettings
    {
        static Dictionary<FrameSettingsOverrides, Action<FrameSettings, FrameSettings>> s_Overrides = new Dictionary<FrameSettingsOverrides, Action<FrameSettings, FrameSettings>>
        {
            {FrameSettingsOverrides.Shadow, (a, b) => { a.enableShadow = b.enableShadow; } },
            {FrameSettingsOverrides.ContactShadow, (a, b) => { a.enableContactShadows = b.enableContactShadows; } },
            {FrameSettingsOverrides.ShadowMask, (a, b) => { a.enableShadowMask = b.enableShadowMask; } },
            {FrameSettingsOverrides.SSR, (a, b) => { a.enableSSR = b.enableSSR; } },
            {FrameSettingsOverrides.SSAO, (a, b) => { a.enableSSAO = b.enableSSAO; } },
            {FrameSettingsOverrides.SubsurfaceScattering, (a, b) => { a.enableSubsurfaceScattering = b.enableSubsurfaceScattering; } },
            {FrameSettingsOverrides.Transmission, (a, b) => { a.enableTransmission = b.enableTransmission; } },
            {FrameSettingsOverrides.AtmosphericScaterring, (a, b) => { a.enableAtmosphericScattering = b.enableAtmosphericScattering; } },
            {FrameSettingsOverrides.Volumetrics, (a, b) => { a.enableVolumetrics = b.enableVolumetrics; } },
            {FrameSettingsOverrides.ReprojectionForVolumetrics, (a, b) => { a.enableReprojectionForVolumetrics = b.enableReprojectionForVolumetrics; } },
            {FrameSettingsOverrides.LightLayers, (a, b) => { a.enableLightLayers = b.enableLightLayers; } },
            {FrameSettingsOverrides.MSAA, (a, b) => { a.enableMSAA = b.enableMSAA; } },
            {FrameSettingsOverrides.TransparentPrepass, (a, b) => { a.enableTransparentPrepass = b.enableTransparentPrepass; } },
            {FrameSettingsOverrides.TransparentPostpass, (a, b) => { a.enableTransparentPostpass = b.enableTransparentPostpass; } },
            {FrameSettingsOverrides.MotionVectors, (a, b) => { a.enableMotionVectors = b.enableMotionVectors; } },
            {FrameSettingsOverrides.ObjectMotionVectors, (a, b) => { a.enableObjectMotionVectors = b.enableObjectMotionVectors; } },
            {FrameSettingsOverrides.Decals, (a, b) => { a.enableDecals = b.enableDecals; } },
            {FrameSettingsOverrides.RoughRefraction, (a, b) => { a.enableRoughRefraction = b.enableRoughRefraction; } },
            {FrameSettingsOverrides.Distortion, (a, b) => { a.enableDistortion = b.enableDistortion; } },
            {FrameSettingsOverrides.Postprocess, (a, b) => { a.enablePostprocess = b.enablePostprocess; } },
            {FrameSettingsOverrides.ForwardRenderingOnly, (a, b) => { a.enableForwardRenderingOnly = b.enableForwardRenderingOnly; } },
            {FrameSettingsOverrides.DepthPrepassWithDeferredRendering, (a, b) => { a.enableDepthPrepassWithDeferredRendering = b.enableDepthPrepassWithDeferredRendering; } },
            {FrameSettingsOverrides.AsyncCompute, (a, b) => { a.enableAsyncCompute = b.enableAsyncCompute; } },
            {FrameSettingsOverrides.OpaqueObjects, (a, b) => { a.enableOpaqueObjects = b.enableOpaqueObjects; } },
            {FrameSettingsOverrides.TransparentObjects, (a, b) => { a.enableTransparentObjects = b.enableTransparentObjects; } },
            {FrameSettingsOverrides.RealtimePlanarReflection, (a, b) => { a.enableRealtimePlanarReflection = b.enableRealtimePlanarReflection; } },
            {FrameSettingsOverrides.Stereo, (a, b) => { a.enableStereo = b.enableStereo; } },
            {FrameSettingsOverrides.XrGraphicSettings, (a, b) => { a.xrGraphicsConfig = b.xrGraphicsConfig; } },
        };

        public FrameSettingsOverrides overrides;

        // Lighting
        // Setup by users
        public bool enableShadow = true;
        public bool enableContactShadows = true;
        public bool enableShadowMask = true;
        public bool enableSSR = false;
        public bool enableSSAO = true;
        public bool enableSubsurfaceScattering = true;
        public bool enableTransmission = true;  // Caution: this is only for debug, it doesn't save the cost of Transmission execution
        public bool enableAtmosphericScattering = true;
        public bool enableVolumetrics = true;
        public bool enableReprojectionForVolumetrics = true;
        public bool enableLightLayers = true;

        // Setup by system
        public float diffuseGlobalDimmer = 1.0f;
        public float specularGlobalDimmer = 1.0f;

        // View
        public bool enableForwardRenderingOnly = false; // TODO: Currently there is no way to strip the extra forward shaders generated by the shaders compiler, so we can switch dynamically.
        public bool enableDepthPrepassWithDeferredRendering = false;

        public bool enableTransparentPrepass = true;
        public bool enableMotionVectors = true; // Enable/disable whole motion vectors pass (Camera + Object).
        public bool enableObjectMotionVectors = true;
        [FormerlySerializedAs("enableDBuffer")]
        public bool enableDecals = true;
        public bool enableRoughRefraction = true; // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable rough refraction ?
        public bool enableTransparentPostpass = true;
        public bool enableDistortion = true;
        public bool enablePostprocess = true;

        public bool enableStereo = false;
        public XRGraphicsConfig xrGraphicsConfig = new XRGraphicsConfig();

        public bool enableAsyncCompute = true;

        public bool enableOpaqueObjects = true;
        public bool enableTransparentObjects = true;
        public bool enableRealtimePlanarReflection = true;

        public bool enableMSAA = false;

        // GC.Alloc
        // FrameSettings..ctor() 
        public LightLoopSettings lightLoopSettings = new LightLoopSettings();

        public FrameSettings() {
        }
        public FrameSettings(FrameSettings toCopy)
        {
            toCopy.CopyTo(this);
        }
        
        public void CopyTo(FrameSettings frameSettings)
        {
            frameSettings.enableShadow = this.enableShadow;
            frameSettings.enableContactShadows = this.enableContactShadows;
            frameSettings.enableShadowMask = this.enableShadowMask;
            frameSettings.enableSSR = this.enableSSR;
            frameSettings.enableSSAO = this.enableSSAO;
            frameSettings.enableSubsurfaceScattering = this.enableSubsurfaceScattering;
            frameSettings.enableTransmission = this.enableTransmission;
            frameSettings.enableAtmosphericScattering = this.enableAtmosphericScattering;
            frameSettings.enableVolumetrics = this.enableVolumetrics;
            frameSettings.enableReprojectionForVolumetrics = this.enableReprojectionForVolumetrics;
            frameSettings.enableLightLayers = this.enableLightLayers;

            frameSettings.diffuseGlobalDimmer = this.diffuseGlobalDimmer;
            frameSettings.specularGlobalDimmer = this.specularGlobalDimmer;

            frameSettings.enableForwardRenderingOnly = this.enableForwardRenderingOnly;
            frameSettings.enableDepthPrepassWithDeferredRendering = this.enableDepthPrepassWithDeferredRendering;

            frameSettings.enableTransparentPrepass = this.enableTransparentPrepass;
            frameSettings.enableMotionVectors = this.enableMotionVectors;
            frameSettings.enableObjectMotionVectors = this.enableObjectMotionVectors;
            frameSettings.enableDecals = this.enableDecals;
            frameSettings.enableRoughRefraction = this.enableRoughRefraction;
            frameSettings.enableTransparentPostpass = this.enableTransparentPostpass;
            frameSettings.enableDistortion = this.enableDistortion;
            frameSettings.enablePostprocess = this.enablePostprocess;

            frameSettings.enableStereo = this.enableStereo;

            this.xrGraphicsConfig.CopyTo(frameSettings.xrGraphicsConfig);

            frameSettings.enableOpaqueObjects = this.enableOpaqueObjects;
            frameSettings.enableTransparentObjects = this.enableTransparentObjects;
            frameSettings.enableRealtimePlanarReflection = this.enableRealtimePlanarReflection;            

            frameSettings.enableAsyncCompute = this.enableAsyncCompute;

            frameSettings.enableMSAA = this.enableMSAA;

            frameSettings.overrides = this.overrides;

            this.lightLoopSettings.CopyTo(frameSettings.lightLoopSettings);
        }

        public FrameSettings Override(FrameSettings overridedFrameSettings)
        {
            if(overrides == 0)
            {
                //nothing to override
                return overridedFrameSettings;
            }

            FrameSettings result = new FrameSettings(overridedFrameSettings);
            Array values = Enum.GetValues(typeof(FrameSettingsOverrides));
            foreach(FrameSettingsOverrides val in values)
            {
                if((val & overrides) > 0)
                {
                    s_Overrides[val](result, this);
                }
            }

            result.lightLoopSettings = lightLoopSettings.Override(overridedFrameSettings.lightLoopSettings);

            //propagate override to be chained
            result.overrides = overrides | overridedFrameSettings.overrides;
            return result;
        }

        // Init a FrameSettings from renderpipeline settings, frame settings and debug settings (if any)
        // This will aggregate the various option
        public static void InitializeFrameSettings(Camera camera, RenderPipelineSettings renderPipelineSettings, FrameSettings srcFrameSettings, ref FrameSettings aggregate)
        {
            if (aggregate == null)
                aggregate = new FrameSettings();

            // When rendering reflection probe we disable specular as it is view dependent
            if (camera.cameraType == CameraType.Reflection)
            {
                aggregate.diffuseGlobalDimmer = 1.0f;
                aggregate.specularGlobalDimmer = 0.0f;
            }
            else
            {
                aggregate.diffuseGlobalDimmer = 1.0f;
                aggregate.specularGlobalDimmer = 1.0f;
            }

            aggregate.enableShadow = srcFrameSettings.enableShadow;
            aggregate.enableContactShadows = srcFrameSettings.enableContactShadows;
            aggregate.enableShadowMask = srcFrameSettings.enableShadowMask && renderPipelineSettings.supportShadowMask;
            aggregate.enableSSR = camera.cameraType != CameraType.Reflection && srcFrameSettings.enableSSR && renderPipelineSettings.supportSSR; // No recursive reflections
            aggregate.enableSSAO = srcFrameSettings.enableSSAO && renderPipelineSettings.supportSSAO;
            aggregate.enableSubsurfaceScattering = camera.cameraType != CameraType.Reflection && srcFrameSettings.enableSubsurfaceScattering && renderPipelineSettings.supportSubsurfaceScattering;
            aggregate.enableTransmission = srcFrameSettings.enableTransmission;
            aggregate.enableAtmosphericScattering = srcFrameSettings.enableAtmosphericScattering;
            // We must take care of the scene view fog flags in the editor
            if (!CoreUtils.IsSceneViewFogEnabled(camera))
                aggregate.enableAtmosphericScattering = false;
            // Volumetric are disabled if there is no atmospheric scattering
            aggregate.enableVolumetrics = srcFrameSettings.enableVolumetrics && renderPipelineSettings.supportVolumetrics && aggregate.enableAtmosphericScattering;
            aggregate.enableReprojectionForVolumetrics = srcFrameSettings.enableReprojectionForVolumetrics;

            // TODO: Add support of volumetric in planar reflection
            if (camera.cameraType == CameraType.Reflection)
                aggregate.enableVolumetrics = false;

            aggregate.enableLightLayers = srcFrameSettings.enableLightLayers && renderPipelineSettings.supportLightLayers;

            // We have to fall back to forward-only rendering when scene view is using wireframe rendering mode
            // as rendering everything in wireframe + deferred do not play well together
            aggregate.enableForwardRenderingOnly = srcFrameSettings.enableForwardRenderingOnly || GL.wireframe || renderPipelineSettings.supportOnlyForward;
            aggregate.enableDepthPrepassWithDeferredRendering = srcFrameSettings.enableDepthPrepassWithDeferredRendering;

            aggregate.enableTransparentPrepass = srcFrameSettings.enableTransparentPrepass;
            aggregate.enableMotionVectors = camera.cameraType != CameraType.Reflection && srcFrameSettings.enableMotionVectors && renderPipelineSettings.supportMotionVectors;
            // Object motion vector are disabled if motion vector are disabled
            aggregate.enableObjectMotionVectors = srcFrameSettings.enableObjectMotionVectors && aggregate.enableMotionVectors;
            aggregate.enableDecals = srcFrameSettings.enableDecals && renderPipelineSettings.supportDecals;
            aggregate.enableRoughRefraction = srcFrameSettings.enableRoughRefraction;
            aggregate.enableTransparentPostpass = srcFrameSettings.enableTransparentPostpass;
            aggregate.enableDistortion = camera.cameraType != CameraType.Reflection && srcFrameSettings.enableDistortion;

            // Planar and real time cubemap doesn't need post process and render in FP16
            aggregate.enablePostprocess = camera.cameraType != CameraType.Reflection && srcFrameSettings.enablePostprocess;

            aggregate.enableStereo = ((camera.cameraType == CameraType.Game) || (camera.cameraType == CameraType.VR)) && srcFrameSettings.enableStereo && XRGraphicsConfig.enabled && (camera.stereoTargetEye == StereoTargetEyeMask.Both);

            srcFrameSettings.xrGraphicsConfig.CopyTo(aggregate.xrGraphicsConfig);

            aggregate.enableAsyncCompute = srcFrameSettings.enableAsyncCompute && SystemInfo.supportsAsyncCompute;

            aggregate.enableOpaqueObjects = srcFrameSettings.enableOpaqueObjects;
            aggregate.enableTransparentObjects = srcFrameSettings.enableTransparentObjects;
            aggregate.enableRealtimePlanarReflection = srcFrameSettings.enableRealtimePlanarReflection;       

            aggregate.enableMSAA = srcFrameSettings.enableMSAA && renderPipelineSettings.supportMSAA;

            aggregate.ConfigureMSAADependentSettings();
            aggregate.ConfigureStereoDependentSettings();

            // Disable various option for the preview except if we are a Camera Editor preview
            if (HDUtils.IsRegularPreviewCamera(camera))
            {
                aggregate.enableShadow = false;
                aggregate.enableContactShadows = false;
                aggregate.enableShadowMask = false;
                aggregate.enableSSR = false;
                aggregate.enableSSAO = false;
                aggregate.enableAtmosphericScattering = false;
                aggregate.enableVolumetrics = false;
                aggregate.enableReprojectionForVolumetrics = false;
                aggregate.enableLightLayers = false;
                aggregate.enableTransparentPrepass = false;
                aggregate.enableMotionVectors = false;
                aggregate.enableObjectMotionVectors = false;
                aggregate.enableDecals = false;
                aggregate.enableTransparentPostpass = false;
                aggregate.enableDistortion = false;
                aggregate.enablePostprocess = false;
                aggregate.enableStereo = false;
            }

            LightLoopSettings.InitializeLightLoopSettings(camera, aggregate, renderPipelineSettings, srcFrameSettings, ref aggregate.lightLoopSettings);
        }

        public void ConfigureMSAADependentSettings()
        {
            if (enableMSAA)
            {
                // Initially, MSAA will only support forward
                enableForwardRenderingOnly = true;

                // TODO: The work will be implemented piecemeal to support all passes
                enableDistortion = false; // no gaussian final color
                enableSSR = false;
            }
        }

        public void ConfigureStereoDependentSettings()
        {
            if (enableStereo)
            {
                // Stereo deferred rendering still has the following problems:
                // VR TODO: Dispatch tile light-list compute per-eye
                // VR TODO: Update compute lighting shaders for stereo
                enableForwardRenderingOnly = true;

                // TODO: The work will be implemented piecemeal to support all passes
                //enableMotionVectors = !enableMSAA;
                enableMotionVectors = enablePostprocess && !enableMSAA;
                enableDecals = false;
                enableDistortion = false;
                //enablePostprocess = false;
                enableRoughRefraction = false;
                enableSSAO = false;
                enableSSR = false;
                enableSubsurfaceScattering = false;
                enableTransparentObjects = false;

                xrGraphicsConfig.SetConfig();
            }
        }

        public static void RegisterDebug(string menuName, FrameSettings frameSettings)
        {
            List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
            widgets.AddRange(
            new DebugUI.Widget[]
            {
                new DebugUI.Foldout
                {
                    displayName = "Rendering Passes",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Enable Transparent Prepass", getter = () => frameSettings.enableTransparentPrepass, setter = value => frameSettings.enableTransparentPrepass = value },
                        new DebugUI.BoolField { displayName = "Enable Transparent Postpass", getter = () => frameSettings.enableTransparentPostpass, setter = value => frameSettings.enableTransparentPostpass = value },
                        new DebugUI.BoolField { displayName = "Enable Motion Vectors", getter = () => frameSettings.enableMotionVectors, setter = value => frameSettings.enableMotionVectors = value },
                        new DebugUI.BoolField { displayName = "  Enable Object Motion Vectors", getter = () => frameSettings.enableObjectMotionVectors, setter = value => frameSettings.enableObjectMotionVectors = value },
                        new DebugUI.BoolField { displayName = "Enable DBuffer", getter = () => frameSettings.enableDecals, setter = value => frameSettings.enableDecals = value },
                        new DebugUI.BoolField { displayName = "Enable Rough Refraction", getter = () => frameSettings.enableRoughRefraction, setter = value => frameSettings.enableRoughRefraction = value },
                        new DebugUI.BoolField { displayName = "Enable Distortion", getter = () => frameSettings.enableDistortion, setter = value => frameSettings.enableDistortion = value },
                        new DebugUI.BoolField { displayName = "Enable Postprocess", getter = () => frameSettings.enablePostprocess, setter = value => frameSettings.enablePostprocess = value },
                    }
                },
                new DebugUI.Foldout
                {
                    displayName = "Rendering Settings",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Forward Only", getter = () => frameSettings.enableForwardRenderingOnly, setter = value => frameSettings.enableForwardRenderingOnly = value },
                        new DebugUI.BoolField { displayName = "Deferred Depth Prepass", getter = () => frameSettings.enableDepthPrepassWithDeferredRendering, setter = value => frameSettings.enableDepthPrepassWithDeferredRendering = value },
                        new DebugUI.BoolField { displayName = "Enable Async Compute", getter = () => frameSettings.enableAsyncCompute, setter = value => frameSettings.enableAsyncCompute = value },
                        new DebugUI.BoolField { displayName = "Enable Opaque Objects", getter = () => frameSettings.enableOpaqueObjects, setter = value => frameSettings.enableOpaqueObjects = value },
                        new DebugUI.BoolField { displayName = "Enable Transparent Objects", getter = () => frameSettings.enableTransparentObjects, setter = value => frameSettings.enableTransparentObjects = value },
                        new DebugUI.BoolField { displayName = "Enable Realtime Planar Reflection", getter = () => frameSettings.enableRealtimePlanarReflection, setter = value => frameSettings.enableRealtimePlanarReflection = value },                        
                        new DebugUI.BoolField { displayName = "Enable MSAA", getter = () => frameSettings.enableMSAA, setter = value => frameSettings.enableMSAA = value },
                    }
                },
                new DebugUI.Foldout
                {
                    displayName = "Lighting Settings",
                    children =
                    {
                        new DebugUI.BoolField { displayName = "Enable SSR", getter = () => frameSettings.enableSSR, setter = value => frameSettings.enableSSR = value },
                        new DebugUI.BoolField { displayName = "Enable SSAO", getter = () => frameSettings.enableSSAO, setter = value => frameSettings.enableSSAO = value },
                        new DebugUI.BoolField { displayName = "Enable SubsurfaceScattering", getter = () => frameSettings.enableSubsurfaceScattering, setter = value => frameSettings.enableSubsurfaceScattering = value },
                        new DebugUI.BoolField { displayName = "Enable Transmission", getter = () => frameSettings.enableTransmission, setter = value => frameSettings.enableTransmission = value },
                        new DebugUI.BoolField { displayName = "Enable Shadows", getter = () => frameSettings.enableShadow, setter = value => frameSettings.enableShadow = value },
                        new DebugUI.BoolField { displayName = "Enable Contact Shadows", getter = () => frameSettings.enableContactShadows, setter = value => frameSettings.enableContactShadows = value },
                        new DebugUI.BoolField { displayName = "Enable ShadowMask", getter = () => frameSettings.enableShadowMask, setter = value => frameSettings.enableShadowMask = value },
                        new DebugUI.BoolField { displayName = "Enable Atmospheric Scattering", getter = () => frameSettings.enableAtmosphericScattering, setter = value => frameSettings.enableAtmosphericScattering = value },
                        new DebugUI.BoolField { displayName = "Enable Volumetrics", getter = () => frameSettings.enableVolumetrics, setter = value => frameSettings.enableVolumetrics = value },
                        new DebugUI.BoolField { displayName = "Enable Reprojection For Volumetrics", getter = () => frameSettings.enableReprojectionForVolumetrics, setter = value => frameSettings.enableReprojectionForVolumetrics = value },
                        new DebugUI.BoolField { displayName = "Enable LightLayers", getter = () => frameSettings.enableLightLayers, setter = value => frameSettings.enableLightLayers = value },
                    }
                }
            });

            LightLoopSettings.RegisterDebug(frameSettings.lightLoopSettings, widgets);

            var panel = DebugManager.instance.GetPanel(menuName, true);
            panel.children.Add(widgets.ToArray());
        }

        public static void UnRegisterDebug(string menuName)
        {
            DebugManager.instance.RemovePanel(menuName);
        }
    }
}
