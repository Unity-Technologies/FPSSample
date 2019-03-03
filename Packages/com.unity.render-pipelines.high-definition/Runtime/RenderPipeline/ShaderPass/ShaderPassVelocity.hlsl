#if SHADERPASS != SHADERPASS_VELOCITY
#error SHADERPASS_is_not_correctly_define
#endif

// Available semantic start from TEXCOORD4
struct AttributesPass
{
    float3 previousPositionOS : TEXCOORD4; // Contain previous transform position (in case of skinning for example)
};

struct VaryingsPassToPS
{
    // Note: Z component is not use currently
    // This is the clip space position. Warning, do not confuse with the value of positionCS in PackedVarying which is SV_POSITION and store in positionSS
    float4 positionCS;
    float4 previousPositionCS;
};

// Available interpolator start from TEXCOORD8
struct PackedVaryingsPassToPS
{
    // Note: Z component is not use
    float3 interpolators0 : TEXCOORD8;
    float3 interpolators1 : TEXCOORD9;
};

PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS input)
{
    PackedVaryingsPassToPS output;
    output.interpolators0 = float3(input.positionCS.xyw);
    output.interpolators1 = float3(input.previousPositionCS.xyw);

    return output;
}

VaryingsPassToPS UnpackVaryingsPassToPS(PackedVaryingsPassToPS input)
{
    VaryingsPassToPS output;
    output.positionCS = float4(input.interpolators0.xy, 0.0, input.interpolators0.z);
    output.previousPositionCS = float4(input.interpolators1.xy, 0.0, input.interpolators1.z);

    return output;
}

#ifdef TESSELLATION_ON

// Available interpolator start from TEXCOORD4

// Same as ToPS here
#define VaryingsPassToDS VaryingsPassToPS
#define PackedVaryingsPassToDS PackedVaryingsPassToPS
#define PackVaryingsPassToDS PackVaryingsPassToPS
#define UnpackVaryingsPassToDS UnpackVaryingsPassToPS

VaryingsPassToDS InterpolateWithBaryCoordsPassToDS(VaryingsPassToDS input0, VaryingsPassToDS input1, VaryingsPassToDS input2, float3 baryCoords)
{
    VaryingsPassToDS output;

    TESSELLATION_INTERPOLATE_BARY(positionCS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(previousPositionCS, baryCoords);

    return output;
}

#endif // TESSELLATION_ON

#ifdef TESSELLATION_ON
#define VaryingsPassType VaryingsPassToDS
#else
#define VaryingsPassType VaryingsPassToPS
#endif

// We will use custom attributes for this pass
#define VARYINGS_NEED_PASS
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

// Transforms normal from object to world space
float3 TransformPreviousObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return normalize(mul((float3x3)unity_MatrixPreviousM, normalOS));
#else
    // Normal need to be multiply by inverse transpose
    return normalize(mul(normalOS, (float3x3)unity_MatrixPreviousMI));
#endif
}

// Transforms local position to camera relative world space
float3 TransformPreviousObjectToWorld(float3 positionOS)
{
    float4x4 previousModelMatrix = ApplyCameraTranslationToMatrix(unity_MatrixPreviousM);
    return mul(previousModelMatrix, float4(positionOS, 1.0)).xyz;
}

void VelocityPositionZBias(VaryingsToPS input)
{
#if defined(UNITY_REVERSED_Z)
    input.vmesh.positionCS.z -= unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
#else
    input.vmesh.positionCS.z += unity_MotionVectorsParams.z * input.vmesh.positionCS.w;
#endif
}

PackedVaryingsType Vert(AttributesMesh inputMesh,
                        AttributesPass inputPass)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);

#if !defined(TESSELLATION_ON)
    VelocityPositionZBias(varyingsType);
#endif

    // It is not possible to correctly generate the motion vector for tesselated geometry as tessellation parameters can change
    // from one frame to another (adaptative, lod) + in Unity we only receive information for one non tesselated vertex.
    // So motion vetor will be based on interpolate previous position at vertex level instead.
    varyingsType.vpass.positionCS = mul(_NonJitteredViewProjMatrix, float4(varyingsType.vmesh.positionRWS, 1.0));

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
    {
        varyingsType.vpass.previousPositionCS = float4(0.0, 0.0, 0.0, 1.0);
    }
    else
    {
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0; // Skin or morph target

        // Need to apply any vertex animation to the previous worldspace position, if we want it to show up in the velocity buffer
#if defined(HAVE_MESH_MODIFICATION)
        AttributesMesh previousMesh = inputMesh;
        if (hasDeformation)
            previousMesh.positionOS = inputPass.previousPositionOS;
        previousMesh = ApplyMeshModification(previousMesh);
        float3 previousPositionRWS = TransformPreviousObjectToWorld(previousMesh.positionOS);
#else
        float3 previousPositionRWS = TransformPreviousObjectToWorld(hasDeformation ? inputPass.previousPositionOS : inputMesh.positionOS);
#endif

#ifdef ATTRIBUTES_NEED_NORMAL
        float3 normalWS = TransformPreviousObjectToWorldNormal(inputMesh.normalOS);
#else
        float3 normalWS = float3(0.0, 0.0, 0.0);
#endif

 #if defined(HAVE_VERTEX_MODIFICATION)
        ApplyVertexModification(inputMesh, normalWS, previousPositionRWS, _LastTime);
#endif

        varyingsType.vpass.previousPositionCS = mul(_PrevViewProjMatrix, float4(previousPositionRWS, 1.0));
    }

    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;

    output.vmesh = VertMeshTesselation(input.vmesh);

    VelocityPositionZBias(output);

    output.vpass.positionCS = input.vpass.positionCS;
    output.vpass.previousPositionCS = input.vpass.previousPositionCS;

    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

void Frag(  PackedVaryingsToPS packedInput
            // The velocity if always the first buffer
            , out float4 outVelocity : SV_Target0

            // Write the normal buffer
            #ifdef WRITE_NORMAL_BUFFER
            , out float4 outNormalBuffer : SV_Target1
                // Output the depth as a color if required
                #ifdef WRITE_MSAA_DEPTH
                , out float1 depthColor : SV_Target2
                #endif
            #endif

            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
            #endif
        )
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

    VaryingsPassToPS inputPass = UnpackVaryingsPassToPS(packedInput.vpass);
#ifdef _DEPTHOFFSET_ON
    inputPass.positionCS.w += builtinData.depthOffset;
    inputPass.previousPositionCS.w += builtinData.depthOffset;
#endif

    // TODO: How to allow overriden velocity vector from GetSurfaceAndBuiltinData ?
    float2 velocity = CalculateVelocity(inputPass.positionCS, inputPass.previousPositionCS);

    // Convert from Clip space (-1..1) to NDC 0..1 space.
    // Note it doesn't mean we don't have negative value, we store negative or positive offset in NDC space.
    // Note: ((positionCS * 0.5 + 0.5) - (previousPositionCS * 0.5 + 0.5)) = (velocity * 0.5)
    EncodeVelocity(velocity * 0.5, outVelocity);

    // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
    bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
    if (forceNoMotion)
        outVelocity = float4(0.0, 0.0, 0.0, 0.0);

// Normal Buffer Processing
#ifdef WRITE_NORMAL_BUFFER
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), posInput.positionSS, outNormalBuffer);

    #ifdef WRITE_MSAA_DEPTH
    // In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
    depthColor = packedInput.vmesh.positionCS.z;
    #endif
#endif

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif
}
