#include "UnityCG.cginc"

Texture2D _CameraDepthTexture;

float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return UnityWorldToClipPos(posWS);
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    return UnityObjectToClipPos(posOS);
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
    return mul(UNITY_MATRIX_V, float4(posWS, 1.0f)).xyz;
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return unity_ObjectToWorld;
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return unity_WorldToObject;
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)UNITY_MATRIX_V;
}

float3 VFXGetViewWorldPosition()
{
    // Not using _WorldSpaceCameraPos as it's not what expected for the shadow pass
    // (It remains primary camera position not view position)
    return UNITY_MATRIX_I_V._m03_m13_m23;
}

float4x4 VFXGetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
}

float VFXSampleDepth(float4 posSS)
{
    return _CameraDepthTexture.Load(int3(posSS.xy, 0)).r;
}

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth);
}

float4 VFXApplyShadowBias(float4 posCS)
{
    return UnityApplyLinearShadowBias(posCS);
}

float4 VFXApplyFog(float4 color,float4 posSS,float3 posWS)
{
    return color; // TODO
}
