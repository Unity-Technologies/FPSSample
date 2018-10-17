
#define UNITY_UV_STARTS_AT_TOP 1
#define UNITY_REVERSED_Z 0
#define UNITY_GATHER_SUPPORTED 0
#define UNITY_CAN_READ_POSITION_IN_FRAGMENT_PROGRAM 0

#define TEXTURE2D_SAMPLER2D(textureName, samplerName) sampler2D textureName

#define TEXTURE2D(textureName) sampler2D textureName
#define SAMPLER2D(samplerName)

#define TEXTURE2D_ARGS(textureName, samplerName) sampler2D textureName
#define TEXTURE2D_PARAM(textureName, samplerName) textureName

#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) tex2D(textureName, coord2)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) tex2Dlod(textureName, float4(coord2, 0.0, lod))

#define LOAD_TEXTURE2D(textureName, texelSize, icoord2) tex2D(textureName, icoord2 / texelSize)
#define LOAD_TEXTURE2D_LOD(textureName, texelSize, icoord2) tex2Dlod(textureName, float4(icoord2 / texelSize, 0.0, lod))

#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2) tex2D<float>(textureName, coord2).r
#define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod) tex2Dlod<float>(textureName, float4(coord2, 0.0, lod)).r

// 3D textures are not supported on Vita, use 2D to avoid compile errors.
#define TEXTURE3D_SAMPLER3D(textureName, samplerName) sampler2D textureName
#define TEXTURE3D(textureName) sampler2D textureName
#define SAMPLER3D(samplerName)
#define TEXTURE3D_ARGS(textureName, samplerName) sampler2D textureName
#define TEXTURE3D_PARAM(textureName, samplerName) textureName
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3) tex2D(textureName, coord3)

#define UNITY_BRANCH
#define UNITY_FLATTEN
#define UNITY_UNROLL
#define UNITY_LOOP
#define UNITY_FASTOPT

#define CBUFFER_START(name)
#define CBUFFER_END

#define FXAA_HLSL_3 1
#define SMAA_HLSL_3 1

// pragma exclude_renderers is only supported since Unity 2018.1 for compute shaders
#if UNITY_VERSION < 201810 && !defined(SHADER_API_GLCORE)
#    define DISABLE_COMPUTE_SHADERS 1
#    define TRIVIAL_COMPUTE_KERNEL(name) [numthreads(1, 1, 1)] void name() {}
#endif
