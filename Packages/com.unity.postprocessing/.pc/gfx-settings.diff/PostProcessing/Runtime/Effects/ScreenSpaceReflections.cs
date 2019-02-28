using System;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Screen-space Reflections quality presets.
    /// </summary>
    public enum ScreenSpaceReflectionPreset
    {
        Lower, Low, Medium, High, Higher, Ultra, Overkill, Custom
    }

    /// <summary>
    /// Screen-space Reflections buffer sizes.
    /// </summary>
    public enum ScreenSpaceReflectionResolution
    {
        /// <summary>
        /// Downsampled buffer. Faster but lower quality.
        /// </summary>
        Downsampled,

        /// <summary>
        /// Full-sized buffer. Slower but higher quality.
        /// </summary>
        FullSize,

        /// <summary>
        /// Supersampled buffer. Very slow but much higher quality.
        /// </summary>
        Supersampled
    }

    /// <summary>
    /// A volume parameter holding a <see cref="ScreenSpaceReflectionPreset"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceReflectionPresetParameter : ParameterOverride<ScreenSpaceReflectionPreset> { }

    /// <summary>
    /// A volume parameter holding a <see cref="ScreenSpaceReflectionResolution"/> value.
    /// </summary>
    [Serializable]
    public sealed class ScreenSpaceReflectionResolutionParameter : ParameterOverride<ScreenSpaceReflectionResolution> { }

    /// <summary>
    /// This class holds settings for the Screen-space Reflections effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(ScreenSpaceReflectionsRenderer), "Unity/Screen-space reflections")]
    public sealed class ScreenSpaceReflections : PostProcessEffectSettings
    {
        /// <summary>
        /// The quality preset to use for rendering. Use <see cref="ScreenSpaceReflectionPreset.Custom"/>
        /// to tweak settings.
        /// </summary>
        [Tooltip("Choose a quality preset, or use \"Custom\" to create your own custom preset. Don't use a preset higher than \"Medium\" if you desire good performance on consoles.")]
        public ScreenSpaceReflectionPresetParameter preset = new ScreenSpaceReflectionPresetParameter { value = ScreenSpaceReflectionPreset.Medium };

        /// <summary>
        /// The maximum number of steps in the raymarching pass. Higher values mean more reflections.
        /// </summary>
        [Range(0, 256), Tooltip("Maximum number of steps in the raymarching pass. Higher values mean more reflections.")]
        public IntParameter maximumIterationCount = new IntParameter { value = 16 };

        /// <summary>
        /// Changes the size of the internal buffer. Downsample it to maximize performances or
        /// supersample it to get slow but higher quality results.
        /// </summary>
        [Tooltip("Changes the size of the SSR buffer. Downsample it to maximize performances or supersample it for higher quality results with reduced performance.")]
        public ScreenSpaceReflectionResolutionParameter resolution = new ScreenSpaceReflectionResolutionParameter { value = ScreenSpaceReflectionResolution.Downsampled };

        /// <summary>
        /// The ray thickness. Lower values are more expensive but allow the effect to detect
        /// smaller details.
        /// </summary>
        [Range(1f, 64f), Tooltip("Ray thickness. Lower values are more expensive but allow the effect to detect smaller details.")]
        public FloatParameter thickness = new FloatParameter { value = 8f };

        /// <summary>
        /// The maximum distance to traverse in the scene after which it will stop drawing
        /// reflections.
        /// </summary>
        [Tooltip("Maximum distance to traverse after which it will stop drawing reflections.")]
        public FloatParameter maximumMarchDistance = new FloatParameter { value = 100f };

        /// <summary>
        /// Fades reflections close to the near plane. This is useful to hide common artifacts.
        /// </summary>
        [Range(0f, 1f), Tooltip("Fades reflections close to the near planes.")]
        public FloatParameter distanceFade = new FloatParameter { value = 0.5f };

        /// <summary>
        /// Fades reflections close to the screen edges.
        /// </summary>
        [Range(0f, 1f), Tooltip("Fades reflections close to the screen edges.")]
        public FloatParameter vignette = new FloatParameter { value = 0.5f };

        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && context.camera.actualRenderingPath == RenderingPath.DeferredShading
                && SystemInfo.supportsMotionVectors
                && SystemInfo.supportsComputeShaders
                && SystemInfo.copyTextureSupport > CopyTextureSupport.None
                && context.resources.shaders.screenSpaceReflections
                && context.resources.shaders.screenSpaceReflections.isSupported
                && context.resources.computeShaders.gaussianDownsample;
        }
    }

    internal sealed class ScreenSpaceReflectionsRenderer : PostProcessEffectRenderer<ScreenSpaceReflections>
    {
        RenderTexture m_Resolve;
        RenderTexture m_History;
        int[] m_MipIDs;

        class QualityPreset
        {
            public int maximumIterationCount;
            public float thickness;
            public ScreenSpaceReflectionResolution downsampling;
        }

        readonly QualityPreset[] m_Presets =
        {
            new QualityPreset { maximumIterationCount = 10, thickness = 32, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // Lower
            new QualityPreset { maximumIterationCount = 16, thickness = 32, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // Low
            new QualityPreset { maximumIterationCount = 32, thickness = 16, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // Medium
            new QualityPreset { maximumIterationCount = 48, thickness =  8, downsampling = ScreenSpaceReflectionResolution.Downsampled  }, // High
            new QualityPreset { maximumIterationCount = 16, thickness = 32, downsampling = ScreenSpaceReflectionResolution.FullSize }, // Higher
            new QualityPreset { maximumIterationCount = 48, thickness = 16, downsampling = ScreenSpaceReflectionResolution.FullSize }, // Ultra
            new QualityPreset { maximumIterationCount = 128, thickness = 12, downsampling = ScreenSpaceReflectionResolution.Supersampled }, // Overkill
        };

        enum Pass
        {
            Test,
            Resolve,
            Reproject,
            Composite
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        internal void CheckRT(ref RenderTexture rt, int width, int height, RenderTextureFormat format, FilterMode filterMode, bool useMipMap)
        {
            if (rt == null || !rt.IsCreated() || rt.width != width || rt.height != height || rt.format != format)
            {
                if (rt != null)
                {
                    rt.Release();
                    RuntimeUtilities.Destroy(rt);
                }

                rt = new RenderTexture(width, height, 0, format)
                {
                    filterMode = filterMode,
                    useMipMap = useMipMap,
                    autoGenerateMips = false,
                    hideFlags = HideFlags.HideAndDontSave
                };

                rt.Create();
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("Screen-space Reflections");

            // Get quality settings
            if (settings.preset.value != ScreenSpaceReflectionPreset.Custom)
            {
                int id = (int)settings.preset.value;
                settings.maximumIterationCount.value = m_Presets[id].maximumIterationCount;
                settings.thickness.value = m_Presets[id].thickness;
                settings.resolution.value = m_Presets[id].downsampling;
            }

            settings.maximumMarchDistance.value = Mathf.Max(0f, settings.maximumMarchDistance.value);

            // Square POT target
            int size = Mathf.ClosestPowerOfTwo(Mathf.Min(context.width, context.height));

            if (settings.resolution.value == ScreenSpaceReflectionResolution.Downsampled)
                size >>= 1;
            else if (settings.resolution.value == ScreenSpaceReflectionResolution.Supersampled)
                size <<= 1;

            // The gaussian pyramid compute works in blocks of 8x8 so make sure the last lod has a
            // minimum size of 8x8
            const int kMaxLods = 12;
            int lodCount = Mathf.FloorToInt(Mathf.Log(size, 2f) - 3f);
            lodCount = Mathf.Min(lodCount, kMaxLods);

            CheckRT(ref m_Resolve, size, size, context.sourceFormat, FilterMode.Trilinear, true);

            var noiseTex = context.resources.blueNoise256[0];
            var sheet = context.propertySheets.Get(context.resources.shaders.screenSpaceReflections);
            sheet.properties.SetTexture(ShaderIDs.Noise, noiseTex);

            var screenSpaceProjectionMatrix = new Matrix4x4();
            screenSpaceProjectionMatrix.SetRow(0, new Vector4(size * 0.5f, 0f, 0f, size * 0.5f));
            screenSpaceProjectionMatrix.SetRow(1, new Vector4(0f, size * 0.5f, 0f, size * 0.5f));
            screenSpaceProjectionMatrix.SetRow(2, new Vector4(0f, 0f, 1f, 0f));
            screenSpaceProjectionMatrix.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

            var projectionMatrix = GL.GetGPUProjectionMatrix(context.camera.projectionMatrix, false);
            screenSpaceProjectionMatrix *= projectionMatrix;

            sheet.properties.SetMatrix(ShaderIDs.ViewMatrix, context.camera.worldToCameraMatrix);
            sheet.properties.SetMatrix(ShaderIDs.InverseViewMatrix, context.camera.worldToCameraMatrix.inverse);
            sheet.properties.SetMatrix(ShaderIDs.InverseProjectionMatrix, projectionMatrix.inverse);
            sheet.properties.SetMatrix(ShaderIDs.ScreenSpaceProjectionMatrix, screenSpaceProjectionMatrix);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4((float)settings.vignette.value, settings.distanceFade.value, settings.maximumMarchDistance.value, lodCount));
            sheet.properties.SetVector(ShaderIDs.Params2, new Vector4((float)context.width / (float)context.height, (float)size / (float)noiseTex.width, settings.thickness.value, settings.maximumIterationCount.value));

            cmd.GetTemporaryRT(ShaderIDs.Test, size, size, 0, FilterMode.Point, context.sourceFormat);
            cmd.BlitFullscreenTriangle(context.source, ShaderIDs.Test, sheet, (int)Pass.Test);

            if (context.isSceneView)
            {
                cmd.BlitFullscreenTriangle(context.source, m_Resolve, sheet, (int)Pass.Resolve);
            }
            else
            {
                CheckRT(ref m_History, size, size, context.sourceFormat, FilterMode.Bilinear, false);

                if (m_ResetHistory)
                {
                    context.command.BlitFullscreenTriangle(context.source, m_History);
                    m_ResetHistory = false;
                }

                cmd.GetTemporaryRT(ShaderIDs.SSRResolveTemp, size, size, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.BlitFullscreenTriangle(context.source, ShaderIDs.SSRResolveTemp, sheet, (int)Pass.Resolve);

                sheet.properties.SetTexture(ShaderIDs.History, m_History);
                cmd.BlitFullscreenTriangle(ShaderIDs.SSRResolveTemp, m_Resolve, sheet, (int)Pass.Reproject);

                cmd.CopyTexture(m_Resolve, 0, 0, m_History, 0, 0);

                cmd.ReleaseTemporaryRT(ShaderIDs.SSRResolveTemp);
            }

            cmd.ReleaseTemporaryRT(ShaderIDs.Test);

            // Pre-cache mipmaps ids
            if (m_MipIDs == null || m_MipIDs.Length == 0)
            {
                m_MipIDs = new int[kMaxLods];

                for (int i = 0; i < kMaxLods; i++)
                    m_MipIDs[i] = Shader.PropertyToID("_SSRGaussianMip" + i);
            }

            var compute = context.resources.computeShaders.gaussianDownsample;
            int kernel = compute.FindKernel("KMain");

            var last = new RenderTargetIdentifier(m_Resolve);

            for (int i = 0; i < lodCount; i++)
            {
                size >>= 1;
                Assert.IsTrue(size > 0);

                cmd.GetTemporaryRT(m_MipIDs[i], size, size, 0, FilterMode.Bilinear, context.sourceFormat, RenderTextureReadWrite.Default, 1, true);
                cmd.SetComputeTextureParam(compute, kernel, "_Source", last);
                cmd.SetComputeTextureParam(compute, kernel, "_Result", m_MipIDs[i]);
                cmd.SetComputeVectorParam(compute, "_Size", new Vector4(size, size, 1f / size, 1f / size));
                cmd.DispatchCompute(compute, kernel, size / 8, size / 8, 1);
                cmd.CopyTexture(m_MipIDs[i], 0, 0, m_Resolve, 0, i + 1);

                last = m_MipIDs[i];
            }

            for (int i = 0; i < lodCount; i++)
                cmd.ReleaseTemporaryRT(m_MipIDs[i]);

            sheet.properties.SetTexture(ShaderIDs.Resolve, m_Resolve);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Composite);
            cmd.EndSample("Screen-space Reflections");
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_Resolve);
            RuntimeUtilities.Destroy(m_History);
            m_Resolve = null;
            m_History = null;
        }
    }
}
