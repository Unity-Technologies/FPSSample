Shader "Hidden/PostProcessing/Debug/Waveform"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5
        #include "../StdLib.hlsl"

        StructuredBuffer<uint4> _WaveformBuffer;
        float3 _Params; // x: buffer width, y: buffer height, z: exposure, w: unused

        float3 Tonemap(float3 x, float exposure)
        {
            const float a = 6.2;
            const float b = 0.5;
            const float c = 1.7;
            const float d = 0.06;
            x *= exposure;
            x = max((0.0).xxx, x - (0.004).xxx);
            x = (x * (a * x + b)) / (x * (a * x + c) + d);
            return x * x;
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            const float3 red = float3(1.4, 0.03, 0.02);
            const float3 green = float3(0.02, 1.1, 0.05);
            const float3 blue = float3(0.0, 0.25, 1.5);
            float3 color = float3(0.0, 0.0, 0.0);

            uint2 uvI = i.vertex.xy;
            float3 w = _WaveformBuffer[uvI.x * _Params.y + uvI.y].xyz;

            color += red * w.r;
            color += green * w.g;
            color += blue * w.b;
            color = Tonemap(color, _Params.z);

            return float4(saturate(color), 1.0);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
