Shader "Hidden/ScriptableRenderPipeline/DebugDisplayHDShadowMap"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

        float4 _TextureScaleBias;
        float2 _ValidRange;
        SamplerState ltc_linear_clamp_sampler;
        TEXTURE2D(_AtlasTexture);

        struct Attributes
        {
            uint vertexID : VERTEXID_SEMANTIC;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            if (ShouldFlipDebugTexture())
            {
                output.texcoord.y = 1.0f - output.texcoord.y;
            }
            output.texcoord = output.texcoord *_TextureScaleBias.xy + _TextureScaleBias.zw;
            return output;
        }
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "RegularShadow"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragRegular

            float4 FragRegular(Varyings input) : SV_Target
            {
                return saturate((SAMPLE_TEXTURE2D(_AtlasTexture, ltc_linear_clamp_sampler, input.texcoord).x - _ValidRange.x) * _ValidRange.y).xxxx;
            }

            ENDHLSL
        }

        Pass
        {
            Name "VarianceShadow"
            ZTest Off
            Blend One Zero
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment FragVariance

            float4 FragVariance(Varyings input) : SV_Target
            {
                return saturate((SAMPLE_TEXTURE2D(_AtlasTexture, ltc_linear_clamp_sampler, input.texcoord).x - _ValidRange.x) * _ValidRange.y).xxxx;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
