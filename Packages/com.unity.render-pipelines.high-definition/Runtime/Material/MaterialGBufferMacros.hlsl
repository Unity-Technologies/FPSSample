#ifndef UNITY_MATERIAL_GBUFFER_MACROS_INCLUDED
#define UNITY_MATERIAL_GBUFFER_MACROS_INCLUDED

//-----------------------------------------------------------------------------
// Define for GBuffer management. Can be optionally included after material HLSL headers
//-----------------------------------------------------------------------------

// Only one deferred layout is allowed for a HDRenderPipeline, this will be detect by the redefinition of GBUFFERMATERIAL_COUNT
// If GBUFFERMATERIAL_COUNT is define two time, the shaders will not compile

#ifdef GBUFFERMATERIAL_COUNT

#if GBUFFERMATERIAL_COUNT == 2

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))

#elif GBUFFERMATERIAL_COUNT == 3

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))

#elif GBUFFERMATERIAL_COUNT == 4

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))

#elif GBUFFERMATERIAL_COUNT == 5

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))

#elif GBUFFERMATERIAL_COUNT == 6

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5))

#elif GBUFFERMATERIAL_COUNT == 7

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5,    \
        out GBufferType6 MERGE_NAME(NAME, 6) : SV_Target6

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5), MERGE_NAME(NAME, 6))

#elif GBUFFERMATERIAL_COUNT == 8

#define OUTPUT_GBUFFER(NAME)                            \
        out GBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out GBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out GBufferType2 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out GBufferType3 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out GBufferType4 MERGE_NAME(NAME, 4) : SV_Target4,    \
        out GBufferType5 MERGE_NAME(NAME, 5) : SV_Target5,    \
        out GBufferType6 MERGE_NAME(NAME, 6) : SV_Target6,    \
        out GBufferType7 MERGE_NAME(NAME, 7) : SV_Target7

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, NAME) EncodeIntoGBuffer(SURFACE_DATA, BUILTIN_DATA, UNPOSITIONSS, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4), MERGE_NAME(NAME, 5), MERGE_NAME(NAME, 6), MERGE_NAME(NAME, 7))

#endif

#define DECODE_FROM_GBUFFER(UNPOSITIONSS, FEATURE_FLAGS, BSDF_DATA, BUILTIN_DATA) DecodeFromGBuffer(UNPOSITIONSS, FEATURE_FLAGS, BSDF_DATA, BUILTIN_DATA)
#define MATERIAL_FEATURE_FLAGS_FROM_GBUFFER(UNPOSITIONSS) MaterialFeatureFlagsFromGBuffer(UNPOSITIONSS)

#endif // #ifdef GBUFFERMATERIAL_COUNT

#endif // UNITY_MATERIAL_GBUFFER_MACROS_INCLUDED
