using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Ambient occlusion modes.
    /// </summary>
    public enum AmbientOcclusionMode
    {
        /// <summary>
        /// A standard implementation of ambient obscurance that works on non modern platforms. If
        /// you target a compute-enabled platform we recommend that you use
        /// <see cref="MultiScaleVolumetricObscurance"/> instead.
        /// </summary>
        ScalableAmbientObscurance,

        /// <summary>
        /// A modern version of ambient occlusion heavily optimized for consoles and desktop
        /// platforms.
        /// </summary>
        MultiScaleVolumetricObscurance
    }

    /// <summary>
    /// Quality settings for <see cref="AmbientOcclusionMode.ScalableAmbientObscurance"/>.
    /// </summary>
    public enum AmbientOcclusionQuality
    {
        /// <summary>
        /// 4 samples + downsampling.
        /// </summary>
        Lowest,

        /// <summary>
        /// 6 samples + downsampling.
        /// </summary>
        Low,

        /// <summary>
        /// 10 samples + downsampling.
        /// </summary>
        Medium,

        /// <summary>
        /// 8 samples.
        /// </summary>
        High,

        /// <summary>
        /// 12 samples.
        /// </summary>
        Ultra
    }

    /// <summary>
    /// A volume parameter holding a <see cref="AmbientOcclusionMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class AmbientOcclusionModeParameter : ParameterOverride<AmbientOcclusionMode> {}

    /// <summary>
    /// A volume parameter holding a <see cref="AmbientOcclusionQuality"/> value.
    /// </summary>
    [Serializable]
    public sealed class AmbientOcclusionQualityParameter : ParameterOverride<AmbientOcclusionQuality> {}

    /// <summary>
    /// This class holds settings for the Ambient Occlusion effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(AmbientOcclusionRenderer), "Unity/Ambient Occlusion")]
    public sealed class AmbientOcclusion : PostProcessEffectSettings
    {
        // Shared parameters

        /// <summary>
        /// The ambient occlusion method to use.
        /// </summary>
        [Tooltip("The ambient occlusion method to use. \"Multi Scale Volumetric Obscurance\" is higher quality and faster on desktop & console platforms but requires compute shader support.")]
        
        public AmbientOcclusionModeParameter mode = new AmbientOcclusionModeParameter { value = AmbientOcclusionMode.MultiScaleVolumetricObscurance };

        /// <summary>
        /// The degree of darkness added by ambient occlusion.
        /// </summary>
        [Range(0f, 4f), Tooltip("The degree of darkness added by ambient occlusion. Higher values produce darker areas.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        /// <summary>
        /// A custom color to use for the ambient occlusion.
        /// </summary>
        [ColorUsage(false), Tooltip("The custom color to use for the ambient occlusion. The default is black.")]
        
        public ColorParameter color = new ColorParameter { value = Color.black };

        /// <summary>
        /// Only affects ambient lighting. This mode is only available with the Deferred rendering
        /// path and HDR rendering. Objects rendered with the Forward rendering path won't get any
        /// ambient occlusion.
        /// </summary>
        [Tooltip("Check this box to mark this Volume as to only affect ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. Objects rendered with the Forward rendering path won't get any ambient occlusion.")]
        public BoolParameter ambientOnly = new BoolParameter { value = true };

        // MSVO-only parameters

        /// <summary>
        /// The tolerance of the noise filter to changes in the depth pyramid.
        /// </summary>
        [Range(-8f, 0f)]
        public FloatParameter noiseFilterTolerance = new FloatParameter { value = 0f }; // Hidden

        /// <summary>
        /// The tolerance of the bilateral blur filter to depth changes.
        /// </summary>
        [Range(-8f, -1f)]
        public FloatParameter blurTolerance = new FloatParameter { value = -4.6f }; // Hidden

        /// <summary>
        /// The tolerance of the upsampling pass to depth changes.
        /// </summary>
        [Range(-12f, -1f)]
        public FloatParameter upsampleTolerance = new FloatParameter { value = -12f }; // Hidden

        /// <summary>
        /// Modifies the thickness of occluders. This increases dark areas but also introduces dark
        /// halo around objects.
        /// </summary>
        [Range(1f, 10f), Tooltip("This modifies the thickness of occluders. It increases the size of dark areas and also introduces a dark halo around objects.")]
        public FloatParameter thicknessModifier = new FloatParameter { value = 1f };

        // HDRP-only parameters

        /// <summary>
        /// Modifies he influence of direct lighting on ambient occlusion. This is only used in the
        /// HD Render Pipeline currently.
        /// </summary>
        [Range(0f, 1f), Tooltip("Modifies the influence of direct lighting on ambient occlusion.")]
        public FloatParameter directLightingStrength = new FloatParameter { value = 0f };

        // SAO-only parameters
        /// <summary>
        /// Radius of sample points, which affects extent of darkened areas.
        /// </summary>
        [Tooltip("The radius of sample points. This affects the size of darkened areas.")]
        public FloatParameter radius = new FloatParameter { value = 0.25f };

        /// <summary>
        /// The number of sample points, which affects quality and performance. Lowest, Low & Medium
        /// passes are downsampled. High and Ultra are not and should only be used on high-end
        /// hardware.
        /// </summary>
        [Tooltip("The number of sample points. This affects both quality and performance. For \"Lowest\", \"Low\", and \"Medium\", passes are downsampled. For \"High\" and \"Ultra\", they are not and therefore you should only \"High\" and \"Ultra\" on high-end hardware.")]
        public AmbientOcclusionQualityParameter quality = new AmbientOcclusionQualityParameter { value = AmbientOcclusionQuality.Medium };

        // SRPs can call this method without a context set (see HDRP).
        // We need a better way to handle this than checking for a null context, context should
        // never be null.
        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            bool state = enabled.value
                && intensity.value > 0f;

            if (mode.value == AmbientOcclusionMode.ScalableAmbientObscurance)
            {
                state &= !RuntimeUtilities.scriptableRenderPipelineActive;

                if (context != null)
                {
                    state &= context.resources.shaders.scalableAO
                          && context.resources.shaders.scalableAO.isSupported;
                }
            }
            else if (mode.value == AmbientOcclusionMode.MultiScaleVolumetricObscurance)
            {
#if UNITY_2017_1_OR_NEWER
                if (context != null)
                {
                    state &= context.resources.shaders.multiScaleAO
                          && context.resources.shaders.multiScaleAO.isSupported
                          && context.resources.computeShaders.multiScaleAODownsample1
                          && context.resources.computeShaders.multiScaleAODownsample2
                          && context.resources.computeShaders.multiScaleAORender
                          && context.resources.computeShaders.multiScaleAOUpsample;
                }

                state &= SystemInfo.supportsComputeShaders
                      && !RuntimeUtilities.isAndroidOpenGL
                      && RenderTextureFormat.RFloat.IsSupported()
                      && RenderTextureFormat.RHalf.IsSupported()
                      && RenderTextureFormat.R8.IsSupported();
#else
                state = false;
#endif
            }

            return state;
        }
    }

    internal interface IAmbientOcclusionMethod
    {
        DepthTextureMode GetCameraFlags();
        void RenderAfterOpaque(PostProcessRenderContext context);
        void RenderAmbientOnly(PostProcessRenderContext context);
        void CompositeAmbientOnly(PostProcessRenderContext context);
        void Release();
    }

    internal sealed class AmbientOcclusionRenderer : PostProcessEffectRenderer<AmbientOcclusion>
    {
        IAmbientOcclusionMethod[] m_Methods;

        public override void Init()
        {
            if (m_Methods == null)
            {
                m_Methods = new IAmbientOcclusionMethod[]
                {
                    new ScalableAO(settings),
                    new MultiScaleVO(settings),
                };
            }
        }

        public bool IsAmbientOnly(PostProcessRenderContext context)
        {
            var camera = context.camera;
            return settings.ambientOnly.value
                && camera.actualRenderingPath == RenderingPath.DeferredShading
                && camera.allowHDR;
        }

        public IAmbientOcclusionMethod Get()
        {
            return m_Methods[(int)settings.mode.value];
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return Get().GetCameraFlags();
        }

        public override void Release()
        {
            foreach (var m in m_Methods)
                m.Release();
        }

        public ScalableAO GetScalableAO()
        {
            return (ScalableAO)m_Methods[(int)AmbientOcclusionMode.ScalableAmbientObscurance];
        }

        public MultiScaleVO GetMultiScaleVO()
        {
            return (MultiScaleVO)m_Methods[(int)AmbientOcclusionMode.MultiScaleVolumetricObscurance];
        }

        // Unused
        public override void Render(PostProcessRenderContext context)
        {
        }
    }
}
