#ifndef UNITY_SCREEN_SPACE_TRACING_INCLUDED
#define UNITY_SCREEN_SPACE_TRACING_INCLUDED

// How this file works:
// This file is separated in two sections: 1. Library, 2. Constant Buffer Specific Signatures
//
// 1. Library
//   This section contains all function and structures for the Screen Space Tracing.
//
// 2. Constant Buffer Specific Signatures
//   This section defines signatures that will use specifics constant buffers.
// Thus you can use the Screen Space Tracing library with different settings.
// It can be usefull to use it for both reflection and refraction but with different settings' sets.
//
//
// To use this file:
// 1. Define the macro SSRTID
// 2. Include the file
// 3. Undef the macro SSRTID
//
//
// Example for reflection:
// #define SSRTID Reflection
// #include "ScreenSpaceTracing.hlsl"
// #undef SSRTID
//
// Use library here, like ScreenSpaceProxyRaycastReflection(...)

// -------------------------------------------------
// Output
// -------------------------------------------------
struct ScreenSpaceRayHit
{
    uint2 positionSS;           // Position of the hit point (SS)
    float2 positionNDC;         // Position of the hit point (NDC)
    float linearDepth;          // Linear depth of the hit point
};

struct ScreenSpaceProxyRaycastInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)
    EnvLightData proxyData;     // Proxy to use for raycasting
};

// -------------------------------------------------
// Algorithm: Scene Proxy Raycasting
// -------------------------------------------------
// We perform a raycast against a proxy volume that approximate the current scene.
// Is is a simple shape (Sphere, Box).
// -------------------------------------------------
bool ScreenSpaceProxyRaycastRefraction(ScreenSpaceProxyRaycastInput input, out ScreenSpaceRayHit hit)
{
    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);

    float3x3 worldToPS      = WorldToProxySpace(input.proxyData);
    float3 rayOriginPS      = WorldToProxyPosition(input.proxyData, worldToPS, input.rayOriginWS);
    float3 rayDirPS         = mul(input.rayDirWS, worldToPS);

    float projectionDistance = 0.0;

    switch(input.proxyData.influenceShapeType)
    {
        case ENVSHAPETYPE_SPHERE:
        case ENVSHAPETYPE_SKY:
        {
            projectionDistance = IntersectSphereProxy(input.proxyData, rayDirPS, rayOriginPS);
            break;
        }
        case ENVSHAPETYPE_BOX:
            projectionDistance = IntersectBoxProxy(input.proxyData, rayDirPS, rayOriginPS);
            break;
    }

    float3 hitPositionWS    = input.rayOriginWS + input.rayDirWS * projectionDistance;
    float4 hitPositionCS    = ComputeClipSpacePosition(hitPositionWS, GetWorldToHClipMatrix());
    float4 rayOriginCS      = ComputeClipSpacePosition(input.rayOriginWS, GetWorldToHClipMatrix());
    float2 hitPositionNDC   = ComputeNormalizedDeviceCoordinates(hitPositionWS, GetWorldToHClipMatrix());
    uint2 hitPositionSS     = uint2(hitPositionNDC *_ScreenSize.xy);
    float hitLinearDepth    = hitPositionCS.w;

    hit.positionNDC         = hitPositionNDC;
    hit.positionSS          = hitPositionSS;
    hit.linearDepth         = hitLinearDepth;

    bool hitSuccessful      = hitLinearDepth > 0;       // Negative means that the hit is behind the camera

    return hitSuccessful;
}

#endif

