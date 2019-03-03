// Continuation of LightEvaluation.hlsl.

// 15 degrees
#define TRANSMISSION_WRAP_ANGLE (PI/12)
#define TRANSMISSION_WRAP_LIGHT cos(PI/2 - TRANSMISSION_WRAP_ANGLE)

//-----------------------------------------------------------------------------
// Directional lights
//-----------------------------------------------------------------------------

float3 ComputeSunLightDirection(DirectionalLightData lightData, float3 N, float3 V)
{
    float3 L = -lightData.forward;
    float3 R = reflect(-V, N); // Not always the same as preLightData.iblR

    // Fake a highlight of the sun disk by modifying the light vector.
    float t = AngleAttenuation(dot(L, R), lightData.angleScale, lightData.angleOffset);

    // This will be quite inaccurate for large disk radii. Would be better to use SLerp().
    L = NLerp(L, R, t);

    return L;
}

// This function returns transmittance to provide to EvaluateTransmission
float3 PreEvaluateDirectionalLightTransmission(BSDFData bsdfData, inout DirectionalLightData light,
                                               inout float3 N, inout float NdotL)
{
    float3 transmittance = 0.0;

#ifdef MATERIAL_INCLUDE_TRANSMISSION
    if (MaterialSupportsTransmission(bsdfData))
    {
        // We support some kind of transmission.
        if (NdotL <= 0)
        {
            // And since the light is back-facing, it's active.
            if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS))
            {
                // Care must be taken to bias in the direction of the light.
                // TODO: change the sign of the bias: faster & uses fewer VGPRs.
                N = -N;

                // We want to evaluate cookies and light attenuation, so we flip NdotL.
                NdotL = -NdotL;

                // However, we don't want baked or contact shadows.
                light.contactShadowIndex   = -1;
                light.shadowMaskSelector.x = -1;

                // We use the precomputed value (based on "baked" thickness).
                transmittance = bsdfData.transmittance;
            }
            else
            {
                // The mixed thickness mode is not supported by directional lights
                // due to poor quality and high performance impact.
                // Keeping NdotL negative will ensure that nothing is evaluated.
            }
        }
    }
#endif

    return transmittance;
}

DirectLighting ShadeSurface_Directional(LightLoopContext lightLoopContext,
                                        PositionInputs posInput, BuiltinData builtinData,
                                        PreLightData preLightData, DirectionalLightData light,
                                        BSDFData bsdfData, float3 N, float3 V)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L     = ComputeSunLightDirection(light, N, V);
    float  NdotL = dot(N, L); // Do not saturate

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are OK with this
    bool surfaceReflection = NdotL > 0;

    // Caution: this function modifies N, NdotL, contactShadowIndex and shadowMaskSelector.
    float3 transmittance = PreEvaluateDirectionalLightTransmission(bsdfData, light, N, NdotL);

    float3 color; float attenuation;
    EvaluateLight_Directional(lightLoopContext, posInput, light, builtinData, N, L, NdotL,
                              color, attenuation);

    // TODO: transmittance contributes to attenuation, how can we use it for early-out?
    if (attenuation > 0)
    {
        // We must clamp here, otherwise our disk light hack for smooth surfaces does not work.
        // Explanation: for a perfectly smooth surface, lighting is only reflected if (NdotL = NdotV).
        // This implies that (NdotH = 1).
        // Due to the floating point arithmetic (see math in ComputeSunLightDirection() and
        // GetBSDFAngle()), we will never arrive at this exact number, so no lighting will be reflected.
        // If we increase the roughness somewhat, the trick still works.
        ClampRoughness(bsdfData, light.minRoughness);

        float3 diffuseBsdf, specularBsdf;
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, diffuseBsdf, specularBsdf);

        if (surfaceReflection)
        {
            attenuation    *= ComputeMicroShadowing(bsdfData, NdotL);
            float intensity = attenuation * NdotL;

            lighting.diffuse  = diffuseBsdf  * (intensity * light.diffuseDimmer);
            lighting.specular = specularBsdf * (intensity * light.specularDimmer);
        }
        else if (MaterialSupportsTransmission(bsdfData))
        {
             // Apply wrapped lighting to better handle thin objects at grazing angles.
            float wrapNdotL = ComputeWrappedDiffuseLighting(NdotL, TRANSMISSION_WRAP_LIGHT);
            float intensity = attenuation * wrapNdotL;

            // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
            // Note: Disney's LdoV term in 'diffuseBsdf' does not hold a meaningful value
            // in the context of transmission, but we keep it unaltered for performance reasons.
            lighting.diffuse  = transmittance * (diffuseBsdf * (intensity * light.diffuseDimmer));
            lighting.specular = 0; // No spec trans, the compiler should optimize
        }

        // Save ALU by applying light and cookie colors only once.
        lighting.diffuse  *= color;
        lighting.specular *= color;
    }

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * attenuation * saturate(NdotL);
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// Punctual lights
//-----------------------------------------------------------------------------

// This function return transmittance to provide to EvaluateTransmission
float3 PreEvaluatePunctualLightTransmission(LightLoopContext lightLoopContext,
                                            PositionInputs posInput, BSDFData bsdfData,
                                            inout LightData light, float distFrontFaceToLight,
                                            inout float3 N, float3 L, inout float NdotL)
{
    float3 transmittance = 0;

#ifdef MATERIAL_INCLUDE_TRANSMISSION
    if (MaterialSupportsTransmission(bsdfData))
    {
        // We support some kind of transmission.
        if (NdotL <= 0)
        {
            // And since the light is back-facing, it's active.
            // Care must be taken to bias in the direction of the light.
            // TODO: change the sign of the bias: faster & uses fewer VGPRs.
            N = -N;

            // We want to evaluate cookies and light attenuation, so we flip NdotL.
            NdotL = -NdotL;

            // However, we don't want baked or contact shadows.
            light.contactShadowIndex   = -1;
            light.shadowMaskSelector.x = -1;

            transmittance = bsdfData.transmittance;

            if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS) && (light.shadowIndex >= 0))
            {
                // We can compute thickness from shadow.
                // Compute the distance from the light to the back face of the object along the light direction.
                // TODO: SHADOW BIAS.
                float distBackFaceToLight = GetPunctualShadowClosestDistance(lightLoopContext.shadowContext, s_linear_clamp_sampler,
                                                                             posInput.positionWS, light.shadowIndex, L, light.positionRWS,
                                                                             light.lightType == GPULIGHTTYPE_POINT);

                // Our subsurface scattering models use the semi-infinite planar slab assumption.
                // Therefore, we need to find the thickness along the normal.
                // Warning: based on the artist's input, dependence on the NdotL has been disabled.
                float thicknessInUnits       = (distFrontFaceToLight - distBackFaceToLight) /* * -NdotL */;
                float thicknessInMeters      = thicknessInUnits * _WorldScales[bsdfData.diffusionProfile].x;
                float thicknessInMillimeters = thicknessInMeters * MILLIMETERS_PER_METER;

                // We need to make sure it's not less than the baked thickness to minimize light leaking.
                float thicknessDelta = max(0, thicknessInMillimeters - bsdfData.thickness);

                float3 S = _ShapeParams[bsdfData.diffusionProfile].rgb;

            #if 0
                float3 expOneThird = exp(((-1.0 / 3.0) * thicknessDelta) * S);
            #else
                // Help the compiler. S is premultiplied by ((-1.0 / 3.0) * LOG2_E) on the CPU.
                float3 p = thicknessDelta * S;
                float3 expOneThird = exp2(p);
            #endif

                // Approximate the decrease of transmittance by e^(-1/3 * dt * S).
                transmittance *= expOneThird;

                // Avoid double shadowing. TODO: is there a faster option?
                light.shadowIndex = -1;

                // Note: we do not modify the distance to the light, or the light angle for the back face.
                // This is a performance-saving optimization which makes sense as long as the thickness is small.
            }
        }
    }
#endif

    return transmittance;
}

DirectLighting ShadeSurface_Punctual(LightLoopContext lightLoopContext,
                                     PositionInputs posInput, BuiltinData builtinData,
                                     PreLightData preLightData, LightData light,
                                     BSDFData bsdfData, float3 N, float3 V)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 L;
    float3 lightToSample;
    float4 distances; // {d, d^2, 1/d, d_proj}
    GetPunctualLightVectors(posInput.positionWS, light, L, lightToSample, distances);

    float NdotL = dot(N, L); // Do not saturate

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are OK with this
    bool surfaceReflection = NdotL > 0;

    // Caution: this function modifies N, NdotL, shadowIndex, contactShadowIndex and shadowMaskSelector.
    float3 transmittance = PreEvaluatePunctualLightTransmission(lightLoopContext, posInput, bsdfData,
                                                                light, distances.x, N, L, NdotL);
    float3 color; float attenuation;
    EvaluateLight_Punctual(lightLoopContext, posInput, light, builtinData, N, L, NdotL, lightToSample, distances,
                           color, attenuation);

    // TODO: transmittance contributes to attenuation, how can we use it for early-out?
    if (attenuation > 0)
    {
        // Simulate a sphere/disk light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway
        ClampRoughness(bsdfData, light.minRoughness);

        float3 diffuseBsdf, specularBsdf;
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, diffuseBsdf, specularBsdf);

        if (surfaceReflection)
        {
            float intensity = attenuation * NdotL;

            lighting.diffuse  = diffuseBsdf  * (intensity * light.diffuseDimmer);
            lighting.specular = specularBsdf * (intensity * light.specularDimmer);
        }
        else if (MaterialSupportsTransmission(bsdfData))
        {
             // Apply wrapped lighting to better handle thin objects at grazing angles.
            float wrapNdotL = ComputeWrappedDiffuseLighting(NdotL, TRANSMISSION_WRAP_LIGHT);
            float intensity = attenuation * wrapNdotL;

            // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
            // Note: Disney's LdoV term in 'diffuseBsdf' does not hold a meaningful value
            // in the context of transmission, but we keep it unaltered for performance reasons.
            lighting.diffuse  = transmittance * (diffuseBsdf * (intensity * light.diffuseDimmer));
            lighting.specular = 0; // No spec trans, the compiler should optimize
        }

        // Save ALU by applying light and cookie colors only once.
        lighting.diffuse  *= color;
        lighting.specular *= color;
    }

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * attenuation * saturate(NdotL);
    }
#endif

    return lighting;
}
