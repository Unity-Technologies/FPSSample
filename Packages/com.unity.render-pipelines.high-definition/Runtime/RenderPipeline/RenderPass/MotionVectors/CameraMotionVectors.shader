Shader "Hidden/HDRenderPipeline/CameraMotionVectors"
{
    HLSLINCLUDE

        #pragma target 4.5

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"

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

        void Frag(Varyings input, out float4 outColor : SV_Target0)
        {
            float depth = LOAD_TEXTURE2D(_CameraDepthTexture, input.positionCS.xy).x;

            PositionInputs posInput = GetPositionInput_Stereo(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);

            float4 worldPos = float4(posInput.positionWS, 1.0);
            float4 prevPos = worldPos;

            float4 prevClipPos = mul(UNITY_MATRIX_PREV_VP, prevPos);
            float4 curClipPos = mul(UNITY_MATRIX_UNJITTERED_VP, worldPos);

            float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
            float2 positionCS = curClipPos.xy / curClipPos.w;

            // Convert from Clip space (-1..1) to NDC 0..1 space
            float2 velocity = (positionCS - previousPositionCS);
#if UNITY_UV_STARTS_AT_TOP
            velocity.y = -velocity.y;
#endif

            velocity.x = velocity.x * _TextureWidthScaling.y; // _TextureWidthScaling = (2.0, 0.5) for SinglePassDoubleWide (stereo) and (1.0, 1.0) otherwise

            // Convert velocity from Clip space (-1..1) to NDC 0..1 space
            // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
            // Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (velocity * 0.5)
            EncodeVelocity(velocity * 0.5, outColor);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            // We will perform camera motion velocity only where there is no object velocity
            Stencil
            {
                ReadMask 128
                Ref  128 // StencilBitMask.ObjectVelocity
                Comp NotEqual
                Pass Keep
            }

            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
