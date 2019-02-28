using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PostProcessing
{
    // For now and by popular request, this bloom effect is geared toward artists so they have full
    // control over how it looks at the expense of physical correctness.
    // Eventually we will need a "true" natural bloom effect with proper energy conservation.

    /// <summary>
    /// This class holds settings for the Bloom effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(BloomRenderer), "Unity/Bloom")]
    public sealed class Bloom : PostProcessEffectSettings
    {
        /// <summary>
        /// The strength of the bloom filter.
        /// </summary>
        [Min(0f), Tooltip("Strength of the bloom filter. Values higher than 1 will make bloom contribute more energy to the final render.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        /// <summary>
        /// Filters out pixels under this level of brightness. This value is expressed in
        /// gamma-space.
        /// </summary>
        [Min(0f), Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public FloatParameter threshold = new FloatParameter { value = 1f };

        /// <summary>
        /// Makes transition between under/over-threshold gradual (0 = hard threshold, 1 = soft
        /// threshold).
        /// </summary>
        [Range(0f, 1f), Tooltip("Makes transitions between under/over-threshold gradual. 0 for a hard threshold, 1 for a soft threshold).")]
        public FloatParameter softKnee = new FloatParameter { value = 0.5f };

        /// <summary>
        /// Clamps pixels to control the bloom amount. This value is expressed in gamma-space.
        /// </summary>
        [Tooltip("Clamps pixels to control the bloom amount. Value is in gamma-space.")]
        public FloatParameter clamp = new FloatParameter { value = 65472f };

        /// <summary>
        /// Changes extent of veiling effects in a screen resolution-independent fashion. For
        /// maximum quality stick to integer values. Because this value changes the internal
        /// iteration count, animating it isn't recommended as it may introduce small hiccups in
        /// the perceived radius.
        /// </summary>
        [Range(1f, 10f), Tooltip("Changes the extent of veiling effects. For maximum quality, use integer values. Because this value changes the internal iteration count, You should not animating it as it may introduce issues with the perceived radius.")]
        public FloatParameter diffusion = new FloatParameter { value = 7f };

        /// <summary>
        /// Distorts the bloom to give an anamorphic look. Negative values distort vertically,
        /// positive values distort horizontally.
        /// </summary>
        [Range(-1f, 1f), Tooltip("Distorts the bloom to give an anamorphic look. Negative values distort vertically, positive values distort horizontally.")]
        public FloatParameter anamorphicRatio = new FloatParameter { value = 0f };

        /// <summary>
        /// The tint of the Bloom filter.
        /// </summary>
#if UNITY_2018_1_OR_NEWER
        [ColorUsage(false, true), Tooltip("Global tint of the bloom filter.")]
#else
        [ColorUsage(false, true, 0f, 8f, 0.125f, 3f), Tooltip("Global tint of the bloom filter.")]
#endif
        public ColorParameter color = new ColorParameter { value = Color.white };

        /// <summary>
        /// Boost performances by lowering the effect quality.
        /// </summary>
        [FormerlySerializedAs("mobileOptimized")]
        [Tooltip("Boost performance by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms but can also provide a nice performance boost on desktops and consoles.")]
        public BoolParameter fastMode = new BoolParameter { value = false };

        /// <summary>
        /// The dirtiness texture to add smudges or dust to the lens.
        /// </summary>
        [Tooltip("The lens dirt texture used to add smudges or dust to the bloom effect."), DisplayName("Texture")]
        public TextureParameter dirtTexture = new TextureParameter { value = null };

        /// <summary>
        /// The amount of lens dirtiness.
        /// </summary>
        [Min(0f), Tooltip("The intensity of the lens dirtiness."), DisplayName("Intensity")]
        public FloatParameter dirtIntensity = new FloatParameter { value = 0f };

        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && intensity.value > 0f;
        }
    }

    internal sealed class BloomRenderer : PostProcessEffectRenderer<Bloom>
    {
        enum Pass
        {
            Prefilter13,
            Prefilter4,
            Downsample13,
            Downsample4,
            UpsampleTent,
            UpsampleBox,
            DebugOverlayThreshold,
            DebugOverlayTent,
            DebugOverlayBox
        }

        // [down,up]
        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16; // Just to make sure we handle 64k screens... Future-proof!

        struct Level
        {
            internal int down;
            internal int up;
        }

        public override void Init()
        {
            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BloomMipDown" + i),
                    up = Shader.PropertyToID("_BloomMipUp" + i)
                };
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            cmd.BeginSample("BloomPyramid");

            var sheet = context.propertySheets.Get(context.resources.shaders.bloom);

            // Apply auto exposure adjustment in the prefiltering pass
            sheet.properties.SetTexture(ShaderIDs.AutoExposureTex, context.autoExposureTexture);

            // Negative anamorphic ratio values distort vertically - positive is horizontal
            float ratio = Mathf.Clamp(settings.anamorphicRatio, -1, 1);
            float rw = ratio < 0 ? -ratio : 0f;
            float rh = ratio > 0 ?  ratio : 0f;

            // Do bloom on a half-res buffer, full-res doesn't bring much and kills performances on
            // fillrate limited platforms
            int tw = Mathf.FloorToInt(context.screenWidth / (2f - rw));
            int th = Mathf.FloorToInt(context.screenHeight / (2f - rh));
            bool singlePassDoubleWide = (context.stereoActive && (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) && (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int tw_stereo = singlePassDoubleWide ? tw * 2 : tw; 

            // Determine the iteration count
            int s = Mathf.Max(tw, th);
            float logs = Mathf.Log(s, 2f) + Mathf.Min(settings.diffusion.value, 10f) - 10f;
            int logs_i = Mathf.FloorToInt(logs); 
            int iterations = Mathf.Clamp(logs_i, 1, k_MaxPyramidSize);
            float sampleScale = 0.5f + logs - logs_i;
            sheet.properties.SetFloat(ShaderIDs.SampleScale, sampleScale);

            // Prefiltering parameters
            float lthresh = Mathf.GammaToLinearSpace(settings.threshold.value);
            float knee = lthresh * settings.softKnee.value + 1e-5f;
            var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
            sheet.properties.SetVector(ShaderIDs.Threshold, threshold);
            float lclamp = Mathf.GammaToLinearSpace(settings.clamp.value);
            sheet.properties.SetVector(ShaderIDs.Params, new Vector4(lclamp, 0f, 0f, 0f));

            int qualityOffset = settings.fastMode ? 1 : 0;

            // Downsample
            var lastDown = context.source;
            for (int i = 0; i < iterations; i++)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                int pass = i == 0
                    ? (int)Pass.Prefilter13 + qualityOffset
                    : (int)Pass.Downsample13 + qualityOffset;

                context.GetScreenSpaceTemporaryRT(cmd, mipDown, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
                context.GetScreenSpaceTemporaryRT(cmd, mipUp, 0, context.sourceFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, tw_stereo, th);
                cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);

                lastDown = mipDown;
                tw_stereo = (singlePassDoubleWide && ((tw_stereo / 2) % 2 > 0)) ? 1 + tw_stereo / 2 : tw_stereo / 2;
                tw_stereo = Mathf.Max(tw_stereo, 1);
                th = Mathf.Max(th / 2, 1);
            }

            // Upsample
            int lastUp = m_Pyramid[iterations - 1].down;
            for (int i = iterations - 2; i >= 0; i--)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                cmd.SetGlobalTexture(ShaderIDs.BloomTex, mipDown);
                cmd.BlitFullscreenTriangle(lastUp, mipUp, sheet, (int)Pass.UpsampleTent + qualityOffset);
                lastUp = mipUp;
            }

            var linearColor = settings.color.value.linear;
            float intensity = RuntimeUtilities.Exp2(settings.intensity.value / 10f) - 1f;
            var shaderSettings = new Vector4(sampleScale, intensity, settings.dirtIntensity.value, iterations);

            // Debug overlays
            if (context.IsDebugOverlayEnabled(DebugOverlay.BloomThreshold))
            {
                context.PushDebugOverlay(cmd, context.source, sheet, (int)Pass.DebugOverlayThreshold);
            }
            else if (context.IsDebugOverlayEnabled(DebugOverlay.BloomBuffer))
            {
                sheet.properties.SetVector(ShaderIDs.ColorIntensity, new Vector4(linearColor.r, linearColor.g, linearColor.b, intensity));
                context.PushDebugOverlay(cmd, m_Pyramid[0].up, sheet, (int)Pass.DebugOverlayTent + qualityOffset);
            }

            // Lens dirtiness
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = settings.dirtTexture.value == null
                ? RuntimeUtilities.blackTexture
                : settings.dirtTexture.value;

            var dirtRatio = (float)dirtTexture.width / (float)dirtTexture.height;
            var screenRatio = (float)context.screenWidth / (float)context.screenHeight;
            var dirtTileOffset = new Vector4(1f, 1f, 0f, 0f);

            if (dirtRatio > screenRatio)
            {
                dirtTileOffset.x = screenRatio / dirtRatio;
                dirtTileOffset.z = (1f - dirtTileOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtTileOffset.y = dirtRatio / screenRatio;
                dirtTileOffset.w = (1f - dirtTileOffset.y) * 0.5f;
            }

            // Shader properties
            var uberSheet = context.uberSheet;
            if (settings.fastMode)
                uberSheet.EnableKeyword("BLOOM_LOW");
            else
                uberSheet.EnableKeyword("BLOOM");
            uberSheet.properties.SetVector(ShaderIDs.Bloom_DirtTileOffset, dirtTileOffset);
            uberSheet.properties.SetVector(ShaderIDs.Bloom_Settings, shaderSettings);
            uberSheet.properties.SetColor(ShaderIDs.Bloom_Color, linearColor);
            uberSheet.properties.SetTexture(ShaderIDs.Bloom_DirtTex, dirtTexture);
            cmd.SetGlobalTexture(ShaderIDs.BloomTex, lastUp);

            // Cleanup
            for (int i = 0; i < iterations; i++)
            {
                if (m_Pyramid[i].down != lastUp)
                    cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                if (m_Pyramid[i].up != lastUp)
                    cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            cmd.EndSample("BloomPyramid");

            context.bloomBufferNameID = lastUp;
        }
    }
}
