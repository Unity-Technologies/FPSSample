using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedFrameSettings
    {
        public SerializedProperty root;

        public SerializedProperty enableShadow;
        public SerializedProperty enableContactShadow;
        public SerializedProperty enableSSR;
        public SerializedProperty enableSSAO;
        public SerializedProperty enableSubsurfaceScattering;
        public SerializedProperty enableTransmission;
        public SerializedProperty enableAtmosphericScattering;
        public SerializedProperty enableVolumetrics;
        public SerializedProperty enableReprojectionForVolumetrics;
        public SerializedProperty enableLightLayers;

        public SerializedProperty diffuseGlobalDimmer;
        public SerializedProperty specularGlobalDimmer;

        public SerializedProperty enableForwardRenderingOnly;
        public SerializedProperty enableDepthPrepassWithDeferredRendering;

        public SerializedProperty enableTransparentPrepass;
        public SerializedProperty enableMotionVectors;
        public SerializedProperty enableObjectMotionVectors;
        public SerializedProperty enableDecals;
        public SerializedProperty enableRoughRefraction;
        public SerializedProperty enableTransparentPostpass;
        public SerializedProperty enableDistortion;
        public SerializedProperty enablePostprocess;

        public SerializedProperty enableStereo;
        public SerializedProperty xrGraphicsConfig;
        public SerializedProperty enableAsyncCompute;

        public SerializedProperty enableOpaqueObjects;
        public SerializedProperty enableTransparentObjects;
        public SerializedProperty enableRealtimePlanarReflection;        

        public SerializedProperty enableMSAA;

        public SerializedProperty enableShadowMask;

        public SerializedLightLoopSettings lightLoopSettings;

        private  SerializedProperty overrides;
        public bool overridesShadow
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Shadow) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Shadow;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Shadow;
            }
        }
        public bool overridesContactShadow
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.ContactShadow) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.ContactShadow;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.ContactShadow;
            }
        }
        public bool overridesShadowMask
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.ShadowMask) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.ShadowMask;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.ShadowMask;
            }
        }
        public bool overridesSSR
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.SSR) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.SSR;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.SSR;
            }
        }
        public bool overridesSSAO
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.SSAO) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.SSAO;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.SSAO;
            }
        }
        public bool overridesSubsurfaceScattering
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.SubsurfaceScattering) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.SubsurfaceScattering;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.SubsurfaceScattering;
            }
        }
        public bool overridesTransmission
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Transmission) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Transmission;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Transmission;
            }
        }
        public bool overridesAtmosphericScaterring
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.AtmosphericScaterring) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.AtmosphericScaterring;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.AtmosphericScaterring;
            }
        }
        public bool overridesVolumetrics
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Volumetrics) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Volumetrics;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Volumetrics;
            }
        }
        public bool overridesProjectionForVolumetrics
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.ReprojectionForVolumetrics) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.ReprojectionForVolumetrics;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.ReprojectionForVolumetrics;
            }
        }
        public bool overridesLightLayers
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.LightLayers) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.LightLayers;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.LightLayers;
            }
        }
        public bool overridesTransparentPrepass
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.TransparentPrepass) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.TransparentPrepass;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.TransparentPrepass;
            }
        }
        public bool overridesTransparentPostpass
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.TransparentPostpass) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.TransparentPostpass;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.TransparentPostpass;
            }
        }
        public bool overridesMotionVectors
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.MotionVectors) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.MotionVectors;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.MotionVectors;
            }
        }
        public bool overridesObjectMotionVectors
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.ObjectMotionVectors) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.ObjectMotionVectors;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.ObjectMotionVectors;
            }
        }
        public bool overridesDecals
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Decals) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Decals;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Decals;
            }
        }
        public bool overridesRoughRefraction
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.RoughRefraction) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.RoughRefraction;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.RoughRefraction;
            }
        }
        public bool overridesDistortion
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Distortion) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Distortion;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Distortion;
            }
        }
        public bool overridesPostprocess
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Postprocess) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Postprocess;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Postprocess;
            }
        }
        public bool overridesForwardRenderingOnly
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.ForwardRenderingOnly) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.ForwardRenderingOnly;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.ForwardRenderingOnly;
            }
        }
        public bool overridesDepthPrepassWithDeferredRendering
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.DepthPrepassWithDeferredRendering) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.DepthPrepassWithDeferredRendering;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.DepthPrepassWithDeferredRendering;
            }
        }
        public bool overridesAsyncCompute
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.AsyncCompute) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.AsyncCompute;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.AsyncCompute;
            }
        }
        public bool overridesOpaqueObjects
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.OpaqueObjects) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.OpaqueObjects;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.OpaqueObjects;
            }
        }
        public bool overridesTransparentObjects
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.TransparentObjects) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.TransparentObjects;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.TransparentObjects;
            }
        }

        public bool overridesRealtimePlanarReflection
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.RealtimePlanarReflection) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.RealtimePlanarReflection;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.RealtimePlanarReflection;
            }
        }        

        public bool overridesStereo
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.Stereo) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.Stereo;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.Stereo;
            }
        }
        public bool overridesXrGraphicSettings
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.XrGraphicSettings) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.XrGraphicSettings;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.XrGraphicSettings;
            }
        }
        public bool overridesMSAA
        {
            get { return (overrides.intValue & (int)FrameSettingsOverrides.MSAA) > 0; }
            set
            {
                if (value)
                    overrides.intValue |= (int)FrameSettingsOverrides.MSAA;
                else
                    overrides.intValue &= ~(int)FrameSettingsOverrides.MSAA;
            }
        }

        public SerializedFrameSettings(SerializedProperty root)
        {
            this.root = root;

            enableShadow = root.Find((FrameSettings d) => d.enableShadow);
            enableContactShadow = root.Find((FrameSettings d) => d.enableContactShadows);
            enableSSR = root.Find((FrameSettings d) => d.enableSSR);
            enableSSAO = root.Find((FrameSettings d) => d.enableSSAO);
            enableSubsurfaceScattering = root.Find((FrameSettings d) => d.enableSubsurfaceScattering);
            enableTransmission = root.Find((FrameSettings d) => d.enableTransmission);
            enableAtmosphericScattering = root.Find((FrameSettings d) => d.enableAtmosphericScattering);
            enableVolumetrics = root.Find((FrameSettings d) => d.enableVolumetrics);
            enableReprojectionForVolumetrics = root.Find((FrameSettings d) => d.enableReprojectionForVolumetrics);
            enableLightLayers = root.Find((FrameSettings d) => d.enableLightLayers);
            diffuseGlobalDimmer = root.Find((FrameSettings d) => d.diffuseGlobalDimmer);
            specularGlobalDimmer = root.Find((FrameSettings d) => d.specularGlobalDimmer);
            enableForwardRenderingOnly = root.Find((FrameSettings d) => d.enableForwardRenderingOnly);
            enableDepthPrepassWithDeferredRendering = root.Find((FrameSettings d) => d.enableDepthPrepassWithDeferredRendering);
            enableTransparentPrepass = root.Find((FrameSettings d) => d.enableTransparentPrepass);
            enableMotionVectors = root.Find((FrameSettings d) => d.enableMotionVectors);
            enableObjectMotionVectors = root.Find((FrameSettings d) => d.enableObjectMotionVectors);
            enableDecals = root.Find((FrameSettings d) => d.enableDecals);
            enableRoughRefraction = root.Find((FrameSettings d) => d.enableRoughRefraction);
            enableTransparentPostpass = root.Find((FrameSettings d) => d.enableTransparentPostpass);
            enableDistortion = root.Find((FrameSettings d) => d.enableDistortion);
            enablePostprocess = root.Find((FrameSettings d) => d.enablePostprocess);
            enableStereo = root.Find((FrameSettings d) => d.enableStereo);
            xrGraphicsConfig = root.Find((FrameSettings d) => d.xrGraphicsConfig);
            enableAsyncCompute = root.Find((FrameSettings d) => d.enableAsyncCompute);
            enableOpaqueObjects = root.Find((FrameSettings d) => d.enableOpaqueObjects);
            enableTransparentObjects = root.Find((FrameSettings d) => d.enableTransparentObjects);
            enableRealtimePlanarReflection = root.Find((FrameSettings d) => d.enableRealtimePlanarReflection);
            enableMSAA = root.Find((FrameSettings d) => d.enableMSAA);
            enableShadowMask = root.Find((FrameSettings d) => d.enableShadowMask);
            overrides = root.Find((FrameSettings d) => d.overrides);

            lightLoopSettings = new SerializedLightLoopSettings(root.Find((FrameSettings d) => d.lightLoopSettings));
        }
    }
}
