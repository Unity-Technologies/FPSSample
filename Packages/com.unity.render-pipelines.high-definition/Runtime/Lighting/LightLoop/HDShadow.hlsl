#ifndef LIGHTLOOP_HD_SHADOW_HLSL
#define LIGHTLOOP_HD_SHADOW_HLSL

#define SHADOW_OPTIMIZE_REGISTER_USAGE 1

#ifndef SHADOW_USE_VIEW_BIAS_SCALING
#define SHADOW_USE_VIEW_BIAS_SCALING            1   // Enable view bias scaling to mitigate light leaking across edges. Uses the light vector if SHADOW_USE_ONLY_VIEW_BASED_BIASING is defined, otherwise uses the normal.
#endif
// Note: Sample biasing work well but is very costly in term of VGPR, disable it for now
#define SHADOW_USE_SAMPLE_BIASING               0   // Enable per sample biasing for wide multi-tap PCF filters. Incompatible with SHADOW_USE_ONLY_VIEW_BASED_BIASING.
#define SHADOW_USE_DEPTH_BIAS                   0   // Enable clip space z biasing

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
#   pragma warning( disable : 3557 ) // loop only executes for 1 iteration(s)
#endif

# include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDShadowContext.hlsl"

float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L)
{
    return EvalShadow_CascadedDepth_Blend(shadowContext, _ShadowmapCascadeAtlas, sampler_ShadowmapCascadeAtlas, positionWS, normalWS, shadowDataIndex, L);
}

float GetDirectionalShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 positionSS)
{
    return GetDirectionalShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L);
}

float GetPunctualShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, bool pointLight, bool perspecive)
{
    // Note: Here we assume that all the shadow map cube faces have been added contiguously in the buffer to retreive the shadow information
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];

    if (pointLight)
    {
        sd.rot0 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot0;
        sd.rot1 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot1;
        sd.rot2 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot2;
        sd.atlasOffset = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].atlasOffset;
    }

    return EvalShadow_PunctualDepth(sd, _ShadowmapAtlas, sampler_ShadowmapAtlas, positionWS, normalWS, L, L_dist, perspecive);
}

float GetPunctualShadowAttenuation(HDShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS, bool pointLight, bool perspecive)
{
    return GetPunctualShadowAttenuation(shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist, pointLight, perspecive);
}

float GetPunctualShadowClosestDistance(HDShadowContext shadowContext, SamplerState sampl, real3 positionWS, int shadowDataIndex, float3 L, float3 lightPositionWS, bool pointLight)
{
    // Note: Here we assume that all the shadow map cube faces have been added contiguously in the buffer to retreive the shadow information
    // TODO: if on the light type to retrieve the good shadow data
    HDShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
    
    if (pointLight)
    {
        sd.shadowToWorld = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].shadowToWorld;
        sd.atlasOffset = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].atlasOffset;
        sd.rot0 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot0;
        sd.rot1 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot1;
        sd.rot2 = shadowContext.shadowDatas[shadowDataIndex + CubeMapFaceID(-L)].rot2;
    }
    
    return EvalShadow_SampleClosestDistance_Punctual(sd, _ShadowmapAtlas, sampl, positionWS, L, lightPositionWS);
}

#endif // LIGHTLOOP_HD_SHADOW_HLSL
