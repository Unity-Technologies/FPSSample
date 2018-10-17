Shader "Hidden/PostProcessing/Editor/ConvertToLog"
{
    Properties
    {
        _MainTex ("", 2D) = "white" {}
    }

    CGINCLUDE

        #include "UnityCG.cginc"

        struct ParamsLogC
        {
            float cut;
            float a, b, c, d, e, f;
        };

        static const ParamsLogC LogC =
        {
            0.011361, // cut
            5.555556, // a
            0.047996, // b
            0.244161, // c
            0.386036, // d
            5.301883, // e
            0.092819  // f
        };

        float LinearToLogC_Precise(half x)
        {
            float o;
            if (x > LogC.cut)
                o = LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
            else
                o = LogC.e * x + LogC.f;
            return o;
        }

        sampler2D _MainTex;

        float4 Frag(v2f_img i) : SV_Target
        {
            float4 color = tex2D(_MainTex, i.uv);
            color.rgb = float3(LinearToLogC_Precise(color.r), LinearToLogC_Precise(color.g), LinearToLogC_Precise(color.b));
            color.a = 1.0;
            return color;
        }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM

                #pragma vertex vert_img
                #pragma fragment Frag

            ENDCG
        }
    }
}
