#if SHADERPASS != SHADERPASS_LIGHT_TRANSPORT
#error SHADERPASS_is_not_correctly_define
#endif

CBUFFER_START(UnityMetaPass)
// x = use uv1 as raster position
// y = use uv2 as raster position
bool4 unity_MetaVertexControl;

// x = return albedo
// y = return normal
bool4 unity_MetaFragmentControl;
CBUFFER_END


// This was not in constant buffer in original unity, so keep outiside. But should be in as ShaderRenderPass frequency
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

#include "VertMesh.hlsl"

PackedVaryingsToPS Vert(AttributesMesh inputMesh)
{
    VaryingsToPS output;

    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_TRANSFER_INSTANCE_ID(inputMesh, output.vmesh);

    // Output UV coordinate in vertex shader
    float2 uv;

    if (unity_MetaVertexControl.x)
    {
        uv = inputMesh.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
    }
    else if (unity_MetaVertexControl.y)
    {
        uv = inputMesh.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    }

    // OpenGL right now needs to actually use the incoming vertex position
    // so we create a fake dependency on it here that haven't any impact.
    output.vmesh.positionCS = float4(uv * 2.0 - 1.0, inputMesh.positionOS.z > 0 ? 1.0e-4 : 0.0, 1.0);

#ifdef VARYINGS_NEED_POSITION_WS
    output.vmesh.positionRWS = TransformObjectToWorld(inputMesh.positionOS);
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    // Normal is required for triplanar mapping
    output.vmesh.normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
    // Not required but assign to silent compiler warning
    output.vmesh.tangentWS = float4(1.0, 0.0, 0.0, 0.0);
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.vmesh.texCoord0 = inputMesh.uv0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.vmesh.texCoord1 = inputMesh.uv1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.vmesh.texCoord2 = inputMesh.uv2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.vmesh.texCoord3 = inputMesh.uv3;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.vmesh.color = inputMesh.color;
#endif

    return PackVaryingsToPS(output);
}

float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
{
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // no debug apply during light transport pass

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
    LightTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);

    // This shader is call two times. Once for getting emissiveColor, the other time to get diffuseColor
    // We use unity_MetaFragmentControl to make the distinction.
    float4 res = float4(0.0, 0.0, 0.0, 1.0);

    if (unity_MetaFragmentControl.x)
    {
        // Apply diffuseColor Boost from LightmapSettings.
        // put abs here to silent a warning, no cost, no impact as color is assume to be positive.
        res.rgb = clamp(pow(abs(lightTransportData.diffuseColor), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }

    if (unity_MetaFragmentControl.y)
    {
        // emissive use HDR format
        res.rgb = lightTransportData.emissiveColor;
    }

    return res;
}
