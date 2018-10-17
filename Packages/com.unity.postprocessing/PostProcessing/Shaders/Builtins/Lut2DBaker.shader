Shader "Hidden/PostProcessing/Lut2DBaker"
{
    HLSLINCLUDE

        #pragma target 3.0
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../ACES.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _Lut2D_Params;
        float4 _UserLut2D_Params;

        float3 _ColorBalance;
        float3 _ColorFilter;
        float3 _HueSatCon;
        float _Brightness; // LDR only

        float3 _ChannelMixerRed;
        float3 _ChannelMixerGreen;
        float3 _ChannelMixerBlue;

        float3 _Lift;
        float3 _InvGamma;
        float3 _Gain;

        TEXTURE2D_SAMPLER2D(_Curves, sampler_Curves);
        
        float4 _CustomToneCurve;
        float4 _ToeSegmentA;
        float4 _ToeSegmentB;
        float4 _MidSegmentA;
        float4 _MidSegmentB;
        float4 _ShoSegmentA;
        float4 _ShoSegmentB;

        float3 ApplyCommonGradingSteps(float3 colorLinear)
        {
            colorLinear = WhiteBalance(colorLinear, _ColorBalance);
            colorLinear *= _ColorFilter;
            colorLinear = ChannelMixer(colorLinear, _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue);
            colorLinear = LiftGammaGainHDR(colorLinear, _Lift, _InvGamma, _Gain);

            // Do NOT feed negative values to RgbToHsv or they'll wrap around
            colorLinear = max((float3)0.0, colorLinear);

            float3 hsv = RgbToHsv(colorLinear);

            // Hue Vs Sat
            float satMult;
            satMult = saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(hsv.x, 0.25), 0).y) * 2.0;

            // Sat Vs Sat
            satMult *= saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(hsv.y, 0.25), 0).z) * 2.0;

            // Lum Vs Sat
            satMult *= saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(Luminance(colorLinear), 0.25), 0).w) * 2.0;

            // Hue Vs Hue
            float hue = hsv.x + _HueSatCon.x;
            float offset = saturate(SAMPLE_TEXTURE2D_LOD(_Curves, sampler_Curves, float2(hue, 0.25), 0).x) - 0.5;
            hue += offset;
            hsv.x = RotateHue(hue, 0.0, 1.0);

            colorLinear = HsvToRgb(hsv);
            colorLinear = Saturation(colorLinear, _HueSatCon.y * satMult);

            return colorLinear;
        }

        //
        // LDR Grading process
        //
        float3 ColorGradeLDR(float3 colorLinear)
        {
            // Brightness is a simple linear multiplier. Works better in LDR than using e.v.
            colorLinear *= _Brightness;

            // Contrast is done in linear, switching to log for that in LDR is pointless and doesn't
            // feel as good to tweak
            const float kMidGrey = pow(0.5, 2.2);
            colorLinear = Contrast(colorLinear, kMidGrey, _HueSatCon.z);

            colorLinear = ApplyCommonGradingSteps(colorLinear);

            // YRGB only works in LDR for now as we don't do any curve range remapping
            colorLinear = YrgbCurve(saturate(colorLinear), TEXTURE2D_PARAM(_Curves, sampler_Curves));

            return saturate(colorLinear);
        }

        float4 FragLDRFromScratch(VaryingsDefault i) : SV_Target
        {
            float3 colorLinear = GetLutStripValue(i.texcoordStereo, _Lut2D_Params);
            float3 graded = ColorGradeLDR(colorLinear);
            return float4(graded, 1.0);
        }

        float4 FragLDR(VaryingsDefault i) : SV_Target
        {
            // Note: user luts may not have the same size as the internal one
            float3 neutralColorLinear = GetLutStripValue(i.texcoordStereo, _Lut2D_Params);
            float3 lookup = ApplyLut2D(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), neutralColorLinear, _UserLut2D_Params.xyz);
            float3 colorLinear = lerp(neutralColorLinear, lookup, _UserLut2D_Params.w);
            float3 graded = ColorGradeLDR(colorLinear);
            return float4(graded, 1.0);
        }

        //
        // HDR Grading process
        //
        float3 LogGradeHDR(float3 colorLog)
        {
            // HDR contrast feels a lot more natural when done in log rather than doing it in linear
            colorLog = Contrast(colorLog, ACEScc_MIDGRAY, _HueSatCon.z);
            return colorLog;
        }

        float3 LinearGradeHDR(float3 colorLinear)
        {
            colorLinear = ApplyCommonGradingSteps(colorLinear);
            return colorLinear;
        }

        float3 ColorGradeHDR(float3 colorLutSpace)
        {
            #if TONEMAPPING_ACES
            {
                float3 colorLinear = LUT_SPACE_DECODE(colorLutSpace);
                float3 aces = unity_to_ACES(colorLinear);

                // ACEScc (log) space
                float3 acescc = ACES_to_ACEScc(aces);
                acescc = LogGradeHDR(acescc);
                aces = ACEScc_to_ACES(acescc);

                // ACEScg (linear) space
                float3 acescg = ACES_to_ACEScg(aces);
                acescg = LinearGradeHDR(acescg);

                // Tonemap ODT(RRT(aces))
                aces = ACEScg_to_ACES(acescg);
                colorLinear = AcesTonemap(aces);

                return colorLinear;
            }
            #else
            {
                // colorLutSpace is already in log space
                colorLutSpace = LogGradeHDR(colorLutSpace);

                // Switch back to linear
                float3 colorLinear = LUT_SPACE_DECODE(colorLutSpace);
                colorLinear = LinearGradeHDR(colorLinear);
                colorLinear = max(0.0, colorLinear);

                // Tonemap
                #if TONEMAPPING_NEUTRAL
                {
                    colorLinear = NeutralTonemap(colorLinear);
                }
                #elif TONEMAPPING_CUSTOM
                {
                    colorLinear = CustomTonemap(
                        colorLinear, _CustomToneCurve.xyz,
                        _ToeSegmentA, _ToeSegmentB.xy,
                        _MidSegmentA, _MidSegmentB.xy,
                        _ShoSegmentA, _ShoSegmentB.xy
                    );
                }
                #endif

                return colorLinear;
            }
            #endif
        }

        float4 FragHDR(VaryingsDefault i) : SV_Target
        {
            float3 colorLutSpace = GetLutStripValue(i.texcoord, _Lut2D_Params);
            float3 graded = ColorGradeHDR(colorLutSpace);
            return float4(graded, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragLDRFromScratch

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragLDR

            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragHDR
                #pragma multi_compile __ TONEMAPPING_ACES TONEMAPPING_NEUTRAL TONEMAPPING_CUSTOM

            ENDHLSL
        }
    }
}
