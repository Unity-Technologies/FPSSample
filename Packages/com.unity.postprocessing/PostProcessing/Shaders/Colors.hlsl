#ifndef UNITY_POSTFX_COLOR
#define UNITY_POSTFX_COLOR

#include "StdLib.hlsl"
#include "ACES.hlsl"

#define LUT_SPACE_ENCODE(x) LinearToLogC(x)
#define LUT_SPACE_DECODE(x) LogCToLinear(x)

#ifndef USE_PRECISE_LOGC
    // Set to 1 to use more precise but more expensive log/linear conversions. I haven't found a proper
    // use case for the high precision version yet so I'm leaving this to 0.
    #define USE_PRECISE_LOGC 0
#endif

#ifndef TONEMAPPING_USE_FULL_ACES
    // Set to 1 to use the full reference ACES tonemapper. This should only be used for research
    // purposes as it's quite heavy and generally overkill.
    #define TONEMAPPING_USE_FULL_ACES 0
#endif

#ifndef DEFAULT_MAX_PQ
    // PQ ST.2048 max value
    // 1.0 = 100nits, 100.0 = 10knits
    #define DEFAULT_MAX_PQ 100.0
#endif

#ifndef USE_VERY_FAST_SRGB
    #if defined(SHADER_API_MOBILE)
        #define USE_VERY_FAST_SRGB 1
    #else
        #define USE_VERY_FAST_SRGB 0
    #endif
#endif

#ifndef USE_FAST_SRGB
    #if defined(SHADER_API_CONSOLE)
        #define USE_FAST_SRGB 1
    #else
        #define USE_FAST_SRGB 0
    #endif
#endif

//
// Alexa LogC converters (El 1000)
// See http://www.vocas.nl/webfm_send/964
// Max range is ~58.85666
//
struct ParamsLogC
{
    float cut;
    float a, b, c, d, e, f;
};

static const ParamsLogC LogC =
{
    0.011361, // cut
    5.555556, // a
    0.047996, // b
    0.244161, // c
    0.386036, // d
    5.301883, // e
    0.092819  // f
};

float LinearToLogC_Precise(half x)
{
    float o;
    if (x > LogC.cut)
        o = LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
    else
        o = LogC.e * x + LogC.f;
    return o;
}

float3 LinearToLogC(float3 x)
{
#if USE_PRECISE_LOGC
    return float3(
        LinearToLogC_Precise(x.x),
        LinearToLogC_Precise(x.y),
        LinearToLogC_Precise(x.z)
    );
#else
    return LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
#endif
}

float LogCToLinear_Precise(float x)
{
    float o;
    if (x > LogC.e * LogC.cut + LogC.f)
        o = (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
    else
        o = (x - LogC.f) / LogC.e;
    return o;
}

float3 LogCToLinear(float3 x)
{
#if USE_PRECISE_LOGC
    return float3(
        LogCToLinear_Precise(x.x),
        LogCToLinear_Precise(x.y),
        LogCToLinear_Precise(x.z)
    );
#else
    return (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
#endif
}

//
// SMPTE ST.2084 (PQ) transfer functions
// Used for HDR Lut storage, max range depends on the maxPQValue parameter
//
struct ParamsPQ
{
    float N, M;
    float C1, C2, C3;
};

static const ParamsPQ PQ =
{
    2610.0 / 4096.0 / 4.0,   // N
    2523.0 / 4096.0 * 128.0, // M
    3424.0 / 4096.0,         // C1
    2413.0 / 4096.0 * 32.0,  // C2
    2392.0 / 4096.0 * 32.0,  // C3
};

float3 LinearToPQ(float3 x, float maxPQValue)
{
    x = PositivePow(x / maxPQValue, PQ.N);
    float3 nd = (PQ.C1 + PQ.C2 * x) / (1.0 + PQ.C3 * x);
    return PositivePow(nd, PQ.M);
}

float3 LinearToPQ(float3 x)
{
    return LinearToPQ(x, DEFAULT_MAX_PQ);
}

float3 PQToLinear(float3 x, float maxPQValue)
{
    x = PositivePow(x, rcp(PQ.M));
    float3 nd = max(x - PQ.C1, 0.0) / (PQ.C2 - (PQ.C3 * x));
    return PositivePow(nd, rcp(PQ.N)) * maxPQValue;
}

float3 PQToLinear(float3 x)
{
    return PQToLinear(x, DEFAULT_MAX_PQ);
}

//
// sRGB transfer functions
// Fast path ref: http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
//
half SRGBToLinear(half c)
{
#if USE_VERY_FAST_SRGB
    return c * c;
#elif USE_FAST_SRGB
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
#else
    half linearRGBLo = c / 12.92;
    half linearRGBHi = PositivePow((c + 0.055) / 1.055, 2.4);
    half linearRGB = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
#endif
}

half3 SRGBToLinear(half3 c)
{
#if USE_VERY_FAST_SRGB
    return c * c;
#elif USE_FAST_SRGB
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
#else
    half3 linearRGBLo = c / 12.92;
    half3 linearRGBHi = PositivePow((c + 0.055) / 1.055, half3(2.4, 2.4, 2.4));
    half3 linearRGB = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
#endif
}

half4 SRGBToLinear(half4 c)
{
    return half4(SRGBToLinear(c.rgb), c.a);
}

half LinearToSRGB(half c)
{
#if USE_VERY_FAST_SRGB
    return sqrt(c);
#elif USE_FAST_SRGB
    return max(1.055 * PositivePow(c, 0.416666667) - 0.055, 0.0);
#else
    half sRGBLo = c * 12.92;
    half sRGBHi = (PositivePow(c, 1.0 / 2.4) * 1.055) - 0.055;
    half sRGB = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
#endif
}

half3 LinearToSRGB(half3 c)
{
#if USE_VERY_FAST_SRGB
    return sqrt(c);
#elif USE_FAST_SRGB
    return max(1.055 * PositivePow(c, 0.416666667) - 0.055, 0.0);
#else
    half3 sRGBLo = c * 12.92;
    half3 sRGBHi = (PositivePow(c, half3(1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4)) * 1.055) - 0.055;
    half3 sRGB = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
#endif
}

half4 LinearToSRGB(half4 c)
{
    return half4(LinearToSRGB(c.rgb), c.a);
}

//
// Convert rgb to luminance with rgb in linear space with sRGB primaries and D65 white point
//
half Luminance(half3 linearRgb)
{
    return dot(linearRgb, float3(0.2126729, 0.7151522, 0.0721750));
}

half Luminance(half4 linearRgba)
{
    return Luminance(linearRgba.rgb);
}

//
// Quadratic color thresholding
// curve = (threshold - knee, knee * 2, 0.25 / knee)
//
half4 QuadraticThreshold(half4 color, half threshold, half3 curve)
{
    // Pixel brightness
    half br = Max3(color.r, color.g, color.b);

    // Under-threshold part: quadratic curve
    half rq = clamp(br - curve.x, 0.0, curve.y);
    rq = curve.z * rq * rq;

    // Combine and apply the brightness response curve.
    color *= max(rq, br - threshold) / max(br, EPSILON);

    return color;
}

//
// Fast reversible tonemapper
// http://gpuopen.com/optimized-reversible-tonemapper-for-resolve/
//
float3 FastTonemap(float3 c)
{
    return c * rcp(Max3(c.r, c.g, c.b) + 1.0);
}

float4 FastTonemap(float4 c)
{
    return float4(FastTonemap(c.rgb), c.a);
}

float3 FastTonemap(float3 c, float w)
{
    return c * (w * rcp(Max3(c.r, c.g, c.b) + 1.0));
}

float4 FastTonemap(float4 c, float w)
{
    return float4(FastTonemap(c.rgb, w), c.a);
}

float3 FastTonemapInvert(float3 c)
{
    return c * rcp(1.0 - Max3(c.r, c.g, c.b));
}

float4 FastTonemapInvert(float4 c)
{
    return float4(FastTonemapInvert(c.rgb), c.a);
}

//
// Neutral tonemapping (Hable/Hejl/Frostbite)
// Input is linear RGB
//
float3 NeutralCurve(float3 x, float a, float b, float c, float d, float e, float f)
{
    return ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
}

float3 NeutralTonemap(float3 x)
{
    // Tonemap
    float a = 0.2;
    float b = 0.29;
    float c = 0.24;
    float d = 0.272;
    float e = 0.02;
    float f = 0.3;
    float whiteLevel = 5.3;
    float whiteClip = 1.0;

    float3 whiteScale = (1.0).xxx / NeutralCurve(whiteLevel, a, b, c, d, e, f);
    x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
    x *= whiteScale;

    // Post-curve white point adjustment
    x /= whiteClip.xxx;

    return x;
}

//
// Raw, unoptimized version of John Hable's artist-friendly tone curve
// Input is linear RGB
//
float EvalCustomSegment(float x, float4 segmentA, float2 segmentB)
{
    const float kOffsetX = segmentA.x;
    const float kOffsetY = segmentA.y;
    const float kScaleX  = segmentA.z;
    const float kScaleY  = segmentA.w;
    const float kLnA     = segmentB.x;
    const float kB       = segmentB.y;

    float x0 = (x - kOffsetX) * kScaleX;
    float y0 = (x0 > 0.0) ? exp(kLnA + kB * log(x0)) : 0.0;
    return y0 * kScaleY + kOffsetY;
}

float EvalCustomCurve(float x, float3 curve, float4 toeSegmentA, float2 toeSegmentB, float4 midSegmentA, float2 midSegmentB, float4 shoSegmentA, float2 shoSegmentB)
{
    float4 segmentA;
    float2 segmentB;

    if (x < curve.y)
    {
        segmentA = toeSegmentA;
        segmentB = toeSegmentB;
    }
    else if (x < curve.z)
    {
        segmentA = midSegmentA;
        segmentB = midSegmentB;
    }
    else
    {
        segmentA = shoSegmentA;
        segmentB = shoSegmentB;
    }

    return EvalCustomSegment(x, segmentA, segmentB);
}

// curve: x: inverseWhitePoint, y: x0, z: x1
float3 CustomTonemap(float3 x, float3 curve, float4 toeSegmentA, float2 toeSegmentB, float4 midSegmentA, float2 midSegmentB, float4 shoSegmentA, float2 shoSegmentB)
{
    float3 normX = x * curve.x;
    float3 ret;
    ret.x = EvalCustomCurve(normX.x, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
    ret.y = EvalCustomCurve(normX.y, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
    ret.z = EvalCustomCurve(normX.z, curve, toeSegmentA, toeSegmentB, midSegmentA, midSegmentB, shoSegmentA, shoSegmentB);
    return ret;
}

//
// Filmic tonemapping (ACES fitting, unless TONEMAPPING_USE_FULL_ACES is set to 1)
// Input is ACES2065-1 (AP0 w/ linear encoding)
//
float3 AcesTonemap(float3 aces)
{
#if TONEMAPPING_USE_FULL_ACES

    float3 oces = RRT(aces);
    float3 odt = ODT_RGBmonitor_100nits_dim(oces);
    return odt;

#else

    // --- Glow module --- //
    float saturation = rgb_2_saturation(aces);
    float ycIn = rgb_2_yc(aces);
    float s = sigmoid_shaper((saturation - 0.4) / 0.2);
    float addedGlow = 1.0 + glow_fwd(ycIn, RRT_GLOW_GAIN * s, RRT_GLOW_MID);
    aces *= addedGlow;

    // --- Red modifier --- //
    float hue = rgb_2_hue(aces);
    float centeredHue = center_hue(hue, RRT_RED_HUE);
    float hueWeight;
    {
        //hueWeight = cubic_basis_shaper(centeredHue, RRT_RED_WIDTH);
        hueWeight = smoothstep(0.0, 1.0, 1.0 - abs(2.0 * centeredHue / RRT_RED_WIDTH));
        hueWeight *= hueWeight;
    }

    aces.r += hueWeight * saturation * (RRT_RED_PIVOT - aces.r) * (1.0 - RRT_RED_SCALE);

    // --- ACES to RGB rendering space --- //
    float3 acescg = max(0.0, ACES_to_ACEScg(aces));

    // --- Global desaturation --- //
    //acescg = mul(RRT_SAT_MAT, acescg);
    acescg = lerp(dot(acescg, AP1_RGB2Y).xxx, acescg, RRT_SAT_FACTOR.xxx);

    // Luminance fitting of *RRT.a1.0.3 + ODT.Academy.RGBmonitor_100nits_dim.a1.0.3*.
    // https://github.com/colour-science/colour-unity/blob/master/Assets/Colour/Notebooks/CIECAM02_Unity.ipynb
    // RMSE: 0.0012846272106
    const float a = 278.5085;
    const float b = 10.7772;
    const float c = 293.6045;
    const float d = 88.7122;
    const float e = 80.6889;
    float3 x = acescg;
    float3 rgbPost = (x * (a * x + b)) / (x * (c * x + d) + e);

    // Scale luminance to linear code value
    // float3 linearCV = Y_2_linCV(rgbPost, CINEMA_WHITE, CINEMA_BLACK);

    // Apply gamma adjustment to compensate for dim surround
    float3 linearCV = darkSurround_to_dimSurround(rgbPost);

    // Apply desaturation to compensate for luminance difference
    //linearCV = mul(ODT_SAT_MAT, color);
    linearCV = lerp(dot(linearCV, AP1_RGB2Y).xxx, linearCV, ODT_SAT_FACTOR.xxx);

    // Convert to display primary encoding
    // Rendering space RGB to XYZ
    float3 XYZ = mul(AP1_2_XYZ_MAT, linearCV);

    // Apply CAT from ACES white point to assumed observer adapted white point
    XYZ = mul(D60_2_D65_CAT, XYZ);

    // CIE XYZ to display primaries
    linearCV = mul(XYZ_2_REC709_MAT, XYZ);

    return linearCV;

#endif
}

//
// 3D LUT grading
// scaleOffset = (1 / lut_size, lut_size - 1)
//
half3 ApplyLut3D(TEXTURE3D_ARGS(tex, samplerTex), float3 uvw, float2 scaleOffset)
{
    uvw.xyz = uvw.xyz * scaleOffset.yyy * scaleOffset.xxx + scaleOffset.xxx * 0.5;
    return SAMPLE_TEXTURE3D(tex, samplerTex, uvw).rgb;
}

//
// 2D LUT grading
// scaleOffset = (1 / lut_width, 1 / lut_height, lut_height - 1)
//
half3 ApplyLut2D(TEXTURE2D_ARGS(tex, samplerTex), float3 uvw, float3 scaleOffset)
{
    // Strip format where `height = sqrt(width)`
    uvw.z *= scaleOffset.z;
    float shift = floor(uvw.z);
    uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
    uvw.x += shift * scaleOffset.y;
    uvw.xyz = lerp(
        SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy).rgb,
        SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy + float2(scaleOffset.y, 0.0)).rgb,
        uvw.z - shift
    );
    return uvw;
}

//
// Returns the default value for a given position on a 2D strip-format color lookup table
// params = (lut_height, 0.5 / lut_width, 0.5 / lut_height, lut_height / lut_height - 1)
//
float3 GetLutStripValue(float2 uv, float4 params)
{
    uv -= params.yz;
    float3 color;
    color.r = frac(uv.x * params.x);
    color.b = uv.x - color.r / params.x;
    color.g = uv.y;
    return color * params.w;
}

//
// White balance
// Recommended workspace: ACEScg (linear)
//
static const float3x3 LIN_2_LMS_MAT = {
    3.90405e-1, 5.49941e-1, 8.92632e-3,
    7.08416e-2, 9.63172e-1, 1.35775e-3,
    2.31082e-2, 1.28021e-1, 9.36245e-1
};

static const float3x3 LMS_2_LIN_MAT = {
    2.85847e+0, -1.62879e+0, -2.48910e-2,
    -2.10182e-1,  1.15820e+0,  3.24281e-4,
    -4.18120e-2, -1.18169e-1,  1.06867e+0
};

float3 WhiteBalance(float3 c, float3 balance)
{
    float3 lms = mul(LIN_2_LMS_MAT, c);
    lms *= balance;
    return mul(LMS_2_LIN_MAT, lms);
}

//
// RGB / Full-range YCbCr conversions (ITU-R BT.601)
//
float3 RgbToYCbCr(float3 c)
{
    float Y  =  0.299 * c.r + 0.587 * c.g + 0.114 * c.b;
    float Cb = -0.169 * c.r - 0.331 * c.g + 0.500 * c.b;
    float Cr =  0.500 * c.r - 0.419 * c.g - 0.081 * c.b;
    return float3(Y, Cb, Cr);
}

float3 YCbCrToRgb(float3 c)
{
    float R = c.x + 0.000 * c.y + 1.403 * c.z;
    float G = c.x - 0.344 * c.y - 0.714 * c.z;
    float B = c.x - 1.773 * c.y + 0.000 * c.z;
    return float3(R, G, B);
}

//
// Hue, Saturation, Value
// Ranges:
//  Hue [0.0, 1.0]
//  Sat [0.0, 1.0]
//  Lum [0.0, HALF_MAX]
//
float3 RgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = EPSILON;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HsvToRgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

float RotateHue(float value, float low, float hi)
{
    return (value < low)
            ? value + hi
            : (value > hi)
                ? value - hi
                : value;
}

//
// RGB Saturation (closer to a vibrance effect than actual saturation)
// Recommended workspace: ACEScg (linear)
// Optimal range: [0.0, 2.0]
//
float3 Saturation(float3 c, float sat)
{
    float luma = Luminance(c);
    return luma.xxx + sat.xxx * (c - luma.xxx);
}

//
// Contrast (reacts better when applied in log)
// Optimal range: [0.0, 2.0]
//
float3 Contrast(float3 c, float midpoint, float contrast)
{
    return (c - midpoint) * contrast + midpoint;
}

//
// Lift, Gamma (pre-inverted), Gain tuned for HDR use - best used with the ACES tonemapper as
// negative values will creep in the result
// Expected workspace: ACEScg (linear)
//
float3 LiftGammaGainHDR(float3 c, float3 lift, float3 invgamma, float3 gain)
{
    c = c * gain + lift;

    // ACEScg will output negative values, as clamping to 0 will lose precious information we'll
    // mirror the gamma function instead
    return FastSign(c) * pow(abs(c), invgamma);
}

//
// Lift, Gamma (pre-inverted), Gain tuned for LDR use
// Input is linear RGB
//
float3 LiftGammaGainLDR(float3 c, float3 lift, float3 invgamma, float3 gain)
{
    c = saturate(PositivePow(saturate(c), invgamma));
    return gain * c + lift * (1.0 - c);
}

//
// Remaps Y/R/G/B values
// curveTex has to be 128 pixels wide
//
float3 YrgbCurve(float3 c, TEXTURE2D_ARGS(curveTex, sampler_curveTex))
{
    const float kHalfPixel = (1.0 / 128.0) / 2.0;

    // Y (master)
    c += kHalfPixel.xxx;
    float mr = SAMPLE_TEXTURE2D(curveTex, sampler_curveTex, float2(c.r, 0.75)).a;
    float mg = SAMPLE_TEXTURE2D(curveTex, sampler_curveTex, float2(c.g, 0.75)).a;
    float mb = SAMPLE_TEXTURE2D(curveTex, sampler_curveTex, float2(c.b, 0.75)).a;
    c = saturate(float3(mr, mg, mb));

    // RGB
    c += kHalfPixel.xxx;
    float r = SAMPLE_TEXTURE2D(curveTex, sampler_curveTex, float2(c.r, 0.75)).r;
    float g = SAMPLE_TEXTURE2D(curveTex, sampler_curveTex, float2(c.g, 0.75)).g;
    float b = SAMPLE_TEXTURE2D(curveTex, sampler_curveTex, float2(c.b, 0.75)).b;
    return saturate(float3(r, g, b));
}

//
// Channel mixing (same as Photoshop's and DaVinci's Resolve)
// Recommended workspace: ACEScg (linear)
//      Input mixers should be in range [-2.0; 2.0]
//
float3 ChannelMixer(float3 c, float3 red, float3 green, float3 blue)
{
    return float3(
        dot(c, red),
        dot(c, green),
        dot(c, blue)
    );
}

#endif // UNITY_POSTFX_COLOR
