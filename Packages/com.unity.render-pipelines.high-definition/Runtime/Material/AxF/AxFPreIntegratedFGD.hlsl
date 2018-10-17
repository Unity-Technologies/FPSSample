TEXTURE2D(_PreIntegratedFGD_Ward);

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular Ward and Lambert
// reflectivity is  Integral{(BSDF_GGX / F) - used for multiscattering
void GetPreIntegratedFGDWardAndLambert(float NdotV, float perceptualRoughness, float3 F0, out float3 specularFGD, out float diffuseFGD, out float reflectivity)
{
    float2 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_Ward, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xy;

    specularFGD = lerp(preFGD.xxx, preFGD.yyy, F0);
    diffuseFGD = 1.0;

    reflectivity = preFGD.y;
}

TEXTURE2D(_PreIntegratedFGD_CookTorrance);

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular Ward and Lambert
// reflectivity is  Integral{(BSDF_GGX / F) - used for multiscattering
void GetPreIntegratedFGDCookTorranceAndLambert(float NdotV, float perceptualRoughness, float3 F0, out float3 specularFGD, out float diffuseFGD, out float reflectivity)
{
    float2 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_CookTorrance, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xy;

    specularFGD = lerp(preFGD.xxx, preFGD.yyy, F0);
    diffuseFGD = 1.0;

    reflectivity = preFGD.y;
}
