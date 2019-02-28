// Pi variables are redefined here as UnityCG.cginc is not included for compute shader as it adds too many unused uniforms to constant buffers
#ifndef UNITY_CG_INCLUDED
#define UNITY_PI            3.14159265359f
#define UNITY_TWO_PI        6.28318530718f
#define UNITY_FOUR_PI       12.56637061436f
#define UNITY_INV_PI        0.31830988618f
#define UNITY_INV_TWO_PI    0.15915494309f
#define UNITY_INV_FOUR_PI   0.07957747155f
#define UNITY_HALF_PI       1.57079632679f
#define UNITY_INV_HALF_PI   0.636619772367f
#endif

// TODO Null implem at the moment
float4 VFXTransformPositionWorldToClip(float3 posWS)
{
    return (float4)0.0f;
}

float4 VFXTransformPositionObjectToClip(float3 posOS)
{
    return (float4)0.0f;
}

float3 VFXTransformPositionWorldToView(float3 posWS)
{
    return (float3)0.0f;
}

float4x4 VFXGetObjectToWorldMatrix()
{
    return (float4x4)0.0f;
}

float4x4 VFXGetWorldToObjectMatrix()
{
    return (float4x4)0.0f;
}

float3x3 VFXGetWorldToViewRotMatrix()
{
    return (float3x3)0.0f;
}

float3 VFXGetViewWorldPosition()
{
    return (float3)0.0f;
}

float VFXLinearEyeDepth(float4 posSS)
{
    return 0.0f;
}

float4 VFXApplyFog(float4 color,float4 posSS,float3 posWS)
{
    return color;
}
