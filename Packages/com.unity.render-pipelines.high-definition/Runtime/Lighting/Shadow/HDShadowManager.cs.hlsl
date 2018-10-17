//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDSHADOWMANAGER_CS_HLSL
#define HDSHADOWMANAGER_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.HDShadowFlag:  static fields
//
#define HDSHADOWFLAG_SAMPLE_BIAS_SCALE (1)
#define HDSHADOWFLAG_EDGE_LEAK_FIXUP (2)
#define HDSHADOWFLAG_EDGE_TOLERANCE_NORMAL (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
// PackingRules = Exact
struct HDShadowData
{
    float3 rot0;
    float3 rot1;
    float3 rot2;
    float3 pos;
    float4 proj;
    float2 atlasOffset;
    float edgeTolerance;
    int flags;
    float4 zBufferParam;
    float4 shadowMapSize;
    float4 viewBias;
    float3 normalBias;
    float _padding;
    float4 shadowFilterParams0;
    float4x4 shadowToWorld;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDDirectionalShadowData
// PackingRules = Exact
struct HDDirectionalShadowData
{
    float4 sphereCascades[4];
    float4 cascadeDirection;
    float cascadeBorders[4];
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDShadowData
//
float3 GetRot0(HDShadowData value)
{
    return value.rot0;
}
float3 GetRot1(HDShadowData value)
{
    return value.rot1;
}
float3 GetRot2(HDShadowData value)
{
    return value.rot2;
}
float3 GetPos(HDShadowData value)
{
    return value.pos;
}
float4 GetProj(HDShadowData value)
{
    return value.proj;
}
float2 GetAtlasOffset(HDShadowData value)
{
    return value.atlasOffset;
}
float GetEdgeTolerance(HDShadowData value)
{
    return value.edgeTolerance;
}
int GetFlags(HDShadowData value)
{
    return value.flags;
}
float4 GetZBufferParam(HDShadowData value)
{
    return value.zBufferParam;
}
float4 GetShadowMapSize(HDShadowData value)
{
    return value.shadowMapSize;
}
float4 GetViewBias(HDShadowData value)
{
    return value.viewBias;
}
float3 GetNormalBias(HDShadowData value)
{
    return value.normalBias;
}
float Get_padding(HDShadowData value)
{
    return value._padding;
}
float4 GetShadowFilterParams0(HDShadowData value)
{
    return value.shadowFilterParams0;
}
float4x4 GetShadowToWorld(HDShadowData value)
{
    return value.shadowToWorld;
}

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.HDDirectionalShadowData
//
float4 GetSphereCascades(HDDirectionalShadowData value, int index)
{
    return value.sphereCascades[index];
}
float4 GetCascadeDirection(HDDirectionalShadowData value)
{
    return value.cascadeDirection;
}
float GetCascadeBorders(HDDirectionalShadowData value, int index)
{
    return value.cascadeBorders[index];
}


#endif
