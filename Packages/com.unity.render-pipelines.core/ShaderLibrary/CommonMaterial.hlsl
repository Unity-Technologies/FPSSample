#ifndef UNITY_COMMON_MATERIAL_INCLUDED
#define UNITY_COMMON_MATERIAL_INCLUDED

//-----------------------------------------------------------------------------
// Define constants
//-----------------------------------------------------------------------------

#define DEFAULT_SPECULAR_VALUE 0.04

//-----------------------------------------------------------------------------
// Helper functions for roughness
//-----------------------------------------------------------------------------

real PerceptualRoughnessToRoughness(real perceptualRoughness)
{
    return perceptualRoughness * perceptualRoughness;
}

real RoughnessToPerceptualRoughness(real roughness)
{
    return sqrt(roughness);
}

real RoughnessToPerceptualSmoothness(real roughness)
{
    return 1.0 - sqrt(roughness);
}

real PerceptualSmoothnessToRoughness(real perceptualSmoothness)
{
    return (1.0 - perceptualSmoothness) * (1.0 - perceptualSmoothness);
}

real PerceptualSmoothnessToPerceptualRoughness(real perceptualSmoothness)
{
    return (1.0 - perceptualSmoothness);
}

// WARNING: this has been deprecated, and should not be used!
// Using roughness values of 0 leads to INFs and NANs. The only sensible place to use the roughness
// value of 0 is IBL, so we do not modify the perceptual roughness which is used to select the MIP map level.
// Note: making the constant too small results in aliasing.
real ClampRoughnessForAnalyticalLights(real roughness)
{
    return max(roughness, 1.0 / 1024.0);
}

void ConvertAnisotropyToRoughness(real perceptualRoughness, real anisotropy, out real roughnessT, out real roughnessB)
{
    real roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    // Use the parametrization of Sony Imageworks.
    // Ref: Revisiting Physically Based Shading at Imageworks, p. 15.
    roughnessT = roughness * (1 + anisotropy);
    roughnessB = roughness * (1 - anisotropy);
}

void ConvertRoughnessToAnisotropy(real roughnessT, real roughnessB, out real anisotropy)
{
    anisotropy = ((roughnessT - roughnessB) / max(roughnessT + roughnessB, 0.0001));
}

// WARNING: this has been deprecated, and should not be used!
// Same as ConvertAnisotropyToRoughness but
// roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
void ConvertAnisotropyToClampRoughness(real perceptualRoughness, real anisotropy, out real roughnessT, out real roughnessB)
{
    ConvertAnisotropyToRoughness(perceptualRoughness, anisotropy, roughnessT, roughnessB);

    roughnessT = ClampRoughnessForAnalyticalLights(roughnessT);
    roughnessB = ClampRoughnessForAnalyticalLights(roughnessB);
}

// Use with stack BRDF (clear coat / coat) - This only used same equation to convert from Blinn-Phong spec power to Beckmann roughness
real RoughnessToVariance(real roughness)
{
    return 2.0 / Sq(roughness) - 2.0;
}

real VarianceToRoughness(real variance)
{
    return sqrt(2.0 / (variance + 2.0));
}

// Normal Map Filtering - This must match HDRP\Editor\AssetProcessors\NormalMapFilteringTexturePostprocessor.cs - highestVarianceAllowed (TODO: Move in core)
#define NORMALMAP_HIGHEST_VARIANCE 0.03125

float DecodeVariance(float gradientW)
{
    return gradientW * NORMALMAP_HIGHEST_VARIANCE;
}

// Return modified perceptualSmoothness based on provided variance (get from GeometricNormalVariance + TextureNormalVariance)
float NormalFiltering(float perceptualSmoothness, float variance, float threshold)
{
    float roughness = PerceptualSmoothnessToRoughness(perceptualSmoothness);
    // Ref: Geometry into Shading - http://graphics.pixar.com/library/BumpRoughness/paper.pdf - equation (3)
    float squaredRoughness = saturate(roughness * roughness + min(2.0 * variance, threshold * threshold)); // threshold can be really low, square the value for easier control

    return RoughnessToPerceptualSmoothness(sqrt(squaredRoughness));
}

// Reference: Error Reduction and Simplification for Shading Anti-Aliasing
// Specular antialiasing for geometry-induced normal (and NDF) variations: Tokuyoshi / Kaplanyan et al.'s method.
// This is the deferred approximation, which works reasonably well so we keep it for forward too for now.
// screenSpaceVariance should be at most 0.5^2 = 0.25, as that corresponds to considering
// a gaussian pixel reconstruction kernel with a standard deviation of 0.5 of a pixel, thus 2 sigma covering the whole pixel.
float GeometricNormalVariance(float3 geometricNormalWS, float screenSpaceVariance)
{
    float3 deltaU = ddx(geometricNormalWS);
    float3 deltaV = ddy(geometricNormalWS);

    return screenSpaceVariance * (dot(deltaU, deltaU) + dot(deltaV, deltaV));
}

// Return modified perceptualSmoothness
float GeometricNormalFiltering(float perceptualSmoothness, float3 geometricNormalWS, float screenSpaceVariance, float threshold)
{
    float variance = GeometricNormalVariance(geometricNormalWS, screenSpaceVariance);
    return NormalFiltering(perceptualSmoothness, variance, threshold);
}

// Normal map filtering based on The Order : 1886 SIGGRAPH course notes implementation.
// Basically Toksvig with an intermediate single vMF lobe induced dispersion (Han et al. 2007)
//
// This returns 2 times the variance of the induced "mesoNDF" lobe (an NDF induced from a section of
// the normal map) from the level 0 mip normals covered by the "current texel".
//
// avgNormalLength gives the dispersion information for the covered normals.
//
// Note that hw filtering on the normal map should be trilinear to be conservative, while anisotropic
// risk underfiltering. Could also compute average normal on the fly with a proper normal map format,
// like Toksvig.
float TextureNormalVariance(float avgNormalLength)
{
    float variance = 0.0;

    if (avgNormalLength < 1.0)
    {
        float avgNormLen2 = avgNormalLength * avgNormalLength;
        float kappa = (3.0 * avgNormalLength - avgNormalLength * avgNormLen2) / (1.0 - avgNormLen2);

        // Ref: Frequency Domain Normal Map Filtering - http://www.cs.columbia.edu/cg/normalmap/normalmap.pdf (equation 21)
        // Relationship between between the standard deviation of a Gaussian distribution and the roughness parameter of a Beckmann distribution.
        // is roughness^2 = 2 variance    (note: variance is sigma^2)
        // (Ref: Filtering Distributions of Normals for Shading Antialiasing - Equation just after (14))
        // Relationship between gaussian lobe and vMF lobe is 2 * variance = 1 / (2 * kappa) = roughness^2
        // (Equation 36 of  Normal map filtering based on The Order : 1886 SIGGRAPH course notes implementation).
        // So to get variance we must use variance = 1 / (4 * kappa)
        variance = 0.25 / kappa;
    }

    return variance;
}

float TextureNormalFiltering(float perceptualSmoothness, float avgNormalLength, float threshold)
{
    float variance = TextureNormalVariance(avgNormalLength);
    return NormalFiltering(perceptualSmoothness, variance, threshold);
}

// ----------------------------------------------------------------------------
// Helper for Disney parametrization
// ----------------------------------------------------------------------------

float3 ComputeDiffuseColor(float3 baseColor, float metallic)
{
    return baseColor * (1.0 - metallic);
}

float3 ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)
{
    return lerp(dielectricF0.xxx, baseColor, metallic);
}

// ----------------------------------------------------------------------------
// Helper for normal blending
// ----------------------------------------------------------------------------

// ref https://www.gamedev.net/topic/678043-how-to-blend-world-space-normals/#entry5287707
// assume compositing in world space
// Note: Using vtxNormal = real3(0, 0, 1) give the BlendNormalRNM formulation.
// TODO: Untested
real3 BlendNormalWorldspaceRNM(real3 n1, real3 n2, real3 vtxNormal)
{
    // Build the shortest-arc quaternion
    real4 q = real4(cross(vtxNormal, n2), dot(vtxNormal, n2) + 1.0) / sqrt(2.0 * (dot(vtxNormal, n2) + 1));

    // Rotate the normal
    return n1 * (q.w * q.w - dot(q.xyz, q.xyz)) + 2 * q.xyz * dot(q.xyz, n1) + 2 * q.w * cross(q.xyz, n1);
}

// ref http://blog.selfshadow.com/publications/blending-in-detail/
// ref https://gist.github.com/selfshadow/8048308
// Reoriented Normal Mapping
// Blending when n1 and n2 are already 'unpacked' and normalised
// assume compositing in tangent space
real3 BlendNormalRNM(real3 n1, real3 n2)
{
    real3 t = n1.xyz + real3(0.0, 0.0, 1.0);
    real3 u = n2.xyz * real3(-1.0, -1.0, 1.0);
    real3 r = (t / t.z) * dot(t, u) - u;
    return r;
}

// assume compositing in tangent space
real3 BlendNormal(real3 n1, real3 n2)
{
    return normalize(real3(n1.xy * n2.z + n2.xy * n1.z, n1.z * n2.z));
}

// ----------------------------------------------------------------------------
// Helper for triplanar
// ----------------------------------------------------------------------------

// Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch01.html / http://www.slideshare.net/icastano/cascades-demo-secrets
real3 ComputeTriplanarWeights(real3 normal)
{
    // Determine the blend weights for the 3 planar projections.
    real3 blendWeights = abs(normal);
    // Tighten up the blending zone
    blendWeights = (blendWeights - 0.2);
    blendWeights = blendWeights * blendWeights * blendWeights; // pow(blendWeights, 3);
    // Force weights to sum to 1.0 (very important!)
    blendWeights = max(blendWeights, real3(0.0, 0.0, 0.0));
    blendWeights /= dot(blendWeights, 1.0);

    return blendWeights;
}

// Planar/Triplanar convention for Unity in world space
void GetTriplanarCoordinate(float3 position, out float2 uvXZ, out float2 uvXY, out float2 uvZY)
{
    // Caution: This must follow the same rule as what is use for SurfaceGradient triplanar
    // TODO: Currently the normal mapping looks wrong without SURFACE_GRADIENT option because we don't handle corretly the tangent space
    uvXZ = float2(position.z, position.x);
    uvXY = float2(position.x, position.y);
    uvZY = float2(position.z, position.y);
}

// ----------------------------------------------------------------------------
// Helper for detail map operation
// ----------------------------------------------------------------------------

real LerpWhiteTo(real b, real t)
{
    real oneMinusT = 1.0 - t;
    return oneMinusT + b * t;
}

real3 LerpWhiteTo(real3 b, real t)
{
    real oneMinusT = 1.0 - t;
    return real3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}

#endif // UNITY_COMMON_MATERIAL_INCLUDED
