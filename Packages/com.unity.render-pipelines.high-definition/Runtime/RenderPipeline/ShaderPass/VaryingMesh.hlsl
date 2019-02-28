struct AttributesMesh
{
    float3 positionOS   : POSITION;
#ifdef ATTRIBUTES_NEED_NORMAL
    float3 normalOS     : NORMAL;
#endif
#ifdef ATTRIBUTES_NEED_TANGENT
    float4 tangentOS    : TANGENT; // Store sign in w
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD0
    float2 uv0          : TEXCOORD0;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD1
    float2 uv1          : TEXCOORD1;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD2
    float2 uv2          : TEXCOORD2;
#endif
#ifdef ATTRIBUTES_NEED_TEXCOORD3
    float2 uv3          : TEXCOORD3;
#endif
#ifdef ATTRIBUTES_NEED_COLOR
    float4 color        : COLOR;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsMeshToPS
{
    float4 positionCS;
#ifdef VARYINGS_NEED_POSITION_WS
    float3 positionRWS;
#endif
#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float3 normalWS;
    float4 tangentWS;  // w contain mirror sign
#endif
#ifdef VARYINGS_NEED_TEXCOORD0
    float2 texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYINGS_NEED_COLOR
    float4 color;
#endif

UNITY_VERTEX_INPUT_INSTANCE_ID

};

struct PackedVaryingsMeshToPS
{
    float4 positionCS : SV_Position;

#ifdef VARYINGS_NEED_POSITION_WS
    float3 interpolators0 : TEXCOORD0;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float3 interpolators1 : TEXCOORD1;
    float4 interpolators2 : TEXCOORD2;
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYINGS_NEED_TEXCOORD1
    float4 interpolators3 : TEXCOORD3;
#elif defined(VARYINGS_NEED_TEXCOORD0)
    float2 interpolators3 : TEXCOORD3;
#endif

#ifdef VARYINGS_NEED_TEXCOORD3
    float4 interpolators4 : TEXCOORD4;
#elif defined(VARYINGS_NEED_TEXCOORD2)
    float2 interpolators4 : TEXCOORD4;
#endif

#ifdef VARYINGS_NEED_COLOR
    float4 interpolators5 : TEXCOORD5;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID // Must be declare before FRONT_FACE_SEMANTIC

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
#endif
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsMeshToPS PackVaryingsMeshToPS(VaryingsMeshToPS input)
{
    PackedVaryingsMeshToPS output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionCS = input.positionCS;

#ifdef VARYINGS_NEED_POSITION_WS
    output.interpolators0 = input.positionRWS;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.interpolators1 = input.normalWS;
    output.interpolators2 = input.tangentWS;
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.interpolators3.xy = input.texCoord0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.interpolators3.zw = input.texCoord1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.interpolators4.xy = input.texCoord2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.interpolators4.zw = input.texCoord3;
#endif

#ifdef VARYINGS_NEED_COLOR
    output.interpolators5 = input.color;
#endif

    return output;
}

FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
    FragInputs output;
    ZERO_INITIALIZE(FragInputs, output);

    UNITY_SETUP_INSTANCE_ID(input);

    // Init to some default value to make the computer quiet (else it output "divide by zero" warning even if value is not used).
    // TODO: this is a really poor workaround, but the variable is used in a bunch of places
    // to compute normals which are then passed on elsewhere to compute other values...
    output.worldToTangent = k_identity3x3;

    output.positionSS = input.positionCS; // input.positionCS is SV_Position

#ifdef VARYINGS_NEED_POSITION_WS
    output.positionRWS.xyz = input.interpolators0.xyz;
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    float4 tangentWS = float4(input.interpolators2.xyz, input.interpolators2.w > 0.0 ? 1.0 : -1.0); // must not be normalized (mikkts requirement)
    output.worldToTangent = BuildWorldToTangent(tangentWS, input.interpolators1.xyz);
#endif // VARYINGS_NEED_TANGENT_TO_WORLD

#ifdef VARYINGS_NEED_TEXCOORD0
    output.texCoord0.xy = input.interpolators3.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.texCoord1.xy = input.interpolators3.zw;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.texCoord2.xy = input.interpolators4.xy;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.texCoord3.xy = input.interpolators4.zw;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.color = input.interpolators5;
#endif

#if defined(VARYINGS_NEED_CULLFACE) && SHADER_STAGE_FRAGMENT
    output.isFrontFace = IS_FRONT_VFACE(input.cullFace, true, false);
#endif

    return output;
}

#ifdef TESSELLATION_ON

// Varying DS - use for domain shader
// We can deduce these defines from the other defines
// We need to pass to DS any varying required by pixel shader
// If we have required an attributes that is not present in varyings it mean we will be for DS
#if defined(VARYINGS_NEED_TANGENT_TO_WORLD) || defined(ATTRIBUTES_NEED_TANGENT)
#define VARYINGS_DS_NEED_TANGENT
#endif
#if defined(VARYINGS_NEED_TEXCOORD0) || defined(ATTRIBUTES_NEED_TEXCOORD0)
#define VARYINGS_DS_NEED_TEXCOORD0
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(ATTRIBUTES_NEED_TEXCOORD1)
#define VARYINGS_DS_NEED_TEXCOORD1
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(ATTRIBUTES_NEED_TEXCOORD2)
#define VARYINGS_DS_NEED_TEXCOORD2
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(ATTRIBUTES_NEED_TEXCOORD3)
#define VARYINGS_DS_NEED_TEXCOORD3
#endif
#if defined(VARYINGS_NEED_COLOR) || defined(ATTRIBUTES_NEED_COLOR)
#define VARYINGS_DS_NEED_COLOR
#endif

// Varying for domain shader
// Position and normal are always present (for tessellation) and in world space
struct VaryingsMeshToDS
{
    float3 positionRWS;
    float3 normalWS;
#ifdef VARYINGS_DS_NEED_TANGENT
    float4 tangentWS;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    float2 texCoord0;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    float2 texCoord1;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    float2 texCoord2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    float2 texCoord3;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    float4 color;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PackedVaryingsMeshToDS
{
    float3 interpolators0 : INTERNALTESSPOS; // positionRWS
    float3 interpolators1 : NORMAL; // NormalWS

#ifdef VARYINGS_DS_NEED_TANGENT
    float4 interpolators2 : TANGENT;
#endif

    // Allocate only necessary space if shader compiler in the future are able to automatically pack
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    float4 interpolators3 : TEXCOORD0;
#elif defined(VARYINGS_DS_NEED_TEXCOORD0)
    float2 interpolators3 : TEXCOORD0;
#endif

#ifdef VARYINGS_DS_NEED_TEXCOORD3
    float4 interpolators4 : TEXCOORD1;
#elif defined(VARYINGS_DS_NEED_TEXCOORD2)
    float2 interpolators4 : TEXCOORD1;
#endif

#ifdef VARYINGS_DS_NEED_COLOR
    float4 interpolators5 : TEXCOORD2;
#endif

     UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Functions to pack data to use as few interpolator as possible, the ShaderGraph should generate these functions
PackedVaryingsMeshToDS PackVaryingsMeshToDS(VaryingsMeshToDS input)
{
    PackedVaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.interpolators0 = input.positionRWS;
    output.interpolators1 = input.normalWS;
#ifdef VARYINGS_DS_NEED_TANGENT
    output.interpolators2 = input.tangentWS;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    output.interpolators3.xy = input.texCoord0;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    output.interpolators3.zw = input.texCoord1;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    output.interpolators4.xy = input.texCoord2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    output.interpolators4.zw = input.texCoord3;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    output.interpolators5 = input.color;
#endif

    return output;
}

VaryingsMeshToDS UnpackVaryingsMeshToDS(PackedVaryingsMeshToDS input)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);

    output.positionRWS = input.interpolators0;
    output.normalWS = input.interpolators1;
#ifdef VARYINGS_DS_NEED_TANGENT
    output.tangentWS = input.interpolators2;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    output.texCoord0 = input.interpolators3.xy;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    output.texCoord1 = input.interpolators3.zw;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    output.texCoord2 = input.interpolators4.xy;
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    output.texCoord3 = input.interpolators4.zw;
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    output.color = input.interpolators5;
#endif

    return output;
}

VaryingsMeshToDS InterpolateWithBaryCoordsMeshToDS(VaryingsMeshToDS input0, VaryingsMeshToDS input1, VaryingsMeshToDS input2, float3 baryCoords)
{
    VaryingsMeshToDS output;

    UNITY_TRANSFER_INSTANCE_ID(input0, output);

    TESSELLATION_INTERPOLATE_BARY(positionRWS, baryCoords);
    TESSELLATION_INTERPOLATE_BARY(normalWS, baryCoords);
#ifdef VARYINGS_DS_NEED_TANGENT
    // This will interpolate the sign but should be ok in practice as we may expect a triangle to have same sign (? TO CHECK)
    TESSELLATION_INTERPOLATE_BARY(tangentWS, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD0
    TESSELLATION_INTERPOLATE_BARY(texCoord0, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD1
    TESSELLATION_INTERPOLATE_BARY(texCoord1, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD2
    TESSELLATION_INTERPOLATE_BARY(texCoord2, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_TEXCOORD3
    TESSELLATION_INTERPOLATE_BARY(texCoord3, baryCoords);
#endif
#ifdef VARYINGS_DS_NEED_COLOR
    TESSELLATION_INTERPOLATE_BARY(color, baryCoords);
#endif

    return output;
}

#endif // TESSELLATION_ON
