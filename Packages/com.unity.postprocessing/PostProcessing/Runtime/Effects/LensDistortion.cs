using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    [PostProcess(typeof(LensDistortionRenderer), "Unity/Lens Distortion")]
    public sealed class LensDistortion : PostProcessEffectSettings
    {
        [Range(-100f, 100f), Tooltip("Total distortion amount.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        [Range(0f, 1f), DisplayName("Y Multiplier"), Tooltip("Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis.")]
        public FloatParameter intensityX = new FloatParameter { value = 1f };

        [Range(0f, 1f), DisplayName("X Multiplier"), Tooltip("Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis.")]
        public FloatParameter intensityY = new FloatParameter { value = 1f };

        [Space]
        [Range(-1f, 1f), Tooltip("Distortion center point (X axis).")]
        public FloatParameter centerX = new FloatParameter { value = 0f };

        [Range(-1f, 1f), Tooltip("Distortion center point (Y axis).")]
        public FloatParameter centerY = new FloatParameter { value = 0f };

        [Space]
        [Range(0.01f, 5f), Tooltip("Global screen scaling.")]
        public FloatParameter scale = new FloatParameter { value = 1f };

        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && !Mathf.Approximately(intensity, 0f)
                && (intensityX > 0f || intensityY > 0f)
                && !RuntimeUtilities.isVREnabled;
        }
    }

    public sealed class LensDistortionRenderer : PostProcessEffectRenderer<LensDistortion>
    {
        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.uberSheet;
            
            float amount = 1.6f * Math.Max(Mathf.Abs(settings.intensity.value), 1f);
            float theta = Mathf.Deg2Rad * Math.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var p0 = new Vector4(settings.centerX.value, settings.centerY.value, Mathf.Max(settings.intensityX.value, 1e-4f), Mathf.Max(settings.intensityY.value, 1e-4f));
            var p1 = new Vector4(settings.intensity.value >= 0f ? theta : 1f / theta, sigma, 1f / settings.scale.value, settings.intensity.value);

            sheet.EnableKeyword("DISTORT");
            sheet.properties.SetVector(ShaderIDs.Distortion_CenterScale, p0);
            sheet.properties.SetVector(ShaderIDs.Distortion_Amount, p1);
        }
    }
}
