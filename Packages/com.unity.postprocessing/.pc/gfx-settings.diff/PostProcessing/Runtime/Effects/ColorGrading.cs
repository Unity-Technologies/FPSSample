using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Color grading modes.
    /// </summary>
    public enum GradingMode
    {
        /// <summary>
        /// This mode is aimed at lower-end platforms but it can be used on any platform. Grading is
        /// applied to the final rendered frame clamped in a [0,1] range and stored in a standard
        /// LUT.
        /// </summary>
        LowDefinitionRange,

        /// <summary>
        /// This mode is aimed at platforms that support HDR rendering. All the color operations
        /// will be applied in HDR and stored into a 3D log-encoded LUT to ensure a sufficient range
        /// coverage and precision (Alexa LogC El1000).
        /// </summary>
        HighDefinitionRange,

        /// <summary>
        /// This mode allows you to provide a custom 3D LUT authored in an external software. 
        /// </summary>
        External
    }

    /// <summary>
    /// Tonemapping methods.
    /// </summary>
    public enum Tonemapper
    {
        /// <summary>
        /// No tonemapping will be applied.
        /// </summary>
        None,

        /// <summary>
        /// This method only does range-remapping with minimal impact on color hue & saturation and
        /// is generally a great starting point for extensive color grading.
        /// </summary>
        Neutral,

        /// <summary>
        /// This method uses a close approximation of the reference ACES tonemapper for a more
        /// filmic look. Because of that, it is more contrasted than <see cref="Neutral"/>and has an
        /// effect on actual color hue & saturation. Note that if you enable this tonemapper all the
        /// grading operations will be done in the ACES color spaces for optimal precision and
        /// results.
        /// </summary>
        ACES,

        /// <summary>
        /// This method offers a fully parametric, artist-friendly tonemapper.
        /// </summary>
        Custom
    }

    /// <summary>
    /// A volume parameter holding a <see cref="GradingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class GradingModeParameter : ParameterOverride<GradingMode> { }

    /// <summary>
    /// A volume parameter holding a <see cref="Tonemapper"/> value.
    /// </summary>
    [Serializable]
    public sealed class TonemapperParameter : ParameterOverride<Tonemapper> {}

    /// <summary>
    /// This class holds settings for the Color Grading effect.
    /// </summary>
    // TODO: Could use some refactoring, too much duplicated code here
    [Serializable]
    [PostProcess(typeof(ColorGradingRenderer), "Unity/Color Grading")]
    public sealed class ColorGrading : PostProcessEffectSettings
    {
        /// <summary>
        /// The grading mode to use.
        /// </summary>
        [DisplayName("Mode"), Tooltip("Select a color grading mode that fits your dynamic range and workflow. Use HDR if your camera is set to render in HDR and your target platform supports it. Use LDR for low-end mobiles or devices that don't support HDR. Use External if you prefer authoring a Log LUT in an external software.")]
        public GradingModeParameter gradingMode = new GradingModeParameter { value = GradingMode.HighDefinitionRange };

        /// <summary>
        /// A custom 3D log-encoded texture.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.External"/>.
        /// </remarks>
        [DisplayName("Lookup Texture"), Tooltip("A custom 3D log-encoded texture.")]
        public TextureParameter externalLut = new TextureParameter { value = null };

        /// <summary>
        /// The tonemapping algorithm to use at the end of the color grading process.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.HighDefinitionRange"/>.
        /// </remarks>
        [DisplayName("Mode"), Tooltip("Select a tonemapping algorithm to use at the end of the color grading process.")]
        public TonemapperParameter tonemapper = new TonemapperParameter { value = Tonemapper.None };

        /// <summary>
        /// Affects the transition between the toe and the mid section of the curve. A value of 0
        /// means no toe, a value of 1 means a very hard transition.
        /// </summary>
        /// <remarks>
        /// This is only used when <see cref="Tonemapper.Custom"/> is active.
        /// </remarks>
        [DisplayName("Toe Strength"), Range(0f, 1f), Tooltip("Affects the transition between the toe and the mid section of the curve. A value of 0 means no toe, a value of 1 means a very hard transition.")]
        public FloatParameter toneCurveToeStrength = new FloatParameter { value = 0f };

        /// <summary>
        /// Affects how much of the dynamic range is in the toe. With a small value, the toe will be
        /// very short and quickly transition into the linear section, and with a longer value
        /// having a longer toe.
        /// </summary>
        /// <remarks>
        /// This is only used when <see cref="Tonemapper.Custom"/> is active.
        /// </remarks>
        [DisplayName("Toe Length"), Range(0f, 1f), Tooltip("Affects how much of the dynamic range is in the toe. With a small value, the toe will be very short and quickly transition into the linear section, with a larger value, the toe will be longer.")]
        public FloatParameter toneCurveToeLength = new FloatParameter { value = 0.5f };

        /// <summary>
        /// Affects the transition between the mid section and the shoulder of the curve. A value of
        /// 0 means no shoulder, value of 1 means a very hard transition.
        /// </summary>
        /// <remarks>
        /// This is only used when <see cref="Tonemapper.Custom"/> is active.
        /// </remarks>
        [DisplayName("Shoulder Strength"), Range(0f, 1f), Tooltip("Affects the transition between the mid section and the shoulder of the curve. A value of 0 means no shoulder, a value of 1 means a very hard transition.")]
        public FloatParameter toneCurveShoulderStrength = new FloatParameter { value = 0f };

        /// <summary>
        /// Affects how many F-stops (EV) to add to the dynamic range of the curve.
        /// </summary>
        /// <remarks>
        /// This is only used when <see cref="Tonemapper.Custom"/> is active.
        /// </remarks>
        [DisplayName("Shoulder Length"), Min(0f), Tooltip("Affects how many F-stops (EV) to add to the dynamic range of the curve.")]
        public FloatParameter toneCurveShoulderLength = new FloatParameter { value = 0.5f };

        /// <summary>
        /// Affects how much overshot to add to the shoulder.
        /// </summary>
        /// <remarks>
        /// This is only used when <see cref="Tonemapper.Custom"/> is active.
        /// </remarks>
        [DisplayName("Shoulder Angle"), Range(0f, 1f), Tooltip("Affects how much overshoot to add to the shoulder.")]
        public FloatParameter toneCurveShoulderAngle = new FloatParameter { value = 0f };

        /// <summary>
        /// Applies a gamma function to the curve.
        /// </summary>
        /// <remarks>
        /// This is only used when <see cref="Tonemapper.Custom"/> is active.
        /// </remarks>
        [DisplayName("Gamma"), Min(0.001f), Tooltip("Applies a gamma function to the curve.")]
        public FloatParameter toneCurveGamma = new FloatParameter { value = 1f };

        /// <summary>
        /// A custom lookup texture (strip format, e.g. 256x16) to apply before the rest of the
        /// color grading operators. If none is provided, a neutral one will be generated
        /// internally.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        [DisplayName("Lookup Texture"), Tooltip("Custom lookup texture (strip format, for example 256x16) to apply before the rest of the color grading operators. If none is provided, a neutral one will be generated internally.")]
        public TextureParameter ldrLut = new TextureParameter { value = null, defaultState = TextureParameterDefault.Lut2D }; // LDR only

        /// <summary>
        /// How much of the lookup texture will contribute to the color grading.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        [DisplayName("Contribution"), Range(0f, 1f), Tooltip("How much of the lookup texture will contribute to the color grading effect.")]
        public FloatParameter ldrLutContribution = new FloatParameter { value = 1f };

        /// <summary>
        /// Sets the white balance to a custom color temperature.
        /// </summary>
        [DisplayName("Temperature"), Range(-100f, 100f), Tooltip("Sets the white balance to a custom color temperature.")]
        public FloatParameter temperature = new FloatParameter { value = 0f };

        /// <summary>
        /// Sets the white balance to compensate for a green or magenta tint.
        /// </summary>
        [DisplayName("Tint"), Range(-100f, 100f), Tooltip("Sets the white balance to compensate for a green or magenta tint.")]
        public FloatParameter tint = new FloatParameter { value = 0f };

        /// <summary>
        /// Tints the render by multiplying a color.
        /// </summary>
#if UNITY_2018_1_OR_NEWER
        [DisplayName("Color Filter"), ColorUsage(false, true), Tooltip("Tint the render by multiplying a color.")]
#else
        [DisplayName("Color Filter"), ColorUsage(false, true, 0f, 8f, 0.125f, 3f), Tooltip("Tint the render by multiplying a color.")]
#endif
        public ColorParameter colorFilter = new ColorParameter { value = Color.white };

        /// <summary>
        /// Shifts the hue of all colors.
        /// </summary>
        [DisplayName("Hue Shift"), Range(-180f, 180f), Tooltip("Shift the hue of all colors.")]
        public FloatParameter hueShift = new FloatParameter { value = 0f };

        /// <summary>
        /// Pushes the intensity of all colors.
        /// </summary>
        [DisplayName("Saturation"), Range(-100f, 100f), Tooltip("Pushes the intensity of all colors.")]
        public FloatParameter saturation = new FloatParameter { value = 0f };

        /// <summary>
        /// Makes the image brighter or darker.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        [DisplayName("Brightness"), Range(-100f, 100f), Tooltip("Makes the image brighter or darker.")]
        public FloatParameter brightness = new FloatParameter { value = 0f }; // LDR only

        /// <summary>
        /// Adjusts the overall exposure of the scene in EV units. This is applied after HDR effect
        /// and right before tonemapping so it won’t affect previous effects in the chain.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.HighDefinitionRange"/>.
        /// </remarks>
        [DisplayName("Post-exposure (EV)"), Tooltip("Adjusts the overall exposure of the scene in EV units. This is applied after the HDR effect and right before tonemapping so it won't affect previous effects in the chain.")]
        public FloatParameter postExposure = new FloatParameter { value = 0f }; // HDR only

        /// <summary>
        /// Expands or shrinks the overall range of tonal values.
        /// </summary>
        [DisplayName("Contrast"), Range(-100f, 100f), Tooltip("Expands or shrinks the overall range of tonal values.")]
        public FloatParameter contrast = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the red channel within the overall mix.
        /// </summary>
        [DisplayName("Red"), Range(-200f, 200f), Tooltip("Modify influence of the red channel in the overall mix.")]
        public FloatParameter mixerRedOutRedIn = new FloatParameter { value = 100f };

        /// <summary>
        /// Modifies the influence of the green channel within the overall mix.
        /// </summary>
        [DisplayName("Green"), Range(-200f, 200f), Tooltip("Modify influence of the green channel in the overall mix.")]
        public FloatParameter mixerRedOutGreenIn = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the blue channel within the overall mix.
        /// </summary>
        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("Modify influence of the blue channel in the overall mix.")]
        public FloatParameter mixerRedOutBlueIn = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the red channel within the overall mix.
        /// </summary>
        [DisplayName("Red"), Range(-200f, 200f), Tooltip("Modify influence of the red channel in the overall mix.")]
        public FloatParameter mixerGreenOutRedIn = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the green channel within the overall mix.
        /// </summary>
        [DisplayName("Green"), Range(-200f, 200f), Tooltip("Modify influence of the green channel in the overall mix.")]
        public FloatParameter mixerGreenOutGreenIn = new FloatParameter { value = 100f };

        /// <summary>
        /// Modifies the influence of the blue channel within the overall mix.
        /// </summary>
        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("Modify influence of the blue channel in the overall mix.")]
        public FloatParameter mixerGreenOutBlueIn = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the red channel within the overall mix.
        /// </summary>
        [DisplayName("Red"), Range(-200f, 200f), Tooltip("Modify influence of the red channel in the overall mix.")]
        public FloatParameter mixerBlueOutRedIn = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the green channel within the overall mix.
        /// </summary>
        [DisplayName("Green"), Range(-200f, 200f), Tooltip("Modify influence of the green channel in the overall mix.")]
        public FloatParameter mixerBlueOutGreenIn = new FloatParameter { value = 0f };

        /// <summary>
        /// Modifies the influence of the blue channel within the overall mix.
        /// </summary>
        [DisplayName("Blue"), Range(-200f, 200f), Tooltip("Modify influence of the blue channel in the overall mix.")]
        public FloatParameter mixerBlueOutBlueIn = new FloatParameter { value = 100f };

        /// <summary>
        /// Controls the darkest portions of the render.
        /// </summary>
        /// <remarks>
        /// The neutral value is <c>(1, 1, 1, 0)</c>.
        /// </remarks>
        [DisplayName("Lift"), Tooltip("Controls the darkest portions of the render."), Trackball(TrackballAttribute.Mode.Lift)]
        public Vector4Parameter lift = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };

        /// <summary>
        /// A power function that controls mid-range tones.
        /// </summary>
        /// <remarks>
        /// The neutral value is <c>(1, 1, 1, 0)</c>.
        /// </remarks>
        [DisplayName("Gamma"), Tooltip("Power function that controls mid-range tones."), Trackball(TrackballAttribute.Mode.Gamma)]
        public Vector4Parameter gamma = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };

        /// <summary>
        /// Controls the lightest portions of the render.
        /// </summary>
        /// <remarks>
        /// The neutral value is <c>(1, 1, 1, 0)</c>.
        /// </remarks>
        [DisplayName("Gain"), Tooltip("Controls the lightest portions of the render."), Trackball(TrackballAttribute.Mode.Gain)]
        public Vector4Parameter gain = new Vector4Parameter { value = new Vector4(1f, 1f, 1f, 0f) };

        /// <summary>
        /// Remaps the luminosity values.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        public SplineParameter masterCurve   = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the red channel.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        public SplineParameter redCurve      = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the green channel/
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        public SplineParameter greenCurve    = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the blue channel.
        /// </summary>
        /// <remarks>
        /// This is only used when working with <see cref="GradingMode.LowDefinitionRange"/>.
        /// </remarks>
        public SplineParameter blueCurve     = new SplineParameter { value = new Spline(new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f)), 0f, false, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the hue according to the current hue.
        /// </summary>
        public SplineParameter hueVsHueCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the saturation according to the current hue.
        /// </summary>
        public SplineParameter hueVsSatCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, true, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the saturation according to the current saturation.
        /// </summary>
        public SplineParameter satVsSatCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };

        /// <summary>
        /// Remaps the saturation according to the current luminance.
        /// </summary>
        public SplineParameter lumVsSatCurve = new SplineParameter { value = new Spline(new AnimationCurve(), 0.5f, false, new Vector2(0f, 1f)) };

        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            if (gradingMode.value == GradingMode.External)
            {
                if (!SystemInfo.supports3DRenderTextures || !SystemInfo.supportsComputeShaders)
                    return false;
            }

            return enabled.value;
        }
    }

    internal sealed class ColorGradingRenderer : PostProcessEffectRenderer<ColorGrading>
    {
        enum Pass
        {
            LutGenLDRFromScratch,
            LutGenLDR,
            LutGenHDR2D
        }

        Texture2D m_GradingCurves;
        readonly Color[] m_Pixels = new Color[Spline.k_Precision * 2]; // Avoids GC stress

        RenderTexture m_InternalLdrLut;
        RenderTexture m_InternalLogLut;
        const int k_Lut2DSize = 32;
        const int k_Lut3DSize = 33;

        readonly HableCurve m_HableCurve = new HableCurve();

        public override void Render(PostProcessRenderContext context)
        {
            var gradingMode = settings.gradingMode.value;
            var supportComputeTex3D = SystemInfo.supports3DRenderTextures
                && SystemInfo.supportsComputeShaders
                && context.resources.computeShaders.lut3DBaker != null
                && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore
                && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3;

            if (gradingMode == GradingMode.External)
                RenderExternalPipeline3D(context);
            else if (gradingMode == GradingMode.HighDefinitionRange && supportComputeTex3D)
                RenderHDRPipeline3D(context);
            else if (gradingMode == GradingMode.HighDefinitionRange)
                RenderHDRPipeline2D(context);
            else
                RenderLDRPipeline2D(context);
        }

        // Do color grading using an externally authored 3D lut; it requires Texture3D support and
        // compute shaders in case blending is required - Desktop / Consoles / Some high-end mobiles
        void RenderExternalPipeline3D(PostProcessRenderContext context)
        {
            var lut = settings.externalLut.value;

            if (lut == null)
                return;

            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR_3D");
            uberSheet.properties.SetTexture(ShaderIDs.Lut3D, lut);
            uberSheet.properties.SetVector(ShaderIDs.Lut3D_Params, new Vector2(1f / lut.width, lut.width - 1f));
            uberSheet.properties.SetFloat(ShaderIDs.PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));
            context.logLut = lut;
        }

        // HDR color pipeline is rendered to a 3D lut; it requires Texture3D & compute shaders
        // support - Desktop / Consoles / Some high-end mobiles
        // TODO: Use ShaderIDs for compute once the compatible APIs go in
        void RenderHDRPipeline3D(PostProcessRenderContext context)
        {
            // Unfortunately because AnimationCurve doesn't implement GetHashCode and we don't have
            // any reliable way to figure out if a curve data is different from another one we can't
            // skip regenerating the Lut if nothing has changed. So it has to be done on every
            // frame...
            // It's not a very expensive operation anyway (we're talking about filling a 33x33x33
            // Lut on the GPU) but every little thing helps, especially on mobile.
            {
                CheckInternalLogLut();

                // Lut setup
                var compute = context.resources.computeShaders.lut3DBaker;
                int kernel = 0;

                switch (settings.tonemapper.value)
                {
                    case Tonemapper.None: kernel = compute.FindKernel("KGenLut3D_NoTonemap");
                        break;
                    case Tonemapper.Neutral: kernel = compute.FindKernel("KGenLut3D_NeutralTonemap");
                        break;
                    case Tonemapper.ACES: kernel = compute.FindKernel("KGenLut3D_AcesTonemap");
                        break;
                    case Tonemapper.Custom: kernel = compute.FindKernel("KGenLut3D_CustomTonemap");
                        break;
                }

                var cmd = context.command;
                cmd.SetComputeTextureParam(compute, kernel, "_Output", m_InternalLogLut);
                cmd.SetComputeVectorParam(compute, "_Size", new Vector4(k_Lut3DSize, 1f / (k_Lut3DSize - 1f), 0f, 0f));

                var colorBalance = ColorUtilities.ComputeColorBalance(settings.temperature.value, settings.tint.value);
                cmd.SetComputeVectorParam(compute, "_ColorBalance", colorBalance);
                cmd.SetComputeVectorParam(compute, "_ColorFilter", settings.colorFilter.value);

                float hue = settings.hueShift.value / 360f;         // Remap to [-0.5;0.5]
                float sat = settings.saturation.value / 100f + 1f;  // Remap to [0;2]
                float con = settings.contrast.value / 100f + 1f;    // Remap to [0;2]
                cmd.SetComputeVectorParam(compute, "_HueSatCon", new Vector4(hue, sat, con, 0f));

                var channelMixerR = new Vector4(settings.mixerRedOutRedIn, settings.mixerRedOutGreenIn, settings.mixerRedOutBlueIn, 0f);
                var channelMixerG = new Vector4(settings.mixerGreenOutRedIn, settings.mixerGreenOutGreenIn, settings.mixerGreenOutBlueIn, 0f);
                var channelMixerB = new Vector4(settings.mixerBlueOutRedIn, settings.mixerBlueOutGreenIn, settings.mixerBlueOutBlueIn, 0f);
                cmd.SetComputeVectorParam(compute, "_ChannelMixerRed", channelMixerR / 100f); // Remap to [-2;2]
                cmd.SetComputeVectorParam(compute, "_ChannelMixerGreen", channelMixerG / 100f);
                cmd.SetComputeVectorParam(compute, "_ChannelMixerBlue", channelMixerB / 100f);

                var lift = ColorUtilities.ColorToLift(settings.lift.value * 0.2f);
                var gain = ColorUtilities.ColorToGain(settings.gain.value * 0.8f);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value * 0.8f);
                cmd.SetComputeVectorParam(compute, "_Lift", new Vector4(lift.x, lift.y, lift.z, 0f));
                cmd.SetComputeVectorParam(compute, "_InvGamma", new Vector4(invgamma.x, invgamma.y, invgamma.z, 0f));
                cmd.SetComputeVectorParam(compute, "_Gain", new Vector4(gain.x, gain.y, gain.z, 0f));

                cmd.SetComputeTextureParam(compute, kernel, "_Curves", GetCurveTexture(true));

                if (settings.tonemapper.value == Tonemapper.Custom)
                {
                    m_HableCurve.Init(
                        settings.toneCurveToeStrength.value,
                        settings.toneCurveToeLength.value,
                        settings.toneCurveShoulderStrength.value,
                        settings.toneCurveShoulderLength.value,
                        settings.toneCurveShoulderAngle.value,
                        settings.toneCurveGamma.value
                    );

                    cmd.SetComputeVectorParam(compute, "_CustomToneCurve", m_HableCurve.uniforms.curve);
                    cmd.SetComputeVectorParam(compute, "_ToeSegmentA", m_HableCurve.uniforms.toeSegmentA);
                    cmd.SetComputeVectorParam(compute, "_ToeSegmentB", m_HableCurve.uniforms.toeSegmentB);
                    cmd.SetComputeVectorParam(compute, "_MidSegmentA", m_HableCurve.uniforms.midSegmentA);
                    cmd.SetComputeVectorParam(compute, "_MidSegmentB", m_HableCurve.uniforms.midSegmentB);
                    cmd.SetComputeVectorParam(compute, "_ShoSegmentA", m_HableCurve.uniforms.shoSegmentA);
                    cmd.SetComputeVectorParam(compute, "_ShoSegmentB", m_HableCurve.uniforms.shoSegmentB);
                }

                // Generate the lut
                context.command.BeginSample("HdrColorGradingLut3D");
                int groupSize = Mathf.CeilToInt(k_Lut3DSize / 4f);
                cmd.DispatchCompute(compute, kernel, groupSize, groupSize, groupSize);
                context.command.EndSample("HdrColorGradingLut3D");
            }

            var lut = m_InternalLogLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR_3D");
            uberSheet.properties.SetTexture(ShaderIDs.Lut3D, lut);
            uberSheet.properties.SetVector(ShaderIDs.Lut3D_Params, new Vector2(1f / lut.width, lut.width - 1f));
            uberSheet.properties.SetFloat(ShaderIDs.PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));

            context.logLut = lut;
        }

        // HDR color pipeline is rendered to a 2D strip lut (works on HDR platforms without compute
        // and 3D texture support). Precision is sliiiiiiightly lower than when using a 3D texture
        // LUT (33^3 -> 32^3) but most of the time it's imperceptible.
        void RenderHDRPipeline2D(PostProcessRenderContext context)
        {
            // For the same reasons as in RenderHDRPipeline3D, regen LUT on every frame
            {
                CheckInternalStripLut();

                // Lut setup
                var lutSheet = context.propertySheets.Get(context.resources.shaders.lut2DBaker);
                lutSheet.ClearKeywords();

                lutSheet.properties.SetVector(ShaderIDs.Lut2D_Params, new Vector4(k_Lut2DSize, 0.5f / (k_Lut2DSize * k_Lut2DSize), 0.5f / k_Lut2DSize, k_Lut2DSize / (k_Lut2DSize - 1f)));

                var colorBalance = ColorUtilities.ComputeColorBalance(settings.temperature.value, settings.tint.value);
                lutSheet.properties.SetVector(ShaderIDs.ColorBalance, colorBalance);
                lutSheet.properties.SetVector(ShaderIDs.ColorFilter, settings.colorFilter.value);

                float hue = settings.hueShift.value / 360f;         // Remap to [-0.5;0.5]
                float sat = settings.saturation.value / 100f + 1f;  // Remap to [0;2]
                float con = settings.contrast.value / 100f + 1f;    // Remap to [0;2]
                lutSheet.properties.SetVector(ShaderIDs.HueSatCon, new Vector3(hue, sat, con));

                var channelMixerR = new Vector3(settings.mixerRedOutRedIn, settings.mixerRedOutGreenIn, settings.mixerRedOutBlueIn);
                var channelMixerG = new Vector3(settings.mixerGreenOutRedIn, settings.mixerGreenOutGreenIn, settings.mixerGreenOutBlueIn);
                var channelMixerB = new Vector3(settings.mixerBlueOutRedIn, settings.mixerBlueOutGreenIn, settings.mixerBlueOutBlueIn);
                lutSheet.properties.SetVector(ShaderIDs.ChannelMixerRed, channelMixerR / 100f);            // Remap to [-2;2]
                lutSheet.properties.SetVector(ShaderIDs.ChannelMixerGreen, channelMixerG / 100f);
                lutSheet.properties.SetVector(ShaderIDs.ChannelMixerBlue, channelMixerB / 100f);

                var lift = ColorUtilities.ColorToLift(settings.lift.value * 0.2f);
                var gain = ColorUtilities.ColorToGain(settings.gain.value * 0.8f);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value * 0.8f);
                lutSheet.properties.SetVector(ShaderIDs.Lift, lift);
                lutSheet.properties.SetVector(ShaderIDs.InvGamma, invgamma);
                lutSheet.properties.SetVector(ShaderIDs.Gain, gain);

                lutSheet.properties.SetTexture(ShaderIDs.Curves, GetCurveTexture(true));

                var tonemapper = settings.tonemapper.value;
                if (tonemapper == Tonemapper.Custom)
                {
                    lutSheet.EnableKeyword("TONEMAPPING_CUSTOM");

                    m_HableCurve.Init(
                        settings.toneCurveToeStrength.value,
                        settings.toneCurveToeLength.value,
                        settings.toneCurveShoulderStrength.value,
                        settings.toneCurveShoulderLength.value,
                        settings.toneCurveShoulderAngle.value,
                        settings.toneCurveGamma.value
                    );

                    lutSheet.properties.SetVector(ShaderIDs.CustomToneCurve, m_HableCurve.uniforms.curve);
                    lutSheet.properties.SetVector(ShaderIDs.ToeSegmentA, m_HableCurve.uniforms.toeSegmentA);
                    lutSheet.properties.SetVector(ShaderIDs.ToeSegmentB, m_HableCurve.uniforms.toeSegmentB);
                    lutSheet.properties.SetVector(ShaderIDs.MidSegmentA, m_HableCurve.uniforms.midSegmentA);
                    lutSheet.properties.SetVector(ShaderIDs.MidSegmentB, m_HableCurve.uniforms.midSegmentB);
                    lutSheet.properties.SetVector(ShaderIDs.ShoSegmentA, m_HableCurve.uniforms.shoSegmentA);
                    lutSheet.properties.SetVector(ShaderIDs.ShoSegmentB, m_HableCurve.uniforms.shoSegmentB);
                }
                else if (tonemapper == Tonemapper.ACES)
                    lutSheet.EnableKeyword("TONEMAPPING_ACES");
                else if (tonemapper == Tonemapper.Neutral)
                    lutSheet.EnableKeyword("TONEMAPPING_NEUTRAL");

                // Generate the lut
                context.command.BeginSample("HdrColorGradingLut2D");
                context.command.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_InternalLdrLut, lutSheet, (int)Pass.LutGenHDR2D);
                context.command.EndSample("HdrColorGradingLut2D");
            }

            var lut = m_InternalLdrLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_HDR_2D");
            uberSheet.properties.SetVector(ShaderIDs.Lut2D_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            uberSheet.properties.SetTexture(ShaderIDs.Lut2D, lut);
            uberSheet.properties.SetFloat(ShaderIDs.PostExposure, RuntimeUtilities.Exp2(settings.postExposure.value));
        }

        // LDR color pipeline is rendered to a 2D strip lut (works on every platform)
        void RenderLDRPipeline2D(PostProcessRenderContext context)
        {
            // For the same reasons as in RenderHDRPipeline3D, regen LUT on every frame
            {
                CheckInternalStripLut();

                // Lut setup
                var lutSheet = context.propertySheets.Get(context.resources.shaders.lut2DBaker);
                lutSheet.ClearKeywords();

                lutSheet.properties.SetVector(ShaderIDs.Lut2D_Params, new Vector4(k_Lut2DSize, 0.5f / (k_Lut2DSize * k_Lut2DSize), 0.5f / k_Lut2DSize, k_Lut2DSize / (k_Lut2DSize - 1f)));

                var colorBalance = ColorUtilities.ComputeColorBalance(settings.temperature.value, settings.tint.value);
                lutSheet.properties.SetVector(ShaderIDs.ColorBalance, colorBalance);
                lutSheet.properties.SetVector(ShaderIDs.ColorFilter, settings.colorFilter.value);

                float hue = settings.hueShift.value / 360f;         // Remap to [-0.5;0.5]
                float sat = settings.saturation.value / 100f + 1f;  // Remap to [0;2]
                float con = settings.contrast.value / 100f + 1f;    // Remap to [0;2]
                lutSheet.properties.SetVector(ShaderIDs.HueSatCon, new Vector3(hue, sat, con));

                var channelMixerR = new Vector3(settings.mixerRedOutRedIn, settings.mixerRedOutGreenIn, settings.mixerRedOutBlueIn);
                var channelMixerG = new Vector3(settings.mixerGreenOutRedIn, settings.mixerGreenOutGreenIn, settings.mixerGreenOutBlueIn);
                var channelMixerB = new Vector3(settings.mixerBlueOutRedIn, settings.mixerBlueOutGreenIn, settings.mixerBlueOutBlueIn);
                lutSheet.properties.SetVector(ShaderIDs.ChannelMixerRed, channelMixerR / 100f);            // Remap to [-2;2]
                lutSheet.properties.SetVector(ShaderIDs.ChannelMixerGreen, channelMixerG / 100f);
                lutSheet.properties.SetVector(ShaderIDs.ChannelMixerBlue, channelMixerB / 100f);

                var lift = ColorUtilities.ColorToLift(settings.lift.value);
                var gain = ColorUtilities.ColorToGain(settings.gain.value);
                var invgamma = ColorUtilities.ColorToInverseGamma(settings.gamma.value);
                lutSheet.properties.SetVector(ShaderIDs.Lift, lift);
                lutSheet.properties.SetVector(ShaderIDs.InvGamma, invgamma);
                lutSheet.properties.SetVector(ShaderIDs.Gain, gain);

                lutSheet.properties.SetFloat(ShaderIDs.Brightness, (settings.brightness.value + 100f) / 100f);
                lutSheet.properties.SetTexture(ShaderIDs.Curves, GetCurveTexture(false));

                // Generate the lut
                context.command.BeginSample("LdrColorGradingLut2D");

                var userLut = settings.ldrLut.value;
                if (userLut == null || userLut.width != userLut.height * userLut.height)
                {
                    context.command.BlitFullscreenTriangle(BuiltinRenderTextureType.None, m_InternalLdrLut, lutSheet, (int)Pass.LutGenLDRFromScratch);
                }
                else
                {
                    lutSheet.properties.SetVector(ShaderIDs.UserLut2D_Params, new Vector4(1f / userLut.width, 1f / userLut.height, userLut.height - 1f, settings.ldrLutContribution));
                    context.command.BlitFullscreenTriangle(userLut, m_InternalLdrLut, lutSheet, (int)Pass.LutGenLDR);
                }

                context.command.EndSample("LdrColorGradingLut2D");
            }

            var lut = m_InternalLdrLut;
            var uberSheet = context.uberSheet;
            uberSheet.EnableKeyword("COLOR_GRADING_LDR_2D");
            uberSheet.properties.SetVector(ShaderIDs.Lut2D_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
            uberSheet.properties.SetTexture(ShaderIDs.Lut2D, lut);
        }

        void CheckInternalLogLut()
        {
            // Check internal lut state, (re)create it if needed
            if (m_InternalLogLut == null || !m_InternalLogLut.IsCreated())
            {
                RuntimeUtilities.Destroy(m_InternalLogLut);

                var format = GetLutFormat();
                m_InternalLogLut = new RenderTexture(k_Lut3DSize, k_Lut3DSize, 0, format, RenderTextureReadWrite.Linear)
                {
                    name = "Color Grading Log Lut",
                    dimension = TextureDimension.Tex3D,
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    enableRandomWrite = true,
                    volumeDepth = k_Lut3DSize,
                    autoGenerateMips = false,
                    useMipMap = false
                };
                m_InternalLogLut.Create();
            }
        }

        void CheckInternalStripLut()
        {
            // Check internal lut state, (re)create it if needed
            if (m_InternalLdrLut == null || !m_InternalLdrLut.IsCreated())
            {
                RuntimeUtilities.Destroy(m_InternalLdrLut);

                var format = GetLutFormat();
                m_InternalLdrLut = new RenderTexture(k_Lut2DSize * k_Lut2DSize, k_Lut2DSize, 0, format, RenderTextureReadWrite.Linear)
                {
                    name = "Color Grading Strip Lut",
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    anisoLevel = 0,
                    autoGenerateMips = false,
                    useMipMap = false
                };
                m_InternalLdrLut.Create();
            }
        }

        Texture2D GetCurveTexture(bool hdr)
        {
            if (m_GradingCurves == null)
            {
                var format = GetCurveFormat();
                m_GradingCurves = new Texture2D(Spline.k_Precision, 2, format, false, true)
                {
                    name = "Internal Curves Texture",
                    hideFlags = HideFlags.DontSave,
                    anisoLevel = 0,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            var hueVsHueCurve = settings.hueVsHueCurve.value;
            var hueVsSatCurve = settings.hueVsSatCurve.value;
            var satVsSatCurve = settings.satVsSatCurve.value;
            var lumVsSatCurve = settings.lumVsSatCurve.value;
            var masterCurve = settings.masterCurve.value;
            var redCurve = settings.redCurve.value;
            var greenCurve = settings.greenCurve.value;
            var blueCurve = settings.blueCurve.value;

            var pixels = m_Pixels;

            for (int i = 0; i < Spline.k_Precision; i++)
            {
                // Secondary/VS curves
                float x = hueVsHueCurve.cachedData[i];
                float y = hueVsSatCurve.cachedData[i];
                float z = satVsSatCurve.cachedData[i];
                float w = lumVsSatCurve.cachedData[i];
                pixels[i] = new Color(x, y, z, w);

                // YRGB
                if (!hdr)
                {
                    float m = masterCurve.cachedData[i];
                    float r = redCurve.cachedData[i];
                    float g = greenCurve.cachedData[i];
                    float b = blueCurve.cachedData[i];
                    pixels[i + Spline.k_Precision] = new Color(r, g, b, m);
                }
            }

            m_GradingCurves.SetPixels(pixels);
            m_GradingCurves.Apply(false, false);

            return m_GradingCurves;
        }

        static RenderTextureFormat GetLutFormat()
        {
            // Use ARGBHalf if possible, fallback on ARGB2101010 and ARGB32 otherwise
            var format = RenderTextureFormat.ARGBHalf;

            if (!format.IsSupported())
            {
                format = RenderTextureFormat.ARGB2101010;

                // Note that using a log lut in ARGB32 is a *very* bad idea but we need it for
                // compatibility reasons (else if a platform doesn't support one of the previous
                // format it'll output a black screen, or worse will segfault on the user).
                if (!format.IsSupported())
                    format = RenderTextureFormat.ARGB32;
            }

            return format;
        }

        static TextureFormat GetCurveFormat()
        {
            // Use RGBAHalf if possible, fallback on ARGB32 otherwise
            var format = TextureFormat.RGBAHalf;

            if (!SystemInfo.SupportsTextureFormat(format))
                format = TextureFormat.ARGB32;

            return format;
        }

        public override void Release()
        {
            RuntimeUtilities.Destroy(m_InternalLdrLut);
            m_InternalLdrLut = null;

            RuntimeUtilities.Destroy(m_InternalLogLut);
            m_InternalLogLut = null;

            RuntimeUtilities.Destroy(m_GradingCurves);
            m_GradingCurves = null;
        }
    }
}
