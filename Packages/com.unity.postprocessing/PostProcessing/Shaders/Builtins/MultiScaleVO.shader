Shader "Hidden/PostProcessing/MultiScaleVO"
{
    HLSLINCLUDE

        #pragma exclude_renderers gles gles3 d3d11_9x
        #pragma target 4.5

        #include "../StdLib.hlsl"
        #include "Fog.hlsl"

        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        TEXTURE2D_SAMPLER2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture);
        float3 _AOColor;

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Depth copy with procedural draw
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo);
                }

            ENDHLSL
        }

        // 1 - Composite to G-buffer with procedural draw
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

                struct Output
                {
                    float4 gbuffer0 : SV_Target0;
                    float4 gbuffer3 : SV_Target1;
                };

                Output Frag(VaryingsDefault i)
                {
                    float ao = 1.0 - SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, i.texcoordStereo).r;
                    Output o;
                    o.gbuffer0 = float4(0.0, 0.0, 0.0, ao);
                    o.gbuffer3 = float4(ao * _AOColor, 0.0);
                    return o;
                }

            ENDHLSL
        }

        // 2 - Composite to the frame buffer
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma multi_compile _ APPLY_FORWARD_FOG
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #pragma vertex VertDefault
                #pragma fragment Frag

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    half ao = 1.0 - SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, i.texcoordStereo).r;

                    // Apply fog when enabled (forward-only)
                #if (APPLY_FORWARD_FOG)
                    float d = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo));
                    d = ComputeFogDistance(d);
                    ao *= ComputeFog(d);
                #endif

                    return float4(ao * _AOColor, 0.0);
                }

            ENDHLSL
        }

        // 3 - Debug overlay
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

                float4 Frag(VaryingsDefault i) : SV_Target
                {
                    half ao = SAMPLE_TEXTURE2D(_MSVOcclusionTexture, sampler_MSVOcclusionTexture, i.texcoordStereo).r;
                    return float4(ao.rrr, 1.0);
                }

            ENDHLSL
        }
    }
}
