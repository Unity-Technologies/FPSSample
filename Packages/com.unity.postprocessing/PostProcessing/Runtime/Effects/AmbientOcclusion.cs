using System;

namespace UnityEngine.Rendering.PostProcessing
{
    public enum AmbientOcclusionMode
    {
        ScalableAmbientObscurance,
        MultiScaleVolumetricObscurance
    }

    public enum AmbientOcclusionQuality
    {
        Lowest,
        Low,
        Medium,
        High,
        Ultra
    }

    [Serializable]
    public sealed class AmbientOcclusionModeParameter : ParameterOverride<AmbientOcclusionMode> {}

    [Serializable]
    public sealed class AmbientOcclusionQualityParameter : ParameterOverride<AmbientOcclusionQuality> {}

    [Serializable]
    [PostProcess(typeof(AmbientOcclusionRenderer), "Unity/Ambient Occlusion")]
    public sealed class AmbientOcclusion : PostProcessEffectSettings
    {
        // Shared parameters
        [Tooltip("The ambient occlusion method to use. \"MSVO\" is higher quality and faster on desktop & console platforms but requires compute shader support.")]
        public AmbientOcclusionModeParameter mode = new AmbientOcclusionModeParameter { value = AmbientOcclusionMode.MultiScaleVolumetricObscurance };

        [Range(0f, 4f), Tooltip("Degree of darkness added by ambient occlusion.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        [ColorUsage(false), Tooltip("Custom color to use for the ambient occlusion.")]
        public ColorParameter color = new ColorParameter { value = Color.black };

        [Tooltip("Only affects ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. Objects rendered with the Forward rendering path won't get any ambient occlusion.")]
        public BoolParameter ambientOnly = new BoolParameter { value = true };

        // MSVO-only parameters
        [Range(-8f, 0f)]
        public FloatParameter noiseFilterTolerance = new FloatParameter { value = 0f }; // Hidden

        [Range(-8f, -1f)]
        public FloatParameter blurTolerance = new FloatParameter { value = -4.6f }; // Hidden

        [Range(-12f, -1f)]
        public FloatParameter upsampleTolerance = new FloatParameter { value = -12f }; // Hidden

        [Range(1f, 10f), Tooltip("Modifies thickness of occluders. This increases dark areas but also introduces dark halo around objects.")]
        public FloatParameter thicknessModifier = new FloatParameter { value = 1f };

        // HDRP-only parameters
        [Range(0f, 1f), Tooltip("")]
        public FloatParameter directLightingStrength = new FloatParameter { value = 0f };

        // SAO-only parameters
        [Tooltip("Radius of sample points, which affects extent of darkened areas.")]
        public FloatParameter radius = new FloatParameter { value = 0.25f };

        [Tooltip("Number of sample points, which affects quality and performance. Lowest, Low & Medium passes are downsampled. High and Ultra are not and should only be used on high-end hardware.")]
        public AmbientOcclusionQualityParameter quality = new AmbientOcclusionQualityParameter { value = AmbientOcclusionQuality.Medium };

        // sample-game begin: added globalEnable
        public static bool globalEnable = true;
        // sample-game end

        // SRPs can call this method without a context set (see HDRP)
        // We need a better way to handle this than checking for a null context, context should
        // never be null.
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
        // sample-game begin: added globalEnable
            bool state = enabled.value
                && globalEnable
                && intensity.value > 0f;
        // sample-game end

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

    public interface IAmbientOcclusionMethod
    {
        DepthTextureMode GetCameraFlags();
        void RenderAfterOpaque(PostProcessRenderContext context);
        void RenderAmbientOnly(PostProcessRenderContext context);
        void CompositeAmbientOnly(PostProcessRenderContext context);
        void Release();
    }

    public sealed class AmbientOcclusionRenderer : PostProcessEffectRenderer<AmbientOcclusion>
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
