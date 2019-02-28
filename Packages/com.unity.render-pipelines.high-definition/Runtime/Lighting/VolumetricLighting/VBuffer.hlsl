#ifndef UNITY_VBUFFER_INCLUDED
#define UNITY_VBUFFER_INCLUDED

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
                     float  linearDistance,
                     float4 VBufferResolution,
                     float2 VBufferUvScale,
                     float2 VBufferUvLimit,
                     float4 VBufferDistanceEncodingParams,
                     float4 VBufferDistanceDecodingParams,
                     bool   quadraticFilterXY,
                     bool   clampToBorder)
{
    // These are the viewport coordinates.
    float2 uv = positionNDC;
    float  w  = EncodeLogarithmicDepthGeneralized(linearDistance, VBufferDistanceEncodingParams);

    bool coordIsInsideFrustum = true;

    if (clampToBorder)
    {
        // Coordinates are always clamped to edge. We just introduce a clipping operation.
        float3 positionCS = float3(uv, w) * 2 - 1;

        coordIsInsideFrustum = Max3(abs(positionCS.x), abs(positionCS.y), abs(positionCS.z)) < 1;
    }

    float4 result = 0;

    if (coordIsInsideFrustum)
    {
        if (quadraticFilterXY)
        {
            float2 xy = uv * VBufferResolution.xy;
            float2 ic = floor(xy);
            float2 fc = frac(xy);

            float2 weights[2], offsets[2];
            BiquadraticFilter(1 - fc, weights, offsets); // Inverse-translate the filter centered around 0.5

            const float2 ssToUv = VBufferResolution.zw * VBufferUvScale;

            // The sampler clamps to edge. This takes care of 4 frustum faces out of 6.
            // Due to the RTHandle scaling system, we must take care of the other 2 manually.
            // TODO: perform per-sample (4, in this case) bilateral filtering, rather than per-pixel. This should reduce leaking.
            result = (weights[0].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[0].y)) * ssToUv, VBufferUvLimit), w), 0)  // Top left
                   + (weights[1].x * weights[0].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[0].y)) * ssToUv, VBufferUvLimit), w), 0)  // Top right
                   + (weights[0].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[0].x, offsets[1].y)) * ssToUv, VBufferUvLimit), w), 0)  // Bottom left
                   + (weights[1].x * weights[1].y) * SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min((ic + float2(offsets[1].x, offsets[1].y)) * ssToUv, VBufferUvLimit), w), 0); // Bottom right
        }
        else
        {
            // The sampler clamps to edge. This takes care of 4 frustum faces out of 6.
            // Due to the RTHandle scaling system, we must take care of the other 2 manually.
            result = SAMPLE_TEXTURE3D_LOD(VBuffer, clampSampler, float3(min(uv * VBufferUvScale, VBufferUvLimit), w), 0);
        }
    }

    return result;
}

float4 SampleVBuffer(TEXTURE3D_ARGS(VBuffer, clampSampler),
                     float3   positionWS,
                     float3   cameraPositionWS,
                     float4x4 viewProjMatrix,
                     float4   VBufferResolution,
                     float2   VBufferUvScale,
                     float2   VBufferUvLimit,
                     float4   VBufferDistanceEncodingParams,
                     float4   VBufferDistanceDecodingParams,
                     bool     quadraticFilterXY,
                     bool     clampToBorder)
{
    float2 positionNDC = ComputeNormalizedDeviceCoordinates(positionWS, viewProjMatrix);
    float  linearDistance = distance(positionWS, cameraPositionWS);

    return SampleVBuffer(TEXTURE3D_PARAM(VBuffer, clampSampler),
                         positionNDC,
                         linearDistance,
                         VBufferResolution,
                         VBufferUvScale,
                         VBufferUvLimit,
                         VBufferDistanceEncodingParams,
                         VBufferDistanceDecodingParams,
                         quadraticFilterXY,
                         clampToBorder);
}

#endif // UNITY_VBUFFER_INCLUDED
