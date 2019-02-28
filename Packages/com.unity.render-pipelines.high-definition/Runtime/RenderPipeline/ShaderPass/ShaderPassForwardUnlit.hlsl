#if SHADERPASS != SHADERPASS_FORWARD_UNLIT
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

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

    // Not lit here (but emissive is allowed)
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    // TODO: we must not access bsdfData here, it break the genericity of the code!
    float4 outColor = ApplyBlendMode(bsdfData.color + builtinData.emissiveColor, builtinData.opacity);
    outColor = EvaluateAtmosphericScattering(posInput, V, outColor);

#ifdef DEBUG_DISPLAY
    // Same code in ShaderPassForward.shader
    if (_DebugViewMaterial != 0)
    {
        float3 result = float3(1.0, 0.0, 1.0);
        bool needLinearToSRGB = false;

        GetPropertiesDataDebug(_DebugViewMaterial, result, needLinearToSRGB);
        GetVaryingsDataDebug(_DebugViewMaterial, input, result, needLinearToSRGB);
        GetBuiltinDataDebug(_DebugViewMaterial, builtinData, result, needLinearToSRGB);
        GetSurfaceDataDebug(_DebugViewMaterial, surfaceData, result, needLinearToSRGB);
        GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB);

        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate.
        if (!needLinearToSRGB)
            result = SRGBToLinear(max(0, result));

        outColor = float4(result, 1.0);
    }
#endif

    return outColor;
}
