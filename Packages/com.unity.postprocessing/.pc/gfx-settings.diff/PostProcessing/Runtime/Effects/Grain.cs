using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Grain effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(GrainRenderer), "Unity/Grain")]
    public sealed class Grain : PostProcessEffectSettings
    {
        /// <summary>
        /// Set to <c>true</c> to render colored grain, <c>false</c> for grayscale grain.
        /// </summary>
        [Tooltip("Enable the use of colored grain.")]
        public BoolParameter colored = new BoolParameter { value = true };

        /// <summary>
        /// The strength (or visibility) of the Grain effect on screen. Higher values mean more visible grain.
        /// </summary>
        [Range(0f, 1f), Tooltip("Grain strength. Higher values mean more visible grain.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        /// <summary>
        /// The size of grain particle on screen.
        /// </summary>
        [Range(0.3f, 3f), Tooltip("Grain particle size.")]
        public FloatParameter size = new FloatParameter { value = 1f };

        /// <summary>
        /// Controls the noisiness response curve based on scene luminance. Lower values mean less noise in dark areas.
        /// </summary>
        [Range(0f, 1f), DisplayName("Luminance Contribution"), Tooltip("Controls the noise response curve based on scene luminance. Lower values mean less noise in dark areas.")]
        public FloatParameter lumContrib = new FloatParameter { value = 0.8f };
        
        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && intensity.value > 0f;
        }
    }

#if POSTFX_DEBUG_STATIC_GRAIN
    #pragma warning disable 414
#endif

    internal sealed class GrainRenderer : PostProcessEffectRenderer<Grain>
    {
        RenderTexture m_GrainLookupRT;

        const int k_SampleCount = 1024;
        int m_SampleIndex;

        public override void Render(PostProcessRenderContext context)
        {
#if POSTFX_DEBUG_STATIC_GRAIN
            // Chosen by a fair dice roll
            float time = 0.4f;
            float rndOffsetX = 0f;
            float rndOffsetY = 0f;
#else
            float time = Time.realtimeSinceStartup;
            float rndOffsetX = HaltonSeq.Get(m_SampleIndex & 1023, 2);
            float rndOffsetY = HaltonSeq.Get(m_SampleIndex & 1023, 3);

            if (++m_SampleIndex >= k_SampleCount)
                m_SampleIndex = 0;
#endif

            // Generate the grain lut for the current frame first
            if (m_GrainLookupRT == null || !m_GrainLookupRT.IsCreated())
            {
                RuntimeUtilities.Destroy(m_GrainLookupRT);

                m_GrainLookupRT = new RenderTexture(128, 128, 0, GetLookupFormat())
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Repeat,
                    anisoLevel = 0,
                    name = "Grain Lookup Texture"
                };

                m_GrainLookupRT.Create();
            }

            var sheet = context.propertySheets.Get(context.resources.shaders.grainBaker);
            sheet.properties.Clear();
            sheet.properties.SetFloat(ShaderIDs.Phase, time % 10f);
            sheet.properties.SetVector(ShaderIDs.GrainNoiseParameters, new Vector3(12.9898f, 78.233f, 43758.5453f));

            context.command.BeginSample("GrainLookup");
            context.command.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_GrainLookupRT, sheet, settings.colored.value ? 1 : 0);
            context.command.EndSample("GrainLookup");

            // Send everything to the uber shader
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("GRAIN");
            uberSheet.properties.SetTexture(ShaderIDs.GrainTex, m_GrainLookupRT);
            uberSheet.properties.SetVector(ShaderIDs.Grain_Params1, new Vector2(settings.lumContrib.value, settings.intensity.value * 20f));
            uberSheet.properties.SetVector(ShaderIDs.Grain_Params2, new Vector4((float)context.width / (float)m_GrainLookupRT.width / settings.size.value, (float)context.height / (float)m_GrainLookupRT.height / settings.size.value, rndOffsetX, rndOffsetY));
        }

        RenderTextureFormat GetLookupFormat()
        {
            if (RenderTextureFormat.ARGBHalf.IsSupported())
                return RenderTextureFormat.ARGBHalf;

            return RenderTextureFormat.ARGB32;
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_GrainLookupRT);
            m_GrainLookupRT = null;
            m_SampleIndex = 0;
        }
    }
    
#if POSTFX_DEBUG_STATIC_GRAIN
    #pragma warning restore 414
#endif
}
