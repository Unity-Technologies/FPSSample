#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/VolumetricLighting/VBuffer.hlsl"

#ifdef DEBUG_DISPLAY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#endif

float3 GetFogColor(float3 V, float fragDist)
{
    if (_FogColorMode == FOGCOLORMODE_CONSTANT_COLOR)
    {
        return _FogColor.rgb;
    }
    else if (_FogColorMode == FOGCOLORMODE_SKY_COLOR)
    {
        // Based on Uncharted 4 "Mip Sky Fog" trick: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
        float mipLevel = (1.0 - _MipFogMaxMip * saturate((fragDist - _MipFogNear) / (_MipFogFar - _MipFogNear))) * _SkyTextureMipCount;
        // For the atmosphéric scattering, we use the GGX convoluted version of the cubemap. That matches the of the idnex 0
        return SampleSkyTexture(-V, mipLevel, 0).rgb;
    }
    else // Should not be possible.
        return  float3(0.0, 0.0, 0.0);
}

// Returns fog color in rgb and fog factor (opacity) in alpha.
// The color is premultiplied by alpha.
float4 EvaluateAtmosphericScattering(PositionInputs posInput, float3 V)
{
    // TODO: do not recompute this, but rather pass it directly.
    float fragDist = distance(posInput.positionWS, GetCurrentViewPosition());

    float3 fogColor = 0;
    float  fogFactor = 0;

#ifdef DEBUG_DISPLAY
    // Don't sample atmospheric scattering when lighting debug more are enabled so fog is not visible
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING || _DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING || _DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        return float4(0, 0, 0, 0);
#endif

    switch (_AtmosphericScatteringType)
    {
        case FOGTYPE_LINEAR:
        {
            fogColor = GetFogColor(V, fragDist);
            fogFactor = _FogDensity * saturate((fragDist - _LinearFogStart) * _LinearFogOneOverRange) * saturate((_LinearFogHeightEnd - GetAbsolutePositionWS(posInput.positionWS).y) * _LinearFogHeightOneOverRange);
            fogColor *= fogFactor;
            break;
        }
        case FOGTYPE_EXPONENTIAL:
        {
            fogColor = GetFogColor(V, fragDist);
            float fogHeight = max(0.0, GetAbsolutePositionWS(posInput.positionWS).y - _ExpFogBaseHeight);
            fogFactor = _FogDensity * TransmittanceHomogeneousMedium(_ExpFogHeightAttenuation, fogHeight) * (1.0f - TransmittanceHomogeneousMedium(1.0f / _ExpFogDistance, fragDist));
            fogColor *= fogFactor;
            break;
        }
        case FOGTYPE_VOLUMETRIC:
        {
            float4 value = SampleVBuffer(TEXTURE3D_PARAM(_VBufferLighting, s_linear_clamp_sampler),
                                         posInput.positionNDC,
                                         fragDist,
                                         _VBufferResolution,
                                         _VBufferUvScaleAndLimit.xy,
                                         _VBufferUvScaleAndLimit.zw,
                                         _VBufferDistanceEncodingParams,
                                         _VBufferDistanceDecodingParams,
                                         true, false);

            // TODO: add some slowly animated noise (dither?) to the reconstructed value.
            // TODO: re-enable tone mapping after implementing pre-exposure.
            float4 volFog = DelinearizeRGBA(float4(/*FastTonemapInvert*/(value.rgb), value.a));

            // TODO: if 'posInput.linearDepth' is computed using 'posInput.positionWS',
            // and the latter resides on the far plane, the computation will be numerically unstable.
            float distDelta = fragDist - _VBufferLastSliceDist;

            if ((_EnableDistantFog != 0) && (distDelta > 0))
            {
                // Apply the distant (fallback) fog.
                float3 positionWS  = GetCurrentViewPosition() - V * _VBufferLastSliceDist;
                float  startHeight = positionWS.y;
                float  cosZenith   = -V.y;

                // For both homogeneous and exponential media,
                // Integrate[Transmittance[x] * Scattering[x], {x, 0, t}] = Albedo * Opacity[t].
                // Note that pulling the incoming radiance (which is affected by the fog) out of the
                // integral is wrong, as it means that shadow rays are not volumetrically shadowed.
                // This will result in fog looking overly bright.

                float3 volAlbedo  = _HeightFogBaseScattering / _HeightFogBaseExtinction;
                float  odFallback = OpticalDepthHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight,
                                                          _HeightFogExponents, cosZenith, startHeight, distDelta);
                float  trFallback = TransmittanceFromOpticalDepth(odFallback);
                float  trCamera   = 1 - volFog.a;

                volFog.rgb += trCamera * GetFogColor(V, fragDist) * volAlbedo * (1 - trFallback);
                volFog.a    = 1 - (trCamera * trFallback);
            }

            fogColor  = volFog.rgb; // Pre-multiplied by design
            fogFactor = volFog.a;
            break;
        }
    }

    return float4(fogColor, fogFactor);
}

#endif
