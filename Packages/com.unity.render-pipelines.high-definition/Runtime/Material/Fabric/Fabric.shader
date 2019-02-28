Shader "Hidden/HDRenderPipeline/Fabric"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        // Reminder. Color here are in linear but the UI (color picker) do the conversion sRGB to linear
        // Be careful, do not change the name here to _Color. It will conflict with the "fake" parameters (see end of properties) required for GI.
        
        // Fabric type
        [Enum(Silk, 0, CottonWool, 1)] _FabricType("Fabric Type", Float) = 0

        // Value used to define which uv channel is used for the first set of textures
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVBase("UV Set for base", Float) = 0
        [HideInInspector] _UVMappingMask("_UVMappingMask", Color) = (1, 0, 0, 0)

        // Base color
        _BaseColor("BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}

        // Normal map
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _NormalScale("_NormalScale", Range(0.0, 2.0)) = 1

        // Tangent map
        _TangentMap("TangentMap", 2D) = "bump" {}

        // Smoothness values (overriden by the mask map)
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    
        // The mask texture and the matching remapping values for it        
        _MaskMap("MaskMap", 2D) = "white" {}
        _AORemapMin("AORemapMin", Float) = 0.0
        _AORemapMax("AORemapMax", Float) = 1.0
        _SmoothnessRemapMin("SmoothnessRemapMin", Float) = 0.0
        _SmoothnessRemapMax("SmoothnessRemapMax", Float) = 1.0

        // TODO
        //_BentNormalMap("_BentNormalMap", 2D) = "bump" {}

        // Specular Tint
        _SpecularColor("SpecularColor", Color) = (1.0, 1.0, 1.0)

        // Thread Data
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVThread("UV Set for thread", Float) = 0
        [HideInInspector] _UVMappingMaskThread("UVMappingMaskThread", Color) = (1, 0, 0, 0)
        _ThreadMap("ThreadMap", 2D) = "black" {}
        _ThreadAOScale("ThreadAOScale", Range(0.0, 1.0)) = 0.5
        _ThreadNormalScale("ThreadNormalScale", Range(0.0, 2.0)) = 1
        _ThreadSmoothnessScale("ThreadSmoothnessScale", Range(0.0, 2.0)) = 1

        // Fuzz Detail
        _FuzzDetailMap("FuzzDetailMap", 2D) = "white" {}
        _FuzzDetailScale("_FuzzDetailScale", Range(0.0, 1.0)) = 0.0
        _FuzzDetailUVScale("_FuzzDetailUVScale", Range(0.01, 1.0)) = 0.25

        [ToggleUI] _LinkDetailsWithBase("LinkDetailsWithBase", Float) = 1.0

        // Emissive Data
        [Enum(UV0, 0, UV1, 1, UV2, 2, UV3, 3)] _UVEmissive("UV Set for emissive", Float) = 0
        [HideInInspector] _UVMappingMaskEmissive("_UVMappingMaskEmissive", Color) = (1, 0, 0, 0)
        [HDR] _EmissiveColor("EmissiveColor", Color) = (0, 0, 0)
        _EmissiveColorMap("EmissiveColorMap", 2D) = "white" {}
        [ToggleUI] _AlbedoAffectEmissive("Albedo Affect Emissive", Float) = 0.0

        // Anisotropy Data
        _Anisotropy("Anisotropy", Range(-1.0, 1.0)) = 0
        _AnisotropyMap("AnisotropyMap", 2D) = "white" {}

        // Diffusion Data
        _DiffusionProfile("Diffusion Profile", Int) = 0

        // Transmission Data
        [ToggleUI]  _EnableTransmission("_EnableTransmission", Float) = 0.0

        // Subsurface Data
        _SubsurfaceMask("Subsurface Radius", Range(0.0, 1.0)) = 1.0
        _SubsurfaceMaskMap("Subsurface Radius Map", 2D) = "white" {}

        // Thickness Data
        _Thickness("Thickness", Range(0.0, 1.0)) = 1.0
        _ThicknessMap("Thickness Map", 2D) = "white" {}
        _ThicknessRemap("Thickness Remap", Vector) = (0, 1, 0, 0)

        //[ToggleUI]  _EnableSpecularOcclusion("Enable specular occlusion", Float) = 0.0

        // Double sided
        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(Flip, 0, Mirror, 1, None, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        // Transparency
        [ToggleUI] _PreRefractionPass("PreRefractionPass", Float) = 0.0

        [ToggleUI]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
        _AlphaCutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _TransparentSortPriority("_TransparentSortPriority", Float) = 0

        // Stencil state
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2 // StencilLightingUsage.RegularLighting  (fixed at compile time)
        [HideInInspector] _StencilWriteMask("_StencilWriteMask", Int) = 7 // StencilMask.Lighting  (fixed at compile time)
        [HideInInspector] _StencilRefMV("_StencilRefMV", Int) = 128 // StencilLightingUsage.RegularLighting  (fixed at compile time)
        [HideInInspector] _StencilWriteMaskMV("_StencilWriteMaskMV", Int) = 128 // StencilMask.ObjectsVelocity  (fixed at compile time)

        // Blending state
        [HideInInspector] _SurfaceType("__surfacetype", Float) = 0.0
        [HideInInspector] _BlendMode("__blendmode", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _CullMode("__cullmode", Float) = 2.0
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal

        [ToggleUI] _EnableFogOnTransparent("Enable Fog", Float) = 1.0
        [ToggleUI] _EnableBlendModePreserveSpecularLighting("Enable Blend Mode Preserve Specular Lighting", Float) = 1.0

        _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        // This is required by motion vector pass to be able to disable the pass by default
        [HideInInspector] _EnableMotionVectorForVertexAnimation("EnableMotionVectorForVertexAnimation", Float) = 0.0
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DOUBLESIDED_ON

    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
    #pragma shader_feature _BENTNORMALMAP
    #pragma shader_feature _ENABLESPECULAROCCLUSION
    #pragma shader_feature _TANGENTMAP
    #pragma shader_feature _ANISOTROPYMAP
    #pragma shader_feature _THREAD_MAP
    #pragma shader_feature _FUZZDETAIL_MAP
    #pragma shader_feature _SUBSURFACE_MASK_MAP
    #pragma shader_feature _THICKNESSMAP
    #pragma shader_feature _EMISSIVE_COLOR_MAP

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING
    #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT

    // MaterialFeature are used as shader feature to allow compiler to optimize properly
    #pragma shader_feature _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
    #pragma shader_feature _MATERIAL_FEATURE_TRANSMISSION
    #pragma shader_feature _MATERIAL_FEATURE_COTTON_WOOL
    
    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer

    // If we use subsurface scattering, enable output split lighting (for forward pass)
    #if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING)
    #define OUTPUT_SPLIT_LIGHTING
    #endif

    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"

    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDFabricShader" }

        // Caution: The outline selection in the editor use the vertex shader/hull/domain shader of the first pass declare. So it should not be the meta pass.

        Pass
        {
            Name "Depth prepass"
            Tags{ "LightMode" = "DepthForwardOnly" }

            Cull[_CullMode]

            ZWrite On

            Stencil
            {
                WriteMask[_StencilDepthPrepassWriteMask]
                Ref[_StencilDepthPrepassRef]
                Comp Always
                Pass Replace
            }
            HLSLPROGRAM

            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/ShaderPass/FabricDepthPass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags{ "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            // Lightmap memo
            // DYNAMICLIGHTMAP_ON is used when we have an "enlighten lightmap" ie a lightmap updated at runtime by enlighten.This lightmap contain indirect lighting from realtime lights and realtime emissive material.Offline baked lighting(from baked material / light,
            // both direct and indirect lighting) will hand up in the "regular" lightmap->LIGHTMAP_ON.

            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/ShaderPass/FabricSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{ "LightMode" = "ShadowCaster" }

            Cull[_CullMode]

            ZClip [_ZClip]
            ZWrite On
            ZTest LEqual

            ColorMask 0

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_SHADOWS
            #define USE_LEGACY_UNITY_MATRIX_VARIABLES
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/ShaderPass/FabricDepthPass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Motion Vectors"
            Tags{ "LightMode" = "MotionVectors" } // Caution, this need to be call like this to setup the correct parameters by C++ (legacy Unity)

            // If velocity pass (motion vectors) is enabled we tag the stencil so it don't perform CameraMotionVelocity
            Stencil
            {
                WriteMask [_StencilWriteMaskMV]
                Ref [_StencilRefMV]
                Comp Always
                Pass Replace
            }

            Cull[_CullMode]

            ZWrite On

            HLSLPROGRAM
            #define WRITE_NORMAL_BUFFER
            #pragma multi_compile _ WRITE_MSAA_DEPTH
            
            #define SHADERPASS SHADERPASS_VELOCITY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/ShaderPass/FabricSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassVelocity.hlsl"

            ENDHLSL
        }

        // Fabric shader always render in forward
        Pass
        {
            Name "Forward" // Name is not used
            Tags { "LightMode" = "ForwardOnly" }

            Stencil
            {
                WriteMask [_StencilWriteMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            Blend [_SrcBlend] [_DstBlend]
            // In case of forward we want to have depth equal for opaque alpha tested mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull[_CullMode]

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Forward.hlsl" : nothing left in there.
            //#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #define LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT

            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH

            #define SHADERPASS SHADERPASS_FORWARD
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

        #ifdef DEBUG_DISPLAY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #endif

            // The light loop (or lighting architecture) is in charge to:
            // - Define light list
            // - Define the light loop
            // - Setup the constant/data
            // - Do the reflection hierarchy
            // - Provide sampling function for shadowmap, ies, cookie and reflection (depends on the specific use with the light loops like index array or atlas or single and texture format (cubemap/latlong))

            #define HAS_LIGHTLOOP

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/Fabric.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

            //...this will include #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl" but also LightLoop which the forward pass directly uses.

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/ShaderPass/FabricSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Fabric/FabricData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }
    }
}
