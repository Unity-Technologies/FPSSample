//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"

#include "TerrainLitSplatCommon.hlsl"

// We don't use emission for terrain
#define _EmissiveColor float3(0,0,0)
#define _AlbedoAffectEmissive 0
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitBuiltinData.hlsl"
#undef _EmissiveColor
#undef _AlbedoAffectEmissive

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"

void GetSurfaceAndBuiltinData(inout FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
#ifdef ENABLE_TERRAIN_PERPIXEL_NORMAL
    {
        float3 normalOS = SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_Control0, (input.texCoord0.xy + 0.5f) * _TerrainHeightmapRecipSize.xy).rgb * 2 - 1;
        float3 normalWS = mul((float3x3)GetObjectToWorldMatrix(), normalOS);
        float3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, normalWS);
        float renormFactor = 1.0 / length(normalWS);

        // bitangent on the fly option in xnormal to reduce vertex shader outputs.
        // this is the mikktspace transformation (must use unnormalized attributes)
        float3x3 worldToTangent = CreateWorldToTangent(normalWS, tangentWS.xyz, 1);

        // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
        // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
        input.worldToTangent[0] = worldToTangent[0] * renormFactor;
        input.worldToTangent[1] = worldToTangent[1] * renormFactor;
        input.worldToTangent[2] = worldToTangent[2] * renormFactor;		// normalizes the interpolated vertex normal

        input.texCoord0.xy *= _TerrainHeightmapRecipSize.zw;
    }
#endif

    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;

    float3 normalTS;
    TerrainSplatBlend(input.texCoord0.xy, input.worldToTangent[0], input.worldToTangent[1],
        surfaceData.baseColor, normalTS, surfaceData.perceptualSmoothness, surfaceData.metallic, surfaceData.ambientOcclusion);

    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = 0;
    surfaceData.thickness = 1;
    surfaceData.diffusionProfile = 0;

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    GetNormalWS(input, normalTS, surfaceData.normalWS);
    float3 bentNormalWS = surfaceData.normalWS;

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
#ifdef _MASKMAP
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.specularOcclusion = 1.0;
#endif

#if HAVE_DECALS
    if (_EnableDecals)
    {
        float alpha = 1.0; // unused
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, alpha);
        ApplyDecalToSurfaceData(decalSurfaceData, surfaceData);
    }
#endif

#ifdef DEBUG_DISPLAY
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_CONTROL)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy, _Control0, _Control0_TexelSize, _Control0_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER0)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat0_ST.xy + _Splat0_ST.zw, _Splat0, _Splat0_TexelSize, _Splat0_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER1)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat1_ST.xy + _Splat1_ST.zw, _Splat1, _Splat1_TexelSize, _Splat1_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER2)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat2_ST.xy + _Splat2_ST.zw, _Splat2, _Splat2_TexelSize, _Splat2_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER3)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat3_ST.xy + _Splat3_ST.zw, _Splat3, _Splat3_TexelSize, _Splat3_MipInfo, surfaceData.baseColor);
    #ifdef _TERRAIN_8_LAYERS
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER4)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat4_ST.xy + _Splat4_ST.zw, _Splat4, _Splat4_TexelSize, _Splat4_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER5)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat5_ST.xy + _Splat5_ST.zw, _Splat5, _Splat5_TexelSize, _Splat5_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER6)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat6_ST.xy + _Splat6_ST.zw, _Splat6, _Splat6_TexelSize, _Splat6_MipInfo, surfaceData.baseColor);
        else if (_DebugMipMapModeTerrainTexture == DEBUGMIPMAPMODETERRAINTEXTURE_LAYER7)
            surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, input.texCoord0.xy * _Splat7_ST.xy + _Splat7_ST.zw, _Splat7, _Splat7_TexelSize, _Splat7_MipInfo, surfaceData.baseColor);
    #endif
        surfaceData.metallic = 0;
    }
#endif

    GetBuiltinData(input, V, posInput, surfaceData, 1, bentNormalWS, 0, builtinData);
}
