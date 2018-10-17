Shader "Hidden/HDRenderPipeline/DrawDiffusionProfile"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile SSS_MODEL_BASIC SSS_MODEL_DISNEY

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            //-------------------------------------------------------------------------------------
            // Inputs & outputs
            //-------------------------------------------------------------------------------------

            float4 _ShapeParam; float _MaxRadius; // See 'DiffusionProfile'

            //-------------------------------------------------------------------------------------
            // Implementation
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                float3 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.vertex   = TransformWorldToHClip(input.vertex);
                output.texcoord = input.texcoord.xy;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // Profile display does not use premultiplied S.
                float  r = (2 * length(input.texcoord - 0.5)) * _MaxRadius;
                float3 S = _ShapeParam.rgb;
                float3 M = S * (exp(-r * S) + exp(-r * S * (1.0 / 3.0))) / (8 * PI * r);
                float3 A = _MaxRadius / S;

                // N.b.: we multiply by the surface albedo of the actual geometry during shading.
                // Apply gamma for visualization only. Do not apply gamma to the color.
                return float4(sqrt(M) * A, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
