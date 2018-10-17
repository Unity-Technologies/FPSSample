Shader "Hidden/HDRenderPipeline/DebugViewMaterialGBuffer"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ SHADOWS_SHADOWMASK

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // CAUTION: In case deferred lighting need to support various lighting model statically, we will require to do multicompile with different define like UNITY_MATERIAL_LIT
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #define DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // input.positionCS is SV_Position
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, input.positionCS.xy).x;
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

                BSDFData bsdfData;
                BuiltinData builtinData;
                DECODE_FROM_GBUFFER(posInput.positionSS, UINT_MAX, bsdfData, builtinData);

                // Init to not expected value
                float3 result = float3(-666.0, 0.0, 0.0);
                bool needLinearToSRGB = false;

                if (_DebugViewMaterial == DEBUGVIEWGBUFFER_DEPTH)
                {
                    float linearDepth = frac(posInput.linearDepth * 0.1);
                    result = linearDepth.xxx;
                }
                // Caution: This value is not the same than the builtin data bakeDiffuseLighting. It also include emissive and multiply by the albedo
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_DIFFUSE_LIGHTING_WITH_ALBEDO_PLUS_EMISSIVE)
                {
                    result = builtinData.bakeDiffuseLighting;;
                    result *= exp2(_DebugExposure);
                    needLinearToSRGB = true;
                }
                #ifdef SHADOWS_SHADOWMASK
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK0)
                {
                    result = builtinData.shadowMask0.xxx;
                }
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK1)
                {
                    result = builtinData.shadowMask1.xxx;
                }
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK2)
                {
                    result = builtinData.shadowMask2.xxx;
                }
                else if (_DebugViewMaterial == DEBUGVIEWGBUFFER_BAKE_SHADOW_MASK3)
                {
                    result = builtinData.shadowMask3.xxx;
                }
                #endif

                GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB);

                // f we haven't touch result, we don't blend it. This allow to have the GBuffer debug pass working with the regular forward debug pass.
                // The forward debug pass will write its value and then the deferred will overwrite only touched texels.
                if (result.x == -666.0)
                {
                    return float4(0.0, 0.0, 0.0, 0.0);
                }
                else
                {
                    // TEMP!
                    // For now, the final blit in the backbuffer performs an sRGB write
                    // So in the meantime we apply the inverse transform to linear data to compensate.
                    if (!needLinearToSRGB)
                        result = SRGBToLinear(max(0, result));

                    return float4(result, 1.0);
                }
            }

            ENDHLSL
        }

    }
    Fallback Off
}
