#ifndef UNITY_POSTFX_SMAA_BRIDGE
#define UNITY_POSTFX_SMAA_BRIDGE

#include "../StdLib.hlsl"

TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
TEXTURE2D_SAMPLER2D(_BlendTex, sampler_BlendTex);
TEXTURE2D_SAMPLER2D(_AreaTex, sampler_AreaTex);
TEXTURE2D_SAMPLER2D(_SearchTex, sampler_SearchTex);
float4 _MainTex_TexelSize;

#define SMAA_RT_METRICS _MainTex_TexelSize
#define SMAA_AREATEX_SELECT(s) s.rg
#define SMAA_SEARCHTEX_SELECT(s) s.a
#define LinearSampler sampler_MainTex
#define PointSampler sampler_MainTex

#include "SubpixelMorphologicalAntialiasing.hlsl"

// ----------------------------------------------------------------------------------------
// Edge Detection

struct VaryingsEdge
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float4 offsets[3] : TEXCOORD1;
};

VaryingsEdge VertEdge(AttributesDefault v)
{
    VaryingsEdge o;
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
    o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

    o.offsets[0] = mad(SMAA_RT_METRICS.xyxy, float4(-1.0, 0.0, 0.0, -1.0), o.texcoord.xyxy);
    o.offsets[1] = mad(SMAA_RT_METRICS.xyxy, float4( 1.0, 0.0, 0.0,  1.0), o.texcoord.xyxy);
    o.offsets[2] = mad(SMAA_RT_METRICS.xyxy, float4(-2.0, 0.0, 0.0, -2.0), o.texcoord.xyxy);

    return o;
}

float4 FragEdge(VaryingsEdge i) : SV_Target
{
    return float4(SMAAColorEdgeDetectionPS(i.texcoord, i.offsets, _MainTex), 0.0, 0.0);
}

// ----------------------------------------------------------------------------------------
// Blend Weights Calculation

struct VaryingsBlend
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float2 pixcoord : TEXCOORD1;
    float4 offsets[3] : TEXCOORD2;
};

VaryingsBlend VertBlend(AttributesDefault v)
{
    VaryingsBlend o;
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
    o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

    o.pixcoord = o.texcoord * SMAA_RT_METRICS.zw;

    // We will use these offsets for the searches later on (see @PSEUDO_GATHER4):
    o.offsets[0] = mad(SMAA_RT_METRICS.xyxy, float4(-0.250, -0.125,  1.250, -0.125), o.texcoord.xyxy);
    o.offsets[1] = mad(SMAA_RT_METRICS.xyxy, float4(-0.125, -0.250, -0.125,  1.250), o.texcoord.xyxy);

    // And these for the searches, they indicate the ends of the loops:
    o.offsets[2] = mad(SMAA_RT_METRICS.xxyy, float4(-2.0, 2.0, -2.0, 2.0) * float(SMAA_MAX_SEARCH_STEPS),
        float4(o.offsets[0].xz, o.offsets[1].yw));

    return o;
}

float4 FragBlend(VaryingsBlend i) : SV_Target
{
    return SMAABlendingWeightCalculationPS(i.texcoord, i.pixcoord, i.offsets, _MainTex, _AreaTex, _SearchTex, 0);
}

// ----------------------------------------------------------------------------------------
// Neighborhood Blending

struct VaryingsNeighbor
{
    float4 vertex : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float4 offset : TEXCOORD1;
};

VaryingsNeighbor VertNeighbor(AttributesDefault v)
{
    VaryingsNeighbor o;
    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
    o.texcoord = TransformTriangleVertexToUV(v.vertex.xy);

#if UNITY_UV_STARTS_AT_TOP
    o.texcoord = o.texcoord * float2(1.0, -1.0) + float2(0.0, 1.0);
#endif

    o.offset = mad(SMAA_RT_METRICS.xyxy, float4(1.0, 0.0, 0.0, 1.0), o.texcoord.xyxy);
    return o;
}

float4 FragNeighbor(VaryingsNeighbor i) : SV_Target
{
    return SMAANeighborhoodBlendingPS(i.texcoord, i.offset, _MainTex, _BlendTex);
}

#endif
