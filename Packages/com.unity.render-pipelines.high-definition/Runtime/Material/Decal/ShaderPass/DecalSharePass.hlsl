#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_MESH)
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT // Always present as we require it also in case of anisotropic lighting
#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_TEXCOORD0
#endif

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
