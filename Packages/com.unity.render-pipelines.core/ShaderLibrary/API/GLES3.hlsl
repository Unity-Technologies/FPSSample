#ifndef SHADER_API_GLES3
#error GLES.hlsl should not be included if SHADER_API_GLES3 is not defined
#endif

#define UNITY_UV_STARTS_AT_TOP 0
#define UNITY_REVERSED_Z 0
#define UNITY_GATHER_SUPPORTED 0
#define UNITY_NEAR_CLIP_VALUE (-1.0)

// This value will not go through any matrix projection convertion
#define UNITY_RAW_FAR_CLIP_VALUE (1.0)
#define VERTEXID_SEMANTIC SV_VertexID
#define INSTANCEID_SEMANTIC SV_InstanceID
#define FRONT_FACE_SEMANTIC VFACE
#define FRONT_FACE_TYPE float
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL > 0.0) ? (FRONT) : (BACK))

#define ERROR_ON_UNSUPPORTED_FUNCTION(funcName) #error ##funcName is not supported on GLES 3.0

#define CBUFFER_START(name)
#define CBUFFER_END

// flow control attributes
#define UNITY_BRANCH        [branch]
#define UNITY_FLATTEN       [flatten]
#define UNITY_UNROLL        [unroll]
#define UNITY_UNROLLX(_x)   [unroll(_x)]
#define UNITY_LOOP          [loop]

// GLES 3.1 + AEP shader feature https://docs.unity3d.com/Manual/SL-ShaderCompileTargets.html
#if (SHADER_TARGET >= 40)
#define GLES3_1_AEP 1
#else
#define GLES3_1_AEP 0
#endif

// Initialize arbitrary structure with zero values.
// Do not exist on some platform, in this case we need to have a standard name that call a function that will initialize all parameters to 0
#define ZERO_INITIALIZE(type, name) name = (type)0;
#define ZERO_INITIALIZE_ARRAY(type, name, arraySize) { for (int arrayIndex = 0; arrayIndex < arraySize; arrayIndex++) { name[arrayIndex] = (type)0; } }

// Texture util abstraction

#define CALCULATE_TEXTURE2D_LOD(textureName, samplerName, coord2) textureName.CalculateLevelOfDetail(samplerName, coord2)

// Texture abstraction

#define TEXTURE2D(textureName)                  Texture2D textureName
#define TEXTURE2D_ARRAY(textureName)            Texture2DArray textureName
#define TEXTURECUBE(textureName)                TextureCube textureName
#define TEXTURECUBE_ARRAY(textureName)          TextureCubeArray textureName
#define TEXTURE3D(textureName)                  Texture3D textureName

#define TEXTURE2D_FLOAT(textureName)            Texture2D_float textureName
#define TEXTURE2D_ARRAY_FLOAT(textureName)      Texture2DArray textureName    // no support to _float on Array, it's being added
#define TEXTURECUBE_FLOAT(textureName)          TextureCube_float textureName
#define TEXTURECUBE_ARRAY_FLOAT(textureName)    TextureCubeArray textureName  // no support to _float on Array, it's being added
#define TEXTURE3D_FLOAT(textureName)            Texture3D_float textureName

#define TEXTURE2D_HALF(textureName)             Texture2D_half textureName
#define TEXTURE2D_ARRAY_HALF(textureName)       Texture2DArray textureName    // no support to _float on Array, it's being added
#define TEXTURECUBE_HALF(textureName)           TextureCube_half textureName
#define TEXTURECUBE_ARRAY_HALF(textureName)     TextureCubeArray textureName  // no support to _float on Array, it's being added
#define TEXTURE3D_HALF(textureName)             Texture3D_half textureName

#define TEXTURE2D_SHADOW(textureName)           TEXTURE2D(textureName)
#define TEXTURE2D_ARRAY_SHADOW(textureName)     TEXTURE2D_ARRAY(textureName)
#define TEXTURECUBE_SHADOW(textureName)         TEXTURECUBE(textureName)
#define TEXTURECUBE_ARRAY_SHADOW(textureName)   TEXTURECUBE_ARRAY(textureName)

#define RW_TEXTURE2D(type, textureName)         ERROR_ON_UNSUPPORTED_FUNCTION(RWTexture2D)
#define RW_TEXTURE2D_ARRAY(type, textureName)   ERROR_ON_UNSUPPORTED_FUNCTION(RWTexture2DArray)
#define RW_TEXTURE3D(type, textureName)         ERROR_ON_UNSUPPORTED_FUNCTION(RWTexture3D)

#define SAMPLER(samplerName)                    SamplerState samplerName
#define SAMPLER_CMP(samplerName)                SamplerComparisonState samplerName

#define TEXTURE2D_ARGS(textureName, samplerName)                 TEXTURE2D(textureName),         SAMPLER(samplerName)
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName)           TEXTURE2D_ARRAY(textureName),   SAMPLER(samplerName)
#define TEXTURECUBE_ARGS(textureName, samplerName)               TEXTURECUBE(textureName),       SAMPLER(samplerName)
#define TEXTURECUBE_ARRAY_ARGS(textureName, samplerName)         TEXTURECUBE_ARRAY(textureName), SAMPLER(samplerName)
#define TEXTURE3D_ARGS(textureName, samplerName)                 TEXTURE3D(textureName),         SAMPLER(samplerName)

#define TEXTURE2D_SHADOW_ARGS(textureName, samplerName)          TEXTURE2D(textureName),         SAMPLER_CMP(samplerName)
#define TEXTURE2D_ARRAY_SHADOW_ARGS(textureName, samplerName)    TEXTURE2D_ARRAY(textureName),   SAMPLER_CMP(samplerName)
#define TEXTURECUBE_SHADOW_ARGS(textureName, samplerName)        TEXTURECUBE(textureName),       SAMPLER_CMP(samplerName)
#define TEXTURECUBE_ARRAY_SHADOW_ARGS(textureName, samplerName)  TEXTURECUBE_ARRAY(textureName), SAMPLER_CMP(samplerName)

#define TEXTURE2D_PARAM(textureName, samplerName)                textureName, samplerName
#define TEXTURE2D_ARRAY_PARAM(textureName, samplerName)          textureName, samplerName
#define TEXTURECUBE_PARAM(textureName, samplerName)              textureName, samplerName
#define TEXTURECUBE_ARRAY_PARAM(textureName, samplerName)        textureName, samplerName
#define TEXTURE3D_PARAM(textureName, samplerName)                textureName, samplerName

#define TEXTURE2D_SHADOW_PARAM(textureName, samplerName)         textureName, samplerName
#define TEXTURE2D_ARRAY_SHADOW_PARAM(textureName, samplerName)   textureName, samplerName
#define TEXTURECUBE_SHADOW_PARAM(textureName, samplerName)       textureName, samplerName
#define TEXTURECUBE_ARRAY_SHADOW_PARAM(textureName, samplerName) textureName, samplerName

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                               textureName.Sample(samplerName, coord2)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod)                      textureName.SampleLevel(samplerName, coord2, lod)
#define SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias)                    textureName.SampleBias(samplerName, coord2, bias)
#define SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, ddx, ddy)                textureName.SampleGrad(samplerName, coord2, ddx, ddy)
#define SAMPLE_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)                  textureName.Sample(samplerName, float3(coord2, index))
#define SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, coord2, index, lod)         textureName.SampleLevel(samplerName, float3(coord2, index), lod)
#define SAMPLE_TEXTURE2D_ARRAY_BIAS(textureName, samplerName, coord2, index, bias)       textureName.SampleBias(samplerName, float3(coord2, index), bias)
#define SAMPLE_TEXTURE2D_ARRAY_GRAD(textureName, samplerName, coord2, index, dpdx, dpdy) textureName.SampleGrad(samplerName, float3(coord2, index), dpdx, dpdy)
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3)                             textureName.Sample(samplerName, coord3)
#define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3, lod)                    textureName.SampleLevel(samplerName, coord3, lod)
#define SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, bias)                  textureName.SampleBias(samplerName, coord3, bias)

#ifdef UNITY_NO_CUBEMAP_ARRAY
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)           ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY)
#define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)  ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY_LOD)
#define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, bias) ERROR_ON_UNSUPPORTED_FUNCTION(SAMPLE_TEXTURECUBE_ARRAY_LOD)
#else
#define SAMPLE_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)           textureName.Sample(samplerName, float4(coord3, index))
#define SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)  textureName.SampleLevel(samplerName, float4(coord3, index), lod)
#define SAMPLE_TEXTURECUBE_ARRAY_BIAS(textureName, samplerName, coord3, index, bias)textureName.SampleBias(samplerName, float4(coord3, index), bias)
#endif

#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3)                          textureName.Sample(samplerName, coord3)

#define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3)                   textureName.SampleCmpLevelZero(samplerName, (coord3).xy, (coord3).z)
#define SAMPLE_TEXTURE2D_ARRAY_SHADOW(textureName, samplerName, coord3, index)      textureName.SampleCmpLevelZero(samplerName, float3((coord3).xy, index), (coord3).z)
#define SAMPLE_TEXTURECUBE_SHADOW(textureName, samplerName, coord4)                 textureName.SampleCmpLevelZero(samplerName, (coord4).xyz, (coord4).w)
#define SAMPLE_TEXTURECUBE_ARRAY_SHADOW(textureName, samplerName, coord4, index)    textureName.SampleCmpLevelZero(samplerName, float4((coord4).xyz, index), (coord4).w)

#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2)                      SAMPLE_TEXTURE2D(textureName, samplerName, coord2).r
#define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod)             SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod).r

#define LOAD_TEXTURE2D(textureName, unCoord2)                                       textureName.Load(int3(unCoord2, 0))
#define LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod)                              textureName.Load(int3(unCoord2, lod))
#define LOAD_TEXTURE2D_MSAA(textureName, unCoord2, sampleIndex)                     textureName.Load(unCoord2, sampleIndex)
#define LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, index)                          textureName.Load(int4(unCoord2, index, 0))
#define LOAD_TEXTURE2D_ARRAY_MSAA(textureName, unCoord2, index, sampleIndex)        textureName.Load(int4(unCoord2, index, 0), sampleIndex)
#define LOAD_TEXTURE2D_ARRAY_LOD(textureName, unCoord2, index, lod)                 textureName.Load(int4(unCoord2, index, lod))

#if GLES3_1_AEP
#define GATHER_TEXTURE2D(textureName, samplerName, coord2)                  textureName.Gather(samplerName, coord2)
#define GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)     textureName.Gather(samplerName, float3(coord2, index))
#define GATHER_TEXTURECUBE(textureName, samplerName, coord3)                textureName.Gather(samplerName, coord3)
#define GATHER_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)   textureName.Gather(samplerName, float4(coord3, index))
#else
#define GATHER_TEXTURE2D(textureName, samplerName, coord2)                  ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURE2D)
#define GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index)     ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURE2D_ARRAY)
#define GATHER_TEXTURECUBE(textureName, samplerName, coord3)                ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURECUBE)
#define GATHER_TEXTURECUBE_ARRAY(textureName, samplerName, coord3, index)   ERROR_ON_UNSUPPORTED_FUNCTION(GATHER_TEXTURECUBE_ARRAY)
#endif

