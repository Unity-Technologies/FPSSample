#ifndef UNITY_LIGHTING_INCLUDED
#define UNITY_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonShadow.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AreaLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"

// The light loop (or lighting architecture) is in charge to:
// - Define light list
// - Define the light loop
// - Setup the constant/data
// - Do the reflection hierarchy
// - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

#define HAS_LIGHTLOOP // Allow to not define LightLoop related function in Material.hlsl

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/HDShadow.hlsl"

#if defined(LIGHTLOOP_SINGLE_PASS) || defined(LIGHTLOOP_TILE_PASS)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl" // Depends on LightLoopDef and shadows

// Volumetrics have their own light loop.
#ifndef UNITY_MATERIAL_VOLUMETRIC
    // LightLoop use evaluation BSDF function for light type define in Material.hlsl
    #if defined(LIGHTLOOP_SINGLE_PASS) || defined(LIGHTLOOP_TILE_PASS)
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
    #endif
#endif

#endif // UNITY_LIGHTING_INCLUDED
