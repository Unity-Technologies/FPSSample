#ifndef UNITY_MACROS_INCLUDED
#define UNITY_MACROS_INCLUDED

// Some shader compiler don't support to do multiple ## for concatenation inside the same macro, it require an indirection.
// This is the purpose of this macro
#define MERGE_NAME(X, Y) X##Y

// These define are use to abstract the way we sample into a cubemap array.
// Some platform don't support cubemap array so we fallback on 2D latlong
#ifdef  UNITY_NO_CUBEMAP_ARRAY
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURE2D_ARRAY
#define TEXTURECUBE_ARRAY_ARGS_ABSTRACT TEXTURE2D_ARRAY_ARGS
#define TEXTURECUBE_ARRAY_PARAM_ABSTRACT TEXTURE2D_ARRAY_PARAM
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, DirectionToLatLongCoordinate(coord3), index, lod)
#else
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURECUBE_ARRAY
#define TEXTURECUBE_ARRAY_ARGS_ABSTRACT TEXTURECUBE_ARRAY_ARGS
#define TEXTURECUBE_ARRAY_PARAM_ABSTRACT TEXTURECUBE_ARRAY_PARAM
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)
#endif

#define PI          3.14159265358979323846
#define TWO_PI      6.28318530717958647693
#define FOUR_PI     12.5663706143591729538
#define INV_PI      0.31830988618379067154
#define INV_TWO_PI  0.15915494309189533577
#define INV_FOUR_PI 0.07957747154594766788
#define HALF_PI     1.57079632679489661923
#define INV_HALF_PI 0.63661977236758134308
#define LOG2_E      1.44269504088896340736

#define MILLIMETERS_PER_METER 1000
#define METERS_PER_MILLIMETER rcp(MILLIMETERS_PER_METER)
#define CENTIMETERS_PER_METER 100
#define METERS_PER_CENTIMETER rcp(CENTIMETERS_PER_METER)

#define FLT_INF  asfloat(0x7F800000)
#define FLT_EPS  5.960464478e-8  // 2^-24, machine epsilon: 1 + EPS = 1 (half of the ULP for 1.0f)
#define FLT_MIN  1.175494351e-38 // Minimum normalized positive floating-point number
#define FLT_MAX  3.402823466e+38 // Maximum representable floating-point number
#define HALF_MIN 6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Small_Float_Formats
#define HALF_MAX 65504.0
#define UINT_MAX 0xFFFFFFFFu

#define TEMPLATE_1_FLT(FunctionName, Parameter1, FunctionBody) \
    float  FunctionName(float  Parameter1) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1) { FunctionBody; }

#define TEMPLATE_1_HALF(FunctionName, Parameter1, FunctionBody) \
    half  FunctionName(half  Parameter1) { FunctionBody; } \
    half2 FunctionName(half2 Parameter1) { FunctionBody; } \
    half3 FunctionName(half3 Parameter1) { FunctionBody; } \
    half4 FunctionName(half4 Parameter1) { FunctionBody; } \
    float  FunctionName(float  Parameter1) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1) { FunctionBody; }

#ifdef SHADER_API_GLES
    #define TEMPLATE_1_INT(FunctionName, Parameter1, FunctionBody) \
    int    FunctionName(int    Parameter1) { FunctionBody; } \
    int2   FunctionName(int2   Parameter1) { FunctionBody; } \
    int3   FunctionName(int3   Parameter1) { FunctionBody; } \
    int4   FunctionName(int4   Parameter1) { FunctionBody; }
#else
    #define TEMPLATE_1_INT(FunctionName, Parameter1, FunctionBody) \
    int    FunctionName(int    Parameter1) { FunctionBody; } \
    int2   FunctionName(int2   Parameter1) { FunctionBody; } \
    int3   FunctionName(int3   Parameter1) { FunctionBody; } \
    int4   FunctionName(int4   Parameter1) { FunctionBody; } \
    uint   FunctionName(uint   Parameter1) { FunctionBody; } \
    uint2  FunctionName(uint2  Parameter1) { FunctionBody; } \
    uint3  FunctionName(uint3  Parameter1) { FunctionBody; } \
    uint4  FunctionName(uint4  Parameter1) { FunctionBody; }
#endif

#define TEMPLATE_2_FLT(FunctionName, Parameter1, Parameter2, FunctionBody) \
    float  FunctionName(float  Parameter1, float  Parameter2) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1, float2 Parameter2) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1, float3 Parameter2) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1, float4 Parameter2) { FunctionBody; }

#define TEMPLATE_2_HALF(FunctionName, Parameter1, Parameter2, FunctionBody) \
    half  FunctionName(half  Parameter1, half  Parameter2) { FunctionBody; } \
    half2 FunctionName(half2 Parameter1, half2 Parameter2) { FunctionBody; } \
    half3 FunctionName(half3 Parameter1, half3 Parameter2) { FunctionBody; } \
    half4 FunctionName(half4 Parameter1, half4 Parameter2) { FunctionBody; } \
    float  FunctionName(float  Parameter1, float  Parameter2) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1, float2 Parameter2) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1, float3 Parameter2) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1, float4 Parameter2) { FunctionBody; }


#ifdef SHADER_API_GLES
    #define TEMPLATE_2_INT(FunctionName, Parameter1, Parameter2, FunctionBody) \
    int    FunctionName(int    Parameter1, int    Parameter2) { FunctionBody; } \
    int2   FunctionName(int2   Parameter1, int2   Parameter2) { FunctionBody; } \
    int3   FunctionName(int3   Parameter1, int3   Parameter2) { FunctionBody; } \
    int4   FunctionName(int4   Parameter1, int4   Parameter2) { FunctionBody; }
#else
    #define TEMPLATE_2_INT(FunctionName, Parameter1, Parameter2, FunctionBody) \
    int    FunctionName(int    Parameter1, int    Parameter2) { FunctionBody; } \
    int2   FunctionName(int2   Parameter1, int2   Parameter2) { FunctionBody; } \
    int3   FunctionName(int3   Parameter1, int3   Parameter2) { FunctionBody; } \
    int4   FunctionName(int4   Parameter1, int4   Parameter2) { FunctionBody; } \
    uint   FunctionName(uint   Parameter1, uint   Parameter2) { FunctionBody; } \
    uint2  FunctionName(uint2  Parameter1, uint2  Parameter2) { FunctionBody; } \
    uint3  FunctionName(uint3  Parameter1, uint3  Parameter2) { FunctionBody; } \
    uint4  FunctionName(uint4  Parameter1, uint4  Parameter2) { FunctionBody; }
#endif

#define TEMPLATE_3_FLT(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) \
    float  FunctionName(float  Parameter1, float  Parameter2, float  Parameter3) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1, float2 Parameter2, float2 Parameter3) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1, float3 Parameter2, float3 Parameter3) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1, float4 Parameter2, float4 Parameter3) { FunctionBody; }

#define TEMPLATE_3_HALF(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) \
    half  FunctionName(half  Parameter1, half  Parameter2, half  Parameter3) { FunctionBody; } \
    half2 FunctionName(half2 Parameter1, half2 Parameter2, half2 Parameter3) { FunctionBody; } \
    half3 FunctionName(half3 Parameter1, half3 Parameter2, half3 Parameter3) { FunctionBody; } \
    half4 FunctionName(half4 Parameter1, half4 Parameter2, half4 Parameter3) { FunctionBody; } \
    float  FunctionName(float  Parameter1, float  Parameter2, float  Parameter3) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1, float2 Parameter2, float2 Parameter3) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1, float3 Parameter2, float3 Parameter3) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1, float4 Parameter2, float4 Parameter3) { FunctionBody; }

#ifdef SHADER_API_GLES
    #define TEMPLATE_3_INT(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) \
    int    FunctionName(int    Parameter1, int    Parameter2, int    Parameter3) { FunctionBody; } \
    int2   FunctionName(int2   Parameter1, int2   Parameter2, int2   Parameter3) { FunctionBody; } \
    int3   FunctionName(int3   Parameter1, int3   Parameter2, int3   Parameter3) { FunctionBody; } \
    int4   FunctionName(int4   Parameter1, int4   Parameter2, int4   Parameter3) { FunctionBody; }
#else
    #define TEMPLATE_3_INT(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) \
    int    FunctionName(int    Parameter1, int    Parameter2, int    Parameter3) { FunctionBody; } \
    int2   FunctionName(int2   Parameter1, int2   Parameter2, int2   Parameter3) { FunctionBody; } \
    int3   FunctionName(int3   Parameter1, int3   Parameter2, int3   Parameter3) { FunctionBody; } \
    int4   FunctionName(int4   Parameter1, int4   Parameter2, int4   Parameter3) { FunctionBody; } \
    uint   FunctionName(uint   Parameter1, uint   Parameter2, uint   Parameter3) { FunctionBody; } \
    uint2  FunctionName(uint2  Parameter1, uint2  Parameter2, uint2  Parameter3) { FunctionBody; } \
    uint3  FunctionName(uint3  Parameter1, uint3  Parameter2, uint3  Parameter3) { FunctionBody; } \
    uint4  FunctionName(uint4  Parameter1, uint4  Parameter2, uint4  Parameter3) { FunctionBody; }
#endif

#ifdef SHADER_API_GLES
    #define TEMPLATE_SWAP(FunctionName) \
    void FunctionName(inout real  a, inout real  b) { real  t = a; a = b; b = t; } \
    void FunctionName(inout real2 a, inout real2 b) { real2 t = a; a = b; b = t; } \
    void FunctionName(inout real3 a, inout real3 b) { real3 t = a; a = b; b = t; } \
    void FunctionName(inout real4 a, inout real4 b) { real4 t = a; a = b; b = t; } \
    void FunctionName(inout int    a, inout int    b) { int    t = a; a = b; b = t; } \
    void FunctionName(inout int2   a, inout int2   b) { int2   t = a; a = b; b = t; } \
    void FunctionName(inout int3   a, inout int3   b) { int3   t = a; a = b; b = t; } \
    void FunctionName(inout int4   a, inout int4   b) { int4   t = a; a = b; b = t; } \
    void FunctionName(inout bool   a, inout bool   b) { bool   t = a; a = b; b = t; } \
    void FunctionName(inout bool2  a, inout bool2  b) { bool2  t = a; a = b; b = t; } \
    void FunctionName(inout bool3  a, inout bool3  b) { bool3  t = a; a = b; b = t; } \
    void FunctionName(inout bool4  a, inout bool4  b) { bool4  t = a; a = b; b = t; }
#else
    #if HAS_HALF
        #define TEMPLATE_SWAP(FunctionName) \
        void FunctionName(inout real  a, inout real  b) { real  t = a; a = b; b = t; } \
        void FunctionName(inout real2 a, inout real2 b) { real2 t = a; a = b; b = t; } \
        void FunctionName(inout real3 a, inout real3 b) { real3 t = a; a = b; b = t; } \
        void FunctionName(inout real4 a, inout real4 b) { real4 t = a; a = b; b = t; } \
        void FunctionName(inout float  a, inout float  b) { float  t = a; a = b; b = t; } \
        void FunctionName(inout float2 a, inout float2 b) { float2 t = a; a = b; b = t; } \
        void FunctionName(inout float3 a, inout float3 b) { float3 t = a; a = b; b = t; } \
        void FunctionName(inout float4 a, inout float4 b) { float4 t = a; a = b; b = t; } \
        void FunctionName(inout int    a, inout int    b) { int    t = a; a = b; b = t; } \
        void FunctionName(inout int2   a, inout int2   b) { int2   t = a; a = b; b = t; } \
        void FunctionName(inout int3   a, inout int3   b) { int3   t = a; a = b; b = t; } \
        void FunctionName(inout int4   a, inout int4   b) { int4   t = a; a = b; b = t; } \
        void FunctionName(inout uint   a, inout uint   b) { uint   t = a; a = b; b = t; } \
        void FunctionName(inout uint2  a, inout uint2  b) { uint2  t = a; a = b; b = t; } \
        void FunctionName(inout uint3  a, inout uint3  b) { uint3  t = a; a = b; b = t; } \
        void FunctionName(inout uint4  a, inout uint4  b) { uint4  t = a; a = b; b = t; } \
        void FunctionName(inout bool   a, inout bool   b) { bool   t = a; a = b; b = t; } \
        void FunctionName(inout bool2  a, inout bool2  b) { bool2  t = a; a = b; b = t; } \
        void FunctionName(inout bool3  a, inout bool3  b) { bool3  t = a; a = b; b = t; } \
        void FunctionName(inout bool4  a, inout bool4  b) { bool4  t = a; a = b; b = t; }
    #else
        #define TEMPLATE_SWAP(FunctionName) \
        void FunctionName(inout real  a, inout real  b) { real  t = a; a = b; b = t; } \
        void FunctionName(inout real2 a, inout real2 b) { real2 t = a; a = b; b = t; } \
        void FunctionName(inout real3 a, inout real3 b) { real3 t = a; a = b; b = t; } \
        void FunctionName(inout real4 a, inout real4 b) { real4 t = a; a = b; b = t; } \
        void FunctionName(inout int    a, inout int    b) { int    t = a; a = b; b = t; } \
        void FunctionName(inout int2   a, inout int2   b) { int2   t = a; a = b; b = t; } \
        void FunctionName(inout int3   a, inout int3   b) { int3   t = a; a = b; b = t; } \
        void FunctionName(inout int4   a, inout int4   b) { int4   t = a; a = b; b = t; } \
        void FunctionName(inout uint   a, inout uint   b) { uint   t = a; a = b; b = t; } \
        void FunctionName(inout uint2  a, inout uint2  b) { uint2  t = a; a = b; b = t; } \
        void FunctionName(inout uint3  a, inout uint3  b) { uint3  t = a; a = b; b = t; } \
        void FunctionName(inout uint4  a, inout uint4  b) { uint4  t = a; a = b; b = t; } \
        void FunctionName(inout bool   a, inout bool   b) { bool   t = a; a = b; b = t; } \
        void FunctionName(inout bool2  a, inout bool2  b) { bool2  t = a; a = b; b = t; } \
        void FunctionName(inout bool3  a, inout bool3  b) { bool3  t = a; a = b; b = t; } \
        void FunctionName(inout bool4  a, inout bool4  b) { bool4  t = a; a = b; b = t; }
    #endif
#endif


// MACRO from Legacy Untiy
// Transforms 2D UV by scale/bias property
#define TRANSFORM_TEX(tex, name) ((tex.xy) * name##_ST.xy + name##_ST.zw)
#define GET_TEXELSIZE_NAME(name) (name##_TexelSize)

#if UNITY_REVERSED_Z
# define COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, zDevice)      (shadowMapDepth >  zDevice) 
# define COMPARE_DEVICE_DEPTH_CLOSEREQUAL(shadowMapDepth, zDevice) (shadowMapDepth >= zDevice) 
#else
# define COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, zDevice)      (shadowMapDepth <  zDevice) 
# define COMPARE_DEVICE_DEPTH_CLOSEREQUAL(shadowMapDepth, zDevice) (shadowMapDepth <= zDevice) 
#endif

#endif // UNITY_MACROS_INCLUDED
