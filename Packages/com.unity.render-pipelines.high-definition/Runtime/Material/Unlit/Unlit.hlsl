//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.cs.hlsl"

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData data)
{
    BSDFData output;
    output.color = data.color;

    return output;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// No light evaluation, this is unlit
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    lightTransportData.diffuseColor = float3(0.0, 0.0, 0.0);
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}
