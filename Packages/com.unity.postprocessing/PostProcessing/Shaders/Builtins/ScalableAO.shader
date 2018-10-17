Shader "Hidden/PostProcessing/ScalableAO"
{
    HLSLINCLUDE

        #pragma exclude_renderers psp2
        #pragma target 3.0
  
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0 - Occlusion estimation with CameraDepthTexture
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAO
                #pragma multi_compile _ APPLY_FORWARD_FOG
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #define SOURCE_DEPTH
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 1 - Occlusion estimation with G-Buffer
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragAO
                #pragma multi_compile _ APPLY_FORWARD_FOG
                #pragma multi_compile _ FOG_LINEAR FOG_EXP FOG_EXP2
                #define SOURCE_GBUFFER
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 2 - Separable blur (horizontal pass) with CameraDepthNormalsTexture
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_DEPTHNORMALS
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 3 - Separable blur (horizontal pass) with G-Buffer
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define SOURCE_GBUFFER
                #define BLUR_HORIZONTAL
                #define BLUR_SAMPLE_CENTER_NORMAL
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 4 - Separable blur (vertical pass)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBlur
                #define BLUR_VERTICAL
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 5 - Final composition
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragComposition
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 6 - Final composition (ambient only mode)
        Pass
        {
            Blend Zero OneMinusSrcColor, Zero OneMinusSrcAlpha

            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragCompositionGBuffer
                #include "ScalableAO.hlsl"

            ENDHLSL
        }

        // 7 - Debug overlay
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragDebugOverlay
                #include "ScalableAO.hlsl"

            ENDHLSL
        }
    }
}
