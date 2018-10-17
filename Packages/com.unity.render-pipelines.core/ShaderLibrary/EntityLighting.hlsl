#ifndef UNITY_ENTITY_LIGHTING_INCLUDED
#define UNITY_ENTITY_LIGHTING_INCLUDED

#include "Common.hlsl"

#define LIGHTMAP_RGBM_MAX_GAMMA     real(5.0)       // NB: Must match value in RGBMRanges.h
#define LIGHTMAP_RGBM_MAX_LINEAR    real(34.493242) // LIGHTMAP_RGBM_MAX_GAMMA ^ 2.2

#ifdef UNITY_LIGHTMAP_RGBM_ENCODING
    #ifdef UNITY_COLORSPACE_GAMMA
        #define LIGHTMAP_HDR_MULTIPLIER LIGHTMAP_RGBM_MAX_GAMMA
        #define LIGHTMAP_HDR_EXPONENT   real(1.0)   // Not used in gamma color space
    #else
        #define LIGHTMAP_HDR_MULTIPLIER LIGHTMAP_RGBM_MAX_LINEAR
        #define LIGHTMAP_HDR_EXPONENT   real(2.2)
    #endif
#elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    #ifdef UNITY_COLORSPACE_GAMMA
        #define LIGHTMAP_HDR_MULTIPLIER real(2.0)
    #else
        #define LIGHTMAP_HDR_MULTIPLIER real(4.59) // 2.0 ^ 2.2
    #endif
    #define LIGHTMAP_HDR_EXPONENT real(0.0)
#else // (UNITY_LIGHTMAP_FULL_HDR)
    #define LIGHTMAP_HDR_MULTIPLIER real(1.0)
    #define LIGHTMAP_HDR_EXPONENT real(1.0)
#endif

// TODO: Check if PI is correctly handled!

// Ref: "Efficient Evaluation of Irradiance Environment Maps" from ShaderX 2
real3 SHEvalLinearL0L1(real3 N, real4 shAr, real4 shAg, real4 shAb)
{
    real4 vA = real4(N, 1.0);

    real3 x1;
    // Linear (L1) + constant (L0) polynomial terms
    x1.r = dot(shAr, vA);
    x1.g = dot(shAg, vA);
    x1.b = dot(shAb, vA);

    return x1;
}

real3 SHEvalLinearL2(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)
{
    real3 x2;
    // 4 of the quadratic (L2) polynomials
    real4 vB = N.xyzz * N.yzzx;
    x2.r = dot(shBr, vB);
    x2.g = dot(shBg, vB);
    x2.b = dot(shBb, vB);

    // Final (5th) quadratic (L2) polynomial
    real vC = N.x * N.x - N.y * N.y;
    real3 x3 = shC.rgb * vC;

    return x2 + x3;
}

#if HAS_HALF
half3 SampleSH9(half4 SHCoefficients[7], half3 N)
{
    half4 shAr = SHCoefficients[0];
    half4 shAg = SHCoefficients[1];
    half4 shAb = SHCoefficients[2];
    half4 shBr = SHCoefficients[3];
    half4 shBg = SHCoefficients[4];
    half4 shBb = SHCoefficients[5];
    half4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

    return res;
}
#endif
float3 SampleSH9(float4 SHCoefficients[7], float3 N)
{
    float4 shAr = SHCoefficients[0];
    float4 shAg = SHCoefficients[1];
    float4 shAb = SHCoefficients[2];
    float4 shBr = SHCoefficients[3];
    float4 shBg = SHCoefficients[4];
    float4 shBb = SHCoefficients[5];
    float4 shCr = SHCoefficients[6];

    // Linear + constant polynomial terms
    float3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);

    return res;
}


// This sample a 3D volume storing SH
// Volume is store as 3D texture with 4 R, G, B, Occ set of 4 coefficient store atlas in same 3D texture. Occ is use for occlusion.
// TODO: the packing here is inefficient as we will fetch values far away from each other and they may not fit into the cache - Suggest we pack RGB continuously
// TODO: The calcul of texcoord could be perform with a single matrix multicplication calcualted on C++ side that will fold probeVolumeMin and probeVolumeSizeInv into it and handle the identity case, no reasons to do it in C++ (ask Ionut about it)
// It should also handle the camera relative path (if the render pipeline use it)
float3 SampleProbeVolumeSH4(TEXTURE3D_ARGS(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float3 normalWS, float4x4 WorldToTexture,
                            float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv)
{
    float3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
    float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;
    // Each component is store in the same texture 3D. Each use one quater on the x axis
    // Here we get R component then increase by step size (0.25) to get other component. This assume 4 component
    // but last one is not used.
    // Clamp to edge of the "internal" texture, as R is from half texel to size of R texture minus half texel.
    // This avoid leaking
    texCoord.x = clamp(texCoord.x * 0.25, 0.5 * texelSizeX, 0.25 - 0.5 * texelSizeX);

    float4 shAr = SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
    texCoord.x += 0.25;
    float4 shAg = SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
    texCoord.x += 0.25;
    float4 shAb = SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);

    return SHEvalLinearL0L1(normalWS, shAr, shAg, shAb);
}

float4 SampleProbeOcclusion(TEXTURE3D_ARGS(SHVolumeTexture, SHVolumeSampler), float3 positionWS, float4x4 WorldToTexture,
                            float transformToLocal, float texelSizeX, float3 probeVolumeMin, float3 probeVolumeSizeInv)
{
    float3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
    float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;

    // Sample fourth texture in the atlas
    // We need to compute proper U coordinate to sample.
    // Clamp the coordinate otherwize we'll have leaking between ShB coefficients and Probe Occlusion(Occ) info
    texCoord.x = max(texCoord.x * 0.25 + 0.75, 0.75 + 0.5 * texelSizeX);

    return SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
}

// Following functions are to sample enlighten lightmaps (or lightmaps encoded the same way as our
// enlighten implementation). They assume use of RGB9E5 for dynamic illuminance map and RGBM for baked ones.
// It is required for other platform that aren't supporting this format to implement variant of these functions
// (But these kind of platform should use regular render loop and not news shaders).

// TODO: This is the max value allowed for emissive (bad name - but keep for now to retrieve it) (It is 8^2.2 (gamma) and 8 is the limit of punctual light slider...), comme from UnityCg.cginc. Fix it!
// Ask Jesper if this can be change for HDRenderPipeline
#define EMISSIVE_RGBM_SCALE 97.0

// RGBM stuff is temporary. For now baked lightmap are in RGBM and the RGBM range for lightmaps is specific so we can't use the generic method.
// In the end baked lightmaps are going to be BC6H so the code will be the same as dynamic lightmaps.
// Same goes for emissive packed as an input for Enlighten with another hard coded multiplier.

// TODO: This function is used with the LightTransport pass to encode lightmap or emissive
real4 PackEmissiveRGBM(real3 rgb)
{
    real kOneOverRGBMMaxRange = 1.0 / EMISSIVE_RGBM_SCALE;
    const real kMinMultiplier = 2.0 * 1e-2;

    real4 rgbm = real4(rgb * kOneOverRGBMMaxRange, 1.0);
        rgbm.a = max(max(rgbm.r, rgbm.g), max(rgbm.b, kMinMultiplier));
    rgbm.a = ceil(rgbm.a * 255.0) / 255.0;

    // Division-by-zero warning from d3d9, so make compiler happy.
    rgbm.a = max(rgbm.a, kMinMultiplier);

    rgbm.rgb /= rgbm.a;
    return rgbm;
}

real3 UnpackLightmapRGBM(real4 rgbmInput, real4 decodeInstructions)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return rgbmInput.rgb * (rgbmInput.a * decodeInstructions.x);
#else
    return rgbmInput.rgb * (PositivePow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x);
#endif
}

real3 UnpackLightmapDoubleLDR(real4 encodedColor, real4 decodeInstructions)
{
    return encodedColor.rgb * decodeInstructions.x;
}

real3 DecodeLightmap(real4 encodedIlluminance, real4 decodeInstructions)
{
#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    return UnpackLightmapRGBM(encodedIlluminance, decodeInstructions);
#elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
    return UnpackLightmapDoubleLDR(encodedIlluminance, decodeInstructions);
#else // (UNITY_LIGHTMAP_FULL_HDR)
    return encodedIlluminance.rgb;
#endif
}

real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
{
    // Take into account texture alpha if decodeInstructions.w is true(the alpha value affects the RGB channels)
    real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);

    // If Linear mode is not supported we can skip exponent part
    return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
}

real3 SampleSingleLightmap(TEXTURE2D_ARGS(lightmapTex, lightmapSampler), float2 uv, float4 transform, bool encodedLightmap, real4 decodeInstructions)
{
    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;
    real3 illuminance = real3(0.0, 0.0, 0.0);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    if (encodedLightmap)
    {
        real4 encodedIlluminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgba;
        illuminance = DecodeLightmap(encodedIlluminance, decodeInstructions);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgb;
    }
    return illuminance;
}

real3 SampleDirectionalLightmap(TEXTURE2D_ARGS(lightmapTex, lightmapSampler), TEXTURE2D_ARGS(lightmapDirTex, lightmapDirSampler), float2 uv, float4 transform, float3 normalWS, bool encodedLightmap, real4 decodeInstructions)
{
    // In directional mode Enlighten bakes dominant light direction
    // in a way, that using it for half Lambert and then dividing by a "rebalancing coefficient"
    // gives a result close to plain diffuse response lightmaps, but normalmapped.

    // Note that dir is not unit length on purpose. Its length is "directionality", like
    // for the directional specular lightmaps.

    // transform is scale and bias
    uv = uv * transform.xy + transform.zw;

    real4 direction = SAMPLE_TEXTURE2D(lightmapDirTex, lightmapDirSampler, uv);
    // Remark: baked lightmap is RGBM for now, dynamic lightmap is RGB9E5
    real3 illuminance = real3(0.0, 0.0, 0.0);
    if (encodedLightmap)
    {
        real4 encodedIlluminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgba;
        illuminance = DecodeLightmap(encodedIlluminance, decodeInstructions);
    }
    else
    {
        illuminance = SAMPLE_TEXTURE2D(lightmapTex, lightmapSampler, uv).rgb;
    }
    real halfLambert = dot(normalWS, direction.xyz - 0.5) + 0.5;
    return illuminance * halfLambert / max(1e-4, direction.w);
}

#endif // UNITY_ENTITY_LIGHTING_INCLUDED
