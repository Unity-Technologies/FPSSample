//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Reference
//-----------------------------------------------------------------------------

void IntegrateBSDF_LineRef(float3 V, float3 positionWS,
                           PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                           out float3 diffuseLighting, out float3 specularLighting,
                           int sampleCount = 128)
{
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    const float  len = lightData.size.x;
    const float3 T   = lightData.right;
    const float3 P1  = lightData.positionRWS - T * (0.5 * len);
    const float  dt  = len * rcp(sampleCount);
    const float  off = 0.5 * dt;

    // Uniformly sample the line segment with the Pdf = 1 / len.
    const float invPdf = len;

    for (int i = 0; i < sampleCount; ++i)
    {
        // Place the sample in the middle of the interval.
        float  t     = off + i * dt;
        float3 sPos  = P1 + t * T;
        float3 unL   = sPos - positionWS;
        float  dist2 = dot(unL, unL);
        float3 L     = normalize(unL);
        float  sinLT = length(cross(L, T));
        float  NdotL = saturate(dot(bsdfData.normalWS, L));

        if (NdotL > 0)
        {
            float3 lightDiff, lightSpec;

            BSDF(V, L, NdotL, positionWS, preLightData, bsdfData, lightDiff, lightSpec);

            diffuseLighting  += lightDiff * (sinLT / dist2 * NdotL);
            specularLighting += lightSpec * (sinLT / dist2 * NdotL);
        }
    }

    // The factor of 2 is due to the fact: Integral{0, 2 PI}{max(0, cos(x))dx} = 2.
    float normFactor = 2.0 * invPdf * rcp(sampleCount);

    diffuseLighting  *= normFactor * lightData.diffuseDimmer  * lightData.color;
    specularLighting *= normFactor * lightData.specularDimmer * lightData.color;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Reference
//-----------------------------------------------------------------------------

void IntegrateBSDF_AreaRef(float3 V, float3 positionWS,
                           PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                           out float3 diffuseLighting, out float3 specularLighting,
                           uint sampleCount = 512)
{
    diffuseLighting = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float3 P = float3(0.0, 0.0, 0.0);   // Sample light point. Random point on the light shape in local space.
        float3 Ns = float3(0.0, 0.0, 0.0);  // Unit surface normal at P
        float lightPdf = 0.0;               // Pdf of the light sample

        float2 u = Hammersley2d(i, sampleCount);

        // Lights in Unity point backward.
        float4x4 localToWorld = float4x4(float4(lightData.right, 0.0), float4(lightData.up, 0.0), float4(-lightData.forward, 0.0), float4(lightData.positionRWS, 1.0));

        switch (lightData.lightType)
        {
            //case GPULIGHTTYPE_SPHERE:
            //    SampleSphere(u, localToWorld, lightData.size.x, lightPdf, P, Ns);
            //    break;
            //case GPULIGHTTYPE_HEMISPHERE:
            //    SampleHemisphere(u, localToWorld, lightData.size.x, lightPdf, P, Ns);
            //    break;
            //case GPULIGHTTYPE_CYLINDER:
            //    SampleCylinder(u, localToWorld, lightData.size.x, lightData.size.y, lightPdf, P, Ns);
            //    break;
            case GPULIGHTTYPE_RECTANGLE:
                SampleRectangle(u, localToWorld, lightData.size.x, lightData.size.y, lightPdf, P, Ns);
                break;
            //case GPULIGHTTYPE_DISK:
            //    SampleDisk(u, localToWorld, lightData.size.x, lightPdf, P, Ns);
            //   break;
            // case GPULIGHTTYPE_TUBE: handled by a separate function.
        }

        // Get distance
        float3 unL = P - positionWS;
        float sqrDist = dot(unL, unL);
        float3 L = normalize(unL);

        // Cosine of the angle between the light direction and the normal of the light's surface.
        float cosLNs = saturate(dot(-L, Ns));

        // We calculate area reference light with the area integral rather than the solid angle one.
        float NdotL = saturate(dot(bsdfData.normalWS, L));
        float illuminance = cosLNs * NdotL / (sqrDist * lightPdf);

        float3 localDiffuseLighting = float3(0.0, 0.0, 0.0);
        float3 localSpecularLighting = float3(0.0, 0.0, 0.0);

        if (illuminance > 0.0)
        {
            BSDF(V, L, NdotL, positionWS, preLightData, bsdfData, localDiffuseLighting, localSpecularLighting);
            localDiffuseLighting *= lightData.color * illuminance * lightData.diffuseDimmer;
            localSpecularLighting *= lightData.color * illuminance * lightData.specularDimmer;
        }

        diffuseLighting += localDiffuseLighting;
        specularLighting += localSpecularLighting;
    }

    diffuseLighting /= float(sampleCount);
    specularLighting /= float(sampleCount);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env - Reference
// ----------------------------------------------------------------------------

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateLambertIBLRef(LightLoopContext lightLoopContext,
                              float3 V, EnvLightData lightData, BSDFData bsdfData,
                              uint sampleCount = 4096)
{
    float3x3 localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
    float3   acc          = float3(0.0, 0.0, 0.0);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u = Hammersley2d(i, sampleCount);

        float3 L;
        float NdotL;
        float weightOverPdf;
        ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            acc += LambertNoPI() * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

float3 IntegrateDisneyDiffuseIBLRef(LightLoopContext lightLoopContext,
                                    float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    uint sampleCount = 4096)
{
    float3x3 localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
    float    NdotV        = ClampNdotV(dot(bsdfData.normalWS, V));
    float3   acc          = float3(0.0, 0.0, 0.0);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u = Hammersley2d(i, sampleCount);

        float3 L;
        float NdotL;
        float weightOverPdf;
        // for Disney we still use a Cosine importance sampling, true Disney importance sampling imply a look up table
        ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);

        if (NdotL > 0.0)
        {
            float LdotV = dot(L, V);
            // Note: we call DisneyDiffuse that require to multiply by Albedo / PI. Divide by PI is already taken into account
            // in weightOverPdf of ImportanceSampleLambert call.
            float disneyDiffuse = DisneyDiffuse(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness);

            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);
            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            acc += disneyDiffuse * weightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}

// Ref: Moving Frostbite to PBR (Appendix A)
float3 IntegrateSpecularGGXIBLRef(LightLoopContext lightLoopContext,
                                  float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                  uint sampleCount = 2048)
{
    float3x3 localToWorld;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
    }
    else
    {
        // We do not have a tangent frame unless we use anisotropic GGX.
        localToWorld = GetLocalFrame(bsdfData.normalWS);
    }

    float  NdotV = ClampNdotV(dot(bsdfData.normalWS, V));
    float3 acc   = float3(0.0, 0.0, 0.0);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u = Hammersley2d(i, sampleCount);

        float VdotH;
        float NdotL;
        float3 L;
        float weightOverPdf;

        // GGX BRDF
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
        {
            ImportanceSampleAnisoGGX(u, V, localToWorld, bsdfData.roughnessT, bsdfData.roughnessB, NdotV, L, VdotH, NdotL, weightOverPdf);
        }
        else
        {
            ImportanceSampleGGX(u, V, localToWorld, bsdfData.roughnessT, NdotV, L, VdotH, NdotL, weightOverPdf);
        }

        if (NdotL > 0.0)
        {
            // Fresnel component is apply here as describe in ImportanceSampleGGX function
            float3 FweightOverPdf = F_Schlick(bsdfData.fresnel0, VdotH) * weightOverPdf;

            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            acc += FweightOverPdf * val.rgb;
        }
    }

    return acc / sampleCount;
}
