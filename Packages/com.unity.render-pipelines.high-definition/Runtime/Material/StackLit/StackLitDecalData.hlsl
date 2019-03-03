void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
        surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
        surfaceData.normalWS.xyz = normalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

    // TODOTODO: _MATERIAL_FEATURE_SPECULAR_COLOR and _MATERIAL_FEATURE_HAZY_GLOSS

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_MASK)
    {
#ifdef DECALS_4RT // only smoothness in 3RT mode
        surfaceData.metallic = surfaceData.metallic * decalSurfaceData.MAOSBlend.x + decalSurfaceData.mask.x;
        surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif
        surfaceData.perceptualSmoothnessA = surfaceData.perceptualSmoothnessA * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
        surfaceData.perceptualSmoothnessB = surfaceData.perceptualSmoothnessB * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
        surfaceData.coatPerceptualSmoothness = surfaceData.coatPerceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
    }
}
