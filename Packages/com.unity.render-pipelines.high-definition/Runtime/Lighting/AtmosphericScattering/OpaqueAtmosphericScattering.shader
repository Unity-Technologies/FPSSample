Shader "Hidden/HDRenderPipeline/OpaqueAtmosphericScattering"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #pragma multi_compile _ DEBUG_DISPLAY

        // #pragma enable_d3d11_debug_symbols

        float4x4 _PixelCoordToViewDirWS; // Actually just 3x3, but Unity can only set 4x4

        Texture2DMS<float> _DepthTextureMS;
        
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

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

        inline float4 AtmosphericScatteringCompute(Varyings input, float3 V, float depth)
        {
            PositionInputs posInput = GetPositionInput_Stereo(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);

#if defined(UNITY_SINGLE_PASS_STEREO)
            // XRTODO: fixup and consolidate stereo code relying on _PixelCoordToViewDirWS
            V = -normalize(posInput.positionWS);
#endif

            if (depth == UNITY_RAW_FAR_CLIP_VALUE)
            {
                // When a pixel is at far plane, the world space coordinate reconstruction is not reliable.
                // So in order to have a valid position (for example for height fog) we just consider that the sky is a sphere centered on camera with a radius of 5km (arbitrarily chosen value!)
                // And recompute the position on the sphere with the current camera direction.
                posInput.positionWS = GetCurrentViewPosition() - V * _MaxFogDistance;

                // Warning: we do not modify depth values. Do not use them!
            }

            return EvaluateAtmosphericScattering(posInput, V); // Premultiplied alpha
        }

        float4 Frag(Varyings input) : SV_Target
        {
            float2 positionSS = input.positionCS.xy;
            float3 V          = normalize(mul(float3(positionSS, 1.0), (float3x3)_PixelCoordToViewDirWS));
            float  depth      = LOAD_TEXTURE2D(_CameraDepthTexture, (int2)positionSS).x;

            return AtmosphericScatteringCompute(input, V, depth);
        }

        float4 FragMSAA(Varyings input, uint sampleIndex: SV_SampleIndex) : SV_Target
        {
            float2 positionSS = input.positionCS.xy;
            float3 V          = normalize(mul(float3(positionSS, 1.0), (float3x3)_PixelCoordToViewDirWS));
            float  depth      = _DepthTextureMS.Load((int2)positionSS, sampleIndex).x;

            return AtmosphericScatteringCompute(input, V, depth);
        }
    ENDHLSL

    SubShader
    {
        // 0: NOMSAA
        Pass
        {
            Cull Off ZTest  Always ZWrite Off Blend One OneMinusSrcAlpha // Premultiplied alpha

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }

        // 1: MSAA
        Pass
        {
            Cull Off ZTest  Always ZWrite Off Blend One OneMinusSrcAlpha // Premultiplied alpha

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragMSAA
            ENDHLSL
        }
    }
}
