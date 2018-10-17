#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/DiffusionProfile/DiffusionProfileSettings.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/DiffusionProfile/DiffusionProfile.hlsl"

// ----------------------------------------------------------------------------
// helper functions
// ----------------------------------------------------------------------------

// 0: [ albedo = albedo ]
// 1: [ albedo = 1 ]
// 2: [ albedo = sqrt(albedo) ]
uint GetSubsurfaceScatteringTexturingMode(int diffusionProfile)
{
    uint texturingMode = 0;

#if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_SUBSURFACE_SCATTERING)
    // If the SSS pass is executed, we know we have SSS enabled.
    bool enableSss = true;
#else
    bool enableSss = _EnableSubsurfaceScattering != 0;
#endif

    if (enableSss)
    {
        bool performPostScatterTexturing = IsBitSet(asuint(_TexturingModeFlags), diffusionProfile);

        if (performPostScatterTexturing)
        {
            // Post-scatter texturing mode: the albedo is only applied during the SSS pass.
        #if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SUBSURFACE_SCATTERING)
            texturingMode = 1;
        #endif
        }
        else
        {
            // Pre- and post- scatter texturing mode.
            texturingMode = 2;
        }
    }

    return texturingMode;
}

// Returns the modified albedo (diffuse color) for materials with subsurface scattering.
// See GetSubsurfaceScatteringTexturingMode() above for more details.
// Ref: Advanced Techniques for Realistic Real-Time Skin Rendering.
float3 ApplySubsurfaceScatteringTexturingMode(uint texturingMode, float3 color)
{
    switch (texturingMode)
    {
        case 2:  color = sqrt(color); break;
        case 1:  color = 1;           break;
        default: color = color;       break;
    }

    return color;
}

// ----------------------------------------------------------------------------
// Encoding/decoding SSS buffer functions
// ----------------------------------------------------------------------------

struct SSSData
{
    float3 diffuseColor;
    float  subsurfaceMask;
    uint   diffusionProfile;
};

#define SSSBufferType0 float4 // Must match GBufferType0 in deferred

// SSSBuffer texture declaration
TEXTURE2D(_SSSBufferTexture0);

// Note: The SSS buffer used here is sRGB
void EncodeIntoSSSBuffer(SSSData sssData, uint2 positionSS, out SSSBufferType0 outSSSBuffer0)
{
    outSSSBuffer0 = float4(sssData.diffuseColor, PackFloatInt8bit(sssData.subsurfaceMask, sssData.diffusionProfile, 16));
}

// Note: The SSS buffer used here is sRGB
void DecodeFromSSSBuffer(float4 sssBuffer, uint2 positionSS, out SSSData sssData)
{
    sssData.diffuseColor = sssBuffer.rgb;
    UnpackFloatInt8bit(sssBuffer.a, 16, sssData.subsurfaceMask, sssData.diffusionProfile);
}

void DecodeFromSSSBuffer(uint2 positionSS, out SSSData sssData)
{
    float4 sssBuffer = LOAD_TEXTURE2D(_SSSBufferTexture0, positionSS);
    DecodeFromSSSBuffer(sssBuffer, positionSS, sssData);
}

// OUTPUT_SSSBUFFER start from SV_Target2 as SV_Target0 and SV_Target1 are used for lighting buffer
#define OUTPUT_SSSBUFFER(NAME) out SSSBufferType0 MERGE_NAME(NAME, 0) : SV_Target2
#define ENCODE_INTO_SSSBUFFER(SURFACE_DATA, UNPOSITIONSS, NAME) EncodeIntoSSSBuffer(ConvertSurfaceDataToSSSData(SURFACE_DATA), UNPOSITIONSS, MERGE_NAME(NAME, 0))

#define DECODE_FROM_SSSBUFFER(UNPOSITIONSS, SSS_DATA) DecodeFromSSSBuffer(UNPOSITIONSS, SSS_DATA)

// In order to support subsurface scattering, we need to know which pixels have an SSS material.
// It can be accomplished by reading the stencil buffer.
// A faster solution (which avoids an extra texture fetch) is to simply make sure that
// all pixels which belong to an SSS material are not black (those that don't always are).
// We choose the blue color channel since it's perceptually the least noticeable.
float3 TagLightingForSSS(float3 subsurfaceLighting)
{
    subsurfaceLighting.b = max(subsurfaceLighting.b, HALF_MIN);
    return subsurfaceLighting;
}

// See TagLightingForSSS() for details.
bool TestLightingForSSS(float3 subsurfaceLighting)
{
    return subsurfaceLighting.b > 0;
}

// ----------------------------------------------------------------------------
// Helper functions to use SSS/Transmission with a material
// ----------------------------------------------------------------------------

// Following function allow to easily setup SSS and transmission inside a material.
// User can request either SSS functions, or Transmission functions, or both, by defining MATERIAL_INCLUDE_SUBSURFACESCATTERING and/or MATERIAL_INCLUDE_TRANSMISSION
// before including this file.
// + It require that the material follow naming convention for properties inside BSDFData

// struct BSDFData
// {
//     (...)
//     // Share for SSS and Transmission
//     uint materialFeatures;
//     uint diffusionProfile;
//     // For SSS
//     float3 diffuseColor;
//     float3 fresnel0;
//     float subsurfaceMask;
//     // For transmission
//     float thickness;
//     bool useThickObjectMode;
//     float3 transmittance;
//     perceptualRoughness; // Only if user chose to support DisneyDiffuse
//     (...)
// }

// Note: Transmission functions for light evaluation are included in LightEvaluation.hlsl file also based on the MATERIAL_INCLUDE_TRANSMISSION
// For LightEvaluation.hlsl file it is required to define a BRDF for the transmission. Defining USE_DIFFUSE_LAMBERT_BRDF use Lambert, otherwise it use Disneydiffuse

#define MATERIALFEATUREFLAGS_SSS_TRANSMISSION_START (1 << 16) // It should be safe to start these flags

#define MATERIALFEATUREFLAGS_SSS_OUTPUT_SPLIT_LIGHTING         ((MATERIALFEATUREFLAGS_SSS_TRANSMISSION_START) << 0)
#define MATERIALFEATUREFLAGS_SSS_TEXTURING_MODE_OFFSET FastLog2((MATERIALFEATUREFLAGS_SSS_TRANSMISSION_START) << 1) // Note: The texture mode is 2bit, thus go from '<< 1' to '<< 3'
// Flags used as a shortcut to know if we have thin mode transmission
#define MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS  ((MATERIALFEATUREFLAGS_SSS_TRANSMISSION_START) << 3)

#ifdef MATERIAL_INCLUDE_SUBSURFACESCATTERING

void FillMaterialSSS(uint diffusionProfile, float subsurfaceMask, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfile = diffusionProfile;
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[diffusionProfile].a;
    bsdfData.subsurfaceMask = subsurfaceMask;
    bsdfData.materialFeatures |= MATERIALFEATUREFLAGS_SSS_OUTPUT_SPLIT_LIGHTING;
    bsdfData.materialFeatures |= GetSubsurfaceScatteringTexturingMode(diffusionProfile) << MATERIALFEATUREFLAGS_SSS_TEXTURING_MODE_OFFSET;
}

bool ShouldOutputSplitLighting(BSDFData bsdfData)
{
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_OUTPUT_SPLIT_LIGHTING);
}

float3 GetModifiedDiffuseColorForSSS(BSDFData bsdfData)
{
    // Subsurface scattering mode
    uint   texturingMode = (bsdfData.materialFeatures >> MATERIALFEATUREFLAGS_SSS_TEXTURING_MODE_OFFSET) & 3;
    return ApplySubsurfaceScatteringTexturingMode(texturingMode, bsdfData.diffuseColor);
}

#endif

#ifdef MATERIAL_INCLUDE_TRANSMISSION

// Assume that bsdfData.diffusionProfile is init
void FillMaterialTransmission(uint diffusionProfile, float thickness, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfile = diffusionProfile;
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[diffusionProfile].a;

    bsdfData.thickness = _ThicknessRemaps[diffusionProfile].x + _ThicknessRemaps[diffusionProfile].y * thickness;

    // The difference between the thin and the regular (a.k.a. auto-thickness) modes is the following:
    // * in the thin object mode, we assume that the geometry is thin enough for us to safely share
    // the shadowing information between the front and the back faces;
    // * the thin mode uses baked (textured) thickness for all transmission calculations;
    // * the thin mode uses wrapped diffuse lighting for the NdotL;
    // * the auto-thickness mode uses the baked (textured) thickness to compute transmission from
    // indirect lighting and non-shadow-casting lights; for shadowed lights, it calculates
    // the thickness using the distance to the closest occluder sampled from the shadow map.
    // If the distance is large, it may indicate that the closest occluder is not the back face of
    // the current object. That's not a problem, since large thickness will result in low intensity.
    bool useThinObjectMode = IsBitSet(asuint(_TransmissionFlags), diffusionProfile);

    bsdfData.materialFeatures |= useThinObjectMode ? MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THIN_THICKNESS : 0;

    // Compute transmittance using baked thickness here. It may be overridden for direct lighting
    // in the auto-thickness mode (but is always used for indirect lighting).
    bsdfData.transmittance = ComputeTransmittanceDisney(_ShapeParams[diffusionProfile].rgb,
                                                        _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                        bsdfData.thickness);
}

#endif
