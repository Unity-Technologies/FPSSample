Shader "Hidden/HDRenderPipeline/Sky/HDRISky"
{
    HLSLINCLUDE

    #pragma vertex Vert
    #pragma fragment Frag

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    TEXTURECUBE(_Cubemap);
    SAMPLER(sampler_Cubemap);

    float4   _SkyParam; // x exposure, y multiplier, z rotation
    float4x4 _PixelCoordToViewDirWS; // Actually just 3x3, but Unity can only set 4x4

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
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID, UNITY_RAW_FAR_CLIP_VALUE);
        return output;
    }

    float4 Frag(Varyings input) : SV_Target
    {
#if defined(UNITY_SINGLE_PASS_STEREO)
        // The computed PixelCoordToViewDir matrix doesn't seem to capture stereo eye offset. 
        // So for VR, we compute WSPosition using the stereo matrices instead.
        PositionInputs posInput = GetPositionInput_Stereo(input.positionCS.xy, _ScreenSize.zw, input.positionCS.z, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, unity_StereoEyeIndex);
        float3 dir = normalize(posInput.positionWS);
#else
        // Points towards the camera
        float3 viewDirWS = normalize(mul(float3(input.positionCS.xy, 1.0), (float3x3)_PixelCoordToViewDirWS));
        // Reverse it to point into the scene
        float3 dir = -viewDirWS;
#endif

        // Rotate direction
        float phi = DegToRad(_SkyParam.z);
        float cosPhi, sinPhi;
        sincos(phi, sinPhi, cosPhi);
        float3 rotDirX = float3(cosPhi, 0, -sinPhi);
        float3 rotDirY = float3(sinPhi, 0, cosPhi);
        dir = float3(dot(rotDirX, dir), dir.y, dot(rotDirY, dir));

        float3 skyColor = ClampToFloat16Max(SAMPLE_TEXTURECUBE_LOD(_Cubemap, sampler_Cubemap, dir, 0).rgb * exp2(_SkyParam.x) * _SkyParam.y);
        return float4(skyColor, 1.0);
    }

    ENDHLSL

    SubShader
    {
        // For cubemap
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            ENDHLSL

        }

        // For fullscreen Sky
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend Off
            Cull Off

            HLSLPROGRAM
            ENDHLSL
        }

    }
    Fallback Off
}
