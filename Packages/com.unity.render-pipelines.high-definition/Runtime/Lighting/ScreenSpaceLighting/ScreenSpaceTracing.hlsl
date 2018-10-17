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

// #################################################
// Notes
// #################################################
// Some banding issues can occurs when raymarching the depth buffer.
//
// This can be hidden by offsetting the ray origin with a jitter.
// Combined with a temporal filtering, the banding artifact will be smoothed.
// This will trade banding for noise.
//
// This happens when we raymarch with a ray direction that is quite different from the view vector.
// Exemple when raymarching with a direction perpendicular to the view vector:
//
// Depth buffer  far
//                |
//                v
//               near
//
//         --------
//  hit ==>xx
//         xx
//
//  fail ===>
//           xx
//  hit  ===>xx
//
//              xx


// #################################################
// Screen Space Tracing Library
// #################################################

// -------------------------------------------------
// Algorithm uniform parameters
// -------------------------------------------------
const float DepthPlaneBias = 1E-5;

// -------------------------------------------------
// Output
// -------------------------------------------------

struct ScreenSpaceRayHit
{
    uint2 positionSS;           // Position of the hit point (SS)
    float2 positionNDC;         // Position of the hit point (NDC)
    float linearDepth;          // Linear depth of the hit point

#ifdef DEBUG_DISPLAY
    float3 debugOutput;
#endif
};

struct ScreenSpaceRaymarchInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)

#ifdef DEBUG_DISPLAY
    bool debug;
#endif
};

struct ScreenSpaceProxyRaycastInput
{
    float3 rayOriginWS;         // Ray origin (WS)
    float3 rayDirWS;            // Ray direction (WS)
    EnvLightData proxyData;     // Proxy to use for raycasting

#ifdef DEBUG_DISPLAY
    bool debug;
#endif
};

// -------------------------------------------------
// Utilities
// -------------------------------------------------

// Calculate the ray origin and direction in SS
void CalculateRaySS(
    float3 rayOriginWS,             // Ray origin (World Space)
    float3 rayDirWS,                // Ray direction (World Space)
    uint2 bufferSize,               // Texture size of screen buffers
    out float3 positionSS,          // (x, y, 1/linearDepth)
    out float3 raySS,               // (dx, dy, d(1/linearDepth))
    out float rayEndDepth           // Linear depth of the end point used to calculate raySS
)
{
    const float kNearClipPlane = -0.01;
    const float kMaxRayTraceDistance = 1000;

    float3 rayOriginVS = mul(GetWorldToViewMatrix(), float4(rayOriginWS, 1.0)).xyz;
    float3 rayDirVS = mul((float3x3)GetWorldToViewMatrix(), rayDirWS);
    // Clip ray to near plane to avoid raymarching behind camera
    float rayLength = ((rayOriginVS.z + rayDirVS.z * kMaxRayTraceDistance) > kNearClipPlane)
        ? ((kNearClipPlane - rayOriginVS.z) / rayDirVS.z)
        : kMaxRayTraceDistance;

    float3 positionWS = rayOriginWS;
    float3 rayEndWS = rayOriginWS + rayDirWS * rayLength;

    float4 positionCS = ComputeClipSpacePosition(positionWS, GetWorldToHClipMatrix());
    float4 rayEndCS = ComputeClipSpacePosition(rayEndWS, GetWorldToHClipMatrix());

    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, GetWorldToHClipMatrix());
    float2 rayEndNDC = ComputeNormalizedDeviceCoordinates(rayEndWS, GetWorldToHClipMatrix());
    rayEndDepth = rayEndCS.w;

    float3 rayStartSS = float3(
        positionNDC.xy * bufferSize,
        1.0 / positionCS.w); // Screen space depth interpolate properly in 1/z

    float3 rayEndSS = float3(
        rayEndNDC.xy * bufferSize,
        1.0 / rayEndDepth); // Screen space depth interpolate properly in 1/z

    positionSS = rayStartSS;
    raySS = rayEndSS - rayStartSS;
}

// Sample the Depth buffer at a specific mip and linear depth
float2 LoadDepth(float2 positionSS, int level)
{
    float2 pyramidDepth = LOAD_TEXTURE2D_LOD(_DepthPyramidTexture, int2(positionSS.xy) >> level, level).rg;
    float2 linearDepth = float2(LinearEyeDepth(pyramidDepth.r, _ZBufferParams), LinearEyeDepth(pyramidDepth.g, _ZBufferParams));
    return linearDepth;
}

// Sample the Depth buffer at a specific mip and return 1/linear depth
float2 LoadInvDepth(float2 positionSS, int level)
{
    float2 linearDepth = LoadDepth(positionSS, level);
    float2 invLinearDepth = 1 / linearDepth;
    return invLinearDepth;
}

bool CellAreEquals(int2 cellA, int2 cellB)
{
    return cellA.x == cellB.x && cellA.y == cellB.y;
}

// Calculate intersection between the ray and the depth plane
// positionSS.z is 1/depth
// raySS.z is 1/depth
float3 IntersectDepthPlane(float3 positionSS, float3 raySS, float invDepth)
{
    // The depth of the intersection with the depth plane is: positionSS.z + raySS.z * t = invDepth
    float t = (invDepth - positionSS.z) / raySS.z;

    // (t<0) When the ray is going away from the depth plane,
    //  put the intersection away.
    // Instead the intersection with the next tile will be used.
    // (t>=0) Add a small distance to go through the depth plane.
    t = t >= 0.0f ? (t + DepthPlaneBias) : 1E5;

    // Return the point on the ray
    return positionSS + raySS * t;
}

float2 CalculateDistanceToCellPlanes(
    float3 positionSS,              // Ray Origin (Screen Space, 1/LinearDepth)
    float2 invRaySS,                // 1/RayDirection
    int2 cellId,                    // (Row, Colum) of the cell
    uint2 cellSize,                 // Size of the cell in pixel
    int2 cellPlanes                 // Planes to intersect (one of (0,0), (1, 0), (0, 1), (1, 1))
)
{
    // Planes to check
    int2 planes = (cellId + cellPlanes) * cellSize;
    // Hit distance to each planes
    float2 distanceToCellAxes = float2(planes - positionSS.xy) * invRaySS; // (distance to x axis, distance to y axis)
    return distanceToCellAxes;
}

// Calculate intersection between a ray and a cell
float3 IntersectCellPlanes(
    float3 positionSS,              // Ray Origin (Screen Space, 1/LinearDepth)
    float3 raySS,                   // Ray Direction (Screen Space, 1/LinearDepth)
    float2 invRaySS,                // 1/RayDirection
    int2 cellId,                    // (Row, Colum) of the cell
    uint2 cellSize,                 // Size of the cell in pixel
    int2 cellPlanes,                // Planes to intersect (one of (0,0), (1, 0), (0, 1), (1, 1))
    float2 crossOffset              // Offset to use to ensure cell boundary crossing
)
{
    float2 distanceToCellAxes = CalculateDistanceToCellPlanes(
        positionSS,
        invRaySS,
        cellId,
        cellSize,
        cellPlanes
    );

    float t = min(distanceToCellAxes.x, distanceToCellAxes.y)
        // Offset to ensure cell crossing
        // This assume that length(raySS.xy) == 1;
        + 0.1;
    // Interpolate screen space to get next test point
    float3 testHitPositionSS = positionSS + raySS * t;

    return testHitPositionSS;
}

float CalculateHitWeight(
    ScreenSpaceRayHit hit,
    float2 startPositionSS,
    float minLinearDepth,
    float settingsDepthBufferThickness,
    float settingsRayMaxScreenDistance,
    float settingsRayBlendScreenDistance
)
{
    // Blend when the hit is near the thickness of the object
    //float thicknessWeight = clamp(1 - (hit.linearDepth - minLinearDepth) / settingsDepthBufferThickness, 0, 1);

    // Blend when the ray when the raymarched distance is too long
    float2 screenDistanceNDC = abs(hit.positionSS.xy - startPositionSS) * _ScreenSize.zw;
    float2 screenDistanceWeights = clamp((settingsRayMaxScreenDistance - screenDistanceNDC) / settingsRayBlendScreenDistance, 0, 1);
    float screenDistanceWeight = min(screenDistanceWeights.x, screenDistanceWeights.y);

//    return thicknessWeight * screenDistanceWeight;
    return screenDistanceWeight;
}

float SampleBayer4(uint2 positionSS)
{
    const float4x4 Bayer4 = float4x4(0,  8,  2,  10,
                                         12, 4,  14, 6,
                                         3,  11, 1,  9,
                                         15, 7,  13, 5) / 16;

    return Bayer4[positionSS.x % 4][positionSS.y % 4];
}

// -------------------------------------------------
// Algorithms
// -------------------------------------------------

// -------------------------------------------------
// Algorithm: Linear Raymarching
// -------------------------------------------------
// Based on Digital Differential Analyzer and Morgan McGuire's Screen Space Ray Tracing (http://casual-effects.blogspot.fr/2014/08/screen-space-ray-tracing.html)
//
// Linear raymarching algorithm with precomputed properties
// -------------------------------------------------
bool ScreenSpaceLinearRaymarch(
    ScreenSpaceRaymarchInput input,
    // Settings
    int settingRayLevel,                            // Mip level to use to ray march depth buffer
    uint settingsRayMaxIterations,                  // Maximum number of iterations (= max number of depth samples)
    float settingsDepthBufferThickness,              // Bias to use when trying to detect whenever we raymarch behind a surface
    float settingsRayMaxScreenDistance,             // Maximum screen distance raymarched
    float settingsRayBlendScreenDistance,           // Distance to blend before maximum screen distance is reached
    int settingsDebuggedAlgorithm,                  // currently debugged algorithm (see PROJECTIONMODEL defines)
    // Precomputed properties
    float3 startPositionSS,                         // Start position in Screen Space (x in pixel, y in pixel, z = 1/linearDepth)
    float3 raySS,                                   // Ray direction in Screen Space (dx in pixel, dy in pixel, z = 1/endPointLinearDepth - 1/startPointLinearDepth)
    float rayEndDepth,                              // Linear depth of the end point used to calculate raySS.
    uint2 bufferSize,                               // Texture size of screen buffers
    // Out
    out ScreenSpaceRayHit hit,
    out float hitWeight,
    out uint iteration
)
{
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = false;
    iteration = 0u;
    hitWeight = 0;
    int mipLevel = min(max(settingRayLevel, 0), int(_DepthPyramidScale.z));
    uint maxIterations = settingsRayMaxIterations;

    float3 positionSS = startPositionSS;
    raySS /= max(abs(raySS.x), abs(raySS.y));
    raySS *= 1 << mipLevel;

#ifdef DEBUG_DISPLAY
    float3 debugIterationPositionSS = positionSS;
    uint debugIteration = iteration;
    float debugIterationLinearDepthBufferMin = 0;
    float debugIterationLinearDepthBufferMinThickness = 0;
    float debugIterationLinearDepthBufferMax = 0;
#endif

    float2 invLinearDepth = float2(0.0, 0.0);

    float minLinearDepth                = 0;
    float minLinearDepthWithThickness   = 0;
    float positionLinearDepth           = 0;

    for (iteration = 0u; iteration < maxIterations; ++iteration)
    {
        positionSS += raySS;

        // Sampled as 1/Z so it interpolate properly in screen space.
        invLinearDepth = LoadInvDepth(positionSS.xy, mipLevel);

        minLinearDepth                  = 1 / invLinearDepth.r;
        minLinearDepthWithThickness     = minLinearDepth + settingsDepthBufferThickness;
        positionLinearDepth             = 1 / positionSS.z;
        bool isAboveDepth               = positionLinearDepth < minLinearDepth;
        bool isAboveThickness           = positionLinearDepth < minLinearDepthWithThickness;
        bool isBehindDepth              = !isAboveThickness;
        bool intersectWithDepth         = !isAboveDepth && isAboveThickness;

        if (intersectWithDepth)
        {
            hitSuccessful = true;
            break;
        }

        // Check if we are out of the buffer
        if (any(int2(positionSS.xy) > int2(bufferSize))
            || any(positionSS.xy < 0)
            )
        {
            hitSuccessful = false;
            break;
        }
    }

    if (iteration >= maxIterations)
        hitSuccessful = false;

    hit.linearDepth = 1 / positionSS.z;
    hit.positionNDC = float2(positionSS.xy) / float2(bufferSize);
    hit.positionSS = uint2(positionSS.xy);

    // Detect when we go behind an object given a thickness
    hitWeight = CalculateHitWeight(
        hit,
        startPositionSS.xy,
        invLinearDepth.r,
        settingsDepthBufferThickness,
        settingsRayMaxScreenDistance,
        settingsRayBlendScreenDistance
    );

    if (hitWeight <= 0)
        hitSuccessful = false;

    return hitSuccessful;
}

// -------------------------------------------------
// Algorithm: Scene Proxy Raycasting
// -------------------------------------------------
// We perform a raycast against a proxy volume that approximate the current scene.
// Is is a simple shape (Sphere, Box).
// -------------------------------------------------
bool ScreenSpaceProxyRaycast(
    ScreenSpaceProxyRaycastInput input,
    // Settings
    int settingsDebuggedAlgorithm,             // currently debugged algorithm (see PROJECTIONMODEL defines)
    // Out
    out ScreenSpaceRayHit hit
)
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

// -------------------------------------------------
// Algorithm: Linear Raymarching And Scene Proxy Raycasting
// -------------------------------------------------
// Perform a linear raymarching for close hit detection and fallback on proxy raycasting
// -------------------------------------------------
bool ScreenSpaceLinearProxyRaycast(
    ScreenSpaceProxyRaycastInput input,
    // Settings (linear)
    int settingRayLevel,                    // Mip level to use to ray march depth buffer
    uint settingsRayMaxIterations,          // Maximum number of iterations (= max number of depth samples)
    float settingsDepthBufferThickness,      // Bias to use when trying to detect whenever we raymarch behind a surface
    float settingsRayMaxScreenDistance,     // Maximum screen distance raymarched
    float settingsRayBlendScreenDistance,   // Distance to blend before maximum screen distance is reached
    // Settings (common)
    int settingsDebuggedAlgorithm,          // currently debugged algorithm (see PROJECTIONMODEL defines)
    // Out
    out ScreenSpaceRayHit hit
)
{
    // Perform linear raymarch
    ScreenSpaceRaymarchInput inputLinear;
    inputLinear.rayOriginWS = input.rayOriginWS;
    inputLinear.rayDirWS = input.rayDirWS;
#ifdef DEBUG_DISPLAY
    inputLinear.debug = input.debug;
#endif

    uint2 bufferSize = uint2(_DepthPyramidSize.xy);

    // Compute properties for linear raymarch
    float3 startPositionSS;
    float3 raySS;
    float rayEndDepth;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS,
        rayEndDepth
    );

    uint iteration;
    float hitWeight;
    bool hitSuccessful = ScreenSpaceLinearRaymarch(
        inputLinear,
        // Settings
        settingRayLevel,
        settingsRayMaxIterations,
        settingsDepthBufferThickness,
        settingsRayMaxScreenDistance,
        settingsRayBlendScreenDistance,
        settingsDebuggedAlgorithm,
        // Precomputed properties
        startPositionSS,
        raySS,
        rayEndDepth,
        bufferSize,
        // Out
        hit,
        hitWeight,
        iteration
    );

    if (!hitSuccessful)
    {
        hitSuccessful = ScreenSpaceProxyRaycast(
            input,
            // Settings
            settingsDebuggedAlgorithm,
            // Out
            hit
        );
    }

    return hitSuccessful;
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------
// Based on Yasin Uludag, 2014. "Hi-Z Screen-Space Cone-Traced Reflections", GPU Pro5: Advanced Rendering Techniques
//
// NB: We perform first a linear raymarch to handle close hits, then we perform the actual HiZ raymarching.
// We do this for two reasons:
//  - It is cheaper in case of close hit than starting with HiZ
//  - It will start the HiZ algorithm with an offset, preventing false positive hit at ray origin.
// -------------------------------------------------
bool ScreenSpaceHiZRaymarch(
    ScreenSpaceRaymarchInput input,
    // Settings
    uint settingsRayMinLevel,                       // Minimum mip level to use for ray marching the depth buffer in HiZ
    uint settingsRayMaxLevel,                       // Maximum mip level to use for ray marching the depth buffer in HiZ
    uint settingsRayMaxIterations,                  // Maximum number of iteration for the HiZ raymarching (= number of depth sample for HiZ)
    float settingsDepthBufferThickness,             // Bias to use when trying to detect whenever we raymarch behind a surface
    float settingsRayMaxScreenDistance,             // Maximum screen distance raymarched
    float settingsRayBlendScreenDistance,           // Distance to blend before maximum screen distance is reached
    bool settingsRayMarchBehindObjects,             // Whether to raymarch behind objects
    int settingsDebuggedAlgorithm,                  // currently debugged algorithm (see PROJECTIONMODEL defines)
    // out
    out ScreenSpaceRayHit hit,
    out float hitWeight
)
{
    const float2 CROSS_OFFSET = float2(1, 1);

    // Initialize loop
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    hitWeight = 0;
    bool hitSuccessful = false;
    uint iteration = 0u;
    int minMipLevel = max(settingsRayMinLevel, 0u);
    int maxMipLevel = min(settingsRayMaxLevel, uint(_DepthPyramidScale.z));
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    uint maxIterations = settingsRayMaxIterations;

    float3 startPositionSS;
    float3 raySS;
    float rayEndDepth;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS,
        rayEndDepth
    );

#ifdef DEBUG_DISPLAY
    // Initialize debug variables
    int debugLoopMipMaxUsedLevel = minMipLevel;
    int debugIterationMipLevel = minMipLevel;
    uint2 debugIterationCellSize = uint2(0u, 0u);
    float3 debugIterationPositionSS = float3(0, 0, 0);
    uint debugIteration = 0u;
    uint debugIterationIntersectionKind = 0u;
    float debugIterationLinearDepthBufferMin = 0;
    float debugIterationLinearDepthBufferMinThickness = 0;
    float debugIterationLinearDepthBufferMax = 0;
#endif

    iteration = 0u;
    int intersectionKind = 0;
    float raySSLength = length(raySS.xy);
    raySS /= raySSLength;
    // Initialize raymarching
    float2 invRaySS = float2(1, 1) / raySS.xy;

    // Calculate planes to intersect for each cell
    int2 cellPlanes = sign(raySS.xy);
    float2 crossOffset = CROSS_OFFSET * cellPlanes;
    cellPlanes = clamp(cellPlanes, 0, 1);

    int currentLevel = minMipLevel;
    uint2 cellCount = bufferSize >> currentLevel;
    uint2 cellSize = uint2(1, 1) << currentLevel;

    float3 positionSS = startPositionSS;
    float2 invLinearDepth = float2(0.0, 0.0);

    float positionLinearDepth           = 0;
    float minLinearDepth                = 0;
    float minLinearDepthWithThickness   = 0;

    // Intersect with first cell and add an offsot to avoid HiZ raymarching to stuck at the origin
    {
        const float epsilon = 1E-3;
        const float minTraversal = 2 << currentLevel;

        float2 distanceToCellAxes = CalculateDistanceToCellPlanes(
            positionSS,
            invRaySS,
            int2(positionSS.xy) / cellSize,
            cellSize,
            cellPlanes
        );

        float t = min(distanceToCellAxes.x * minTraversal + epsilon, distanceToCellAxes.y * minTraversal + epsilon);
        positionSS = positionSS + raySS * t;
    }

    bool isBehindDepth = false;
    while (currentLevel >= minMipLevel)
    {
        hitSuccessful = true;
        if (iteration >= maxIterations)
        {
            hitSuccessful = false;
            break;
        }

        cellCount = bufferSize >> currentLevel;
        cellSize = uint2(1, 1) << currentLevel;


#ifdef DEBUG_DISPLAY
        // Fetch pre iteration debug values
        if (input.debug && _DebugStep >= int(iteration))
            debugIterationMipLevel = currentLevel;
#endif

        // Go down in HiZ levels by default
        int mipLevelDelta = -1;

        // Sampled as 1/Z so it interpolate properly in screen space.
        invLinearDepth = LoadInvDepth(positionSS.xy, currentLevel);

        positionLinearDepth                 = 1 / positionSS.z;
        minLinearDepth                      = 1 / invLinearDepth.r;
        minLinearDepthWithThickness         = minLinearDepth + settingsDepthBufferThickness;
        bool isAboveDepth                   = positionLinearDepth < minLinearDepth;
        bool isAboveThickness               = positionLinearDepth < minLinearDepthWithThickness;
        isBehindDepth                       = !isAboveThickness;
        bool intersectWithDepth             = minLinearDepth >= positionLinearDepth && isAboveThickness;

        intersectionKind = HIZINTERSECTIONKIND_NONE;

        // Nominal case, we raymarch in front of the depth buffer and accelerate with HiZ
        if (isAboveDepth)
        {
            float3 candidatePositionSS = IntersectDepthPlane(positionSS, raySS, invLinearDepth.r);

            intersectionKind = HIZINTERSECTIONKIND_DEPTH;

            const int2 cellId = int2(positionSS.xy) / cellSize;
            const int2 candidateCellId = int2(candidatePositionSS.xy) / cellSize;

            // If we crossed the current cell
            if (!CellAreEquals(cellId, candidateCellId))
            {
                candidatePositionSS = IntersectCellPlanes(
                    positionSS,
                    raySS,
                    invRaySS,
                    cellId,
                    cellSize,
                    cellPlanes,
                    crossOffset
                );

                intersectionKind = HIZINTERSECTIONKIND_CELL;

                // Go up a level to go faster
                mipLevelDelta = 1;
            }

            positionSS = candidatePositionSS;
        }
        // Raymarching behind object in depth buffer, this case degenerate into a linear search
        else if (settingsRayMarchBehindObjects && isBehindDepth && currentLevel <= (minMipLevel + 1))
        {
            const int2 cellId = int2(positionSS.xy) / cellSize;

            positionSS = IntersectCellPlanes(
                positionSS,
                raySS,
                invRaySS,
                cellId,
                cellSize,
                cellPlanes,
                crossOffset
            );

            intersectionKind = HIZINTERSECTIONKIND_CELL;

            mipLevelDelta = 1;
        }

        currentLevel = min(currentLevel + mipLevelDelta, maxMipLevel);
        float4 distancesToBorders = float4(positionSS.xy, bufferSize - positionSS.xy);
        float distanceToBorders = min(min(distancesToBorders.x, distancesToBorders.y), min(distancesToBorders.z, distancesToBorders.w));
        int minLevelForBorders = int(log2(distanceToBorders));
        currentLevel = min(currentLevel, minLevelForBorders);

#ifdef DEBUG_DISPLAY
        // Fetch post iteration debug values
        if (input.debug && _DebugStep >= int(iteration))
        {
            debugLoopMipMaxUsedLevel = max(debugLoopMipMaxUsedLevel, currentLevel);
            debugIterationPositionSS = positionSS;
            debugIterationLinearDepthBufferMin = 1 / invLinearDepth.r;
            debugIterationLinearDepthBufferMinThickness = 1 / invLinearDepth.r + settingsDepthBufferThickness;
            debugIterationLinearDepthBufferMax = 1 / invLinearDepth.g;
            debugIteration = iteration;
            debugIterationIntersectionKind = intersectionKind;
            debugIterationCellSize = cellSize;
        }
#endif

        // Check if we are out of the buffer
        if (any(int2(positionSS.xy) > int2(bufferSize))
            || any(positionSS.xy < 0))
        {
            hitSuccessful = false;
            break;
        }

        ++iteration;
    }

    hit.linearDepth = positionLinearDepth;
    hit.positionSS = uint2(positionSS.xy);
    hit.positionNDC = float2(hit.positionSS) / float2(bufferSize);

    // Detect when we go behind an object given a thickness

    hitWeight = CalculateHitWeight(
        hit,
        startPositionSS.xy,
        minLinearDepth,
        settingsDepthBufferThickness,
        settingsRayMaxScreenDistance,
        settingsRayBlendScreenDistance
    );

    if (hitWeight <= 0 || isBehindDepth)
        hitSuccessful = false;

    return hitSuccessful;
}
#endif





// #################################################
// Screen Space Tracing CB Specific Signatures
// #################################################

#ifdef SSRTID
// -------------------------------------------------
// Macros
// -------------------------------------------------
#define SSRT_SETTING(name, SSRTID) _SS ## SSRTID ## name

// -------------------------------------------------
// Constant buffers
// -------------------------------------------------
CBUFFER_START(MERGE_NAME(UnityScreenSpaceRaymarching, SSRTID))
int SSRT_SETTING(RayLevel, SSRTID);
int SSRT_SETTING(RayMinLevel, SSRTID);
int SSRT_SETTING(RayMaxLevel, SSRTID);
int SSRT_SETTING(RayMaxIterations, SSRTID);
float SSRT_SETTING(DepthBufferThickness, SSRTID);
float SSRT_SETTING(RayMaxScreenDistance, SSRTID);
float SSRT_SETTING(RayBlendScreenDistance, SSRTID);
int SSRT_SETTING(RayMarchBehindObjects, SSRTID);

#ifdef DEBUG_DISPLAY
int SSRT_SETTING(DebuggedAlgorithm, SSRTID);
#endif
CBUFFER_END

// -------------------------------------------------
// Algorithm: Linear Raymarching
// -------------------------------------------------
bool MERGE_NAME(ScreenSpaceLinearRaymarch, SSRTID)(
    ScreenSpaceRaymarchInput input,
    out ScreenSpaceRayHit hit,
    out float hitWeight
)
{
    uint2 bufferSize = uint2(_DepthPyramidSize.xy);
    float3 startPositionSS;
    float3 raySS;
    float rayEndDepth;
    CalculateRaySS(
        input.rayOriginWS,
        input.rayDirWS,
        bufferSize,
        startPositionSS,
        raySS,
        rayEndDepth
    );

    uint iteration;
    return ScreenSpaceLinearRaymarch(
        input,
        // settings
        SSRT_SETTING(RayLevel, SSRTID),
        SSRT_SETTING(RayMaxIterations, SSRTID),
        max(0.01, SSRT_SETTING(DepthBufferThickness, SSRTID)),
        SSRT_SETTING(RayMaxScreenDistance, SSRTID),
        SSRT_SETTING(RayBlendScreenDistance, SSRTID),
        0,
        // precomputed properties
        startPositionSS,
        raySS,
        rayEndDepth,
        bufferSize,
        // out
        hit,
        hitWeight,
        iteration
    );
}

// -------------------------------------------------
// Algorithm: Scene Proxy Raycasting
// -------------------------------------------------
bool MERGE_NAME(ScreenSpaceProxyRaycast, SSRTID)(
    ScreenSpaceProxyRaycastInput input,
    out ScreenSpaceRayHit hit
)
{
    int debuggedAlgorithm = int(0);

    return ScreenSpaceProxyRaycast(
        input,
        // Settings
        debuggedAlgorithm,
        // Out
        hit
    );
}

// -------------------------------------------------
// Algorithm: HiZ raymarching
// -------------------------------------------------
bool MERGE_NAME(ScreenSpaceHiZRaymarch, SSRTID)(
    ScreenSpaceRaymarchInput input,
    out ScreenSpaceRayHit hit,
    out float hitWeight
)
{
    return ScreenSpaceHiZRaymarch(
        input,
        // Settings
        SSRT_SETTING(RayMinLevel, SSRTID),
        SSRT_SETTING(RayMaxLevel, SSRTID),
        SSRT_SETTING(RayMaxIterations, SSRTID),
        max(0.01, SSRT_SETTING(DepthBufferThickness, SSRTID)),
        SSRT_SETTING(RayMaxScreenDistance, SSRTID),
        SSRT_SETTING(RayBlendScreenDistance, SSRTID),
        SSRT_SETTING(RayMarchBehindObjects, SSRTID) == 1,
        0,
        // out
        hit,
        hitWeight
    );
}

// -------------------------------------------------
// Cleaning
// -------------------------------------------------
#undef SSRT_SETTING
#endif
