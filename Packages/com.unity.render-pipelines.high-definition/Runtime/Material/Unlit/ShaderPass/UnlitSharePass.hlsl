#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

#define ATTRIBUTES_NEED_TEXCOORD0

#if defined(DEBUG_DISPLAY) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT)
// For the meta pass with emissive we require UV1 and/or UV2
#define ATTRIBUTES_NEED_TEXCOORD1
#define ATTRIBUTES_NEED_TEXCOORD2
#endif

#if defined(_ENABLE_FOG_ON_TRANSPARENT) || (SHADERPASS == SHADERPASS_VELOCITY)
#define VARYINGS_NEED_POSITION_WS
#endif

#define VARYINGS_NEED_TEXCOORD0

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
