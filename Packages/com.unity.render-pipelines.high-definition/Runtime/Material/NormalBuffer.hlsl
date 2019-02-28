#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// ----------------------------------------------------------------------------
// Encoding/decoding normal buffer functions
// ----------------------------------------------------------------------------

struct NormalData
{
    float3 normalWS;
    float  perceptualRoughness;
};

// SSSBuffer texture declaration
TEXTURE2D(_NormalBufferTexture);

void EncodeIntoNormalBuffer(NormalData normalData, uint2 positionSS, out float4 outNormalBuffer0)
{
    // The sign of the Z component of the normal MUST round-trip through the G-Buffer, otherwise
    // the reconstruction of the tangent frame for anisotropic GGX creates a seam along the Z axis.
    // The constant was eye-balled to not cause artifacts.
    // TODO: find a proper solution. E.g. we could re-shuffle the faces of the octahedron
    // s.t. the sign of the Z component round-trips.
    const float seamThreshold = 1.0 / 1024.0;
    normalData.normalWS.z = CopySign(max(seamThreshold, abs(normalData.normalWS.z)), normalData.normalWS.z);

    // RT1 - 8:8:8:8
    // Our tangent encoding is based on our normal.
    float2 octNormalWS = PackNormalOctQuadEncode(normalData.normalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    // We store perceptualRoughness instead of roughness because it is perceptually linear.
    outNormalBuffer0 = float4(packNormalWS, normalData.perceptualRoughness);
}

void DecodeFromNormalBuffer(float4 normalBuffer, uint2 positionSS, out NormalData normalData)
{
    float3 packNormalWS = normalBuffer.rgb;
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
    normalData.normalWS = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);
    normalData.perceptualRoughness = normalBuffer.a;
}

void DecodeFromNormalBuffer(uint2 positionSS, out NormalData normalData)
{
    float4 normalBuffer = LOAD_TEXTURE2D(_NormalBufferTexture, positionSS);
    DecodeFromNormalBuffer(normalBuffer, positionSS, normalData);
}

// OUTPUT_NORMAL_NORMALBUFFER start from SV_Target0 as it is used during depth prepass where there is no color buffer
