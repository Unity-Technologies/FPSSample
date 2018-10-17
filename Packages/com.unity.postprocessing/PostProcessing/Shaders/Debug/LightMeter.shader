Shader "Hidden/PostProcessing/Debug/LightMeter"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5
        #include "../StdLib.hlsl"
        #include "../Builtins/ExposureHistogram.hlsl"
        #pragma multi_compile __ COLOR_GRADING_HDR
        #pragma multi_compile __ AUTO_EXPOSURE

        float4 _Params; // x: lowPercent, y: highPercent, z: minBrightness, w: maxBrightness
        float4 _ScaleOffsetRes; // x: scale, y: offset, w: histogram pass width, h: histogram pass height
        
        TEXTURE3D_SAMPLER3D(_Lut3D, sampler_Lut3D);

        StructuredBuffer<uint> _HistogramBuffer;

        struct VaryingsLightMeter
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            float maxValue : TEXCOORD1;
            float avgLuminance : TEXCOORD2;
        };

        VaryingsLightMeter Vert(AttributesDefault v)
        {
            VaryingsLightMeter o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

        #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

            o.maxValue = 1.0 / FindMaxHistogramValue(_HistogramBuffer);
            o.avgLuminance = GetAverageLuminance(_HistogramBuffer, _Params, o.maxValue, _ScaleOffsetRes.xy);

            return o;
        }

        float4 Frag(VaryingsLightMeter i) : SV_Target
        {
            uint ix = (uint)(round(i.texcoord.x * HISTOGRAM_BINS));
            float bin = saturate(float(_HistogramBuffer[ix]) * i.maxValue);
            float fill = step(i.texcoord.y, bin);

            float4 color = float4(lerp(0.0, 0.75, fill).xxx, 1.0);

        #if AUTO_EXPOSURE
            const float3 kRangeColor = float3(0.05, 0.3, 0.4);
            const float3 kAvgColor = float3(0.75, 0.1, 1.0);

            // Min / max brightness markers
            float luminanceMin = GetHistogramBinFromLuminance(_Params.z, _ScaleOffsetRes.xy);
            float luminanceMax = GetHistogramBinFromLuminance(_Params.w, _ScaleOffsetRes.xy);

            if (i.texcoord.x > luminanceMin && i.texcoord.x < luminanceMax)
            {
                color.rgb = fill.rrr * kRangeColor;
                color.rgb += kRangeColor;
            }
        #endif

        #if COLOR_GRADING_HDR
            // Draw color curves on top
            float4 curves = 0.0;
            float3 lut = SAMPLE_TEXTURE3D(_Lut3D, sampler_Lut3D, i.texcoord.xxx).rgb;

            if (abs(lut.r - i.texcoord.y) < _ScaleOffsetRes.w)
                curves.ra += (1.0).xx;

            if (abs(lut.g - i.texcoord.y) < _ScaleOffsetRes.w)
                curves.ga += (1.0).xx;

            if (abs(lut.b - i.texcoord.y) < _ScaleOffsetRes.w)
                curves.gba += float3(0.5, (1.0).xx);

            color = any(curves) ? curves : color;
        #endif

        #if AUTO_EXPOSURE
            // Current average luminance marker
            float luminanceAvg = GetHistogramBinFromLuminance(i.avgLuminance, _ScaleOffsetRes.xy);
            float avgPx = luminanceAvg * _ScaleOffsetRes.z;

            if (abs(i.texcoord.x - luminanceAvg) < _ScaleOffsetRes.z * 2.0)
                color.rgb = kAvgColor;
        #endif

            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
