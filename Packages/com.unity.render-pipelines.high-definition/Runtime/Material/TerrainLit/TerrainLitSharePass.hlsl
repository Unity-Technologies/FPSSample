#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// This first set of define allow to say which attributes will be use by the mesh in the vertex and domain shader (for tesselation)

// Attributes
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TANGENT // will be filled by ApplyMeshModification()

#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
    #define ATTRIBUTES_NEED_TEXCOORD1
    #define ATTRIBUTES_NEED_TEXCOORD2
#endif

// Varying - Use for pixel shader
// This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_TEXCOORD0

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
    #define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    // With per-pixel normal enabled, tangent space is created in the pixel shader.
    #undef ATTRIBUTES_NEED_NORMAL
    #undef ATTRIBUTES_NEED_TANGENT
    #undef VARYINGS_NEED_TANGENT_TO_WORLD
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
