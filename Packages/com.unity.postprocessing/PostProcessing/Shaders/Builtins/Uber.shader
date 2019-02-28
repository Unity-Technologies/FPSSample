Shader "Hidden/PostProcessing/Uber"
{
    HLSLINCLUDE

        #pragma target 3.0

        #pragma multi_compile __ DISTORT
        #pragma multi_compile __ CHROMATIC_ABERRATION CHROMATIC_ABERRATION_LOW
        #pragma multi_compile __ BLOOM BLOOM_LOW
        #pragma multi_compile __ VIGNETTE
        #pragma multi_compile __ GRAIN
        #pragma multi_compile __ FINALPASS
        // the following keywords are handled in API specific SubShaders below
        // #pragma multi_compile __ COLOR_GRADING_LDR_2D COLOR_GRADING_HDR_2D COLOR_GRADING_HDR_3D
        // #pragma multi_compile __ STEREO_INSTANCING_ENABLED STEREO_DOUBLEWIDE_TARGET
        
        #pragma vertex VertUVTransform
        #pragma fragment FragUber
    
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"
        #include "../Sampling.hlsl"
        #include "Distortion.hlsl"
        #include "Dithering.hlsl"

        #define MAX_CHROMATIC_SAMPLES 16

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        // Auto exposure / eye adaptation
        TEXTURE2D_SAMPLER2D(_AutoExposureTex, sampler_AutoExposureTex);

        // Bloom
        TEXTURE2D_SAMPLER2D(_BloomTex, sampler_BloomTex);
        TEXTURE2D_SAMPLER2D(_Bloom_DirtTex, sampler_Bloom_DirtTex);
        float4 _BloomTex_TexelSize;
        float4 _Bloom_DirtTileOffset; // xy: tiling, zw: offset
        half3 _Bloom_Settings; // x: sampleScale, y: intensity, z: dirt intensity
        half3 _Bloom_Color;

        // Chromatic aberration
        TEXTURE2D_SAMPLER2D(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut);
        half _ChromaticAberration_Amount;

        // Color grading
    #if COLOR_GRADING_HDR_3D

        TEXTURE3D_SAMPLER3D(_Lut3D, sampler_Lut3D);
        float2 _Lut3D_Params;

    #else

        TEXTURE2D_SAMPLER2D(_Lut2D, sampler_Lut2D);
        float3 _Lut2D_Params;

    #endif

        half _PostExposure; // EV (exp2)

        // Vignette
        half3 _Vignette_Color;
        half2 _Vignette_Center; // UV space
        half4 _Vignette_Settings; // x: intensity, y: smoothness, z: roundness, w: rounded
        half _Vignette_Opacity;
        half _Vignette_Mode; // <0.5: procedural, >=0.5: masked
        TEXTURE2D_SAMPLER2D(_Vignette_Mask, sampler_Vignette_Mask);

        // Grain
        TEXTURE2D_SAMPLER2D(_GrainTex, sampler_GrainTex);
        half2 _Grain_Params1; // x: lum_contrib, y: intensity
        float4 _Grain_Params2; // x: xscale, h: yscale, z: xoffset, w: yoffset

        // Misc
        half _LumaInAlpha;

        half4 FragUber(VaryingsDefault i) : SV_Target
        {
            float2 uv = i.texcoord;

            //>>> Automatically skipped by the shader optimizer when not used
            float2 uvDistorted = Distort(i.texcoord);
            float2 uvStereoDistorted = Distort(i.texcoordStereo);
            //<<<

            half autoExposure = SAMPLE_TEXTURE2D(_AutoExposureTex, sampler_AutoExposureTex, uv).r;
            half4 color = (0.0).xxxx;

            // Inspired by the method described in "Rendering Inside" [Playdead 2016]
            // https://twitter.com/pixelmager/status/717019757766123520
            #if CHROMATIC_ABERRATION
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration_Amount;

                float2 diff = end - uv;
                int samples = clamp(int(length(_MainTex_TexelSize.zw * diff / 2.0)), 3, MAX_CHROMATIC_SAMPLES);
                float2 delta = diff / samples;
                float2 pos = uv;
                half4 sum = (0.0).xxxx, filterSum = (0.0).xxxx;

                for (int i = 0; i < samples; i++)
                {
                    half t = (i + 0.5) / samples;
                    half4 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(Distort(pos)), 0);
                    half4 filter = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(t, 0.0), 0).rgb, 1.0);

                    sum += s * filter;
                    filterSum += filter;
                    pos += delta;
                }

                color = sum / filterSum;
            }
            #elif CHROMATIC_ABERRATION_LOW
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration_Amount;
                float2 delta = (end - uv) / 3;

                half4 filterA = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(0.5 / 3, 0.0), 0).rgb, 1.0);
                half4 filterB = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(1.5 / 3, 0.0), 0).rgb, 1.0);
                half4 filterC = half4(SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_SpectralLut, sampler_ChromaticAberration_SpectralLut, float2(2.5 / 3, 0.0), 0).rgb, 1.0);

                half4 texelA = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(Distort(uv)), 0);
                half4 texelB = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(Distort(delta + uv)), 0);
                half4 texelC = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(Distort(delta * 2.0 + uv)), 0);

                half4 sum = texelA * filterA + texelB * filterB + texelC * filterC;
                half4 filterSum = filterA + filterB + filterC;
                color = sum / filterSum;
            }
            #else
            {
                color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvStereoDistorted);
            }
            #endif

            // Gamma space... Gah.
            #if UNITY_COLORSPACE_GAMMA
            {
                color = SRGBToLinear(color);
            }
            #endif

            color.rgb *= autoExposure;

            #if BLOOM || BLOOM_LOW
            {
                #if BLOOM
                half4 bloom = UpsampleTent(TEXTURE2D_PARAM(_BloomTex, sampler_BloomTex), uvDistorted, _BloomTex_TexelSize.xy, _Bloom_Settings.x);
                #else
                half4 bloom = UpsampleBox(TEXTURE2D_PARAM(_BloomTex, sampler_BloomTex), uvDistorted, _BloomTex_TexelSize.xy, _Bloom_Settings.x);
                #endif

                // UVs should be Distort(uv * _Bloom_DirtTileOffset.xy + _Bloom_DirtTileOffset.zw)
                // but considering we use a cover-style scale on the dirt texture the difference
                // isn't massive so we chose to save a few ALUs here instead in case lens distortion
                // is active
                half4 dirt = half4(SAMPLE_TEXTURE2D(_Bloom_DirtTex, sampler_Bloom_DirtTex, uvDistorted * _Bloom_DirtTileOffset.xy + _Bloom_DirtTileOffset.zw).rgb, 0.0);

                // Additive bloom (artist friendly)
                bloom *= _Bloom_Settings.y;
                dirt *= _Bloom_Settings.z;
                color += bloom * half4(_Bloom_Color, 1.0);
                color += dirt * bloom;
            }
            #endif

            #if VIGNETTE
            {
                UNITY_BRANCH
                if (_Vignette_Mode < 0.5)
                {
                    half2 d = abs(uvDistorted - _Vignette_Center) * _Vignette_Settings.x;
                    d.x *= lerp(1.0, _ScreenParams.x / _ScreenParams.y, _Vignette_Settings.w);
                    d = pow(saturate(d), _Vignette_Settings.z); // Roundness
                    half vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
                    color.rgb *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
                    color.a = lerp(1.0, color.a, vfactor);
                }
                else
                {
                    half vfactor = SAMPLE_TEXTURE2D(_Vignette_Mask, sampler_Vignette_Mask, uvDistorted).a;

                    #if !UNITY_COLORSPACE_GAMMA
                    {
                        vfactor = SRGBToLinear(vfactor);
                    }
                    #endif

                    half3 new_color = color.rgb * lerp(_Vignette_Color, (1.0).xxx, vfactor);
                    color.rgb = lerp(color.rgb, new_color, _Vignette_Opacity);
                    color.a = lerp(1.0, color.a, vfactor);
                }
            }
            #endif

            #if GRAIN
            {
                half3 grain = SAMPLE_TEXTURE2D(_GrainTex, sampler_GrainTex, i.texcoordStereo * _Grain_Params2.xy + _Grain_Params2.zw).rgb;

                // Noisiness response curve based on scene luminance
                float lum = 1.0 - sqrt(Luminance(saturate(color)));
                lum = lerp(1.0, lum, _Grain_Params1.x);

                color.rgb += color.rgb * grain * _Grain_Params1.y * lum;
            }
            #endif

            #if COLOR_GRADING_HDR_3D
            {
                color *= _PostExposure;
                float3 colorLutSpace = saturate(LUT_SPACE_ENCODE(color.rgb));
                color.rgb = ApplyLut3D(TEXTURE3D_PARAM(_Lut3D, sampler_Lut3D), colorLutSpace, _Lut3D_Params);
            }
            #elif COLOR_GRADING_HDR_2D
            {
                color *= _PostExposure;
                float3 colorLutSpace = saturate(LUT_SPACE_ENCODE(color.rgb));
                color.rgb = ApplyLut2D(TEXTURE2D_PARAM(_Lut2D, sampler_Lut2D), colorLutSpace, _Lut2D_Params);
            }
            #elif COLOR_GRADING_LDR_2D
            {
                color = saturate(color);

                // LDR Lut lookup needs to be in sRGB - for HDR stick to linear
                color.rgb = LinearToSRGB(color.rgb);
                color.rgb = ApplyLut2D(TEXTURE2D_PARAM(_Lut2D, sampler_Lut2D), color.rgb, _Lut2D_Params);
                color.rgb = SRGBToLinear(color.rgb);
            }
            #endif

            half4 output = color;

            #if FINALPASS
            {
                #if UNITY_COLORSPACE_GAMMA
                {
                    output = LinearToSRGB(output);
                }
                #endif

                output.rgb = Dither(output.rgb, i.texcoord);
            }
            #else
            {
                UNITY_BRANCH
                if (_LumaInAlpha > 0.5)
                {
                    // Put saturated luma in alpha for FXAA - higher quality than "green as luma" and
                    // necessary as RGB values will potentially still be HDR for the FXAA pass
                    half luma = Luminance(saturate(output));
                    output.a = luma;
                }

                #if UNITY_COLORSPACE_GAMMA
                {
                    output = LinearToSRGB(output);
                }
                #endif
            }
            #endif

            // Output RGB is still HDR at that point (unless range was crunched by a tonemapper)
            return output;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
                #pragma exclude_renderers gles vulkan

                #pragma multi_compile __ COLOR_GRADING_LDR_2D COLOR_GRADING_HDR_2D COLOR_GRADING_HDR_3D
                #pragma multi_compile __ STEREO_INSTANCING_ENABLED STEREO_DOUBLEWIDE_TARGET
            ENDHLSL
        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
                #pragma only_renderers vulkan

                #pragma multi_compile __ COLOR_GRADING_LDR_2D COLOR_GRADING_HDR_2D COLOR_GRADING_HDR_3D
                #pragma multi_compile __ STEREO_DOUBLEWIDE_TARGET // disabled for Vulkan because of shader compiler issues in older Unity versions: STEREO_INSTANCING_ENABLED
            ENDHLSL
        }
    }
    
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
                #pragma only_renderers gles

                #pragma multi_compile __ COLOR_GRADING_LDR_2D COLOR_GRADING_HDR_2D // not supported by OpenGL ES 2.0: COLOR_GRADING_HDR_3D
                #pragma multi_compile __ STEREO_DOUBLEWIDE_TARGET // not supported by OpenGL ES 2.0: STEREO_INSTANCING_ENABLED
            ENDHLSL
        }
    }
}
