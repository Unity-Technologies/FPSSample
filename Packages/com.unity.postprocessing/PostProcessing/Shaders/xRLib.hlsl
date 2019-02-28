// VR/AR/xR lib

#ifndef UNITY_POSTFX_XRLIB
#define UNITY_POSTFX_XRLIB

#if defined(UNITY_SINGLE_PASS_STEREO)
CBUFFER_START(UnityStereoGlobals)
    float4x4 unity_StereoMatrixP[2];
    float4x4 unity_StereoMatrixV[2];
    float4x4 unity_StereoMatrixInvV[2];
    float4x4 unity_StereoMatrixVP[2];

    float4x4 unity_StereoCameraProjection[2];
    float4x4 unity_StereoCameraInvProjection[2];
    float4x4 unity_StereoWorldToCamera[2];
    float4x4 unity_StereoCameraToWorld[2];

    float3 unity_StereoWorldSpaceCameraPos[2];
    float4 unity_StereoScaleOffset[2];
CBUFFER_END

CBUFFER_START(UnityStereoEyeIndex)
    int unity_StereoEyeIndex;
CBUFFER_END
#endif

float _RenderViewportScaleFactor;

float2 UnityStereoScreenSpaceUVAdjust(float2 uv, float4 scaleAndOffset)
{
    return uv.xy * scaleAndOffset.xy + scaleAndOffset.zw;
}

float4 UnityStereoScreenSpaceUVAdjust(float4 uv, float4 scaleAndOffset)
{
    return float4(UnityStereoScreenSpaceUVAdjust(uv.xy, scaleAndOffset), UnityStereoScreenSpaceUVAdjust(uv.zw, scaleAndOffset));
}

float2 UnityStereoClampScaleOffset(float2 uv, float4 scaleAndOffset)
{
    return clamp(uv, scaleAndOffset.zw, scaleAndOffset.zw + scaleAndOffset.xy);
}

#if defined(UNITY_SINGLE_PASS_STEREO)
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    scaleOffset.xy *= _RenderViewportScaleFactor;
    return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
}

float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}

float4 UnityStereoTransformScreenSpaceTex(float4 uv)
{
    return float4(UnityStereoTransformScreenSpaceTex(uv.xy), UnityStereoTransformScreenSpaceTex(uv.zw));
}

float2 UnityStereoClamp(float2 uv)
{
    float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
    scaleOffset.xy *= _RenderViewportScaleFactor;
    return UnityStereoClampScaleOffset(uv, scaleOffset);
}

float4 UnityStereoAdjustedTexelSize(float4 texelSize) // Should take in _MainTex_TexelSize
{
	texelSize.x = texelSize.x * 2.0; // texelSize.x = 1/w. For a double-wide texture, the true resolution is given by 2/w. 
	texelSize.z = texelSize.z * 0.5; // texelSize.z = w. For a double-wide texture, the true size of the eye texture is given by w/2. 
	return texelSize;
}
#else
float2 TransformStereoScreenSpaceTex(float2 uv, float w)
{
    return uv * _RenderViewportScaleFactor;
}

float2 UnityStereoTransformScreenSpaceTex(float2 uv)
{
    return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
}

float2 UnityStereoClamp(float2 uv)
{
    float4 scaleOffset = float4(_RenderViewportScaleFactor, _RenderViewportScaleFactor, 0.f, 0.f);
    return UnityStereoClampScaleOffset(uv, scaleOffset);
}

float4 UnityStereoAdjustedTexelSize(float4 texelSize)
{
	return texelSize;
}
#endif

#endif // UNITY_POSTFX_XRLIB
