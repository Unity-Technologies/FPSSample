#ifndef UNITY_INSTANCING_INCLUDED
#define UNITY_INSTANCING_INCLUDED

#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_XBOXONE) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_METAL))
    #define UNITY_SUPPORT_INSTANCING
#endif

#if defined(SHADER_API_SWITCH)
    #define UNITY_SUPPORT_INSTANCING
#endif

#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3)
    #define UNITY_SUPPORT_STEREO_INSTANCING
#endif

// These platforms support dynamically adjusting the instancing CB size according to the current batch.
#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES3) || defined(SHADER_API_METAL) || defined(SHADER_API_PSSL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_SWITCH)
    #define UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE
#endif

#if defined(SHADER_TARGET_SURFACE_ANALYSIS) && defined(UNITY_SUPPORT_INSTANCING)
    #undef UNITY_SUPPORT_INSTANCING
#endif

////////////////////////////////////////////////////////
// instancing paths
// - UNITY_INSTANCING_ENABLED               Defined if instancing path is taken.
// - UNITY_PROCEDURAL_INSTANCING_ENABLED    Defined if procedural instancing path is taken.
// - UNITY_STEREO_INSTANCING_ENABLED        Defined if stereo instancing path is taken.
#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)
    #define UNITY_INSTANCING_ENABLED
#endif
#if defined(UNITY_SUPPORT_INSTANCING) && defined(PROCEDURAL_INSTANCING_ON)
    #define UNITY_PROCEDURAL_INSTANCING_ENABLED
#endif
#if defined(UNITY_SUPPORT_STEREO_INSTANCING) && defined(STEREO_INSTANCING_ON)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN)
    // These platforms have constant buffers disabled normally, but not here (see CBUFFER_START/CBUFFER_END in HLSLSupport.cginc).
    #define UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)  cbuffer name {
    #define UNITY_INSTANCING_CBUFFER_SCOPE_END          }
#else
    #define UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)  CBUFFER_START(name)
    #define UNITY_INSTANCING_CBUFFER_SCOPE_END          CBUFFER_END
#endif

////////////////////////////////////////////////////////
// basic instancing setups
// - UNITY_VERTEX_INPUT_INSTANCE_ID     Declare instance ID field in vertex shader input / output struct.
// - UNITY_GET_INSTANCE_ID              (Internal) Get the instance ID from input struct.
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)

    // A global instance ID variable that functions can directly access.
    static uint unity_InstanceID;

    // Don't make UnityDrawCallInfo an actual CB on GL
    #if !defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)
        UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(UnityDrawCallInfo)
    #endif
            int unity_BaseInstanceID;
            int unity_InstanceCount;
    #if !defined(SHADER_API_GLES3) && !defined(SHADER_API_GLCORE)
        UNITY_INSTANCING_CBUFFER_SCOPE_END
    #endif

    #ifdef SHADER_API_PSSL
        #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID;
        #define UNITY_GET_INSTANCE_ID(input)    _GETINSTANCEID(input)
    #else
        #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
        #define UNITY_GET_INSTANCE_ID(input)    input.instanceID
    #endif

#else
    #define DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif // UNITY_INSTANCING_ENABLED || UNITY_PROCEDURAL_INSTANCING_ENABLED || UNITY_STEREO_INSTANCING_ENABLED

#if !defined(UNITY_VERTEX_INPUT_INSTANCE_ID)
#   define UNITY_VERTEX_INPUT_INSTANCE_ID DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID
#endif

////////////////////////////////////////////////////////
// basic stereo instancing setups
// - UNITY_VERTEX_OUTPUT_STEREO             Declare stereo target eye field in vertex shader output struct.
// - UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO  Assign the stereo target eye.
// - UNITY_TRANSFER_VERTEX_OUTPUT_STEREO    Copy stero target from input struct to output struct. Used in vertex shader.
// - UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
#ifdef UNITY_STEREO_INSTANCING_ENABLED
#if defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          uint stereoTargetEyeIndexSV : SV_RenderTargetArrayIndex; uint stereoTargetEyeIndex : BLENDINDICES0;
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)       output.stereoTargetEyeIndexSV = unity_StereoEyeIndex; output.stereoTargetEyeIndex = unity_StereoEyeIndex;
#else
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO                          uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)       output.stereoTargetEyeIndex = unity_StereoEyeIndex
#endif
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)  output.stereoTargetEyeIndex = input.stereoTargetEyeIndex;
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex = input.stereoTargetEyeIndex;
#elif defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO float stereoTargetEyeIndex : BLENDWEIGHT0;
    // HACK: Workaround for Mali shader compiler issues with directly using GL_ViewID_OVR (GL_OVR_multiview). This array just contains the values 0 and 1.
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output) output.stereoTargetEyeIndex = unity_StereoEyeIndices[unity_StereoEyeIndex].x;
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output) output.stereoTargetEyeIndex = input.stereoTargetEyeIndex;
    #if defined(SHADER_STAGE_VERTEX)
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
    #else
        #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) unity_StereoEyeIndex = (uint) input.stereoTargetEyeIndex;
    #endif
#else
    #define DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
    #define DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
    #define DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)
    #define DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif


#if !defined(UNITY_VERTEX_OUTPUT_STEREO)
#   define UNITY_VERTEX_OUTPUT_STEREO                           DEFAULT_UNITY_VERTEX_OUTPUT_STEREO
#endif
#if !defined(UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO)
#   define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)        DEFAULT_UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output)
#endif
#if !defined(UNITY_TRANSFER_VERTEX_OUTPUT_STEREO)
#   define UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)   DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(input, output)
#endif
#if !defined(UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX)
#   define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)      DEFAULT_UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
#endif

////////////////////////////////////////////////////////
// - UNITY_SETUP_INSTANCE_ID        Should be used at the very beginning of the vertex shader / fragment shader,
//                                  so that succeeding code can have access to the global unity_InstanceID.
//                                  Also procedural function is called to setup instance data.
// - UNITY_TRANSFER_INSTANCE_ID     Copy instance ID from input struct to output struct. Used in vertex shader.

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
    void UnitySetupInstanceID(uint inputInstanceID)
    {
        #ifdef UNITY_STEREO_INSTANCING_ENABLED
            #if defined(SHADER_API_GLES3)
                // We must calculate the stereo eye index differently for GLES3
                // because otherwise,  the unity shader compiler will emit a bitfieldInsert function.
                // bitfieldInsert requires support for glsl version 400 or later.  Therefore the
                // generated glsl code will fail to compile on lower end devices.  By changing the
                // way we calculate the stereo eye index,  we can help the shader compiler to avoid
                // emitting the bitfieldInsert function and thereby increase the number of devices we
                // can run stereo instancing on.
                unity_StereoEyeIndex = round(fmod(inputInstanceID, 2.0));
                unity_InstanceID = unity_BaseInstanceID + (inputInstanceID >> 1);
            #else
                // stereo eye index is automatically figured out from the instance ID
                unity_StereoEyeIndex = inputInstanceID & 0x01;
                unity_InstanceID = unity_BaseInstanceID + (inputInstanceID >> 1);
            #endif
        #else
            unity_InstanceID = inputInstanceID + unity_BaseInstanceID;
        #endif
    }

    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        #ifndef UNITY_INSTANCING_PROCEDURAL_FUNC
            #error "UNITY_INSTANCING_PROCEDURAL_FUNC must be defined."
        #else
            void UNITY_INSTANCING_PROCEDURAL_FUNC(); // forward declaration of the procedural function
            #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)      { UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input)); UNITY_INSTANCING_PROCEDURAL_FUNC();}
        #endif
    #else
        #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)          { UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(input));}
    #endif
    #define UNITY_TRANSFER_INSTANCE_ID(input, output)   output.instanceID = UNITY_GET_INSTANCE_ID(input)
#else
    #define DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
    #define UNITY_TRANSFER_INSTANCE_ID(input, output)
#endif

#if !defined(UNITY_SETUP_INSTANCE_ID)
#   define UNITY_SETUP_INSTANCE_ID(input) DEFAULT_UNITY_SETUP_INSTANCE_ID(input)
#endif

////////////////////////////////////////////////////////
// instanced property arrays
#if defined(UNITY_INSTANCING_ENABLED)

    #ifdef UNITY_FORCE_MAX_INSTANCE_COUNT
        #define UNITY_INSTANCED_ARRAY_SIZE  UNITY_FORCE_MAX_INSTANCE_COUNT
    #elif defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
        #define UNITY_INSTANCED_ARRAY_SIZE  2 // minimum array size that ensures dynamic indexing
    #elif defined(UNITY_MAX_INSTANCE_COUNT)
        #define UNITY_INSTANCED_ARRAY_SIZE  UNITY_MAX_INSTANCE_COUNT
    #else
        #if defined(SHADER_API_VULKAN) && defined(SHADER_API_MOBILE)
            #define UNITY_INSTANCED_ARRAY_SIZE  250
        #else
            #define UNITY_INSTANCED_ARRAY_SIZE  500
        #endif
    #endif

    #define UNITY_INSTANCING_BUFFER_START(buf)      UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(UnityInstancing_##buf) struct {
    #define UNITY_INSTANCING_BUFFER_END(arr)        } arr##Array[UNITY_INSTANCED_ARRAY_SIZE]; UNITY_INSTANCING_CBUFFER_SCOPE_END
    #define UNITY_DEFINE_INSTANCED_PROP(type, var)  type var;
    #define UNITY_ACCESS_INSTANCED_PROP(arr, var)   arr##Array[unity_InstanceID].var

    // Put worldToObject array to a separate CB if UNITY_ASSUME_UNIFORM_SCALING is defined. Most of the time it will not be used.
    #ifdef UNITY_ASSUME_UNIFORM_SCALING
        #define UNITY_WORLDTOOBJECTARRAY_CB 1
    #else
        #define UNITY_WORLDTOOBJECTARRAY_CB 0
    #endif

    #if defined(UNITY_INSTANCED_LOD_FADE) && (defined(LOD_FADE_PERCENTAGE) || defined(LOD_FADE_CROSSFADE))
        #define UNITY_USE_LODFADE_ARRAY
    #endif

    #if defined(UNITY_INSTANCED_RENDERING_LAYER)
        #define UNITY_USE_RENDERINGLAYER_ARRAY
    #endif

    #ifdef UNITY_INSTANCED_LIGHTMAPSTS
        #ifdef LIGHTMAP_ON
            #define UNITY_USE_LIGHTMAPST_ARRAY
        #endif
        #ifdef DYNAMICLIGHTMAP_ON
            #define UNITY_USE_DYNAMICLIGHTMAPST_ARRAY
        #endif
    #endif

    #if defined(UNITY_INSTANCED_SH) && !defined(LIGHTMAP_ON)
        #if !defined(DYNAMICLIGHTMAP_ON)
            #define UNITY_USE_SHCOEFFS_ARRAYS
        #endif
        #if defined(SHADOWS_SHADOWMASK)
            #define UNITY_USE_PROBESOCCLUSION_ARRAY
        #endif
    #endif

    UNITY_INSTANCING_BUFFER_START(PerDraw0)
        #ifndef UNITY_DONT_INSTANCE_OBJECT_MATRICES
            UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_ObjectToWorldArray)
            #if UNITY_WORLDTOOBJECTARRAY_CB == 0
                UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_WorldToObjectArray)
            #endif
        #endif
        #if defined(UNITY_USE_LODFADE_ARRAY) && defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float2, unity_LODFadeArray)
            #define unity_LODFade UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_LODFadeArray).xyxx
        #endif
        #if defined(UNITY_USE_RENDERINGLAYER_ARRAY) && defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float, unity_RenderingLayerArray)
            #define unity_RenderingLayer UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_RenderingLayerArray).xxxx
        #endif
    UNITY_INSTANCING_BUFFER_END(unity_Builtins0)

    UNITY_INSTANCING_BUFFER_START(PerDraw1)
        #if !defined(UNITY_DONT_INSTANCE_OBJECT_MATRICES) && UNITY_WORLDTOOBJECTARRAY_CB == 1
            UNITY_DEFINE_INSTANCED_PROP(float4x4, unity_WorldToObjectArray)
        #endif
        #if defined(UNITY_USE_LODFADE_ARRAY) && !defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float2, unity_LODFadeArray)
            #define unity_LODFade UNITY_ACCESS_INSTANCED_PROP(unity_Builtins1, unity_LODFadeArray).xyxx
        #endif
        #if defined(UNITY_USE_RENDERINGLAYER_ARRAY) && !defined(UNITY_INSTANCING_SUPPORT_FLEXIBLE_ARRAY_SIZE)
            UNITY_DEFINE_INSTANCED_PROP(float, unity_RenderingLayerArray)
            #define unity_RenderingLayer UNITY_ACCESS_INSTANCED_PROP(unity_Builtins1, unity_RenderingLayerArray).xxxx
        #endif
    UNITY_INSTANCING_BUFFER_END(unity_Builtins1)

    UNITY_INSTANCING_BUFFER_START(PerDraw2)
        #ifdef UNITY_USE_LIGHTMAPST_ARRAY
            UNITY_DEFINE_INSTANCED_PROP(float4, unity_LightmapSTArray)
            #define unity_LightmapST UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_LightmapSTArray)
        #endif
        #ifdef UNITY_USE_DYNAMICLIGHTMAPST_ARRAY
            UNITY_DEFINE_INSTANCED_PROP(float4, unity_DynamicLightmapSTArray)
            #define unity_DynamicLightmapST UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_DynamicLightmapSTArray)
        #endif
        #ifdef UNITY_USE_SHCOEFFS_ARRAYS
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHArArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHAgArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHAbArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHBrArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHBgArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHBbArray)
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_SHCArray)
            #define unity_SHAr UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHArArray)
            #define unity_SHAg UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHAgArray)
            #define unity_SHAb UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHAbArray)
            #define unity_SHBr UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHBrArray)
            #define unity_SHBg UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHBgArray)
            #define unity_SHBb UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHBbArray)
            #define unity_SHC  UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_SHCArray)
        #endif
        #ifdef UNITY_USE_PROBESOCCLUSION_ARRAY
            UNITY_DEFINE_INSTANCED_PROP(half4, unity_ProbesOcclusionArray)
            #define unity_ProbesOcclusion UNITY_ACCESS_INSTANCED_PROP(unity_Builtins2, unity_ProbesOcclusionArray)
        #endif
    UNITY_INSTANCING_BUFFER_END(unity_Builtins2)

    #ifndef UNITY_DONT_INSTANCE_OBJECT_MATRICES
        #undef UNITY_MATRIX_M
        #undef UNITY_MATRIX_I_M
        #define MERGE_UNITY_BUILTINS_INDEX(X) unity_Builtins##X
        #ifdef MODIFY_MATRIX_FOR_CAMERA_RELATIVE_RENDERING
            #define UNITY_MATRIX_M      ApplyCameraTranslationToMatrix(UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_ObjectToWorldArray))
            #define UNITY_MATRIX_I_M    ApplyCameraTranslationToInverseMatrix(UNITY_ACCESS_INSTANCED_PROP(MERGE_UNITY_BUILTINS_INDEX(UNITY_WORLDTOOBJECTARRAY_CB), unity_WorldToObjectArray))
        #else
            #define UNITY_MATRIX_M      UNITY_ACCESS_INSTANCED_PROP(unity_Builtins0, unity_ObjectToWorldArray)
            #define UNITY_MATRIX_I_M    UNITY_ACCESS_INSTANCED_PROP(MERGE_UNITY_BUILTINS_INDEX(UNITY_WORLDTOOBJECTARRAY_CB), unity_WorldToObjectArray)
        #endif
    #endif

#else // UNITY_INSTANCING_ENABLED

    // in procedural mode we don't need cbuffer, and properties are not uniforms
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        #define UNITY_INSTANCING_BUFFER_START(buf)
        #define UNITY_INSTANCING_BUFFER_END(arr)
        #define UNITY_DEFINE_INSTANCED_PROP(type, var)      static type var;
    #else
        #define UNITY_INSTANCING_BUFFER_START(buf)          CBUFFER_START(buf)
        #define UNITY_INSTANCING_BUFFER_END(arr)            CBUFFER_END
        #define UNITY_DEFINE_INSTANCED_PROP(type, var)      type var;
    #endif

    #define UNITY_ACCESS_INSTANCED_PROP(arr, var)           var

#endif // UNITY_INSTANCING_ENABLED

#endif // UNITY_INSTANCING_INCLUDED
