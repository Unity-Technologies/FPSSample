#ifndef UNITY_BUILTIN_DATA_INCLUDED
#define UNITY_BUILTIN_DATA_INCLUDED

//-----------------------------------------------------------------------------
// BuiltinData
// This structure include common data that should be present in all material
// and are independent from the BSDF parametrization.
// Note: These parameters can be store in GBuffer if the writer wants
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl" // Require for GetIndexColor auto generated
#include "BuiltinData.cs.hlsl"

//-----------------------------------------------------------------------------
// helper macro
//-----------------------------------------------------------------------------

#define BUILTIN_DATA_SHADOW_MASK float4(builtinData.shadowMask0, builtinData.shadowMask1, builtinData.shadowMask2, builtinData.shadowMask3)

//-----------------------------------------------------------------------------
// common Encode/Decode functions
//-----------------------------------------------------------------------------

// Guideline for velocity buffer.
// The object velocity buffer is potentially fill in several pass.
// - In gbuffer pass with extra RT (Not supported currently)
// - In forward prepass pass
// - In dedicated velocity pass
// So same velocity buffer is use for all scenario, so if deferred define a velocity buffer, the same is reuse for forward case.
// THis is similar to NormalBuffer

// TODO: CAUTION: current DecodeVelocity is not used in motion vector / TAA pass as it come from Postprocess stack
// This will be fix when postprocess will be integrated into HD, but it mean that we must not change the
// EncodeVelocity / DecodeVelocity code for now, i.e it must do nothing like it is doing currently.
// Note2: Motion blur code of posptrocess stack do * 2 - 1 to uncompress velocity which is not expected, TAA is correct.
// Design note: We assume that velocity/distortion fit into a single buffer (i.e not spread on several buffer)
void EncodeVelocity(float2 velocity, out float4 outBuffer)
{
    // RT - 16:16 float
    outBuffer = float4(velocity.xy, 0.0, 0.0);
}

void DecodeVelocity(float4 inBuffer, out float2 velocity)
{
    velocity = inBuffer.xy;
}

void EncodeDistortion(float2 distortion, float distortionBlur, bool isValidSource, out float4 outBuffer)
{
    // RT - 16:16:16:16 float
    // distortionBlur in alpha for a different blend mode
    outBuffer = float4(distortion, isValidSource, distortionBlur);
}

void DecodeDistortion(float4 inBuffer, out float2 distortion, out float distortionBlur, out bool isValidSource)
{
    distortion = inBuffer.xy;
    distortionBlur = inBuffer.a;
    isValidSource = (inBuffer.z != 0.0);
}

void GetBuiltinDataDebug(uint paramId, BuiltinData builtinData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBuiltinDataDebug(paramId, builtinData, result, needLinearToSRGB);

    switch (paramId)
    {
    case DEBUGVIEW_BUILTIN_BUILTINDATA_BAKE_DIFFUSE_LIGHTING:
        // TODO: require a remap
        // TODO: we should not gamma correct, but easier to debug for now without correct high range value
        result = builtinData.bakeDiffuseLighting; needLinearToSRGB = true;
        break;
    case DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET:
        result = builtinData.depthOffset.xxx * 10.0; // * 10 assuming 1 unity is 1m
        break;
    case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION:
        result = float3((builtinData.distortion / (abs(builtinData.distortion) + 1) + 1) * 0.5, 0.5);
        break;
    }
}

#endif // UNITY_BUILTIN_DATA_INCLUDED
