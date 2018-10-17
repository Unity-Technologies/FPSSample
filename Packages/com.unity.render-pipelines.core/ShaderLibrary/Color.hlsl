#ifndef UNITY_COLOR_INCLUDED
#define UNITY_COLOR_INCLUDED

//-----------------------------------------------------------------------------
// Gamma space - Assume positive values
//-----------------------------------------------------------------------------

// Gamma20
real Gamma20ToLinear(real c)
{
    return c * c;
}

real3 Gamma20ToLinear(real3 c)
{
    return c.rgb * c.rgb;
}

real4 Gamma20ToLinear(real4 c)
{
    return real4(Gamma20ToLinear(c.rgb), c.a);
}

real LinearToGamma20(real c)
{
    return sqrt(c);
}

real3 LinearToGamma20(real3 c)
{
    return sqrt(c.rgb);
}

real4 LinearToGamma20(real4 c)
{
    return real4(LinearToGamma20(c.rgb), c.a);
}

// Gamma22
real Gamma22ToLinear(real c)
{
    return PositivePow(c, 2.2);
}

real3 Gamma22ToLinear(real3 c)
{
    return PositivePow(c.rgb, real3(2.2, 2.2, 2.2));
}

real4 Gamma22ToLinear(real4 c)
{
    return real4(Gamma22ToLinear(c.rgb), c.a);
}

real LinearToGamma22(real c)
{
    return PositivePow(c, 0.454545454545455);
}

real3 LinearToGamma22(real3 c)
{
    return PositivePow(c.rgb, real3(0.454545454545455, 0.454545454545455, 0.454545454545455));
}

real4 LinearToGamma22(real4 c)
{
    return real4(LinearToGamma22(c.rgb), c.a);
}

// sRGB
real3 SRGBToLinear(real3 c)
{
    real3 linearRGBLo  = c / 12.92;
    real3 linearRGBHi  = PositivePow((c + 0.055) / 1.055, real3(2.4, 2.4, 2.4));
    real3 linearRGB    = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
}

real4 SRGBToLinear(real4 c)
{
    return real4(SRGBToLinear(c.rgb), c.a);
}

real3 LinearToSRGB(real3 c)
{
    real3 sRGBLo = c * 12.92;
    real3 sRGBHi = (PositivePow(c, real3(1.0/2.4, 1.0/2.4, 1.0/2.4)) * 1.055) - 0.055;
    real3 sRGB   = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
}

real4 LinearToSRGB(real4 c)
{
    return real4(LinearToSRGB(c.rgb), c.a);
}

// TODO: Seb - To verify and refit!
// Ref: http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
real3 FastSRGBToLinear(real3 c)
{
    return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
}

real4 FastSRGBToLinear(real4 c)
{
    return real4(FastSRGBToLinear(c.rgb), c.a);
}

real3 FastLinearToSRGB(real3 c)
{
    return saturate(1.055 * PositivePow(c, 0.416666667) - 0.055);
}

real4 FastLinearToSRGB(real4 c)
{
    return real4(FastLinearToSRGB(c.rgb), c.a);
}

//-----------------------------------------------------------------------------
// Color space
//-----------------------------------------------------------------------------

// Convert rgb to luminance
// with rgb in linear space with sRGB primaries and D65 white point
real Luminance(real3 linearRgb)
{
    return dot(linearRgb, real3(0.2126729, 0.7151522, 0.0721750));
}

real Luminance(real4 linearRgba)
{
    return Luminance(linearRgba.rgb);
}

// This function take a rgb color (best is to provide color in sRGB space)
// and return a YCoCg color in [0..1] space for 8bit (An offset is apply in the function)
// Ref: http://www.nvidia.com/object/real-time-ycocg-dxt-compression.html
#define YCOCG_CHROMA_BIAS (128.0 / 255.0)
real3 RGBToYCoCg(real3 rgb)
{
    real3 YCoCg;
    YCoCg.x = dot(rgb, real3(0.25, 0.5, 0.25));
    YCoCg.y = dot(rgb, real3(0.5, 0.0, -0.5)) + YCOCG_CHROMA_BIAS;
    YCoCg.z = dot(rgb, real3(-0.25, 0.5, -0.25)) + YCOCG_CHROMA_BIAS;

    return YCoCg;
}

real3 YCoCgToRGB(real3 YCoCg)
{
    real Y = YCoCg.x;
    real Co = YCoCg.y - YCOCG_CHROMA_BIAS;
    real Cg = YCoCg.z - YCOCG_CHROMA_BIAS;

    real3 rgb;
    rgb.r = Y + Co - Cg;
    rgb.g = Y + Cg;
    rgb.b = Y - Co - Cg;

    return rgb;
}

// Following function can be use to reconstruct chroma component for a checkboard YCoCg pattern
// Reference: The Compact YCoCg Frame Buffer
real YCoCgCheckBoardEdgeFilter(real centerLum, real2 a0, real2 a1, real2 a2, real2 a3)
{
    real4 lum = real4(a0.x, a1.x, a2.x, a3.x);
    // Optimize: real4 w = 1.0 - step(30.0 / 255.0, abs(lum - centerLum));
    real4 w = 1.0 - saturate((abs(lum.xxxx - centerLum) - 30.0 / 255.0) * HALF_MAX);
    real W = w.x + w.y + w.z + w.w;
    // handle the special case where all the weights are zero.
    return  (W == 0.0) ? a0.y : (w.x * a0.y + w.y* a1.y + w.z* a2.y + w.w * a3.y) / W;
}

// Hue, Saturation, Value
// Ranges:
//  Hue [0.0, 1.0]
//  Sat [0.0, 1.0]
//  Lum [0.0, HALF_MAX]
real3 RgbToHsv(real3 c)
{
    const real4 K = real4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    real4 p = lerp(real4(c.bg, K.wz), real4(c.gb, K.xy), step(c.b, c.g));
    real4 q = lerp(real4(p.xyw, c.r), real4(c.r, p.yzx), step(p.x, c.r));
    real d = q.x - min(q.w, q.y);
    const real e = 1.0e-4;
    return real3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

real3 HsvToRgb(real3 c)
{
    const real4 K = real4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    real3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

// SMPTE ST.2084 (PQ) transfer functions
// 1.0 = 100nits, 100.0 = 10knits
#define DEFAULT_MAX_PQ 100.0

struct ParamsPQ
{
    real N, M;
    real C1, C2, C3;
};

static const ParamsPQ PQ =
{
    2610.0 / 4096.0 / 4.0,   // N
    2523.0 / 4096.0 * 128.0, // M
    3424.0 / 4096.0,         // C1
    2413.0 / 4096.0 * 32.0,  // C2
    2392.0 / 4096.0 * 32.0,  // C3
};

real3 LinearToPQ(real3 x, real maxPQValue)
{
    x = PositivePow(x / maxPQValue, PQ.N);
    real3 nd = (PQ.C1 + PQ.C2 * x) / (1.0 + PQ.C3 * x);
    return PositivePow(nd, PQ.M);
}

real3 LinearToPQ(real3 x)
{
    return LinearToPQ(x, DEFAULT_MAX_PQ);
}

real3 PQToLinear(real3 x, real maxPQValue)
{
    x = PositivePow(x, rcp(PQ.M));
    real3 nd = max(x - PQ.C1, 0.0) / (PQ.C2 - (PQ.C3 * x));
    return PositivePow(nd, rcp(PQ.N)) * maxPQValue;
}

real3 PQToLinear(real3 x)
{
    return PQToLinear(x, DEFAULT_MAX_PQ);
}

// Alexa LogC converters (El 1000)
// See http://www.vocas.nl/webfm_send/964
// Max range is ~58.85666

// Set to 1 to use more precise but more expensive log/linear conversions. I haven't found a proper
// use case for the high precision version yet so I'm leaving this to 0.
#define USE_PRECISE_LOGC 0

struct ParamsLogC
{
    real cut;
    real a, b, c, d, e, f;
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

real LinearToLogC_Precise(real x)
{
    real o;
    if (x > LogC.cut)
        o = LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
    else
        o = LogC.e * x + LogC.f;
    return o;
}

real3 LinearToLogC(real3 x)
{
#if USE_PRECISE_LOGC
    return real3(
        LinearToLogC_Precise(x.x),
        LinearToLogC_Precise(x.y),
        LinearToLogC_Precise(x.z)
    );
#else
    return LogC.c * log10(LogC.a * x + LogC.b) + LogC.d;
#endif
}

real LogCToLinear_Precise(real x)
{
    real o;
    if (x > LogC.e * LogC.cut + LogC.f)
        o = (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
    else
        o = (x - LogC.f) / LogC.e;
    return o;
}

real3 LogCToLinear(real3 x)
{
#if USE_PRECISE_LOGC
    return real3(
        LogCToLinear_Precise(x.x),
        LogCToLinear_Precise(x.y),
        LogCToLinear_Precise(x.z)
    );
#else
    return (pow(10.0, (x - LogC.d) / LogC.c) - LogC.b) / LogC.a;
#endif
}

//-----------------------------------------------------------------------------
// Utilities
//-----------------------------------------------------------------------------

// Fast reversible tonemapper
// http://gpuopen.com/optimized-reversible-tonemapper-for-resolve/
real3 FastTonemap(real3 c)
{
    return c * rcp(Max3(c.r, c.g, c.b) + 1.0);
}

real4 FastTonemap(real4 c)
{
    return real4(FastTonemap(c.rgb), c.a);
}

real3 FastTonemap(real3 c, real w)
{
    return c * (w * rcp(Max3(c.r, c.g, c.b) + 1.0));
}

real4 FastTonemap(real4 c, real w)
{
    return real4(FastTonemap(c.rgb, w), c.a);
}

real3 FastTonemapInvert(real3 c)
{
    return c * rcp(1.0 - Max3(c.r, c.g, c.b));
}

real4 FastTonemapInvert(real4 c)
{
    return real4(FastTonemapInvert(c.rgb), c.a);
}

// 3D LUT grading
// scaleOffset = (1 / lut_size, lut_size - 1)
real3 ApplyLut3D(TEXTURE3D_ARGS(tex, samplerTex), real3 uvw, real2 scaleOffset)
{
    real shift = floor(uvw.z);
    uvw.xy = uvw.xy * scaleOffset.y * scaleOffset.xx + scaleOffset.xx * 0.5;
    uvw.x += shift * scaleOffset.x;
    return SAMPLE_TEXTURE3D(tex, samplerTex, uvw).rgb;
}

// 2D LUT grading
// scaleOffset = (1 / lut_width, 1 / lut_height, lut_height - 1)
real3 ApplyLut2D(TEXTURE2D_ARGS(tex, samplerTex), real3 uvw, real3 scaleOffset)
{
    // Strip format where `height = sqrt(width)`
    uvw.z *= scaleOffset.z;
    real shift = floor(uvw.z);
    uvw.xy = uvw.xy * scaleOffset.z * scaleOffset.xy + scaleOffset.xy * 0.5;
    uvw.x += shift * scaleOffset.y;
    uvw.xyz = lerp(
        SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy).rgb,
        SAMPLE_TEXTURE2D(tex, samplerTex, uvw.xy + real2(scaleOffset.y, 0.0)).rgb,
        uvw.z - shift
    );
    return uvw;
}

// Returns the default value for a given position on a 2D strip-format color lookup table
// params = (lut_height, 0.5 / lut_width, 0.5 / lut_height, lut_height / lut_height - 1)
real3 GetLutStripValue(real2 uv, real4 params)
{
    uv -= params.yz;
    real3 color;
    color.r = frac(uv.x * params.x);
    color.b = uv.x - color.r / params.x;
    color.g = uv.y;
    return color * params.w;
}

#endif // UNITY_COLOR_INCLUDED
