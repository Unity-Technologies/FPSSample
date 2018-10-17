using System;

namespace UnityEngine.Rendering.PostProcessing
{
    public enum VignetteMode
    {
        Classic,
        Masked
    }

    [Serializable]
    public sealed class VignetteModeParameter : ParameterOverride<VignetteMode> {}

    [Serializable]
    [PostProcess(typeof(VignetteRenderer), "Unity/Vignette")]
    public sealed class Vignette : PostProcessEffectSettings
    {
        [Tooltip("Use the \"Classic\" mode for parametric controls. Use the \"Masked\" mode to use your own texture mask.")]
        public VignetteModeParameter mode = new VignetteModeParameter { value = VignetteMode.Classic };

        [Tooltip("Vignette color. Use the alpha channel for transparency.")]
        public ColorParameter color = new ColorParameter { value = new Color(0f, 0f, 0f, 1f) };

        [Tooltip("Sets the vignette center point (screen center is [0.5,0.5]).")]
        public Vector2Parameter center = new Vector2Parameter { value = new Vector2(0.5f, 0.5f) };

        [Range(0f, 1f), Tooltip("Amount of vignetting on screen.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        [Range(0.01f, 1f), Tooltip("Smoothness of the vignette borders.")]
        public FloatParameter smoothness = new FloatParameter { value = 0.2f };

        [Range(0f, 1f), Tooltip("Lower values will make a square-ish vignette.")]
        public FloatParameter roundness = new FloatParameter { value = 1f };

        [Tooltip("Should the vignette be perfectly round or be dependent on the current aspect ratio?")]
        public BoolParameter rounded = new BoolParameter { value = false };

        [Tooltip("A black and white mask to use as a vignette.")]
        public TextureParameter mask = new TextureParameter { value = null };

        [Range(0f, 1f), Tooltip("Mask opacity.")]
        public FloatParameter opacity = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && ((mode.value == VignetteMode.Classic && intensity.value > 0f)
                ||  (mode.value == VignetteMode.Masked && opacity.value > 0f && mask.value != null));
        }
    }

    public sealed class VignetteRenderer : PostProcessEffectRenderer<Vignette>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.uberSheet;
            sheet.EnableKeyword("VIGNETTE");
            sheet.properties.SetColor(ShaderIDs.Vignette_Color, settings.color.value);

            if (settings.mode == VignetteMode.Classic)
            {
                sheet.properties.SetFloat(ShaderIDs.Vignette_Mode, 0f);
                sheet.properties.SetVector(ShaderIDs.Vignette_Center, settings.center.value);
                float roundness = (1f - settings.roundness.value) * 6f + settings.roundness.value;
                sheet.properties.SetVector(ShaderIDs.Vignette_Settings, new Vector4(settings.intensity.value * 3f, settings.smoothness.value * 5f, roundness, settings.rounded.value ? 1f : 0f));
            }
            else // Masked
            {
                sheet.properties.SetFloat(ShaderIDs.Vignette_Mode, 1f);
                sheet.properties.SetTexture(ShaderIDs.Vignette_Mask, settings.mask.value);
                sheet.properties.SetFloat(ShaderIDs.Vignette_Opacity, Mathf.Clamp01(settings.opacity.value));
            }
        }
    }
}
