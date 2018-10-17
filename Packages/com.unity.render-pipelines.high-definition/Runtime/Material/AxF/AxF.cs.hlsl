//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef AXF_CS_HLSL
#define AXF_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.AxF+SurfaceData:  static fields
//
#define DEBUGVIEW_AXF_SURFACEDATA_NORMAL (1200)
#define DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE (1201)
#define DEBUGVIEW_AXF_SURFACEDATA_TANGENT (1202)
#define DEBUGVIEW_AXF_SURFACEDATA_BI_TANGENT (1203)
#define DEBUGVIEW_AXF_SURFACEDATA_DIFFUSE_COLOR (1204)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_COLOR (1205)
#define DEBUGVIEW_AXF_SURFACEDATA_FRESNEL_F0 (1206)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_LOBE (1207)
#define DEBUGVIEW_AXF_SURFACEDATA_HEIGHT (1208)
#define DEBUGVIEW_AXF_SURFACEDATA_ANISOTROPIC_ANGLE (1209)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_UV (1210)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP (1211)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_COLOR (1212)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_NORMAL (1213)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_IOR (1214)
#define DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL (1215)

//
// UnityEngine.Experimental.Rendering.HDPipeline.AxF+BSDFData:  static fields
//
#define DEBUGVIEW_AXF_BSDFDATA_NORMAL_WS (1250)
#define DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE (1251)
#define DEBUGVIEW_AXF_BSDFDATA_TANGENT_WS (1252)
#define DEBUGVIEW_AXF_BSDFDATA_BI_TANGENT_WS (1253)
#define DEBUGVIEW_AXF_BSDFDATA_DIFFUSE_COLOR (1254)
#define DEBUGVIEW_AXF_BSDFDATA_SPECULAR_COLOR (1255)
#define DEBUGVIEW_AXF_BSDFDATA_FRESNEL_F0 (1256)
#define DEBUGVIEW_AXF_BSDFDATA_ROUGHNESS (1257)
#define DEBUGVIEW_AXF_BSDFDATA_HEIGHT_MM (1258)
#define DEBUGVIEW_AXF_BSDFDATA_ANISOTROPY_ANGLE (1259)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_UV (1260)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP (1261)
#define DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_COLOR (1262)
#define DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_NORMAL_WS (1263)
#define DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_IOR (1264)
#define DEBUGVIEW_AXF_BSDFDATA_GEOM_NORMAL_WS (1265)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.AxF+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 normalWS;
    float3 tangentWS;
    float3 biTangentWS;
    float3 diffuseColor;
    float3 specularColor;
    float3 fresnelF0;
    float2 specularLobe;
    float height_mm;
    float anisotropyAngle;
    float2 flakesUV;
    float flakesMipLevel;
    float3 clearcoatColor;
    float3 clearcoatNormalWS;
    float clearcoatIOR;
    float3 geomNormalWS;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.AxF+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 normalWS;
    float3 tangentWS;
    float3 biTangentWS;
    float3 diffuseColor;
    float3 specularColor;
    float3 fresnelF0;
    float2 roughness;
    float height_mm;
    float anisotropyAngle;
    float2 flakesUV;
    float flakesMipLevel;
    float3 clearcoatColor;
    float3 clearcoatNormalWS;
    float clearcoatIOR;
    float3 geomNormalWS;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_BI_TANGENT:
            result = surfacedata.biTangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_DIFFUSE_COLOR:
            result = surfacedata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FRESNEL_F0:
            result = surfacedata.fresnelF0;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_LOBE:
            result = float3(surfacedata.specularLobe, 0.0);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_HEIGHT:
            result = surfacedata.height_mm.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_ANISOTROPIC_ANGLE:
            result = surfacedata.anisotropyAngle.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_UV:
            result = float3(surfacedata.flakesUV, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP:
            result = surfacedata.flakesMipLevel.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_COLOR:
            result = surfacedata.clearcoatColor;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_NORMAL:
            result = surfacedata.clearcoatNormalWS * 0.5 + 0.5;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_IOR:
            result = surfacedata.clearcoatIOR.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
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
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_BI_TANGENT_WS:
            result = bsdfdata.biTangentWS;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_SPECULAR_COLOR:
            result = bsdfdata.specularColor;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FRESNEL_F0:
            result = bsdfdata.fresnelF0;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_ROUGHNESS:
            result = float3(bsdfdata.roughness, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_HEIGHT_MM:
            result = bsdfdata.height_mm.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_ANISOTROPY_ANGLE:
            result = bsdfdata.anisotropyAngle.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_UV:
            result = float3(bsdfdata.flakesUV, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP:
            result = bsdfdata.flakesMipLevel.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_COLOR:
            result = bsdfdata.clearcoatColor;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_NORMAL_WS:
            result = bsdfdata.clearcoatNormalWS;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_IOR:
            result = bsdfdata.clearcoatIOR.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_GEOM_NORMAL_WS:
            result = bsdfdata.geomNormalWS;
            break;
    }
}


#endif
