#ifndef UNITY_SCREENSPACELIGHTING_INCLUDED
 #define UNITY_SCREENSPACELIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"

#define SSRTID Reflection
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#undef SSRTID

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Refraction.hlsl"
#define SSRTID Refraction
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#undef SSRTID

#endif
