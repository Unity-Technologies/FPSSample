using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Lens Distortion effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(LensDistortionRenderer), "Unity/Lens Distortion")]
    public sealed class LensDistortion : PostProcessEffectSettings
    {
        /// <summary>
        /// The total amount of distortion to apply.
        /// </summary>
        [Range(-100f, 100f), Tooltip("Total distortion amount.")]
        public FloatParameter intensity = new FloatParameter { value = 0f };

        /// <summary>
        /// Multiplies the intensity value on the x-axis. Setting this value to 0 will disable distortion on this axis.
        /// </summary>
        [Range(0f, 1f), DisplayName("X Multiplier"), Tooltip("Intensity multiplier on the x-axis. Set it to 0 to disable distortion on this axis.")]
        public FloatParameter intensityX = new FloatParameter { value = 1f };

        /// <summary>
        /// Multiplies the intensity value on the y-axis. Setting this value to 0 will disable distortion on this axis.
        /// </summary>
        [Range(0f, 1f), DisplayName("Y Multiplier"), Tooltip("Intensity multiplier on the y-axis. Set it to 0 to disable distortion on this axis.")]
        public FloatParameter intensityY = new FloatParameter { value = 1f };

        /// <summary>
        /// The center point for the distortion (x-axis).
        /// </summary>
        [Space]
        [Range(-1f, 1f), Tooltip("Distortion center point (x-axis).")]
        public FloatParameter centerX = new FloatParameter { value = 0f };

        /// <summary>
        /// The center point for the distortion (y-axis).
        /// </summary>
        [Range(-1f, 1f), Tooltip("Distortion center point (y-axis).")]
        public FloatParameter centerY = new FloatParameter { value = 0f };

        /// <summary>
        /// A global screen scaling factor.
        /// </summary>
        [Space]
        [Range(0.01f, 5f), Tooltip("Global screen scaling.")]
        public FloatParameter scale = new FloatParameter { value = 1f };

        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && !Mathf.Approximately(intensity, 0f)
                && (intensityX > 0f || intensityY > 0f)
                && !RuntimeUtilities.isVREnabled;
        }
    }

    internal sealed class LensDistortionRenderer : PostProcessEffectRenderer<LensDistortion>
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
