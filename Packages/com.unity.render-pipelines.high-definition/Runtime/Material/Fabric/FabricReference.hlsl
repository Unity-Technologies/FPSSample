//-----------------------------------------------------------------------------
// EvaluateBSDF_Env - Reference
// ----------------------------------------------------------------------------

float3 IntegrateSpecularCottonWoolIBLRef(LightLoopContext lightLoopContext,
                                  float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                  uint sampleCount = 2048)
{
    // In case of the cotton wool, the material is not anisotropic so we don't need a specific local frame
    float3x3 localToWorld = GetLocalFrame(bsdfData.normalWS);
    float    NdotV        = ClampNdotV(dot(bsdfData.normalWS, V));
    float3 acc   = float3(0.0, 0.0, 0.0);
   
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum);

        float3 localL = SampleHemisphereCosine(u.x, u.y);
        float3 L = mul(localL, localToWorld);
        float NdotL = saturate(dot(bsdfData.normalWS, L));

        if (NdotL > 0.0)
        {
            float LdotV, NdotH, LdotH, NdotV, invLenLV;
            GetBSDFAngle(V, L, NdotL, preLightData.NdotV, LdotV, NdotH, LdotH, NdotV, invLenLV);

            // Incident Light intensity
            float4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 0);

            // BRDF Data
            float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
            float D = D_Charlie(NdotH, bsdfData.roughnessT);
            float Vis = V_Charlie(NdotL, NdotV, bsdfData.roughnessT);

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            acc += F * D * Vis * val.rgb;
        }
    }
    return acc / sampleCount;
}

float3 IntegrateSpecularSilkIBLRef(LightLoopContext lightLoopContext,
                                  float3 V, PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                  uint sampleCount = 2048)
{
    // Given that it may be anisotropic we need to compute the tangent oriented basis
    float3x3 localToWorld = float3x3(bsdfData.tangentWS, bsdfData.bitangentWS, bsdfData.normalWS);
    float    NdotV        = ClampNdotV(dot(bsdfData.normalWS, V));
    float3 acc   = float3(0.0, 0.0, 0.0);
   
    // Add some jittering on Hammersley2d
    float2 randNum  = InitRandom(V.xy * 0.5 + 0.5);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float2 u    = Hammersley2d(i, sampleCount);
        u           = frac(u + randNum);

        float VdotH;
        float NdotL;
        float3 L;
        float weightOverPdf;
        ImportanceSampleAnisoGGX(u, V, localToWorld, bsdfData.roughnessT, bsdfData.roughnessB, NdotV, L, VdotH, NdotL, weightOverPdf);
        
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