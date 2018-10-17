#ifndef UNITY_VBUFFER_INCLUDED
#define UNITY_VBUFFER_INCLUDED

// Interpolation of log-encoded values is non-linear.
// Therefore, given 'logEncodedDepth', we compute a new depth value
// which allows us to perform HW interpolation which is linear in the view space.
float ComputeLerpPositionForLogEncoding(float  linearDepth,
                                        float  logEncodedDepth,
                                        float2 VBufferSliceCount,
                                        float4 VBufferDepthDecodingParams)
{
    float z = linearDepth;
    float d = logEncodedDepth;

    float numSlices    = VBufferSliceCount.x;
    float rcpNumSlices = VBufferSliceCount.y;

    float s  = d * numSlices - 0.5;
    float s0 = floor(s);
    float s1 = ceil(s);
    float d0 = saturate(s0 * rcpNumSlices + (0.5 * rcpNumSlices));
    float d1 = saturate(s1 * rcpNumSlices + (0.5 * rcpNumSlices));
    float z0 = DecodeLogarithmicDepthGeneralized(d0, VBufferDepthDecodingParams);
    float z1 = DecodeLogarithmicDepthGeneralized(d1, VBufferDepthDecodingParams);

    // Compute the linear interpolation weight.
    float t = saturate((z - z0) / (z1 - z0));

    // Do not saturate here, we want to know whether we are outside of the near/far plane bounds.
    return d0 + t * rcpNumSlices;
}

// if (correctLinearInterpolation), we use ComputeLerpPositionForLogEncoding() to correct weighting
// of both slices at the cost of extra ALUs.
//
// if (quadraticFilterXY), we perform biquadratic (3x3) reconstruction for each slice to reduce
// aliasing at the cost of extra ALUs and bandwidth.
// Warning: you MUST pass a linear sampler in order for the quadratic filter to work.
//
// Note: for correct filtering, the data has to be stored in the perceptual space.
// This means storing tone mapped radiance and transmittance instead of optical depth.
// See "A Fresh Look at Generalized Sampling", p. 51.
//
// if (clampToBorder), samples outside of the buffer return 0 (we perform a smooth fade).
// Otherwise, the sampler simply clamps the texture coordinate to the edge of the texture.
// Warning: clamping to border may not work as expected with the quadratic filter due to its extent.
float4 SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                     float2 positionNDC,
                     float  linearDepth,
                     float4 VBufferResolution,
                     float2 VBufferSliceCount,
                     float2 VBufferUvScale,
                     float2 VBufferUvLimit,
                     float4 VBufferDepthEncodingParams,
                     float4 VBufferDepthDecodingParams,
                     bool   correctLinearInterpolation,
                     bool   quadraticFilterXY,
                     bool   clampToBorder)
{
    // These are the viewport coordinates.
    float2 uv = positionNDC;
    float  w;

    // The distance between slices is log-encoded.
    float z = linearDepth;
    float d = EncodeLogarithmicDepthGeneralized(z, VBufferDepthEncodingParams);

    if (correctLinearInterpolation)
    {
        // Adjust the texture coordinate for HW linear filtering.
        w = ComputeLerpPositionForLogEncoding(z, d, VBufferSliceCount, VBufferDepthDecodingParams);
    }
    else
    {
        // Ignore non-linearity (for performance reasons) at the cost of accuracy.
        // The results are exact for a stationary camera, but can potentially cause some judder in motion.
        w = d;
    }

    float fadeWeight = 1;

    if (clampToBorder)
    {
        float3 positionCS = float3(uv, w) * 2 - 1;
        float3 rcpLength  = float3(VBufferResolution.xy, VBufferSliceCount.x);

        // Fade to black at the edges (0.5 pixel) of the viewport.
        fadeWeight = Remap10(abs(positionCS.x), rcpLength.x, rcpLength.x)
                   * Remap10(abs(positionCS.y), rcpLength.y, rcpLength.y)
                   * Remap10(abs(positionCS.z), rcpLength.z, rcpLength.z);
    }

    float4 result = 0;

    if (fadeWeight > 0)
    {
        if (quadraticFilterXY)
        {
            float2 xy = uv * VBufferResolution.xy;
            float2 ic = floor(xy);
            float2 fc = frac(xy);

            float2 weights[2], offsets[2];
            BiquadraticFilter(1 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

            // We always clamp to edge only for bottom and right edges. Clamping for 'w' is handled by the hardware.
            // TODO: perform per-sample (4, in this case) bilateral filtering, rather than per-pixel. This should reduce leaking.
            result = (weights[0].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[0].y)) * (VBufferResolution.zw * VBufferUvScale), VBufferUvLimit), w), 0)  // Top left
                   + (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[0].y)) * (VBufferResolution.zw * VBufferUvScale), VBufferUvLimit), w), 0)  // Top right
                   + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[1].y)) * (VBufferResolution.zw * VBufferUvScale), VBufferUvLimit), w), 0)  // Bottom left
                   + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[1].y)) * (VBufferResolution.zw * VBufferUvScale), VBufferUvLimit), w), 0); // Bottom right
        }
        else
        {
            // We always clamp to edge only for bottom and right edges. Clamping for 'w' is handled by the hardware.
            result = SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(uv * VBufferUvScale, VBufferUvLimit), w), 0);
        }

        result *= fadeWeight;
    }

    return result;
}

float4 SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                     float3   positionWS,
                     float4x4 viewProjMatrix,
                     float4   VBufferResolution,
                     float2   VBufferSliceCount,
                     float2   VBufferUvScale,
                     float2   VBufferUvLimit,
                     float4   VBufferDepthEncodingParams,
                     float4   VBufferDepthDecodingParams,
                     bool     correctLinearInterpolation,
                     bool     quadraticFilterXY,
                     bool     clampToBorder)
{
    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, viewProjMatrix);
    float  linearDepth = mul(viewProjMatrix, float4(positionWS, 1)).w;

    return SampleVBuffer(TEXTURE3D_PARAM(VBuffer, clampSampler),
                         positionNDC,
                         linearDepth,
                         VBufferResolution,
                         VBufferSliceCount,
                         VBufferUvScale,
                         VBufferUvLimit,
                         VBufferDepthEncodingParams,
                         VBufferDepthDecodingParams,
                         correctLinearInterpolation,
                         quadraticFilterXY,
                         clampToBorder);
}

// Returns interpolated {volumetric radiance, transmittance}.
float4 SampleVolumetricLighting(TEXTURE3D_ARGS(VBufferLighting, clampSampler),
                                float2 positionNDC,
                                float  linearDepth,
                                float4 VBufferResolution,
                                float2 VBufferSliceCount,
                                float2 VBufferUvScale,
                                float2 VBufferUvLimit,
                                float4 VBufferDepthEncodingParams,
                                float4 VBufferDepthDecodingParams,
                                bool   correctLinearInterpolation,
                                bool   quadraticFilterXY)
{
    // TODO: add some slowly animated noise to the reconstructed value.
    // TODO: re-enable tone mapping after implementing pre-exposure.
    return /*FastTonemapInvert*/(SampleVBuffer(TEXTURE3D_PARAM(VBufferLighting, clampSampler),
                                           positionNDC,
                                           linearDepth,
                                           VBufferResolution,
                                           VBufferSliceCount,
                                           VBufferUvScale,
                                           VBufferUvLimit,
                                           VBufferDepthEncodingParams,
                                           VBufferDepthDecodingParams,
                                           correctLinearInterpolation,
                                           quadraticFilterXY,
                                           false));
}

#endif // UNITY_VBUFFER_INCLUDED
