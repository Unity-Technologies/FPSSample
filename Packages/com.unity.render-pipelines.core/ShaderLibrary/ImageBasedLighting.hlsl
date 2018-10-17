#ifndef UNITY_IMAGE_BASED_LIGHTING_INCLUDED
#define UNITY_IMAGE_BASED_LIGHTING_INCLUDED

#include "CommonLighting.hlsl"
#include "CommonMaterial.hlsl"
#include "BSDF.hlsl"
#include "Random.hlsl"
#include "Sampling/Sampling.hlsl"

#ifndef UNITY_SPECCUBE_LOD_STEPS
    #define UNITY_SPECCUBE_LOD_STEPS 6
#endif

//-----------------------------------------------------------------------------
// Util image based lighting
//-----------------------------------------------------------------------------

// The *approximated* version of the non-linear remapping. It works by
// approximating the cone of the specular lobe, and then computing the MIP map level
// which (approximately) covers the footprint of the lobe with a single texel.
// Improves the perceptual roughness distribution.
real PerceptualRoughnessToMipmapLevel(real perceptualRoughness, uint mipMapCount)
{
    perceptualRoughness = perceptualRoughness * (1.7 - 0.7 * perceptualRoughness);

    return perceptualRoughness * mipMapCount;
}

real PerceptualRoughnessToMipmapLevel(real perceptualRoughness)
{
    return PerceptualRoughnessToMipmapLevel(perceptualRoughness, UNITY_SPECCUBE_LOD_STEPS);
}

// Mapping for convolved Texture2D, this is an empirical remapping to match GGX version of cubemap convolution
real PlanarPerceptualRoughnessToMipmapLevel(real perceptualRoughness, uint mipMapcount)
{
    return PositivePow(perceptualRoughness, 0.8) * uint(max(mipMapcount - 1, 0));
}

// The *accurate* version of the non-linear remapping. It works by
// approximating the cone of the specular lobe, and then computing the MIP map level
// which (approximately) covers the footprint of the lobe with a single texel.
// Improves the perceptual roughness distribution and adds reflection (contact) hardening.
// TODO: optimize!
real PerceptualRoughnessToMipmapLevel(real perceptualRoughness, real NdotR)
{
    real m = PerceptualRoughnessToRoughness(perceptualRoughness);

    // Remap to spec power. See eq. 21 in --> https://dl.dropboxusercontent.com/u/55891920/papers/mm_brdf.pdf
    real n = (2.0 / max(FLT_EPS, m * m)) - 2.0;

    // Remap from n_dot_h formulation to n_dot_r. See section "Pre-convolved Cube Maps vs Path Tracers" --> https://s3.amazonaws.com/docs.knaldtech.com/knald/1.0.0/lys_power_drops.html
    n /= (4.0 * max(NdotR, FLT_EPS));

    // remap back to square root of real roughness (0.25 include both the sqrt root of the conversion and sqrt for going from roughness to perceptualRoughness)
    perceptualRoughness = pow(2.0 / (n + 2.0), 0.25);

    return perceptualRoughness * UNITY_SPECCUBE_LOD_STEPS;
}

// The inverse of the *approximated* version of perceptualRoughnessToMipmapLevel().
real MipmapLevelToPerceptualRoughness(real mipmapLevel)
{
    real perceptualRoughness = saturate(mipmapLevel / UNITY_SPECCUBE_LOD_STEPS);

    return saturate(1.7 / 1.4 - sqrt(2.89 / 1.96 - (2.8 / 1.96) * perceptualRoughness));
}

//-----------------------------------------------------------------------------
// Anisotropic image based lighting
//-----------------------------------------------------------------------------

// Ref: Donald Revie - Implementing Fur Using Deferred Shading (GPU Pro 2)
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to the normal.
// The returned normal is NOT normalized.
real3 ComputeGrainNormal(real3 grainDir, real3 V)
{
    real3 B = cross(grainDir, V);
    return cross(B, grainDir);
}

// Fake anisotropy by distorting the normal (non-negative anisotropy values only).
// The grain direction (e.g. hair or brush direction) is assumed to be orthogonal to N.
// Anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction)
real3 GetAnisotropicModifiedNormal(real3 grainDir, real3 N, real3 V, real anisotropy)
{
    real3 grainNormal = ComputeGrainNormal(grainDir, V);
    return normalize(lerp(N, grainNormal, anisotropy));
}

// For GGX aniso and IBL we have done an empirical (eye balled) approximation compare to the reference.
// We use a single fetch, and we stretch the normal to use based on various criteria.
// result are far away from the reference but better than nothing
// Anisotropic ratio (0->no isotropic; 1->full anisotropy in tangent direction) - positive use bitangentWS - negative use tangentWS
// Note: returned iblPerceptualRoughness shouldn't be use for sampling FGD texture in a pre-integration
void GetGGXAnisotropicModifiedNormalAndRoughness(real3 bitangentWS, real3 tangentWS, real3 N, real3 V, real anisotropy, real perceptualRoughness, out real3 iblN, out real iblPerceptualRoughness)
{
    // For positive anisotropy values: tangent = highlight stretch (anisotropy) direction, bitangent = grain (brush) direction.
    float3 grainDirWS = (anisotropy >= 0.0) ? bitangentWS : tangentWS;
    // Reduce stretching depends on the perceptual roughness
    float stretch = abs(anisotropy) * saturate(1.5 * sqrt(perceptualRoughness));
    // NOTE: If we follow the theory we should use the modified normal for the different calculation implying a normal (like NdotV)
    // However modified normal is just a hack. The goal is just to stretch a cubemap, no accuracy here. Let's save performance instead.
    iblN = GetAnisotropicModifiedNormal(grainDirWS, N, V, stretch);
    iblPerceptualRoughness = perceptualRoughness * saturate(1.2 - abs(anisotropy));
}

// Ref: "Moving Frostbite to PBR", p. 69.
real3 GetSpecularDominantDir(real3 N, real3 R, real perceptualRoughness, real NdotV)
{
    real p = perceptualRoughness;
    real a = 1.0 - p * p;
    real s = sqrt(a);

#ifdef USE_FB_DSD
    // This is the original formulation.
    real lerpFactor = (s + p * p) * a;
#else
    // TODO: tweak this further to achieve a closer match to the reference.
    real lerpFactor = (s + p * p) * saturate(a * a + lerp(0.0, a, NdotV * NdotV));
#endif

    // The result is not normalized as we fetch in a cubemap
    return lerp(N, R, lerpFactor);
}

// ----------------------------------------------------------------------------
// Importance sampling BSDF functions
// ----------------------------------------------------------------------------

void SampleGGXDir(real2   u,
                  real3   V,
                  real3x3 localToWorld,
                  real    roughness,
              out real3   L,
              out real    NdotL,
              out real    NdotH,
              out real    VdotH,
                  bool     VeqN = false)
{
    // GGX NDF sampling
    real cosTheta = sqrt(SafeDiv(1.0 - u.x, 1.0 + (roughness * roughness - 1.0) * u.x));
    real phi      = TWO_PI * u.y;

    real3 localH = SphericalToCartesian(phi, cosTheta);

    NdotH = cosTheta;

    real3 localV;

    if (VeqN)
    {
        // localV == localN
        localV = real3(0.0, 0.0, 1.0);
        VdotH  = NdotH;
    }
    else
    {
        localV = mul(V, transpose(localToWorld));
        VdotH  = saturate(dot(localV, localH));
    }

    // Compute { localL = reflect(-localV, localH) }
    real3 localL = -localV + 2.0 * VdotH * localH;
    NdotL = localL.z;

    L = mul(localL, localToWorld);
}

// Ref: "A Simpler and Exact Sampling Routine for the GGX Distribution of Visible Normals".
// Note: this code will most likely fail if roughness is 0.
void SampleVisibleAnisoGGXDir(real2 u, real3 V, real3x3 localToWorld,
                              real roughnessT, real roughnessB,
                              out real3 L,
                              out real  NdotL,
                              out real  NdotH,
                              out real  VdotH,
                                  bool  VeqN = false)
{
    real3 localV = mul(V, transpose(localToWorld));

    // Construct an orthonormal basis around the stretched view direction.
    real3x3 viewToLocal;
    if (VeqN)
    {
        viewToLocal = k_identity3x3;
    }
    else
    {
        // TODO: this code is tacky. We should make it cleaner.
        viewToLocal[2] = normalize(real3(roughnessT * localV.x, roughnessB * localV.y, localV.z));
        viewToLocal[0] = (viewToLocal[2].z < 0.9999) ? normalize(cross(viewToLocal[2], real3(0, 0, 1))) : real3(1, 0, 0);
        viewToLocal[1] = cross(viewToLocal[0], viewToLocal[2]);
    }

    // Compute a sample point with polar coordinates (r, phi).
    real r   = sqrt(u.x);
    real b   = viewToLocal[2].z + 1;
    real a   = rcp(b);
    real c   = (u.y < a) ? u.y * b : 1 + (u.y * b - 1) / viewToLocal[2].z;
    real phi = PI * c;
    real p1  = r * cos(phi);
    real p2  = r * sin(phi) * ((u.y < a) ? 1 : viewToLocal[2].z);

    // Unstretch.
    real3 viewH = normalize(real3(roughnessT * p1, roughnessB * p2, sqrt(1 - p1 * p1 - p2 * p2)));
    VdotH = viewH.z;

    real3 localH = mul(viewH, viewToLocal);
    NdotH = localH.z;

    // Compute { localL = reflect(-localV, localH) }
    real3 localL = -localV + 2 * VdotH * localH;
    NdotL = localL.z;

    L = mul(localL, localToWorld);
}

// ref: http://blog.selfshadow.com/publications/s2012-shading-course/burley/s2012_pbs_disney_brdf_notes_v3.pdf p26
void SampleAnisoGGXDir(real2 u,
                       real3 V,
                       real3 N,
                       real3 tangentX,
                       real3 tangentY,
                       real  roughnessT,
                       real  roughnessB,
                   out real3 H,
                   out real3 L)
{
    // AnisoGGX NDF sampling
    H = sqrt(u.x / (1.0 - u.x)) * (roughnessT * cos(TWO_PI * u.y) * tangentX + roughnessB * sin(TWO_PI * u.y) * tangentY) + N;
    H = normalize(H);

    // Convert sample from half angle to incident angle
    L = 2.0 * saturate(dot(V, H)) * H - V;
}

// weightOverPdf return the weight (without the diffuseAlbedo term) over pdf. diffuseAlbedo term must be apply by the caller.
void ImportanceSampleLambert(real2   u,
                             real3x3 localToWorld,
                         out real3   L,
                         out real    NdotL,
                         out real    weightOverPdf)
{
#if 0
    real3 localL = SampleHemisphereCosine(u.x, u.y);

    NdotL = localL.z;

    L = mul(localL, localToWorld);
#else
    real3 N = localToWorld[2];

    L     = SampleHemisphereCosine(u.x, u.y, N);
    NdotL = saturate(dot(N, L));
#endif

    // Importance sampling weight for each sample
    // pdf = N.L / PI
    // weight = fr * (N.L) with fr = diffuseAlbedo / PI
    // weight over pdf is:
    // weightOverPdf = (diffuseAlbedo / PI) * (N.L) / (N.L / PI)
    // weightOverPdf = diffuseAlbedo
    // diffuseAlbedo is apply outside the function

    weightOverPdf = 1.0;
}

// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleGGX(real2   u,
                         real3   V,
                         real3x3 localToWorld,
                         real    roughness,
                         real    NdotV,
                     out real3   L,
                     out real    VdotH,
                     out real    NdotL,
                     out real    weightOverPdf)
{
    real NdotH;
    SampleGGXDir(u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH);

    // Importance sampling weight for each sample
    // pdf = D(H) * (N.H) / (4 * (L.H))
    // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
    // weight over pdf is:
    // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
    // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
    // Remind (L.H) == (V.H)
    // F is apply outside the function

    real Vis = V_SmithJointGGX(NdotL, NdotV, roughness);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

// weightOverPdf return the weight (without the Fresnel term) over pdf. Fresnel term must be apply by the caller.
void ImportanceSampleAnisoGGX(real2   u,
                              real3   V,
                              real3x3 localToWorld,
                              real    roughnessT,
                              real    roughnessB,
                              real    NdotV,
                          out real3   L,
                          out real    VdotH,
                          out real    NdotL,
                          out real    weightOverPdf)
{
    real3 tangentX = localToWorld[0];
    real3 tangentY = localToWorld[1];
    real3 N        = localToWorld[2];

    real3 H;
    SampleAnisoGGXDir(u, V, N, tangentX, tangentY, roughnessT, roughnessB, H, L);

    real NdotH = saturate(dot(N, H));
    // Note: since L and V are symmetric around H, LdotH == VdotH
    VdotH = saturate(dot(V, H));
    NdotL = saturate(dot(N, L));

    // Importance sampling weight for each sample
    // pdf = D(H) * (N.H) / (4 * (L.H))
    // weight = fr * (N.L) with fr = F(H) * G(V, L) * D(H) / (4 * (N.L) * (N.V))
    // weight over pdf is:
    // weightOverPdf = F(H) * G(V, L) * (L.H) / ((N.H) * (N.V))
    // weightOverPdf = F(H) * 4 * (N.L) * V(V, L) * (L.H) / (N.H) with V(V, L) = G(V, L) / (4 * (N.L) * (N.V))
    // Remind (L.H) == (V.H)
    // F is apply outside the function

    // For anisotropy we must not saturate these values
    real TdotV = dot(tangentX, V);
    real BdotV = dot(tangentY, V);
    real TdotL = dot(tangentX, L);
    real BdotL = dot(tangentY, L);

    real Vis = V_SmithJointGGXAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB);
    weightOverPdf = 4.0 * Vis * NdotL * VdotH / NdotH;
}

// ----------------------------------------------------------------------------
// Pre-integration
// ----------------------------------------------------------------------------

#if !defined SHADER_API_GLES
// Ref: Listing 18 in "Moving Frostbite to PBR" + https://knarkowicz.wordpress.com/2014/12/27/analytical-dfg-term-for-ibl/
real4 IntegrateGGXAndDisneyDiffuseFGD(real NdotV, real roughness, uint sampleCount = 4096)
{
    // Note that our LUT covers the full [0, 1] range.
    // Therefore, we don't really want to clamp NdotV here (else the lerp slope is wrong).
    // However, if NdotV is 0, the integral is 0, so that's not what we want, either.
    // Our runtime NdotV bias is quite large, so we use a smaller one here instead.
    NdotV     = max(NdotV, FLT_EPS);
    real3 V   = real3(sqrt(1 - NdotV * NdotV), 0, NdotV);
    real4 acc = real4(0.0, 0.0, 0.0, 0.0);

    real3x3 localToWorld = k_identity3x3;

    for (uint i = 0; i < sampleCount; ++i)
    {
        real2 u = Hammersley2d(i, sampleCount);

        real VdotH;
        real NdotL;
        real weightOverPdf;

        real3 L; // Unused
        ImportanceSampleGGX(u, V, localToWorld, roughness, NdotV,
                            L, VdotH, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            // Integral{BSDF * <N,L> dw} =
            // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
            // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
            // (1 - F0) * x + F0 * y = lerp(x, y, F0)

            acc.x += weightOverPdf * pow(1 - VdotH, 5);
            acc.y += weightOverPdf;
        }

        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            real LdotV = dot(L, V);
            real disneyDiffuse = DisneyDiffuseNoPI(NdotV, NdotL, LdotV, RoughnessToPerceptualRoughness(roughness));

            acc.z += disneyDiffuse * weightOverPdf;
        }
    }

    acc /= sampleCount;

    // Remap from the [0.5, 1.5] to the [0, 1] range.
    acc.z -= 0.5;

    return acc;
}
#else
// Not supported due to lack of random library in GLES 2
#define IntegrateGGXAndDisneyDiffuseFGD ERROR_ON_UNSUPPORTED_FUNCTION(IntegrateGGXAndDisneyDiffuseFGD)
#endif

uint GetIBLRuntimeFilterSampleCount(uint mipLevel)
{
    uint sampleCount = 0;

    switch (mipLevel)
    {
        case 1: sampleCount = 21; break;
        case 2: sampleCount = 34; break;
#ifdef SHADER_API_MOBILE
        case 3: sampleCount = 34; break;
        case 4: sampleCount = 34; break;
        case 5: sampleCount = 34; break;
        case 6: sampleCount = 34; break; // UNITY_SPECCUBE_LOD_STEPS
#else
        case 3: sampleCount = 55; break;
        case 4: sampleCount = 89; break;
        case 5: sampleCount = 89; break;
        case 6: sampleCount = 89; break; // UNITY_SPECCUBE_LOD_STEPS
#endif
    }

    return sampleCount;
}

// Ref: Listing 19 in "Moving Frostbite to PBR"
real4 IntegrateLD(TEXTURECUBE_ARGS(tex, sampl),
                   TEXTURE2D(ggxIblSamples),
                   real3 V,
                   real3 N,
                   real roughness,
                   real index,      // Current MIP level minus one
                   real invOmegaP,
                   uint sampleCount, // Must be a Fibonacci number
                   bool prefilter,
                   bool usePrecomputedSamples)
{
    real3x3 localToWorld = GetLocalFrame(N);

#ifndef USE_KARIS_APPROXIMATION
    real NdotV       = 1; // N == V
    real partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
#endif

    real3 lightInt = real3(0.0, 0.0, 0.0);
    real  cbsdfInt = 0.0;

    for (uint i = 0; i < sampleCount; ++i)
    {
        real3 L;
        real  NdotL, NdotH, LdotH;

        if (usePrecomputedSamples)
        {
            // Performance warning: using a texture LUT will generate a vector load,
            // which increases both the VGPR pressure and the workload of the
            // texture unit. A better solution here is to load from a Constant, Raw
            // or Structured buffer, or perhaps even declare all the constants in an
            // HLSL header to allow the compiler to inline everything.
            real3 localL = LOAD_TEXTURE2D(ggxIblSamples, uint2(i, index)).xyz;

            L     = mul(localL, localToWorld);
            NdotL = localL.z;
            LdotH = sqrt(0.5 + 0.5 * NdotL);
        }
        else
        {
            real2 u = Fibonacci2d(i, sampleCount);

            // Note: if (N == V), all of the microsurface normals are visible.
            SampleGGXDir(u, V, localToWorld, roughness, L, NdotL, NdotH, LdotH, true);

            if (NdotL <= 0) continue; // Note that some samples will have 0 contribution
        }

        real mipLevel;

        if (!prefilter) // BRDF importance sampling
        {
            mipLevel = 0;
        }
        else // Prefiltered BRDF importance sampling
        {
            // Use lower MIP-map levels for fetching samples with low probabilities
            // in order to reduce the variance.
            // Ref: http://http.developer.nvidia.com/GPUGems3/gpugems3_ch20.html
            //
            // - OmegaS: Solid angle associated with the sample
            // - OmegaP: Solid angle associated with the texel of the cubemap

            real omegaS;

            if (usePrecomputedSamples)
            {
                omegaS = LOAD_TEXTURE2D(ggxIblSamples, uint2(i, index)).w;
            }
            else
            {
                // real PDF = D * NdotH * Jacobian, where Jacobian = 1 / (4 * LdotH).
                // Since (N == V), NdotH == LdotH.
                real pdf = 0.25 * D_GGX(NdotH, roughness);
                // TODO: improve the accuracy of the sample's solid angle fit for GGX.
                omegaS    = rcp(sampleCount) * rcp(pdf);
            }

            // 'invOmegaP' is precomputed on CPU and provided as a parameter to the function.
            // real omegaP = FOUR_PI / (6.0 * cubemapWidth * cubemapWidth);
            const real mipBias = roughness;
            mipLevel = 0.5 * log2(omegaS * invOmegaP) + mipBias;
        }

        // TODO: use a Gaussian-like filter to generate the MIP pyramid.
        real3 val = SAMPLE_TEXTURECUBE_LOD(tex, sampl, L, mipLevel).rgb;

        // The goal of this function is to use Monte-Carlo integration to find
        // X = Integral{Radiance(L) * CBSDF(L, N, V) dL} / Integral{CBSDF(L, N, V) dL}.
        // Note: Integral{CBSDF(L, N, V) dL} is given by the FDG texture.
        // CBSDF  = F * D * G * NdotL / (4 * NdotL * NdotV) = F * D * G / (4 * NdotV).
        // PDF    = D * NdotH / (4 * LdotH).
        // Weight = CBSDF / PDF = F * G * LdotH / (NdotV * NdotH).
        // Since we perform filtering with the assumption that (V == N),
        // (LdotH == NdotH) && (NdotV == 1) && (Weight == F * G).
        // Therefore, after the Monte Carlo expansion of the integrals,
        // X = Sum(Radiance(L) * Weight) / Sum(Weight) = Sum(Radiance(L) * F * G) / Sum(F * G).

    #ifndef USE_KARIS_APPROXIMATION
        // The choice of the Fresnel factor does not appear to affect the result.
        real F = 1; // F_Schlick(F0, LdotH);
        real G = V_SmithJointGGX(NdotL, NdotV, roughness, partLambdaV) * NdotL * NdotV; // 4 cancels out

        lightInt += F * G * val;
        cbsdfInt += F * G;
    #else
        // Use the approximation from "Real Shading in Unreal Engine 4": Weight ≈ NdotL.
        lightInt += NdotL * val;
        cbsdfInt += NdotL;
    #endif
    }

    return real4(lightInt / cbsdfInt, 1.0);
}

// Searches the row 'j' containing 'n' elements of 'haystack' and
// returns the index of the first element greater or equal to 'needle'.
uint BinarySearchRow(uint j, real needle, TEXTURE2D(haystack), uint n)
{
    uint  i = n - 1;
    real v = LOAD_TEXTURE2D(haystack, uint2(i, j)).r;

    if (needle < v)
    {
        i = 0;

        for (uint b = 1 << firstbithigh(n - 1); b != 0; b >>= 1)
        {
            uint p = i | b;
            v = LOAD_TEXTURE2D(haystack, uint2(p, j)).r;
            if (v <= needle) { i = p; } // Move to the right.
        }
    }

    return i;
}

#if !defined SHADER_API_GLES
real4 IntegrateLD_MIS(TEXTURECUBE_ARGS(envMap, sampler_envMap),
                       TEXTURE2D(marginalRowDensities),
                       TEXTURE2D(conditionalDensities),
                       real3 V,
                       real3 N,
                       real roughness,
                       real invOmegaP,
                       uint width,
                       uint height,
                       uint sampleCount,
                       bool prefilter)
{
    real3x3 localToWorld = GetLocalFrame(N);

    real3 lightInt = real3(0.0, 0.0, 0.0);
    real  cbsdfInt = 0.0;

/*
    // Dedicate 50% of samples to light sampling at 1.0 roughness.
    // Only perform BSDF sampling when roughness is below 0.5.
    const int lightSampleCount = lerp(0, sampleCount / 2, saturate(2.0 * roughness - 1.0));
    const int bsdfSampleCount  = sampleCount - lightSampleCount;
*/

    // The value of the integral of intensity values of the environment map (as a 2D step function).
    real envMapInt2dStep = LOAD_TEXTURE2D(marginalRowDensities, uint2(height, 0)).r;
    // Since we are using equiareal mapping, we need to divide by the area of the sphere.
    real envMapIntSphere = envMapInt2dStep * INV_FOUR_PI;

    // Perform light importance sampling.
    for (uint i = 0; i < sampleCount; i++)
    {
        real2 s = Hammersley2d(i, sampleCount);

        // Sample a row from the marginal distribution.
        uint y = BinarySearchRow(0, s.x, marginalRowDensities, height - 1);

        // Sample a column from the conditional distribution.
        uint x = BinarySearchRow(y, s.y, conditionalDensities, width - 1);

        // Compute the coordinates of the sample.
        // Note: we take the sample in between two texels, and also apply the half-texel offset.
        // We could compute fractional coordinates at the cost of 4 extra texel samples.
        real  u = saturate((real)x / width  + 1.0 / width);
        real  v = saturate((real)y / height + 1.0 / height);
        real3 L = ConvertEquiarealToCubemap(u, v);

        real NdotL = saturate(dot(N, L));

        if (NdotL > 0.0)
        {
            real3 val = SAMPLE_TEXTURECUBE_LOD(envMap, sampler_envMap, L, 0).rgb;
            real  pdf = (val.r + val.g + val.b) / envMapIntSphere;

            if (pdf > 0.0)
            {
                // (N == V) && (acos(VdotL) == 2 * acos(NdotH)).
                real NdotH = sqrt(NdotL * 0.5 + 0.5);

                // *********************************************************************************
                // Our goal is to use Monte-Carlo integration with importance sampling to evaluate
                // X(V)   = Integral{Radiance(L) * CBSDF(L, N, V) dL} / Integral{CBSDF(L, N, V) dL}.
                // CBSDF  = F * D * G * NdotL / (4 * NdotL * NdotV) = F * D * G / (4 * NdotV).
                // Weight = CBSDF / PDF.
                // We use two approximations of Brian Karis from "Real Shading in Unreal Engine 4":
                // (F * G ≈ NdotL) && (NdotV == 1).
                // Weight = D * NdotL / (4 * PDF).
                // *********************************************************************************

                real weight = D_GGX(NdotH, roughness) * NdotL / (4.0 * pdf);

                lightInt += weight * val;
                cbsdfInt += weight;
            }
        }
    }

    // Prevent NaNs arising from the division of 0 by 0.
    cbsdfInt = max(cbsdfInt, FLT_EPS);

    return real4(lightInt / cbsdfInt, 1.0);
}
#else
// Not supported due to lack of random library in GLES 2
#define IntegrateLD_MIS ERROR_ON_UNSUPPORTED_FUNCTION(IntegrateLD_MIS)
#endif

// Little helper to share code between sphere and box reflection probe.
// This function will fade the mask of a reflection volume based on normal orientation compare to direction define by the center of the reflection volume.
float InfluenceFadeNormalWeight(float3 normal, float3 centerToPos)
{
    // Start weight from 0.6f (1 fully transparent) to 0.2f (fully opaque).
    return saturate((-1.0f / 0.4f) * dot(normal, centerToPos) + (0.6f / 0.4f));
}

#endif // UNITY_IMAGE_BASED_LIGHTING_INCLUDED
