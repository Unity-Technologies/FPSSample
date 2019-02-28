Shader "Hidden/PostProcessing/CopyStdFromTexArray"
{
    //Blit from texture array slice. Similar to CopyStd but with texture array as source
    //and sampling from texture array. Having separate shader is cleaner than multiple #if in the code.

    Properties
    {
        _MainTex ("", 2DArray) = "white" {}
    }

    CGINCLUDE
        #pragma target 3.5

        struct Attributes
        {
            float3 vertex : POSITION;
        };

        struct Varyings
        {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
        };

        Texture2DArray _MainTex;
        SamplerState sampler_MainTex;
        float _DepthSlice;

        float2 TransformTriangleVertexToUV(float2 vertex)
        {
            float2 uv = (vertex + 1.0) * 0.5;
            return uv;
        }

        Varyings Vert(Attributes v)
        {
            Varyings o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.texcoord.xy = TransformTriangleVertexToUV(v.vertex.xy);

            #if UNITY_UV_STARTS_AT_TOP
            o.texcoord.xy = o.texcoord.xy * float2(1.0, -1.0) + float2(0.0, 1.0);
            #endif
            o.texcoord.z = _DepthSlice;

            return o;
        }

        float4 Frag(Varyings i) : SV_Target
        {
            float4 color = _MainTex.Sample(sampler_MainTex, i.texcoord);
            return color;
        }

        bool IsNan(float x)
        {
            return (x < 0.0 || x > 0.0 || x == 0.0) ? false : true;
        }

        bool AnyIsNan(float4 x)
        {
            return IsNan(x.x) || IsNan(x.y) || IsNan(x.z) || IsNan(x.w);
        }

        float4 FragKillNaN(Varyings i) : SV_Target
        {
            float4 color = _MainTex.Sample(sampler_MainTex, i.texcoord);

            if (AnyIsNan(color))
            {
                color = (0.0).xxxx;
            }

            return color;
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Copy
        Pass
        {
            CGPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDCG
        }

        // 0 - Copy + NaN killer
        Pass
        {
            CGPROGRAM

                #pragma vertex Vert
                #pragma fragment FragKillNaN

            ENDCG
        }
    }
}
