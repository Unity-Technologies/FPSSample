#ifndef UNITY_POSTFX_AMBIENT_OCCLUSION
#define UNITY_POSTFX_AMBIENT_OCCLUSION

#include "../StdLib.hlsl"
#include "../Colors.hlsl"
#include "Fog.hlsl"

// --------
// Options for further customization
// --------

// By default, a 5-tap Gaussian with the linear sampling technique is used
// in the bilateral noise filter. It can be replaced with a 7-tap Gaussian
// with adaptive sampling by enabling the macro below. Although the
// differences are not noticeable in most cases, it may provide preferable
// results with some special usage (e.g. NPR without textureing).
// #define BLUR_HIGH_QUALITY

// By default, a fixed sampling pattern is used in the AO estimator. Although
// this gives preferable results in most cases, a completely random sampling
// pattern could give aesthetically better results. Disable the macro below
// to use such a random pattern instead of the fixed one.
#define FIX_SAMPLING_PATTERN

// The SampleNormal function normalizes samples from G-buffer because
// they're possibly unnormalized. We can eliminate this if it can be said
// that there is no wrong shader that outputs unnormalized normals.
// #define VALIDATE_NORMALS

// The constant below determines the contrast of occlusion. This allows
// users to control over/under occlusion. At the moment, this is not exposed
// to the editor because it's rarely useful.
static const float kContrast = 0.6;

// The constant below controls the geometry-awareness of the bilateral
// filter. The higher value, the more sensitive it is.
static const float kGeometryCoeff = 0.8;

// The constants below are used in the AO estimator. Beta is mainly used
// for suppressing self-shadowing noise, and Epsilon is used to prevent
// calculation underflow. See the paper (Morgan 2011 http://goo.gl/2iz3P)
// for further details of these constants.
static const float kBeta = 0.002;

// --------

// System built-in variables
TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
TEXTURE2D_SAMPLER2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2);
TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
TEXTURE2D_SAMPLER2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture);

float4 _MainTex_TexelSize;

float4 _AOParams;
float3 _AOColor;

// Sample count
#if !defined(SHADER_API_GLES)
    #define SAMPLE_COUNT _AOParams.w
#else
// GLES2: In many cases, dynamic looping is not supported.
    #define SAMPLE_COUNT 3
#endif

// Source texture properties
TEXTURE2D_SAMPLER2D(_SAOcclusionTexture, sampler_SAOcclusionTexture);
float4 _SAOcclusionTexture_TexelSize;

// Other parameters
#define INTENSITY _AOParams.x
#define RADIUS _AOParams.y
#define DOWNSAMPLE _AOParams.z

// Accessors for packed AO/normal buffer
half4 PackAONormal(half ao, half3 n)
{
    return half4(ao, n * 0.5 + 0.5);
}

half GetPackedAO(half4 p)
{
    return p.r;
}

half3 GetPackedNormal(half4 p)
{
    return p.gba * 2.0 - 1.0;
}

// Boundary check for depth sampler
// (returns a very large value if it lies out of bounds)
float CheckBounds(float2 uv, float d)
{
    float ob = any(uv < 0) + any(uv > 1);
#if defined(UNITY_REVERSED_Z)
    ob += (d <= 0.00001);
#else
    ob += (d >= 0.99999);
#endif
    return ob * 1e8;
}

// Depth/normal sampling functions
float SampleDepth(float2 uv)
{
    float d = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv), 0));
    return d * _ProjectionParams.z + CheckBounds(uv, d);
}

float3 SampleNormal(float2 uv)
{
#if defined(SOURCE_GBUFFER)
    float3 norm = SAMPLE_TEXTURE2D(_CameraGBufferTexture2, sampler_CameraGBufferTexture2, uv).xyz;
    norm = norm * 2 - any(norm); // gets (0,0,0) when norm == 0
    norm = mul((float3x3)unity_WorldToCamera, norm);
#if defined(VALIDATE_NORMALS)
    norm = normalize(norm);
#endif
    return norm;
#else
    float4 cdn = SAMPLE_TEXTURE2D(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, uv);
    return DecodeViewNormalStereo(cdn) * float3(1.0, 1.0, -1.0);
#endif
}

float SampleDepthNormal(float2 uv, out float3 normal)
{
    normal = SampleNormal(UnityStereoTransformScreenSpaceTex(uv));
    return SampleDepth(uv);
}

// Normal vector comparer (for geometry-aware weighting)
half CompareNormal(half3 d1, half3 d2)
{
    return smoothstep(kGeometryCoeff, 1.0, dot(d1, d2));
}

// Trigonometric function utility
float2 CosSin(float theta)
{
    float sn, cs;
    sincos(theta, sn, cs);
    return float2(cs, sn);
}

// Pseudo random number generator with 2D coordinates
float UVRandom(float u, float v)
{
    float f = dot(float2(12.9898, 78.233), float2(u, v));
    return frac(43758.5453 * sin(f));
}

// Check if the camera is perspective.
// (returns 1.0 when orthographic)
float CheckPerspective(float x)
{
    return lerp(x, 1.0, unity_OrthoParams.w);
}

// Reconstruct view-space position from UV and depth.
// p11_22 = (unity_CameraProjection._11, unity_CameraProjection._22)
// p13_31 = (unity_CameraProjection._13, unity_CameraProjection._23)
float3 ReconstructViewPos(float2 uv, float depth, float2 p11_22, float2 p13_31)
{
    return float3((uv * 2.0 - 1.0 - p13_31) / p11_22 * CheckPerspective(depth), depth);
}

// Sample point picker
float3 PickSamplePoint(float2 uv, float index)
{
    // Uniformaly distributed points on a unit sphere
    // http://mathworld.wolfram.com/SpherePointPicking.html
#if defined(FIX_SAMPLING_PATTERN)
    float gn = GradientNoise(uv * DOWNSAMPLE);
    // FIXEME: This was added to avoid a NVIDIA driver issue.
    //                                   vvvvvvvvvvvv
    float u = frac(UVRandom(0.0, index + uv.x * 1e-10) + gn) * 2.0 - 1.0;
    float theta = (UVRandom(1.0, index + uv.x * 1e-10) + gn) * TWO_PI;
#else
    float u = UVRandom(uv.x + _Time.x, uv.y + index) * 2.0 - 1.0;
    float theta = UVRandom(-uv.x - _Time.x, uv.y + index) * TWO_PI;
#endif
    float3 v = float3(CosSin(theta) * sqrt(1.0 - u * u), u);
    // Make them distributed between [0, _Radius]
    float l = sqrt((index + 1.0) / SAMPLE_COUNT) * RADIUS;
    return v * l;
}

//
// Distance-based AO estimator based on Morgan 2011
// "Alchemy screen-space ambient obscurance algorithm"
// http://graphics.cs.williams.edu/papers/AlchemyHPG11/
//
float4 FragAO(VaryingsDefault i) : SV_Target
{
    float2 uv = i.texcoord;

    // Parameters used in coordinate conversion
    float3x3 proj = (float3x3)unity_CameraProjection;
    float2 p11_22 = float2(unity_CameraProjection._11, unity_CameraProjection._22);
    float2 p13_31 = float2(unity_CameraProjection._13, unity_CameraProjection._23);

    // View space normal and depth
    float3 norm_o;
    float depth_o = SampleDepthNormal(uv, norm_o);

    // Reconstruct the view-space position.
    float3 vpos_o = ReconstructViewPos(uv, depth_o, p11_22, p13_31);

    float ao = 0.0;

    for (int s = 0; s < int(SAMPLE_COUNT); s++)
    {
        // Sample point
#if defined(SHADER_API_D3D11)
        // This 'floor(1.0001 * s)' operation is needed to avoid a NVidia shader issue. This issue
        // is only observed on DX11.
        float3 v_s1 = PickSamplePoint(uv, floor(1.0001 * s));
#else
        float3 v_s1 = PickSamplePoint(uv, s);
#endif

        v_s1 = faceforward(v_s1, -norm_o, v_s1);
        float3 vpos_s1 = vpos_o + v_s1;

        // Reproject the sample point
        float3 spos_s1 = mul(proj, vpos_s1);
        float2 uv_s1_01 = (spos_s1.xy / CheckPerspective(vpos_s1.z) + 1.0) * 0.5;

        // Depth at the sample point
        float depth_s1 = SampleDepth(uv_s1_01);

        // Relative position of the sample point
        float3 vpos_s2 = ReconstructViewPos(uv_s1_01, depth_s1, p11_22, p13_31);
        float3 v_s2 = vpos_s2 - vpos_o;

        // Estimate the obscurance value
        float a1 = max(dot(v_s2, norm_o) - kBeta * depth_o, 0.0);
        float a2 = dot(v_s2, v_s2) + EPSILON;
        ao += a1 / a2;
    }

    ao *= RADIUS; // Intensity normalization

    // Apply other parameters.
    ao = PositivePow(ao * INTENSITY / SAMPLE_COUNT, kContrast);

    // Apply fog when enabled (forward-only)
#if (APPLY_FORWARD_FOG)
    float d = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo));
    d = ComputeFogDistance(d);
    ao *= ComputeFog(d);
#endif

    return PackAONormal(ao, norm_o);
}

// Geometry-aware separable bilateral filter
float4 FragBlur(VaryingsDefault i) : SV_Target
{
#if defined(BLUR_HORIZONTAL)
    // Horizontal pass: Always use 2 texels interval to match to
    // the dither pattern.
    float2 delta = float2(_MainTex_TexelSize.x * 2.0, 0.0);
#else
    // Vertical pass: Apply _Downsample to match to the dither
    // pattern in the original occlusion buffer.
    float2 delta = float2(0.0, _MainTex_TexelSize.y / DOWNSAMPLE * 2.0);
#endif

#if defined(BLUR_HIGH_QUALITY)

    // High quality 7-tap Gaussian with adaptive sampling

    half4 p0  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
    half4 p1a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord - delta));
    half4 p1b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + delta));
    half4 p2a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord - delta * 2.0));
    half4 p2b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + delta * 2.0));
    half4 p3a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord - delta * 3.2307692308));
    half4 p3b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + delta * 3.2307692308));

#if defined(BLUR_SAMPLE_CENTER_NORMAL)
    half3 n0 = SampleNormal(i.texcoordStereo);
#else
    half3 n0 = GetPackedNormal(p0);
#endif

    half w0  = 0.37004405286;
    half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.31718061674;
    half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.31718061674;
    half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.19823788546;
    half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.19823788546;
    half w3a = CompareNormal(n0, GetPackedNormal(p3a)) * 0.11453744493;
    half w3b = CompareNormal(n0, GetPackedNormal(p3b)) * 0.11453744493;

    half s;
    s  = GetPackedAO(p0)  * w0;
    s += GetPackedAO(p1a) * w1a;
    s += GetPackedAO(p1b) * w1b;
    s += GetPackedAO(p2a) * w2a;
    s += GetPackedAO(p2b) * w2b;
    s += GetPackedAO(p3a) * w3a;
    s += GetPackedAO(p3b) * w3b;

    s /= w0 + w1a + w1b + w2a + w2b + w3a + w3b;

#else

    // Fater 5-tap Gaussian with linear sampling
    half4 p0  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoordStereo);
    half4 p1a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord - delta * 1.3846153846));
    half4 p1b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + delta * 1.3846153846));
    half4 p2a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord - delta * 3.2307692308));
    half4 p2b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UnityStereoTransformScreenSpaceTex(i.texcoord + delta * 3.2307692308));

#if defined(BLUR_SAMPLE_CENTER_NORMAL)
    half3 n0 = SampleNormal(i.texcoordStereo);
#else
    half3 n0 = GetPackedNormal(p0);
#endif

    half w0  = 0.2270270270;
    half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.3162162162;
    half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.3162162162;
    half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.0702702703;
    half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.0702702703;

    half s;
    s  = GetPackedAO(p0)  * w0;
    s += GetPackedAO(p1a) * w1a;
    s += GetPackedAO(p1b) * w1b;
    s += GetPackedAO(p2a) * w2a;
    s += GetPackedAO(p2b) * w2b;

    s /= w0 + w1a + w1b + w2a + w2b;

#endif

    return PackAONormal(s, n0);
}

// Gamma encoding (only needed in gamma lighting mode)
half EncodeAO(half x)
{
    #if UNITY_COLORSPACE_GAMMA
        return 1.0 - max(LinearToSRGB(1.0 - saturate(x)), 0.0);
    #else
        return x;
    #endif
}

// Geometry-aware bilateral filter (single pass/small kernel)
half BlurSmall(TEXTURE2D_ARGS(tex, samp), float2 uv, float2 delta)
{
    half4 p0 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv));
    half4 p1 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x, -delta.y)));
    half4 p2 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2( delta.x, -delta.y)));
    half4 p3 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2(-delta.x,  delta.y)));
    half4 p4 = SAMPLE_TEXTURE2D(tex, samp, UnityStereoTransformScreenSpaceTex(uv + float2( delta.x,  delta.y)));

    half3 n0 = GetPackedNormal(p0);

    half w0 = 1.0;
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half s;
    s  = GetPackedAO(p0) * w0;
    s += GetPackedAO(p1) * w1;
    s += GetPackedAO(p2) * w2;
    s += GetPackedAO(p3) * w3;
    s += GetPackedAO(p4) * w4;

    return s / (w0 + w1 + w2 + w3 + w4);
}

// Final composition shader
float4 FragComposition(VaryingsDefault i) : SV_Target
{
    float2 delta = _SAOcclusionTexture_TexelSize.xy / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_PARAM(_SAOcclusionTexture, sampler_SAOcclusionTexture), i.texcoord, delta);
    ao = EncodeAO(ao);
    return float4(ao * _AOColor, ao);
}

#if !SHADER_API_GLES // Excluding the MRT pass under GLES2

struct CompositionOutput
{
    half4 gbuffer0 : SV_Target0;
    half4 gbuffer3 : SV_Target1;
};

CompositionOutput FragCompositionGBuffer(VaryingsDefault i)
{
    // Workaround: _SAOcclusionTexture_Texelsize hasn't been set properly
    // for some reasons. Use _ScreenParams instead.
    float2 delta = (_ScreenParams.zw - 1.0) / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_PARAM(_SAOcclusionTexture, sampler_SAOcclusionTexture), i.texcoord, delta);

    CompositionOutput o;
    o.gbuffer0 = half4(0.0, 0.0, 0.0, ao);
    o.gbuffer3 = half4((half3)EncodeAO(ao) * _AOColor, 0.0);
    return o;
}

#else

float4 FragCompositionGBuffer(VaryingsDefault i) : SV_Target
{
    return (0.0).xxxx;
}

#endif

float4 FragDebugOverlay(VaryingsDefault i) : SV_Target
{
    float2 delta = _SAOcclusionTexture_TexelSize.xy / DOWNSAMPLE;
    half ao = BlurSmall(TEXTURE2D_PARAM(_SAOcclusionTexture, sampler_SAOcclusionTexture), i.texcoord, delta);
    ao = EncodeAO(ao);
    return float4(1.0 - ao.xxx, 1.0);
}

#endif // UNITY_POSTFX_AMBIENT_OCCLUSION
