//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIT_CS_HLSL
#define LIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_STANDARD (1)
#define MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR (2)
#define MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING (4)
#define MATERIALFEATUREFLAGS_LIT_TRANSMISSION (8)
#define MATERIALFEATUREFLAGS_LIT_ANISOTROPY (16)
#define MATERIALFEATUREFLAGS_LIT_IRIDESCENCE (32)
#define MATERIALFEATUREFLAGS_LIT_CLEAR_COAT (64)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData:  static fields
//
#define DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES (1000)
#define DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR (1001)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION (1002)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL (1003)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL_VIEW_SPACE (1004)
#define DEBUGVIEW_LIT_SURFACEDATA_SMOOTHNESS (1005)
#define DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION (1006)
#define DEBUGVIEW_LIT_SURFACEDATA_METALLIC (1007)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_MASK (1008)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR (1009)
#define DEBUGVIEW_LIT_SURFACEDATA_DIFFUSION_PROFILE (1010)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_MASK (1011)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS (1012)
#define DEBUGVIEW_LIT_SURFACEDATA_TANGENT (1013)
#define DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY (1014)
#define DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_LAYER_THICKNESS (1015)
#define DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_MASK (1016)
#define DEBUGVIEW_LIT_SURFACEDATA_INDEX_OF_REFRACTION (1017)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR (1018)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_ABSORPTION_DISTANCE (1019)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK (1020)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData:  static fields
//
#define DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES (1050)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR (1051)
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1052)
#define DEBUGVIEW_LIT_BSDFDATA_AMBIENT_OCCLUSION (1053)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION (1054)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS (1055)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE (1056)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS (1057)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_MASK (1058)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE (1059)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK (1060)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1061)
#define DEBUGVIEW_LIT_BSDFDATA_USE_THICK_OBJECT_MODE (1062)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE (1063)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS (1064)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS (1065)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T (1066)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B (1067)
#define DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY (1068)
#define DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_THICKNESS (1069)
#define DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_MASK (1070)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS (1071)
#define DEBUGVIEW_LIT_BSDFDATA_IOR (1072)
#define DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT (1073)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK (1074)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualSmoothness;
    float ambientOcclusion;
    float metallic;
    float coatMask;
    float3 specularColor;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    float3 tangentWS;
    float anisotropy;
    float iridescenceThickness;
    float iridescenceMask;
    float ior;
    float3 transmittanceColor;
    float atDistance;
    float transmittanceMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float coatMask;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    bool useThickObjectMode;
    float3 transmittance;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float anisotropy;
    float iridescenceThickness;
    float iridescenceMask;
    float coatRoughness;
    float ior;
    float3 absorptionCoefficient;
    float transmittanceMask;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_MASK:
            result = surfacedata.coatMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(surfacedata.diffusionProfile);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_LAYER_THICKNESS:
            result = surfacedata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_MASK:
            result = surfacedata.iridescenceMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_INDEX_OF_REFRACTION:
            result = surfacedata.ior.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR:
            result = surfacedata.transmittanceColor;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_ABSORPTION_DISTANCE:
            result = surfacedata.atDistance.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK:
            result = surfacedata.transmittanceMask.xxx;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_MASK:
            result = bsdfdata.coatMask.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(bsdfdata.diffusionProfile);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK:
            result = bsdfdata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = bsdfdata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_USE_THICK_OBJECT_MODE:
            result = (bsdfdata.useThickObjectMode) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T:
            result = bsdfdata.roughnessT.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B:
            result = bsdfdata.roughnessB.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_THICKNESS:
            result = bsdfdata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_MASK:
            result = bsdfdata.iridescenceMask.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS:
            result = bsdfdata.coatRoughness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IOR:
            result = bsdfdata.ior.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT:
            result = bsdfdata.absorptionCoefficient;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK:
            result = bsdfdata.transmittanceMask.xxx;
            break;
    }
}


#endif
