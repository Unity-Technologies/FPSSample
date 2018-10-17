Shader "Hidden/PostProcessing/Debug/Vectorscope"
{
    HLSLINCLUDE
        
        #pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5
        #include "../StdLib.hlsl"
        #include "../Colors.hlsl"

        StructuredBuffer<uint> _VectorscopeBuffer;
        float3 _Params; // x: width, y: height, z: exposure, w: unused

        float Tonemap(float x, float exposure)
        {
            const float a = 6.2;
            const float b = 0.5;
            const float c = 1.7;
            const float d = 0.06;
            x *= exposure;
            x = max(0.0, x - 0.004);
            x = (x * (a * x + b)) / (x * (a * x + c) + d);
            return x * x;
        }

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            i.texcoord.x = 1.0 - i.texcoord.x;
            float2 uv = i.texcoord - (0.5).xx;
            float3 c = YCbCrToRgb(float3(0.5, uv.x, uv.y));

            float dist = sqrt(dot(uv, uv));
            float delta = fwidth(dist) * 0.5;
            float alphaOut = 1.0 - smoothstep(0.5 - delta, 0.5 + delta, dist);

            uint2 uvI = i.texcoord.xy * _Params.xy;
            uint v = _VectorscopeBuffer[uvI.x + uvI.y * _Params.x];
            float vt = saturate(Tonemap(v, _Params.z));

            float4 color = float4(lerp(c, (0.0).xxx, vt), 1.0);
            return color;
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
