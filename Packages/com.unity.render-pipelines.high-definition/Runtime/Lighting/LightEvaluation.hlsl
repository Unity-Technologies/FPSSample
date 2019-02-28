// This files include various function uses to evaluate lights

//-----------------------------------------------------------------------------
// Directional Light evaluation helper
//-----------------------------------------------------------------------------

#ifndef OVERRIDE_EVALUATE_COOKIE_DIRECTIONAL
float3 EvaluateCookie_Directional(LightLoopContext lightLoopContext, DirectionalLightData light,
                                  float3 lightToSample)
{

    // Translate and rotate 'positionWS' into the light space.
    // 'light.right' and 'light.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
    float3   positionLS   = mul(lightToSample, transpose(lightToWorld));

    // Perform orthographic projection.
    float2 positionCS  = positionLS.xy;

    // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
    float2 positionNDC = positionCS * 0.5 + 0.5;

    // We let the sampler handle clamping to border.
    return SampleCookie2D(lightLoopContext, positionNDC, light.cookieIndex, light.tileCookie);
}
#endif

// Does not account for precomputed (screen-space or baked) shadows.
float EvaluateRuntimeSunShadow(LightLoopContext lightLoopContext, PositionInputs posInput,
                               DirectionalLightData light, float3 shadowBiasNormal)
{
    // The relationship with NdotL is complicated and is therefore handled outside the function.
    if ((light.lightDimmer > 0) && (light.shadowDimmer > 0))
    {
        // Shadow dimmer is applied outside this function.
        return GetDirectionalShadowAttenuation(lightLoopContext.shadowContext, posInput.positionWS,
                                               shadowBiasNormal, light.shadowIndex, -light.forward,
                                               posInput.positionSS);
    }
    else
    {
        return 1;
    }
}

// None of the outputs are premultiplied.
void EvaluateLight_Directional(LightLoopContext lightLoopContext, PositionInputs posInput,
                               DirectionalLightData light, BuiltinData builtinData,
                               float3 N, float3 L, float NdotL,
                               out float3 color, out float attenuation)
{
    color = attenuation = 0;
    if ((light.lightDimmer <= 0) || (NdotL <= 0)) return;

    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = light.color;
    attenuation = 1.0;

    // Height fog attenuation.
    {
        float cosZenithAngle = L.y;
        float fragmentHeight = posInput.positionWS.y;
        attenuation *= TransmittanceHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                              _HeightFogExponents, cosZenithAngle, fragmentHeight);
    }

    if (light.cookieIndex >= 0)
    {
        float3 lightToSample = positionWS - light.positionRWS;
        float3 cookie = EvaluateCookie_Directional(lightLoopContext, light, lightToSample);

        color *= cookie;
    }

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (light.shadowMaskSelector.x >= 0.0) ? dot(BUILTIN_DATA_SHADOW_MASK, light.shadowMaskSelector) : 1.0;
#endif

    if ((light.shadowIndex >= 0) && (light.shadowDimmer > 0))
    {
        shadow = lightLoopContext.shadowValue;

    #ifdef SHADOWS_SHADOWMASK
        // TODO: Optimize this code! Currently it is a bit like brute force to get the last transistion and fade to shadow mask, but there is
        // certainly more efficient to do
        // We reuse the transition from the cascade system to fade between shadow mask at max distance
        uint  payloadOffset;
        real  fade;
        int cascadeCount;
        int shadowSplitIndex = 0;

        shadowSplitIndex = EvalShadow_GetSplitIndex(lightLoopContext.shadowContext, light.shadowIndex, positionWS, fade, cascadeCount);

        // we have a fade caclulation for each cascade but we must lerp with shadow mask only for the last one
        // if shadowSplitIndex is -1 it mean we are outside cascade and should return 1.0 to use shadowmask: saturate(-shadowSplitIndex) return 0 for >= 0 and 1 for -1
        fade = ((shadowSplitIndex + 1) == cascadeCount) ? fade : saturate(-shadowSplitIndex);

        // In the transition code (both dithering and blend) we use shadow = lerp( shadow, 1.0, fade ) for last transition
        // mean if we expend the code we have (shadow * (1 - fade) + fade). Here to make transition with shadow mask
        // we will remove fade and add fade * shadowMask which mean we do a lerp with shadow mask
        shadow = shadow - fade + fade * shadowMask;

        // See comment in EvaluateBSDF_Punctual
        shadow = light.nonLightMappedOnly ? min(shadowMask, shadow) : shadow;
    #endif

        shadow = lerp(shadowMask, shadow, light.shadowDimmer);
    }

    // Transparents have no contact shadow information
#ifndef _SURFACE_TYPE_TRANSPARENT
    shadow = min(shadow, GetContactShadow(lightLoopContext, light.contactShadowIndex));
#endif

#ifdef DEBUG_DISPLAY
    if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW && light.shadowIndex == _DebugSingleShadowIndex)
        debugShadowAttenuation = shadow;
#endif

    attenuation *= shadow;
}

//-----------------------------------------------------------------------------
// Punctual Light evaluation helper
//-----------------------------------------------------------------------------

// distances = {d, d^2, 1/d, d_proj}
void ModifyDistancesForFillLighting(inout float4 distances, float lightSqRadius)
{
    // Apply the sphere light hack to soften the core of the punctual light.
    // It is not physically plausible (using max() is more correct, but looks worse).
    // See https://www.desmos.com/calculator/otqhxunqhl
    // We only modify 1/d for performance reasons.
    float sqDist = distances.y;
    distances.z = rsqrt(sqDist + lightSqRadius); // Recompute 1/d
}

// Return L vector for punctual light (normalize surface to light), lightToSample (light to surface non normalize) and
// distances = {d, d^2, 1/d, d_proj}
void GetPunctualLightVectors(float3 positionWS, LightData light, out float3 L, out float3 lightToSample, out float4 distances)
{
    lightToSample = positionWS - light.positionRWS;
    int lightType = light.lightType;

    distances.w = dot(lightToSample, light.forward);

    if (lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        L = -light.forward;
        distances.xyz = 1; // No distance or angle attenuation
    }
    else
    {
        float3 unL     = -lightToSample;
        float  distSq  = dot(unL, unL);
        float  distRcp = rsqrt(distSq);
        float  dist    = distSq * distRcp;

        L = unL * distRcp;
        distances.xyz = float3(dist, distSq, distRcp);

        ModifyDistancesForFillLighting(distances, light.size.x);
    }
}

#ifndef OVERRIDE_EVALUATE_COOKIE_PUNCTUAL
float4 EvaluateCookie_Punctual(LightLoopContext lightLoopContext, LightData light,
                               float3 lightToSample)
{
    int lightType = light.lightType;

    // Translate and rotate 'positionWS' into the light space.
    // 'light.right' and 'light.up' are pre-scaled on CPU.
    float3x3 lightToWorld = float3x3(light.right, light.up, light.forward);
    float3   positionLS   = mul(lightToSample, transpose(lightToWorld));

    float4 cookie;

    UNITY_BRANCH if (lightType == GPULIGHTTYPE_POINT)
    {
        cookie.rgb = SampleCookieCube(lightLoopContext, positionLS, light.cookieIndex);
        cookie.a   = 1;
    }
    else
    {
        // Perform orthographic or perspective projection.
        float  perspectiveZ = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? positionLS.z : 1.0;
        float2 positionCS   = positionLS.xy / perspectiveZ;
        bool   isInBounds   = Max3(abs(positionCS.x), abs(positionCS.y), 1.0 - positionLS.z) <= 1.0;

        // Remap the texture coordinates from [-1, 1]^2 to [0, 1]^2.
        float2 positionNDC = positionCS * 0.5 + 0.5;

        // Manually clamp to border (black).
        cookie.rgb = SampleCookie2D(lightLoopContext, positionNDC, light.cookieIndex, false);
        cookie.a   = isInBounds ? 1 : 0;
    }

    return cookie;
}
#endif

// None of the outputs are premultiplied.
// distances = {d, d^2, 1/d, d_proj}, where d_proj = dot(lightToSample, light.forward).
// Note: When doing transmission we always have only one shadow sample to do: Either front or back. We use NdotL to know on which side we are
void EvaluateLight_Punctual(LightLoopContext lightLoopContext, PositionInputs posInput,
                            LightData light, BuiltinData builtinData,
                            float3 N, float3 L, float NdotL, float3 lightToSample, float4 distances,
                            out float3 color, out float attenuation)
{
    color = attenuation = 0;
    if ((light.lightDimmer <= 0) || (NdotL <= 0)) return;

    float3 positionWS = posInput.positionWS;
    float  shadow     = 1.0;
    float  shadowMask = 1.0;

    color       = light.color;
    attenuation = PunctualLightAttenuation(distances, light.rangeAttenuationScale, light.rangeAttenuationBias,
                                           light.angleScale, light.angleOffset);

    // Height fog attenuation.
    {
        float cosZenithAngle = L.y;
        float distToLight    = (light.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ? distances.w : distances.x;
        float fragmentHeight = posInput.positionWS.y;
        attenuation *= TransmittanceHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                              _HeightFogExponents, cosZenithAngle,
                                              fragmentHeight, distToLight);
    }

    // Projector lights always have cookies, so we can perform clipping inside the if().
    if (light.cookieIndex >= 0)
    {
        float4 cookie = EvaluateCookie_Punctual(lightLoopContext, light, lightToSample);

        color       *= cookie.rgb;
        attenuation *= cookie.a;
    }

#ifdef SHADOWS_SHADOWMASK
    // shadowMaskSelector.x is -1 if there is no shadow mask
    // Note that we override shadow value (in case we don't have any dynamic shadow)
    shadow = shadowMask = (light.shadowMaskSelector.x >= 0.0) ? dot(BUILTIN_DATA_SHADOW_MASK, light.shadowMaskSelector) : 1.0;
#endif

    if ((light.shadowIndex >= 0) && (light.shadowDimmer > 0))
    {
        shadow = GetPunctualShadowAttenuation(lightLoopContext.shadowContext, posInput.positionSS, positionWS, N, light.shadowIndex, L, distances.x, light.lightType == GPULIGHTTYPE_POINT, light.lightType != GPULIGHTTYPE_PROJECTOR_BOX);

#ifdef SHADOWS_SHADOWMASK
        // Note: Legacy Unity have two shadow mask mode. ShadowMask (ShadowMask contain static objects shadow and ShadowMap contain only dynamic objects shadow, final result is the minimun of both value)
        // and ShadowMask_Distance (ShadowMask contain static objects shadow and ShadowMap contain everything and is blend with ShadowMask based on distance (Global distance setup in QualitySettigns)).
        // HDRenderPipeline change this behavior. Only ShadowMask mode is supported but we support both blend with distance AND minimun of both value. Distance is control by light.
        // The following code do this.
        // The min handle the case of having only dynamic objects in the ShadowMap
        // The second case for blend with distance is handled with ShadowDimmer. ShadowDimmer is define manually and by shadowDistance by light.
        // With distance, ShadowDimmer become one and only the ShadowMask appear, we get the blend with distance behavior.
        shadow = light.nonLightMappedOnly ? min(shadowMask, shadow) : shadow;
    #endif

        shadow = lerp(shadowMask, shadow, light.shadowDimmer);
    }

    // Transparents have no contact shadow information
#ifndef _SURFACE_TYPE_TRANSPARENT
    shadow = min(shadow, GetContactShadow(lightLoopContext, light.contactShadowIndex));
#endif

#ifdef DEBUG_DISPLAY
    if (_DebugShadowMapMode == SHADOWMAPDEBUGMODE_SINGLE_SHADOW && light.shadowIndex == _DebugSingleShadowIndex)
        debugShadowAttenuation = step(FLT_EPS, attenuation) * shadow;
#endif

    attenuation *= shadow;
}

#ifndef OVERRIDE_EVALUATE_ENV_INTERSECTION
// Environment map share function
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"

void EvaluateLight_EnvIntersection(float3 positionWS, float3 normalWS, EnvLightData light, int influenceShapeType, inout float3 R, inout float weight)
{
    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3x3 worldToIS = WorldToInfluenceSpace(light); // IS: Influence space
    float3 positionIS = WorldToInfluencePosition(light, worldToIS, positionWS);
    float3 dirIS = normalize(mul(R, worldToIS));

    float3x3 worldToPS = WorldToProxySpace(light); // PS: Proxy space
    float3 positionPS = WorldToProxyPosition(light, worldToPS, positionWS);
    float3 dirPS = mul(R, worldToPS);

    float projectionDistance = 0;

    // Process the projection
    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        projectionDistance = IntersectSphereProxy(light, dirPS, positionPS);
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in light.capturePositionRWS
        R = (positionWS + projectionDistance * R) - light.capturePositionRWS;

        weight = InfluenceSphereWeight(light, normalWS, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        projectionDistance = IntersectBoxProxy(light, dirPS, positionPS);
        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in light.capturePositionRWS
        R = (positionWS + projectionDistance * R) - light.capturePositionRWS;

        weight = InfluenceBoxWeight(light, normalWS, positionWS, positionIS, dirIS);
    }

    // Smooth weighting
    weight = Smoothstep01(weight);
    weight *= light.weight;
}
#endif