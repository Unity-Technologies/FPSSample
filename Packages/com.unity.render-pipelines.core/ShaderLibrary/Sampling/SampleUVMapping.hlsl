// This structure abstract uv mapping inside one struct.
// It represent a mapping of any uv (with its associated tangent space for derivative if SurfaceGradient mode) - UVSet0 to 4, planar, triplanar

#include "../NormalSurfaceGradient.hlsl"

#define UV_MAPPING_UVSET 0
#define UV_MAPPING_PLANAR 1
#define UV_MAPPING_TRIPLANAR 2

struct UVMapping
{
    int mappingType;
    float2 uv;  // Current uv or planar uv

    // Triplanar specific
    float2 uvZY;
    float2 uvXZ;
    float2 uvXY;

    float3 normalWS; // vertex normal
    float3 triplanarWeights;

#ifdef SURFACE_GRADIENT
    // tangent basis to use when mappingType is UV_MAPPING_UVSET
    // these are vertex level in world space
    float3 tangentWS;
    float3 bitangentWS;
    // TODO: store also object normal map for object triplanar
#endif
};

// Multiple includes of the file to handle all variations of textures sampling for regular, lod and bias

// Regular sampling functions
#define ADD_FUNC_SUFFIX(Name) Name
#define SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping, unused) SAMPLE_TEXTURE2D(textureName, samplerName, uvMapping)
#include "SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Lod sampling functions
#define ADD_FUNC_SUFFIX(Name) Name##Lod
#define SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping, lod) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, uvMapping, lod)
#include "SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Bias sampling functions
#define ADD_FUNC_SUFFIX(Name) Name##Bias
#define SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping, bias) SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, uvMapping, bias)
#include "SampleUVMappingInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Macro to improve readibility of surface data
#define SAMPLE_UVMAPPING_TEXTURE2D(textureName, samplerName, uvMapping)             SampleUVMapping(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, 0.0) // Last 0.0 is unused
#define SAMPLE_UVMAPPING_TEXTURE2D_LOD(textureName, samplerName, uvMapping, lod)    SampleUVMappingLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, lod)
#define SAMPLE_UVMAPPING_TEXTURE2D_BIAS(textureName, samplerName, uvMapping, bias)  SampleUVMappingBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, bias)

#define SAMPLE_UVMAPPING_NORMALMAP(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormal(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, 0.0)
#define SAMPLE_UVMAPPING_NORMALMAP_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, bias)

#define SAMPLE_UVMAPPING_NORMALMAP_AG(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormalAG(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, 0.0)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalAGLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_AG_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalAGBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, bias)

#define SAMPLE_UVMAPPING_NORMALMAP_RGB(textureName, samplerName, uvMapping, scale)              SampleUVMappingNormalRGB(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, 0.0)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_LOD(textureName, samplerName, uvMapping, scale, lod)     SampleUVMappingNormalRGBLod(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, lod)
#define SAMPLE_UVMAPPING_NORMALMAP_RGB_BIAS(textureName, samplerName, uvMapping, scale, bias)   SampleUVMappingNormalRGBBias(TEXTURE2D_PARAM(textureName, samplerName), uvMapping, scale, bias)
