#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
#if VFX_WORLD_SPACE
    posWS = GetCameraRelativePositionWS(posWS);
#endif
    return TransformWorldToHClip(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    float3 posWS = TransformObjectToWorld(posOS);
    return VFXTransformPositionWorldToClip(posWS);
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
#if VFX_WORLD_SPACE
    posWS = GetCameraRelativePositionWS(posWS);
#endif
    return TransformWorldToView(posWS);
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return GetObjectToWorldMatrix();
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return GetWorldToObjectMatrix();
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)GetWorldToViewMatrix();
}

float3 VFXGetViewWorldPosition()
{
    float3 pos = GetCurrentViewPosition();
#if VFX_WORLD_SPACE
    pos = GetAbsolutePositionWS(pos);
#endif
    return pos;
}

float4x4 VFXGetViewToWorldMatrix()
{
    float4x4 viewToWorld = UNITY_MATRIX_I_V;
    viewToWorld._14_24_34 = VFXGetViewWorldPosition();
    return viewToWorld;
}

float VFXSampleDepth(float4 posSS)
{
    return LOAD_TEXTURE2D(_CameraDepthTexture, posSS.xy).r;
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth,_ZBufferParams);
}

float4 VFXApplyShadowBias(float4 posCS)
{
    return posCS;
}

float4 VFXApplyFog(float4 color,float4 posCS,float3 posWS)
{
#if VFX_WORLD_SPACE
    posWS = GetCameraRelativePositionWS(posWS); // posWS is absolute in World Space
#endif
    PositionInputs posInput = GetPositionInput(posCS.xy, _ScreenSize.zw, posCS.z, posCS.w, posWS, uint2(0,0));
    float4 fog = EvaluateAtmosphericScattering(posInput, GetWorldSpaceNormalizeViewDir(posWS));
#if VFX_BLENDMODE_ALPHA
    color.rgb = lerp(color.rgb, fog.rgb, fog.a);
#elif VFX_BLENDMODE_ADD
    color.rgb *= 1.0 - fog.a;
#elif VFX_BLENDMODE_PREMULTIPLY
    color.rgb = lerp(color.rgb, fog.rgb * color.a, fog.a);
#endif
    return color;
}
