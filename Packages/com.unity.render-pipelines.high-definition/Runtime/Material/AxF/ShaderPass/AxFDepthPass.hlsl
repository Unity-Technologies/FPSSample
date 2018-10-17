#ifndef SHADERPASS
#error Undefine_SHADERPASS
#endif

// NEWLITTODO : Handling of TESSELATION, DISPLACEMENT, WIND

#ifdef _ALPHATEST_ON
#define ATTRIBUTES_NEED_TEXCOORD0

#define VARYINGS_NEED_TEXCOORD0
#endif //..._ALPHATEST_ON

// This include will define the various Attributes/Varyings structure
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VaryingMesh.hlsl"
