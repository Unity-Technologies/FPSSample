using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Chromatic Aberration effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(ChromaticAberrationRenderer), "Unity/Chromatic Aberration")]
    public sealed class ChromaticAberration : PostProcessEffectSettings
    {
        /// <summary>
        /// A texture used for custom fringing color (it will use a default one when <c>null</c>).
        /// </summary>
        [Tooltip("Shifts the hue of chromatic aberrations.")]
        public TextureParameter spectralLut = new TextureParameter { value = null };

        /// <summary>
        /// The amount of tangential distortion.
        /// </summary>
        [Range(0f, 1f), Tooltip("Amount of tangential distortion.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        /// <summary>
        /// If <c>true</c>, it will use a faster variant of the effect for improved performances.
        /// </summary>
        [FormerlySerializedAs("mobileOptimized")]
        [Tooltip("Boost performances by lowering the effect quality. This settings is meant to be used on mobile and other low-end platforms but can also provide a nice performance boost on desktops and consoles.")]
        public BoolParameter fastMode = new BoolParameter { value = false };
        
        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && intensity.value > 0f;
        }
    }

    internal sealed class ChromaticAberrationRenderer : PostProcessEffectRenderer<ChromaticAberration>
    {
        Texture2D m_InternalSpectralLut;

        public override void Render(PostProcessRenderContext context)
        {
            var spectralLut = settings.spectralLut.value;

            if (spectralLut == null)
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, TextureFormat.RGB24, false)
                    {
                        name = "Chromatic Aberration Spectrum Lookup",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new []
                    {
                        new Color(1f, 0f, 0f),
                        new Color(0f, 1f, 0f),
                        new Color(0f, 0f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }
            
            var sheet = context.uberSheet;
            bool fastMode = settings.fastMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2;

            sheet.EnableKeyword(fastMode
                ? "CHROMATIC_ABERRATION_LOW"
                : "CHROMATIC_ABERRATION"
            );
            sheet.properties.SetFloat(ShaderIDs.ChromaticAberration_Amount, settings.intensity * 0.05f);
            sheet.properties.SetTexture(ShaderIDs.ChromaticAberration_SpectralLut, spectralLut);
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_InternalSpectralLut);
            m_InternalSpectralLut = null;
        }
    }
}
