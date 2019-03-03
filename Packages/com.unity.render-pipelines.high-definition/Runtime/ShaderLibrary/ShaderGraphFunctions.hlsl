#ifndef UNITY_GRAPHFUNCTIONS_HD_INCLUDED
#define UNITY_GRAPHFUNCTIONS_HD_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_HDSampleSceneDepth(uv)
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_HDBakedGI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#define SHADERGRAPH_OBJECT_POSITION GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23)

float shadergraph_HDSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
	return Linear01Depth(rawDepth, _ZBufferParams);
#endif
    return 0;
}

float3 shadergraph_HDBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);
    return SampleBakedGI(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap);
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_HD_INCLUDED
