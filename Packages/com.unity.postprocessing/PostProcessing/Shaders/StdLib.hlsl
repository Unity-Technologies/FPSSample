// Because this framework is supposed to work with the legacy render pipelines AND scriptable render
// pipelines we can't use Unity's shader libraries (some scriptable pipelines come with their own
// shader lib). So here goes a minimal shader lib only used for post-processing to ensure good
// compatibility with all pipelines.

#ifndef UNITY_POSTFX_STDLIB
#define UNITY_POSTFX_STDLIB

// -----------------------------------------------------------------------------
// API macros

#if defined(SHADER_API_PSSL)
    #include "API/PSSL.hlsl"
#elif defined(SHADER_API_XBOXONE)
    #include "API/XboxOne.hlsl"
#elif defined(SHADER_API_D3D11)
    #include "API/D3D11.hlsl"
#elif defined(SHADER_API_D3D12)
    #include "API/D3D12.hlsl"
#elif defined(SHADER_API_D3D9) || defined(SHADER_API_D3D11_9X)
    #include "API/D3D9.hlsl"
#elif defined(SHADER_API_VULKAN)
    #include "API/Vulkan.hlsl"
#elif defined(SHADER_API_SWITCH)
    #include "API/Switch.hlsl"
#elif defined(SHADER_API_METAL)
    #include "API/Metal.hlsl"
#elif defined(SHADER_API_PSP2)
    #include "API/PSP2.hlsl"
#else
    #include "API/OpenGL.hlsl"
#endif

#if defined(SHADER_API_PSSL) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_SWITCH) || defined(SHADER_API_PSP2)
    #define SHADER_API_CONSOLE
#endif

// -----------------------------------------------------------------------------
// Constants

#define HALF_MAX        65504.0 // (2 - 2^-10) * 2^15
#define HALF_MAX_MINUS1 65472.0 // (2 - 2^-9) * 2^15
#define EPSILON         1.0e-4
#define PI              3.14159265359
#define TWO_PI          6.28318530718
#define FOUR_PI         12.56637061436
#define INV_PI          0.31830988618
#define INV_TWO_PI      0.15915494309
#define INV_FOUR_PI     0.07957747155
#define HALF_PI         1.57079632679
#define INV_HALF_PI     0.636619772367

#define FLT_EPSILON     1.192092896e-07 // Smallest positive number, such that 1.0 + FLT_EPSILON != 1.0
#define FLT_MIN         1.175494351e-38 // Minimum representable positive floating-point number
#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number

// -----------------------------------------------------------------------------
// Compatibility functions

#if (SHADER_TARGET < 50 && !defined(SHADER_API_PSSL))
float rcp(float value)
{
    return 1.0 / value;
}
#endif

#if defined(SHADER_API_GLES)
#define mad(a, b, c) (a * b + c)
#endif

#ifndef INTRINSIC_MINMAX3
float Min3(float a, float b, float c)
{
    return min(min(a, b), c);
}

float2 Min3(float2 a, float2 b, float2 c)
{
    return min(min(a, b), c);
}

float3 Min3(float3 a, float3 b, float3 c)
{
    return min(min(a, b), c);
}

float4 Min3(float4 a, float4 b, float4 c)
{
    return min(min(a, b), c);
}

float Max3(float a, float b, float c)
{
    return max(max(a, b), c);
}

float2 Max3(float2 a, float2 b, float2 c)
{
    return max(max(a, b), c);
}

float3 Max3(float3 a, float3 b, float3 c)
{
    return max(max(a, b), c);
}

float4 Max3(float4 a, float4 b, float4 c)
{
    return max(max(a, b), c);
}
#endif // INTRINSIC_MINMAX3

// https://twitter.com/SebAaltonen/status/878250919879639040
// madd_sat + madd
float FastSign(float x)
{
    return saturate(x * FLT_MAX + 0.5) * 2.0 - 1.0;
}

float2 FastSign(float2 x)
{
    return saturate(x * FLT_MAX + 0.5) * 2.0 - 1.0;
}

float3 FastSign(float3 x)
{
    return saturate(x * FLT_MAX + 0.5) * 2.0 - 1.0;
}

float4 FastSign(float4 x)
{
    return saturate(x * FLT_MAX + 0.5) * 2.0 - 1.0;
}

// Using pow often result to a warning like this
// "pow(f, e) will not work for negative f, use abs(f) or conditionally handle negative values if you expect them"
// PositivePow remove this warning when you know the value is positive and avoid inf/NAN.
float PositivePow(float base, float power)
{
    return pow(max(abs(base), float(FLT_EPSILON)), power);
}

float2 PositivePow(float2 base, float2 power)
{
    return pow(max(abs(base), float2(FLT_EPSILON, FLT_EPSILON)), power);
}

float3 PositivePow(float3 base, float3 power)
{
    return pow(max(abs(base), float3(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

float4 PositivePow(float4 base, float4 power)
{
    return pow(max(abs(base), float4(FLT_EPSILON, FLT_EPSILON, FLT_EPSILON, FLT_EPSILON)), power);
}

// NaN checker
// /Gic isn't enabled on fxc so we can't rely on isnan() anymore
bool IsNan(float x)
{
    // For some reason the following tests outputs "internal compiler error" randomly on desktop
    // so we'll use a safer but slightly slower version instead :/
    //return (x <= 0.0 || 0.0 <= x) ? false : true;
    return (x < 0.0 || x > 0.0 || x == 0.0) ? false : true;
}

bool AnyIsNan(float2 x)
{
    return IsNan(x.x) || IsNan(x.y);
}

bool AnyIsNan(float3 x)
{
    return IsNan(x.x) || IsNan(x.y) || IsNan(x.z);
}

bool AnyIsNan(float4 x)
{
    return IsNan(x.x) || IsNan(x.y) || IsNan(x.z) || IsNan(x.w);
}

// -----------------------------------------------------------------------------
// Std unity data

float4x4 unity_CameraProjection;
float4x4 unity_MatrixVP;
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToCamera;
float3 _WorldSpaceCameraPos;
float4 _ProjectionParams;         // x: 1 (-1 flipped), y: near,     z: far,       w: 1/far
float4 unity_ColorSpaceLuminance;
float4 unity_DeltaTime;           // x: dt,             y: 1/dt,     z: smoothDt,  w: 1/smoothDt
float4 unity_OrthoParams;         // x: width,          y: height,   z: unused,    w: ortho ? 1 : 0
float4 _ZBufferParams;            // x: 1-far/near,     y: far/near, z: x/far,     w: y/far
float4 _ScreenParams;             // x: width,          y: height,   z: 1+1/width, w: 1+1/height
float4 _Time;                     // x: t/20,           y: t,        z: t*2,       w: t*3
float4 _SinTime;                  // x: sin(t/20),      y: sin(t),   z: sin(t*2),  w: sin(t*3)
float4 _CosTime;                  // x: cos(t/20),      y: cos(t),   z: cos(t*2),  w: cos(t*3)

// -----------------------------------------------------------------------------
// Std functions

// Z buffer depth to linear 0-1 depth
// Handles orthographic projection correctly
float Linear01Depth(float z)
{
    float isOrtho = unity_OrthoParams.w;
    float isPers = 1.0 - unity_OrthoParams.w;
    z *= _ZBufferParams.x;
    return (1.0 - isOrtho * z) / (isPers * z + _ZBufferParams.y);
}

float LinearEyeDepth(float z)
{
    return rcp(_ZBufferParams.z * z + _ZBufferParams.w);
}

// Clamp HDR value within a safe range
half3 SafeHDR(half3 c)
{
    return min(c, HALF_MAX);
}

half4 SafeHDR(half4 c)
{
    return min(c, HALF_MAX);
}

// Decode normals stored in _CameraDepthNormalsTexture
float3 DecodeViewNormalStereo(float4 enc4)
{
    float kScale = 1.7777;
    float3 nn = enc4.xyz * float3(2.0 * kScale, 2.0 * kScale, 0) + float3(-kScale, -kScale, 1);
    float g = 2.0 / dot(nn.xyz, nn.xyz);
    float3 n;
    n.xy = g * nn.xy;
    n.z = g - 1.0;
    return n;
}

// Interleaved gradient function from Jimenez 2014
// http://www.iryoku.com/next-generation-post-processing-in-call-of-duty-advanced-warfare
float GradientNoise(float2 uv)
{
    uv = floor(uv * _ScreenParams.xy);
    float f = dot(float2(0.06711056, 0.00583715), uv);
    return frac(52.9829189 * frac(f));
}

// Vertex manipulation
float2 TransformTriangleVertexToUV(float2 vertex)
{
    float2 uv = (vertex + 1.0) * 0.5;
    return uv;
}

#include "xRLib.hlsl"

// -----------------------------------------------------------------------------
// Default vertex shaders

struct AttributesDefault
{
    float3 vertex : POSITION;
};

struct VaryingsDefault
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float2 texcoordStereo : TEXCOORD1;
};

VaryingsDefault VertDefault(AttributesDefault v)
{
    VaryingsDefault o;
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
    o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

    o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);

    return o;
}

float4 _UVTransform; // xy: scale, wz: translate

VaryingsDefault VertUVTransform(AttributesDefault v)
{
    VaryingsDefault o;
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    o.texcoord = TransformTriangleVertexToUV(v.vertex.xy) * _UVTransform.xy + _UVTransform.zw;
    o.texcoordStereo = TransformStereoScreenSpaceTex(o.texcoord, 1.0);
    return o;
}

#define TRANSFORM_TEX(tex,name) (tex.xy * name##_ST.xy + name##_ST.zw)

#endif // UNITY_POSTFX_STDLIB
