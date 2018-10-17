#ifndef SHADER_API_GLES
#error GLES.hlsl should not be included if SHADER_API_GLES is not defined
#endif

#define UNITY_UV_STARTS_AT_TOP 0
#define UNITY_REVERSED_Z 0
#define UNITY_GATHER_SUPPORTED 0
#define UNITY_NEAR_CLIP_VALUE (-1.0)

// This value will not go through any matrix projection convertion
#define UNITY_RAW_FAR_CLIP_VALUE (1.0)
#define VERTEXID_SEMANTIC gl_VertexID
#define INSTANCEID_SEMANTIC gl_InstanceID
#define FRONT_FACE_SEMANTIC VFACE
#define FRONT_FACE_TYPE float
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL > 0.0) ? (FRONT) : (BACK))

#define CBUFFER_START(name)
#define CBUFFER_END

// flow control attributes
#define UNITY_BRANCH
#define UNITY_FLATTEN
#define UNITY_UNROLL
#define UNITY_UNROLLX(_x)
#define UNITY_LOOP

#define uint int

#define rcp(x) 1.0 / x
#define ddx_fine ddx
#define ddy_fine ddy
#define asfloat
#define asuint(x) asint(x)
#define f32tof16
#define f16tof32

#define ERROR_ON_UNSUPPORTED_FUNCTION(funcName) #error ##funcName is not supported on GLES 2.0

// Initialize arbitrary structure with zero values.
// Do not exist on some platform, in this case we need to have a standard name that call a function that will initialize all parameters to 0
#define ZERO_INITIALIZE(type, name) name = (type)0;
#define ZERO_INITIALIZE_ARRAY(type, name, arraySize) { for (int arrayIndex = 0; arrayIndex < arraySize; arrayIndex++) { name[arrayIndex] = (type)0; } }

// GLES2 might not have shadow hardware comparison support
#if defined(UNITY_ENABLE_NATIVE_SHADOWS_LOOKUPS)
#define SHADOW2D_TEXTURE_AND_SAMPLER sampler2DShadow
#define SHADOWCUBE_TEXTURE_AND_SAMPLER samplerCUBEShadow
#define SHADOW2D_SAMPLE(textureName, samplerName, coord3) shadow2D(textureName, coord3)
#define SHADOWCUBE_SAMPLE(textureName, samplerName, coord4) ((texCUBE(textureName,(coord4).xyz) < (coord4).w) ? 0.0 : 1.0)
#else
// emulate hardware comparison
#define SHADOW2D_TEXTURE_AND_SAMPLER sampler2D_float
#define SHADOWCUBE_TEXTURE_AND_SAMPLER samplerCUBE_float
#define SHADOW2D_SAMPLE(textureName, samplerName, coord3) ((SAMPLE_DEPTH_TEXTURE(textureName, samplerName, (coord3).xy) < (coord3).z) ? 0.0 : 1.0)
#define SHADOWCUBE_SAMPLE(textureName, samplerName, coord4) ((texCUBE(textureName,(coord4).xyz).r < (coord4).w) ? 0.0 : 1.0)
#endif

// Texture util abstraction

#define CALCULATE_TEXTURE2D_LOD(textureName, samplerName, coord2) #error calculate Level of Detail not supported in GLES2

// Texture abstraction

#define TEXTURE2D(textureName)                          sampler2D textureName
#define TEXTURE2D_ARRAY(textureName)                    samplerCUBE textureName // No support to texture2DArray
#define TEXTURECUBE(textureName)                        samplerCUBE textureName
#define TEXTURECUBE_ARRAY(textureName)                  samplerCUBE textureName // No supoport to textureCubeArray and can't emulate with texture2DArray
#define TEXTURE3D(textureName)                          sampler3D textureName

#define TEXTURE2D_FLOAT(textureName)                    sampler2D_float textureName
#define TEXTURE2D_ARRAY_FLOAT(textureName)              TEXTURECUBE_FLOAT(textureName) // No support to texture2DArray
#define TEXTURECUBE_FLOAT(textureName)                  samplerCUBE_float textureName
#define TEXTURECUBE_ARRAY_FLOAT(textureName)            TEXTURECUBE_FLOAT(textureName) // No support to textureCubeArray
#define TEXTURE3D_FLOAT(textureName)                    sampler3D_float textureName

#define TEXTURE2D_HALF(textureName)                     sampler2D_half textureName
#define TEXTURE2D_ARRAY_HALF(textureName)               TEXTURECUBE_HALF(textureName) // No support to texture2DArray
#define TEXTURECUBE_HALF(textureName)                   samplerCUBE_half textureName
#define TEXTURECUBE_ARRAY_HALF(textureName)             TEXTURECUBE_HALF(textureName) // No support to textureCubeArray
#define TEXTURE3D_HALF(textureName)                     sampler3D_half textureName

#define TEXTURE2D_SHADOW(textureName)                   SHADOW2D_TEXTURE_AND_SAMPLER textureName
#define TEXTURE2D_ARRAY_SHADOW(textureName)             TEXTURECUBE_SHADOW(textureName) // No support to texture array
#define TEXTURECUBE_SHADOW(textureName)                 SHADOWCUBE_TEXTURE_AND_SAMPLER textureName
#define TEXTURECUBE_ARRAY_SHADOW(textureName)           TEXTURECUBE_SHADOW(textureName) // No support to texture array

#define RW_TEXTURE2D(type, textureNam)                  ERROR_ON_UNSUPPORTED_FUNCTION(RWTexture2D)
#define RW_TEXTURE2D_ARRAY(type, textureName)           ERROR_ON_UNSUPPORTED_FUNCTION(RWTexture2DArray)
#define RW_TEXTURE3D(type, textureNam)                  ERROR_ON_UNSUPPORTED_FUNCTION(RWTexture3D)

#define SAMPLER(samplerName)
#define SAMPLER_CMP(samplerName)

#define TEXTURE2D_ARGS(textureName, samplerName)                sampler2D textureName
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName)          samplerCUBE textureName
#define TEXTURECUBE_ARGS(textureName, samplerName)              samplerCUBE textureName
#define TEXTURECUBE_ARRAY_ARGS(textureName, samplerName)        samplerCUBE textureName
#define TEXTURE3D_ARGS(textureName, samplerName)                sampler3D textureName
#define TEXTURE2D_SHADOW_ARGS(textureName, samplerName)         SHADOW2D_TEXTURE_AND_SAMPLER textureName
#define TEXTURE2D_ARRAY_SHADOW_ARGS(textureName, samplerName)   SHADOWCUBE_TEXTURE_AND_SAMPLER textureName
#define TEXTURECUBE_SHADOW_ARGS(textureName, samplerName)       SHADOWCUBE_TEXTURE_AND_SAMPLER textureName

#define TEXTURE2D_PARAM(textureName, samplerName)               textureName
#define TEXTURE2D_ARRAY_PARAM(textureName, samplerName)         textureName
#define TEXTURECUBE_PARAM(textureName, samplerName)             textureName
#define TEXTURECUBE_ARRAY_PARAM(textureName, samplerName)       textureName
#define TEXTURE3D_PARAM(textureName, samplerName)               textureName
#define TEXTURE2D_SHADOW_PARAM(textureName, samplerName)        textureName
#define TEXTURE2D_ARRAY_SHADOW_PARAM(textureName, samplerName)  textureName
#define TEXTURECUBE_SHADOW_PARAM(textureName, samplerName)      textureName

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) tex2D(textureName, coord2)

#if (SHADER_TARGET >= 30)
    #define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) tex2Dlod(textureName, float4(coord2, 0, lod))
#else
    // No lod support. Very poor approximation with bias.
    #define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, lod)
#endif

#define SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias)                       tex2Dbias(textureName, float4(coord2, 0, bias))
#define SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, ddx, ddy)                   SAMPLE_TEXTURE2D(textureName, coord2)
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)                     ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY)
#define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod)            ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY_LOD)
#define SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, bias)          ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY_BIAS)
#define SAMPLE_TEXTURE2D_ARRAY_GRAD(textureName, samplerName, coord2, index, dpdx, dpdy)    ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY_GRAD)
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3)                                texCUBE(textureName, coord3)
// No lod support. Very poor approximation with bias.
#define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3, lod)                       SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, lod)
#define SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, bias)                     texCUBEbias(textureName, float4(coord3, bias))
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)                   ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY)
#define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)          ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY_LOD)
#define SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, bias)        ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY_BIAS)
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                                  tex3D(textureName, coord3)

#define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3)                           SHADOW2D_SAMPLE(textureName, samplerName, coord3)
#define SAMPLE_TEXTURE2D_ARRAY_SHADOW(textureName, samplerName, coord3, index)              ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURE2D_ARRAY_SHADOW)
#define SAMPLE_TEXTURECUBE_SHADOW(textureName, samplerName, coord4)                         SHADOWCUBE_SAMPLE(textureName, samplerName, coord4)
#define SAMPLE_TEXTURECUBE_ARRAY_SHADOW(textureName, samplerName, coord4, index)            ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY_SHADOW)

#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2)                              SAMPLE_TEXTURE2D(textureName, samplerName, coord2).r
#define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod)                     SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod).r

// Not supported. Can't define as error because shader library is calling these functions.
#define LOAD_TEXTURE2D(textureName, unCoord2)                                               half4(0, 0, 0, 0)
#define LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod)                                      half4(0, 0, 0, 0)
#define LOAD_TEXTURE2D_MSAA(textureName, unCoord2, sampleIndex)                             half4(0, 0, 0, 0)
#define LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, index)                                  half4(0, 0, 0, 0)
#define LOAD_TEXTURE2D_ARRAY_MSAA(textureName, unCoord2, index, sampleIndex)                half4(0, 0, 0, 0)
#define LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, index, lod)                         half4(0, 0, 0, 0)

// Gather not supported. Fallback to regular texture sampling.
#define GATHER_TEXTURE2D(textureName, samplerName, coord2)                  ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURE2D)
#define GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)     ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURE2D_ARRAY)
#define GATHER_TEXTURECUBE(textureName, samplerName, coord3)                ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURECUBE)
#define GATHER_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)   ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURECUBE_ARRAY)
