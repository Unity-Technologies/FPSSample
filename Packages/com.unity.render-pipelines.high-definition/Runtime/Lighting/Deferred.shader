Shader "Hidden/HDRenderPipeline/Deferred"
{
    Properties
    {
        // We need to be able to control the blend mode for deferred shader in case we do multiple pass
        [HideInInspector] _SrcBlend("", Float) = 1
        [HideInInspector] _DstBlend("", Float) = 1

        [HideInInspector] _StencilMask("_StencilMask", Int) = 7
        [HideInInspector] _StencilRef("", Int) = 0
        [HideInInspector] _StencilCmp("", Int) = 3
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Stencil
            {
                ReadMask[_StencilMask]
                Ref  [_StencilRef]
                Comp [_StencilCmp]
                Pass Keep
            }

            ZWrite Off
            ZTest  Always
            Blend [_SrcBlend] [_DstBlend], One Zero
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            // Chose supported lighting architecture in case of deferred rendering
            #pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS

            // Split lighting is utilized during the SSS pass.
            #pragma multi_compile _ OUTPUT_SPLIT_LIGHTING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DEBUG_DISPLAY

            #define USE_FPTL_LIGHTLIST // deferred opaque always use FPTL

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #define SHADERPASS SHADERPASS_DEFERRED_LIGHTING

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem),
            // the deferred shader will require to use multicompile.
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
            #endif
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl" // This include Material.hlsl

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            struct Outputs
            {
            #ifdef OUTPUT_SPLIT_LIGHTING
                float4 specularLighting : SV_Target0;
                float3 diffuseLighting  : SV_Target1;
            #else
                float4 combinedLighting : SV_Target0;
            #endif
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                return output;
            }

            Outputs Frag(Varyings input)
            {
                // This need to stay in sync with deferred.compute

                // input.positionCS is SV_Position
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, input.positionCS.xy).x;

                PositionInputs posInput = GetPositionInput_Stereo(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, uint2(input.positionCS.xy) / GetTileSize(), unity_StereoEyeIndex);
                float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

                BSDFData bsdfData;
                BuiltinData builtinData;
                DECODE_FROM_GBUFFER(posInput.positionSS, UINT_MAX, bsdfData, builtinData);

                PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

                float3 diffuseLighting;
                float3 specularLighting;
                LightLoop(V, posInput, preLightData, bsdfData, builtinData, LIGHT_FEATURE_MASK_FLAGS_OPAQUE, diffuseLighting, specularLighting);

                Outputs outputs;

            #ifdef OUTPUT_SPLIT_LIGHTING
                if (_EnableSubsurfaceScattering != 0 && ShouldOutputSplitLighting(bsdfData))
                {
                    outputs.specularLighting = float4(specularLighting, 1.0);
                    outputs.diffuseLighting  = TagLightingForSSS(diffuseLighting);
                }
                else
                {
                    outputs.specularLighting = float4(diffuseLighting + specularLighting, 1.0);
                    outputs.diffuseLighting  = 0;
                }
            #else
                outputs.combinedLighting = float4(diffuseLighting + specularLighting, 1.0);
            #endif

                return outputs;
            }

        ENDHLSL
        }

    }
    Fallback Off
}
