// Various shadow sampling logic.
// Again two versions, one for dynamic resource indexing, one for static resource access.


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ------------------------------------------------------------------
//  PCF Filtering methods
// ------------------------------------------------------------------

float SampleShadow_PCF_Tent_3x3(float4 shadowAtlasSize, float3 coord, float2 sampleBias, Texture2D tex, SamplerComparisonState compSamp)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    float shadow = 0.0;
    float fetchesWeights[4];
    float2 fetchesUV[4];

    SampleShadow_ComputeSamples_Tent_3x3(shadowAtlasSize, coord.xy, fetchesWeights, fetchesUV);
    for (int i = 0; i < 4; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[i].xy, coord.z + dot(fetchesUV[i].xy - coord.xy, sampleBias))).x;
    }
    return shadow;
}

//
//                  5x5 tent PCF sampling (9 taps)
//

// shadowAtlasSize.xy is the shadow atlas size in pixel and shadowAtlasSize.zw is rcp(shadow atlas size)
float SampleShadow_PCF_Tent_5x5(float4 shadowAtlasSize, float3 coord, float2 sampleBias, Texture2D tex, SamplerComparisonState compSamp)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    float shadow = 0.0;
    float fetchesWeights[9];
    float2 fetchesUV[9];

    SampleShadow_ComputeSamples_Tent_5x5(shadowAtlasSize, coord.xy, fetchesWeights, fetchesUV);

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1 && SHADOW_USE_SAMPLE_BIASING == 0
    // the loops are only there to prevent the compiler form coalescing all 9 texture fetches which increases register usage
    int i;
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 0].xy, coord.z + dot(fetchesUV[ 0].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 1].xy, coord.z + dot(fetchesUV[ 1].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 2].xy, coord.z + dot(fetchesUV[ 2].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 3].xy, coord.z + dot(fetchesUV[ 3].xy - coord.xy, sampleBias))).x;
    }

    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 4].xy, coord.z + dot(fetchesUV[ 4].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 5].xy, coord.z + dot(fetchesUV[ 5].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 6].xy, coord.z + dot(fetchesUV[ 6].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 7].xy, coord.z + dot(fetchesUV[ 7].xy - coord.xy, sampleBias))).x;
    }

    shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 8].xy, coord.z + dot(fetchesUV[ 8].xy - coord.xy, sampleBias))).x;
#else
    for (int i = 0; i < 9; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[i].xy, coord.z + dot(fetchesUV[i].xy - coord.xy, sampleBias)), slice).x;
    }
#endif

    return shadow;
}

//
//                  7x7 tent PCF sampling (16 taps)
//
float SampleShadow_PCF_Tent_7x7(float4 shadowAtlasSize, float3 coord, float2 sampleBias, Texture2D tex, SamplerComparisonState compSamp)
{
#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    float shadow = 0.0;
    float fetchesWeights[16];
    float2 fetchesUV[16];

    SampleShadow_ComputeSamples_Tent_7x7(shadowAtlasSize, coord.xy, fetchesWeights, fetchesUV);

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
    // the loops are only there to prevent the compiler form coalescing all 16 texture fetches which increases register usage
    int i;
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 0].xy, coord.z + dot(fetchesUV[ 0].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 1].xy, coord.z + dot(fetchesUV[ 1].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 2].xy, coord.z + dot(fetchesUV[ 2].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 3].xy, coord.z + dot(fetchesUV[ 3].xy - coord.xy, sampleBias))).x;
    }
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 4].xy, coord.z + dot(fetchesUV[ 4].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 5].xy, coord.z + dot(fetchesUV[ 5].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 6].xy, coord.z + dot(fetchesUV[ 6].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 7].xy, coord.z + dot(fetchesUV[ 7].xy - coord.xy, sampleBias))).x;
    }
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 8].xy, coord.z + dot(fetchesUV[ 8].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[ 9] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[ 9].xy, coord.z + dot(fetchesUV[ 9].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[10] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[10].xy, coord.z + dot(fetchesUV[10].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[11] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[11].xy, coord.z + dot(fetchesUV[11].xy - coord.xy, sampleBias))).x;
    }
    UNITY_LOOP
    for (i = 0; i < 1; i++)
    {
        shadow += fetchesWeights[12] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[12].xy, coord.z + dot(fetchesUV[12].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[13] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[13].xy, coord.z + dot(fetchesUV[13].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[14] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[14].xy, coord.z + dot(fetchesUV[14].xy - coord.xy, sampleBias))).x;
        shadow += fetchesWeights[15] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[15].xy, coord.z + dot(fetchesUV[15].xy - coord.xy, sampleBias))).x;
    }
#else
    for(int i = 0; i < 16; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(fetchesUV[i].xy, coord.z + dot(fetchesUV[i].xy - coord.xy, sampleBias))).x;
    }
#endif

    return shadow;
}

//
//                  9 tap adaptive PCF sampling
//
float SampleShadow_PCF_9tap_Adaptive(float4 texelSizeRcp, float3 tcs, float2 sampleBias, float filterSize, Texture2D tex, SamplerComparisonState compSamp)
{
    texelSizeRcp *= filterSize;

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    tcs.z += depthBias;
#endif

    // Terms0 are weights for the individual samples, the other terms are offsets in texel space
    float4 vShadow3x3PCFTerms0 = float4(20.0 / 267.0, 33.0 / 267.0, 55.0 / 267.0, 0.0);
    float4 vShadow3x3PCFTerms1 = float4(texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y);
    float4 vShadow3x3PCFTerms2 = float4(texelSizeRcp.x,  texelSizeRcp.y, 0.0, 0.0);
    float4 vShadow3x3PCFTerms3 = float4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0, 0.0);

    float4 v20Taps;
    v20Taps.x = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z + dot(vShadow3x3PCFTerms1.xy, sampleBias))).x; //  1  1
    v20Taps.y = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z + dot(vShadow3x3PCFTerms1.zy, sampleBias))).x; // -1  1
    v20Taps.z = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z + dot(vShadow3x3PCFTerms1.xw, sampleBias))).x; //  1 -1
    v20Taps.w = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z + dot(vShadow3x3PCFTerms1.zw, sampleBias))).x; // -1 -1
    float flSum = dot(v20Taps.xyzw, float4(0.25, 0.25, 0.25, 0.25));
    // fully in light or shadow? -> bail
    if ((flSum == 0.0) || (flSum == 1.0))
        return flSum;

    // we're in a transition area, do 5 more taps
    flSum *= vShadow3x3PCFTerms0.x * 4.0;

    float4 v33Taps;
    v33Taps.x = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z + dot(vShadow3x3PCFTerms2.xz, sampleBias))).x; //  1  0
    v33Taps.y = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z + dot(vShadow3x3PCFTerms3.xz, sampleBias))).x; // -1  0
    v33Taps.z = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z + dot(vShadow3x3PCFTerms3.zy, sampleBias))).x; //  0 -1
    v33Taps.w = SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, float3(tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z + dot(vShadow3x3PCFTerms2.zy, sampleBias))).x; //  0  1
    flSum += dot(v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy);

    flSum += SAMPLE_TEXTURE2D_SHADOW(tex, compSamp, tcs).x * vShadow3x3PCFTerms0.z;

    return flSum;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/ShadowMoments.hlsl"

//
//                  1 tap VSM sampling
//
float SampleShadow_VSM_1tap(float3 tcs, float lightLeakBias, float varianceBias, Texture2D tex, SamplerState samp)
{
#if UNITY_REVERSED_Z
    float  depth      = 1.0 - tcs.z;
#else
    float  depth      = tcs.z;
#endif

    float2 moments = SAMPLE_TEXTURE2D_LOD(tex, samp, tcs.xy, 0.0).xy;

    return ShadowMoments_ChebyshevsInequality(moments, depth, varianceBias, lightLeakBias);
}

//
//                  1 tap EVSM sampling
//
float SampleShadow_EVSM_1tap(float3 tcs, float lightLeakBias, float varianceBias, float2 evsmExponents, bool fourMoments, Texture2D tex, SamplerState samp)
{
#if UNITY_REVERSED_Z
    float  depth      = 1.0 - tcs.z;
#else
    float  depth      = tcs.z;
#endif

    float2 warpedDepth = ShadowMoments_WarpDepth(depth, evsmExponents);

    float4 moments = SAMPLE_TEXTURE2D_LOD(tex, samp, tcs.xy, 0.0);

    // Derivate of warping at depth
    float2 depthScale  = evsmExponents * warpedDepth;
    float2 minVariance = depthScale * depthScale * varianceBias;

    UNITY_BRANCH
    if (fourMoments)
    {
        float posContrib = ShadowMoments_ChebyshevsInequality(moments.xz, warpedDepth.x, minVariance.x, lightLeakBias);
        float negContrib = ShadowMoments_ChebyshevsInequality(moments.yw, warpedDepth.y, minVariance.y, lightLeakBias);
        return min(posContrib, negContrib);
    }
    else
    {
        return ShadowMoments_ChebyshevsInequality(moments.xy, warpedDepth.x, minVariance.x, lightLeakBias);
    }
}


//
//                  1 tap MSM sampling
//
float SampleShadow_MSM_1tap(float3 tcs, float lightLeakBias, float momentBias, float depthBias, float bpp16, bool useHamburger, Texture2D tex, SamplerState samp)
{
#if UNITY_REVERSED_Z
    float  depth         = (1.0 - tcs.z) - depthBias;
#else
    float  depth         = tcs.z + depthBias;
#endif

    float4 moments = SAMPLE_TEXTURE2D_LOD(tex, samp, tcs.xy, 0.0);
    if (bpp16 != 0.0)
        moments = ShadowMoments_Decode16MSM(moments);

    float3 z;
    float4 b;
    ShadowMoments_SolveMSM(moments, depth, momentBias, z, b);

    if (useHamburger)
        return ShadowMoments_SolveDelta3MSM(z, b.xy, lightLeakBias);
    else
        return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM(z, b, lightLeakBias) : ShadowMoments_SolveDelta3MSM(z, b.xy, lightLeakBias);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDPCSS.hlsl"

//
//                  PCSS sampling
//
float SampleShadow_PCSS(float3 tcs, float2 posSS, float2 scale, float2 offset, float2 sampleBias, float shadowSoftness, float minFilterRadius, int blockerSampleCount, int filterSampleCount, Texture2D tex, SamplerComparisonState compSamp, SamplerState samp)
{
    uint taaFrameIndex = _TaaFrameInfo.z;
    float sampleJitterAngle = InterleavedGradientNoise(posSS.xy, taaFrameIndex) * 2.0 * PI;
    float2 sampleJitter = float2(sin(sampleJitterAngle), cos(sampleJitterAngle));

    //1) Blocker Search
    float averageBlockerDepth = 0.0;
    float numBlockers         = 0.0;
    if (!BlockerSearch(averageBlockerDepth, numBlockers, shadowSoftness + 0.000001, tcs, sampleJitter, sampleBias, tex, samp, blockerSampleCount)) 
        return 1.0;

    //2) Penumbra Estimation
    float filterSize = shadowSoftness * PenumbraSize(tcs.z, averageBlockerDepth);
    filterSize = max(filterSize, minFilterRadius);

    //3) Filter
    return PCSS(tcs, filterSize, scale, offset, sampleBias, sampleJitter, tex, compSamp, filterSampleCount);
}
