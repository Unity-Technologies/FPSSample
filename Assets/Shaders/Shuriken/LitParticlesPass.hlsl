#ifndef LIT_PARTICLE_PASS
#define LIT_PARTICLE_PASS

#if _REQUIRE_UV2
#define _FLIPBOOK_BLENDING 1
#endif

#if EFFECT_BUMP
#define _DISTORTION_ON 1
#endif

#include "LitParticleInstancing.hlsl"

// Vertex shader input
struct appdata_particles
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color : COLOR;
    #if defined(_FLIPBOOK_BLENDING) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    float4 texcoords : TEXCOORD0;
    float texcoordBlend : TEXCOORD1;
    #else
    float2 texcoords : TEXCOORD0;
    #endif
    #if defined(_NORMALMAP)
    float4 tangent : TANGENT;
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Non-surface shader v2f structure
struct VertexOutput
{
    float4 vertex : SV_Position;
    float3 worldPosition : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;

    float2 texcoord : TEXCOORD3;
    #if defined(_FLIPBOOK_BLENDING)
    float3 texcoord2AndBlend : TEXCOORD4;
    #endif
    #if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    float4 projectedPosition : TEXCOORD5;
    #endif
    float4 color : TEXCOORD6;
    UNITY_VERTEX_OUTPUT_STEREO

};
/*
fixed4 readTexture(sampler2D tex, Input IN)
{
    fixed4 color = tex2D (tex, IN.texcoord);
    #ifdef _FLIPBOOK_BLENDING
    fixed4 color2 = tex2D(tex, IN.texcoord2AndBlend.xy);
    color = lerp(color, color2, IN.texcoord2AndBlend.z);
    #endif
    return color;
}

fixed4 readTexture(sampler2D tex, VertexOutput IN)
{
    fixed4 color = tex2D (tex, IN.texcoord);
    #ifdef _FLIPBOOK_BLENDING
    fixed4 color2 = tex2D(tex, IN.texcoord2AndBlend.xy);
    color = lerp(color, color2, IN.texcoord2AndBlend.z);
    #endif
    return color;
}
*/

float4 _SoftParticleFadeParams;
float4 _CameraFadeParams;

#define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x
#define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y

#define CAMERA_NEAR_FADE _CameraFadeParams.x
#define CAMERA_INV_FADE_DISTANCE _CameraFadeParams.y


#if defined (_COLORADDSUBDIFF_ON)
half4 _ColorAddSubDiff;
#endif

#if defined(_COLORCOLOR_ON)
half3 RGBtoHSV(half3 arg1)
{
    half4 K = half4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    half4 P = lerp(half4(arg1.bg, K.wz), half4(arg1.gb, K.xy), step(arg1.b, arg1.g));
    half4 Q = lerp(half4(P.xyw, arg1.r), half4(arg1.r, P.yzx), step(P.x, arg1.r));
    half D = Q.x - min(Q.w, Q.y);
    half E = 1e-10;
    return half3(abs(Q.z + (Q.w - Q.y) / (6.0 * D + E)), D / (Q.x + E), Q.x);
}

half3 HSVtoRGB(half3 arg1)
{
    half4 K = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    half3 P = abs(frac(arg1.xxx + K.xyz) * 6.0 - K.www);
    return arg1.z * lerp(K.xxx, saturate(P - K.xxx), arg1.y);
}
#endif

// Color function
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
#define vertColor(c) \
        vertInstancingColor(c);
#else
#define vertColor(c)
#endif

// Flipbook vertex function
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    #if defined(_FLIPBOOK_BLENDING)
    #define vertTexcoord(v, o) \
        vertInstancingUVs(v.texcoords.xy, o.texcoord, o.texcoord2AndBlend);
    #else
    #define vertTexcoord(v, o) \
        vertInstancingUVs(v.texcoords.xy, o.texcoord); \
        o.texcoord = o.texcoord;
    #endif
#else
    #if defined(_FLIPBOOK_BLENDING)
    #define vertTexcoord(v, o) \
        o.texcoord = v.texcoords.xy; \
        o.texcoord2AndBlend.xy = v.texcoords.zw; \
        o.texcoord2AndBlend.z = v.texcoordBlend;
    #else
    #define vertTexcoord(v, o) \
        o.texcoord = v.texcoords.xy;
    #endif
#endif

// Fading vertex function
#if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
#define vertFading(o) \
    o.projectedPosition = ComputeScreenPos (clipPosition); \
    COMPUTE_EYEDEPTH(o.projectedPosition.z);
#else
#define vertFading(o)
#endif

// Color blending fragment function
#if defined(_COLOROVERLAY_ON)
#define fragColorMode(i) \
    albedo.rgb = lerp(1 - 2 * (1 - albedo.rgb) * (1 - i.color.rgb), 2 * albedo.rgb * i.color.rgb, step(albedo.rgb, 0.5)); \
    albedo.a *= i.color.a;
#elif defined(_COLORCOLOR_ON)
#define fragColorMode(i) \
    half3 aHSL = RGBtoHSV(albedo.rgb); \
    half3 bHSL = RGBtoHSV(i.color.rgb); \
    half3 rHSL = fixed3(bHSL.x, bHSL.y, aHSL.z); \
    albedo = fixed4(HSVtoRGB(rHSL), albedo.a * i.color.a);
#elif defined(_COLORADDSUBDIFF_ON)
#define fragColorMode(i) \
    albedo.rgb = albedo.rgb + i.color.rgb * _ColorAddSubDiff.x; \
    albedo.rgb = lerp(albedo.rgb, abs(albedo.rgb), _ColorAddSubDiff.y); \
    albedo.a *= i.color.a;
#else
#define fragColorMode(i) \
    albedo *= i.color;
#endif

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
#define ALBEDO_MUL albedo
#else
#define ALBEDO_MUL albedo.a
#endif

// Soft particles fragment function
#if defined(SOFTPARTICLES_ON) && defined(_FADING_ON)
#define fragSoftParticles(i) \
    if (SOFT_PARTICLE_NEAR_FADE > 0.0 || SOFT_PARTICLE_INV_FADE_DISTANCE > 0.0) \
    { \
        float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projectedPosition))); \
        float fade = saturate (SOFT_PARTICLE_INV_FADE_DISTANCE * ((sceneZ - SOFT_PARTICLE_NEAR_FADE) - i.projectedPosition.z)); \
        ALBEDO_MUL *= fade; \
    }
#else
#define fragSoftParticles(i)
#endif

// Camera fading fragment function
#if defined(_FADING_ON)
#define fragCameraFading(i) \
    float cameraFade = saturate((i.projectedPosition.z - CAMERA_NEAR_FADE) * CAMERA_INV_FADE_DISTANCE); \
    ALBEDO_MUL *= cameraFade;
#else
#define fragCameraFading(i)
#endif


void SetupWorldToTangent(inout FragInputs fragInputs, VertexOutput input)
{
    float4 tangentWS = float4(input.tangentWS.xyz, input.tangentWS.w > 0.0 ? 1.0 : -1.0);	// must not be normalized (mikkts requirement)

    // Normalize normalWS vector but keep the renormFactor to apply it to bitangent and tangent
	float3 unnormalizedNormalWS = input.normalWS.xyz;
    float renormFactor = 1.0 / length(unnormalizedNormalWS);

    // bitangent on the fly option in xnormal to reduce vertex shader outputs.
	// this is the mikktspace transformation (must use unnormalized attributes)
    float3x3 worldToTangent = CreateWorldToTangent(unnormalizedNormalWS, tangentWS.xyz, tangentWS.w);

	// surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
	// by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
    fragInputs.worldToTangent[0] = worldToTangent[0] * renormFactor;
    fragInputs.worldToTangent[1] = worldToTangent[1] * renormFactor;
    fragInputs.worldToTangent[2] = worldToTangent[2] * renormFactor;		// normalizes the interpolated vertex normal
}


VertexOutput Vert (in appdata_particles v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutput o;

#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    vertInstancingSetup();
#endif

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
    positionWS = GetCameraRelativePositionWS(v.vertex.xyz);

    float4 clipPosition = TransformWorldToHClip(positionWS);

    o.vertex = clipPosition;    
    o.worldPosition = positionWS;

    o.normalWS = TransformObjectToWorldNormal(v.normal);

    #if defined(_NORMALMAP)
    o.tangentWS = float4(TransformObjectToWorldDir(v.tangent.xyz), v.tangent.w);
    #endif

    o.texcoord = float2(0.0, 0.0);
    #if defined(_FLIPBOOK_BLENDING)
    o.texcoord2AndBlend = float3(0.0, 0.0, 0.0);
    #endif
    #if defined(SOFTPARTICLES_ON) || defined(_FADING_ON)
    o.projectedPosition = float4(0.0, 0.0, 0.0, 0.0);
    #endif
    o.color = v.color;

   // vertColor(o.color);
    vertTexcoord(v, o);
    vertFading(o);

    return o;
}

#if SHADERPASS != SHADERPASS_DISTORTION

void Frag(VertexOutput input,
          FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC,
        #ifdef OUTPUT_SPLIT_LIGHTING
            out float4 outColor : SV_Target0,  // outSpecularLighting
            out float4 outDiffuseLighting : SV_Target1,
            OUTPUT_SSSBUFFER(outSSSBuffer)
        #else
            out float4 outColor : SV_Target0
        #endif
        #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : SV_Depth
        #endif
          )
{
    FragInputs fragInput;

    fragInput.positionSS = input.vertex;
    fragInput.positionRWS = GetCameraRelativePositionWS(input.worldPosition);
    fragInput.texCoord0 = float4(input.texcoord,0,0);
    fragInput.texCoord1 = float4(input.texcoord,0,0);
    fragInput.texCoord2 = float4(input.texcoord,0,0);
    fragInput.texCoord3 = float4(input.texcoord,0,0);
    fragInput.color = input.color;
    fragInput.isFrontFace = IS_FRONT_VFACE(cullFace, true, false);

    SetupWorldToTangent(fragInput, input);

    // input.positionSS is SV_Position
    #if SHADERPASS == SHADERPASS_FORWARD
    PositionInputs posInput = GetPositionInput(input.vertex.xy, _ScreenSize.zw, input.vertex.z, input.vertex.w, fragInput.positionRWS.xyz, uint2(input.vertex.xy) / GetTileSize());
    #else
    PositionInputs posInput = GetPositionInput(input.vertex.xy, _ScreenSize.zw, input.vertex.z, input.vertex.w, input.worldPosition.xyz);
    #endif

    float3 V = GetWorldSpaceNormalizeViewDir(fragInput.positionRWS);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(fragInput, V, posInput, surfaceData, builtinData);

    outColor = float4(0.0, 0.0, 0.0, 0.0);

#if SHADERPASS != SHADERPASS_DEPTH_ONLY
    half4 albedo = half4(surfaceData.baseColor, builtinData.opacity);
    albedo *= input.color;
    fragColorMode(input);
    fragSoftParticles(input);
    fragCameraFading(input);

    surfaceData.baseColor = albedo.rgb;
    builtinData.opacity = albedo.a;

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    // We need to skip lighting when doing debug pass because the debug pass is done before lighting so some buffers may not be properly initialized potentially causing crashes on PS4.	  
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
        uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;
#else
        uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
#endif

        float3 diffuseLighting;
        float3 specularLighting;

#if LIT_PARTICLE_FULLLIGHTING
        LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, diffuseLighting, specularLighting);
#else
        specularLighting = float3(0.0, 0.0, 0.0);
        diffuseLighting = builtinData.bakeDiffuseLighting;
#endif

#ifdef OUTPUT_SPLIT_LIGHTING
        if (_EnableSubsurfaceScattering != 0 && ShouldOutputSplitLighting(bsdfData))
        {
            outColor = float4(specularLighting, 1.0);
            outDiffuseLighting = float4(TagLightingForSSS(diffuseLighting), 1.0);
        }
        else
        {
            outColor = float4(diffuseLighting + specularLighting, 1.0);
            outDiffuseLighting = 0;
        }
        ENCODE_INTO_SSSBUFFER(surfaceData, posInput.positionSS, outSSSBuffer);
#else
        outColor = ApplyBlendMode(diffuseLighting, specularLighting, builtinData.opacity);
        outColor = EvaluateAtmosphericScattering(posInput, outColor);
#endif
    }
#endif

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif

}

#else
float4 Frag(VertexOutput input, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
{
    PositionInputs posInput = GetPositionInput(input.vertex.xy, _ScreenSize.zw, input.vertex.z, input.vertex.w, input.worldPosition.xyz);

    float3 V = GetWorldSpaceNormalizeViewDir(input.worldPosition);

    FragInputs fragInput;

    fragInput.positionSS = input.vertex;
    fragInput.positionRWS = GetCameraRelativePositionWS(input.worldPosition);
    fragInput.texCoord0 = float4(input.texcoord,0,0);
    fragInput.texCoord1 = float4(input.texcoord,0,0);
    fragInput.texCoord2 = float4(input.texcoord,0,0);
    fragInput.texCoord3 = float4(input.texcoord,0,0);
    fragInput.color = input.color;
    fragInput.isFrontFace = IS_FRONT_VFACE(cullFace, true, false);
    
    SetupWorldToTangent(fragInput, input);

    // Perform alpha testing + get distortion
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(fragInput, V, posInput, surfaceData, builtinData);

    float4 outBuffer;
    // Mark this pixel as eligible as source for distortion
    EncodeDistortion(builtinData.distortion, builtinData.distortionBlur, true, outBuffer);
    return outBuffer;
}
#endif

#endif // UNITY_STANDARD_PARTICLES_INCLUDED
