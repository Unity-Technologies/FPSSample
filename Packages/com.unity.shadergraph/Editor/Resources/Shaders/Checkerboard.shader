Shader "Hidden/Checkerboard"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            static const float rows = 24;
            static const float columns = 24;

            float4 frag(v2f_img i) : COLOR
            {
                float3 col1 = float3(32.0/255.0, 32.0/255.0, 32.0/255.0);
                float3 col2 = float3(42.0/255.0, 42.0/255.0, 42.0/255.0);
                if (!IsGammaSpace()) {
                    col1 = GammaToLinearSpace(col1);
                    col2 = GammaToLinearSpace(col2);
                }
                float total = floor(i.uv.x * rows) + floor(i.uv.y * columns);
                return float4(lerp(col1, col2, step(fmod(total, 2.0), 0.5)), 1.0);
            }
            ENDCG
        }
    }
}
