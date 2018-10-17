#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// NOTE: Copied from LitSharePass.hlsl, we will need most of this, but at first, vs the unlit, we have 
// diffuse lighting, and also consider the _DOUBLESIDED_ON option.

// This first set of define allow to say which attributes will be use by the mesh in the vertex and domain shader (for tesselation)

// Attributes
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT // Always present as we require it also in case of anisotropic lighting
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_COLOR

#if defined(_REQUIRE_UV2) || defined(_REQUIRE_UV3) || defined(DYNAMICLIGHTMAP_ON) || defined(DEBUG_DISPLAY) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
#define ATTRIBUTES_NEED_TEXCOORD2
#endif
#if defined(_REQUIRE_UV3) || defined(DEBUG_DISPLAY)
#define ATTRIBUTES_NEED_TEXCOORD3
#endif

// Varying - Use for pixel shader
// This second set of define allow to say which varyings will be output in the vertex (no more tesselation)
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
#ifdef ATTRIBUTES_NEED_TEXCOORD2
#define VARYINGS_NEED_TEXCOORD2
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
#define VARYINGS_NEED_TEXCOORD3
#endif
#define VARYINGS_NEED_COLOR

#ifdef _DOUBLESIDED_ON
#define VARYINGS_NEED_CULLFACE
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
