//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIGHTLOOP_CS_HLSL
#define LIGHTLOOP_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.LightVolumeType:  static fields
//
#define LIGHTVOLUMETYPE_CONE (0)
#define LIGHTVOLUMETYPE_SPHERE (1)
#define LIGHTVOLUMETYPE_BOX (2)
#define LIGHTVOLUMETYPE_COUNT (3)

//
// UnityEngine.Experimental.Rendering.HDPipeline.LightCategory:  static fields
//
#define LIGHTCATEGORY_PUNCTUAL (0)
#define LIGHTCATEGORY_AREA (1)
#define LIGHTCATEGORY_ENV (2)
#define LIGHTCATEGORY_DECAL (3)
#define LIGHTCATEGORY_DENSITY_VOLUME (4)
#define LIGHTCATEGORY_COUNT (5)

//
// UnityEngine.Experimental.Rendering.HDPipeline.LightFeatureFlags:  static fields
//
#define LIGHTFEATUREFLAGS_PUNCTUAL (4096)
#define LIGHTFEATUREFLAGS_AREA (8192)
#define LIGHTFEATUREFLAGS_DIRECTIONAL (16384)
#define LIGHTFEATUREFLAGS_ENV (32768)
#define LIGHTFEATUREFLAGS_SKY (65536)
#define LIGHTFEATUREFLAGS_SSREFRACTION (131072)
#define LIGHTFEATUREFLAGS_SSREFLECTION (262144)

//
// UnityEngine.Experimental.Rendering.HDPipeline.LightDefinitions:  static fields
//
#define MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE (512)
#define VIEWPORT_SCALE_Z (1)
#define USE_LEFT_HAND_CAMERA_SPACE (1)
#define TILE_SIZE_FPTL (16)
#define TILE_SIZE_CLUSTERED (32)
#define NUM_FEATURE_VARIANTS (27)
#define LIGHT_FEATURE_MASK_FLAGS (16773120)
#define LIGHT_FEATURE_MASK_FLAGS_OPAQUE (16642048)
#define LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT (16510976)
#define MATERIAL_FEATURE_MASK_FLAGS (4095)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.SFiniteLightBound
// PackingRules = Exact
struct SFiniteLightBound
{
    float3 boxAxisX;
    float3 boxAxisY;
    float3 boxAxisZ;
    float3 center;
    float2 scaleXY;
    float radius;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.LightVolumeData
// PackingRules = Exact
struct LightVolumeData
{
    float3 lightPos;
    uint lightVolume;
    float3 lightAxisX;
    uint lightCategory;
    float3 lightAxisY;
    float radiusSq;
    float3 lightAxisZ;
    float cotan;
    float3 boxInnerDist;
    uint featureFlags;
    float3 boxInvRange;
    float unused2;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.SFiniteLightBound
//
float3 GetBoxAxisX(SFiniteLightBound value)
{
    return value.boxAxisX;
}
float3 GetBoxAxisY(SFiniteLightBound value)
{
    return value.boxAxisY;
}
float3 GetBoxAxisZ(SFiniteLightBound value)
{
    return value.boxAxisZ;
}
float3 GetCenter(SFiniteLightBound value)
{
    return value.center;
}
float2 GetScaleXY(SFiniteLightBound value)
{
    return value.scaleXY;
}
float GetRadius(SFiniteLightBound value)
{
    return value.radius;
}

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.LightVolumeData
//
float3 GetLightPos(LightVolumeData value)
{
    return value.lightPos;
}
uint GetLightVolume(LightVolumeData value)
{
    return value.lightVolume;
}
float3 GetLightAxisX(LightVolumeData value)
{
    return value.lightAxisX;
}
uint GetLightCategory(LightVolumeData value)
{
    return value.lightCategory;
}
float3 GetLightAxisY(LightVolumeData value)
{
    return value.lightAxisY;
}
float GetRadiusSq(LightVolumeData value)
{
    return value.radiusSq;
}
float3 GetLightAxisZ(LightVolumeData value)
{
    return value.lightAxisZ;
}
float GetCotan(LightVolumeData value)
{
    return value.cotan;
}
float3 GetBoxInnerDist(LightVolumeData value)
{
    return value.boxInnerDist;
}
uint GetFeatureFlags(LightVolumeData value)
{
    return value.featureFlags;
}
float3 GetBoxInvRange(LightVolumeData value)
{
    return value.boxInvRange;
}
float GetUnused2(LightVolumeData value)
{
    return value.unused2;
}


#endif
