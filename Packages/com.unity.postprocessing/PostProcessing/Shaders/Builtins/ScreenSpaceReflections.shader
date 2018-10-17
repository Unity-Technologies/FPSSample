Shader "Hidden/PostProcessing/ScreenSpaceReflections"
{
    // We need to use internal Unity lighting structures and functions for this effect so we have to
    // stick to CGPROGRAM instead of HLSLPROGRAM

    CGINCLUDE

        #include "UnityCG.cginc"
        #pragma target 5.0

        // Ported from StdLib, we can't include it as it'll conflict with internal Unity includes
        struct AttributesDefault
        {
            float3 vertex : POSITION;
        };

        struct VaryingsDefault
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            float2 texcoordStereo : TEXCOORD1;
        };

        VaryingsDefault VertDefault(AttributesDefault v)
        {
            VaryingsDefault o;
            o.vertex = float4(v.vertex.xy, 0.0, 1.0);
            o.texcoord = (v.vertex.xy + 1.0) * 0.5;

        #if UNITY_UV_STARTS_AT_TOP
            o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
        #endif

            o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);

            return o;
        }

        #include "ScreenSpaceReflections.hlsl"

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Test
        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragTest

            ENDCG
        }

        // 1 - Resolve
        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragResolve

            ENDCG
        }

        // 2 - Reproject
        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragReproject

            ENDCG
        }

        // 3 - Composite
        Pass
        {
            CGPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragComposite

            ENDCG
        }
    }
}
