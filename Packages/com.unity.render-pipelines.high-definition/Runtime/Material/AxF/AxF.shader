Shader "HDRenderPipeline/AxF"
{
    Properties
    {
        // Following set of parameters represent the parameters node inside the MaterialGraph.
        // They are use to fill a SurfaceData. With a MaterialGraph this should not exist.

        /////////////////////////////////////////////////////////////////////////////
        // General Parameters
        _MaterialTilingU( "Material U Tiling", Float ) = 1
        _MaterialTilingV( "Material V Tiling", Float ) = 1

        [Enum( SVBRDF, CarPaint, BTF )] _AxF_BRDFType( "_AxF_BRDFType", Int ) = 0

        [HideInInspector] _Flags( "_Flags", Int ) = 0

        /////////////////////////////////////////////////////////////////////////////
        // SVBRDF Parameters

        // SVBRDF maps
        _SVBRDF_DiffuseColorMap("_SVBRDF_DiffuseColorMap", 2D) = "white" {}
        _SVBRDF_SpecularColorMap("_SVBRDF_SpecularColorMap", 2D) = "white" {}
        _SVBRDF_NormalMap("_SVBRDF_NormalMap", 2D) = "bump" {}
        _SVBRDF_SpecularLobeMap("_SVBRDF_SpecularLobeMap", 2D) = "white" {}
        _SVBRDF_SpecularLobeMapScale("_SVBRDF_SpecularLobeMapScale", Float) = 1         // Scale is useless if we're directly provided a RG16F format
        _SVBRDF_AlphaMap("_SVBRDF_AlphaMap", 2D) = "white" {}
        _SVBRDF_FresnelMap("_SVBRDF_FresnelMap", 2D) = "white" {}
        _SVBRDF_AnisoRotationMap("_SVBRDF_AnisoRotationMap", 2D) = "black" {}
        _SVBRDF_HeightMap("_SVBRDF_HeightMap", 2D) = "black" {}
        _SVBRDF_ClearcoatColorMap("_SVBRDF_ClearcoatColorMap", 2D) = "white" {}
        _ClearcoatNormalMap("_ClearcoatNormal", 2D) = "bump" {}
        _SVBRDF_ClearcoatIORMap("_SVBRDF_ClearcoatIORMap", 2D) = "black" {}

        // SVBRDF Constants
        [HideInInspector] _SVBRDF_BRDFType( "_SVBRDF_BRDFType", Int ) = 0
        [HideInInspector] _SVBRDF_BRDFVariants( "_SVBRDF_BRDFVariants", Int ) = 0
        [HideInInspector] _SVBRDF_HeightMapMaxMM( "_SVBRDF_HeightMapMax", Float ) = 0


        /////////////////////////////////////////////////////////////////////////////
        // Car Paint Parameters
        _CarPaint2_CTDiffuse("_CarPaint2_CTDiffuse", Float) = 0
        _CarPaint2_ClearcoatIOR("_CarPaint2_ClearcoatIOR", Float) = 1

        // BRDF
        _CarPaint2_BRDFColorMapScale("_CarPaint2_BRDFColorMapScale", Float) = 1        // Scale is useless if we're directly provided a RGBA16F format
        _CarPaint2_BRDFColorMap("_CarPaint2_BRDFColorMap", 2D) = "white" {}

        // Flakes
        _CarPaint2_FlakeTiling("_CarPaint2_FlakeTiling", Float) = 1
        _CarPaint2_BTFFlakeMapScale("_CarPaint2_BTFFlakeMapScale", Float) = 1         // Scale is useless if we're directly provided a RGBA16F format
        _CarPaint2_BTFFlakeMap("_CarPaint2_BTFFlakeMap", 2DArray) = "black" {}
        _CarPaint2_FlakeThetaFISliceLUTMap( "_CarPaint2_FlakeThetaFISliceLUTMap", 2D ) = "black" {}

        _CarPaint2_FlakeMaxThetaI("_CarPaint2_FlakeMaxThetaI", Int) = 0
        _CarPaint2_FlakeNumThetaF("_CarPaint2_FlakeNumThetaF", Int) = 0
        _CarPaint2_FlakeNumThetaI("_CarPaint2_FlakeNumThetaI", Int) = 0

        // Cook-Torrance Lobes Descriptors
        _CarPaint2_LobeCount("_CarPaint2_LobeCount", Int) = 0
        _CarPaint2_CTF0s("_CarPaint2_CTF0s", Color) = (1,1,1,1)
        _CarPaint2_CTCoeffs("_CarPaint2_CTCoeffs", Color) = (1,1,1,1)
        _CarPaint2_CTSpreads("_CarPaint2_CTSpreads", Color) = (1,1,1,1)

//      [ToggleUI]  _AlphaCutoffEnable("Alpha Cutoff Enable", Float) = 0.0
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
        [HideInInspector] _CullModeForward("__cullmodeForward", Float) = 2.0 // This mode is dedicated to Forward to correctly handle backface then front face rendering thin transparent
        [HideInInspector] _ZTestDepthEqualForOpaque("_ZTestDepthEqualForOpaque", Int) = 4 // Less equal
        [HideInInspector] _ZTestModeDistortion("_ZTestModeDistortion", Int) = 8


//      [ToggleUI] _EnableFogOnTransparent("Enable Fog", Float) = 1.0
//      [ToggleUI] _EnableBlendModePreserveSpecularLighting("Enable Blend Mode Preserve Specular Lighting", Float) = 1.0

        [ToggleUI] _DoubleSidedEnable("Double sided enable", Float) = 0.0
        [Enum(Flip, 0, Mirror, 1, None, 2)] _DoubleSidedNormalMode("Double sided normal mode", Float) = 1 // This is for the editor only, see BaseLitUI.cs: _DoubleSidedConstants will be set based on the mode.
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, -1, 0)

        // Caution: C# code in BaseLitUI.cs call LightmapEmissionFlagsProperty() which assume that there is an existing "_EmissionColor"
        // value that exist to identify if the GI emission need to be enabled.
        // In our case we don't use such a mechanism but need to keep the code quiet. We declare the value and always enable it.
        // TODO: Fix the code in legacy unity so we can customize the beahvior for GI
        _EmissionColor("Color", Color) = (1, 1, 1)

        // HACK: GI Baking system relies on some properties existing in the shader ("_MainTex", "_Cutoff" and "_Color") for opacity handling, so we need to store our version of those parameters in the hard-coded name the GI baking system recognizes.
        _MainTex("Albedo", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature _AXF_BRDF_TYPE_SVBRDF _AXF_BRDF_TYPE_CAR_PAINT _AXF_BRDF_TYPE_BTF

    #pragma shader_feature _ALPHATEST_ON
    #pragma shader_feature _DOUBLESIDED_ON

    // Keyword for transparent
    #pragma shader_feature _SURFACE_TYPE_TRANSPARENT
    #pragma shader_feature _ _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
    #pragma shader_feature _BLENDMODE_PRESERVE_SPECULAR_LIGHTING // easily handled in material.hlsl, so adding this already.
    #pragma shader_feature _ENABLE_FOG_ON_TRANSPARENT

    // enable dithering LOD crossfade
    #pragma multi_compile _ LOD_FADE_CROSSFADE

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma instancing_options renderinglayer

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

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        // This tags allow to use the shader replacement features
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "HDAxFShader" }

        Pass
        {
            Name "SceneSelectionPass" // Name is not used
            Tags { "LightMode" = "SceneSelectionPass" }

            Cull Off

            HLSLPROGRAM

            // Note: Require _ObjectId and _PassValue variables

            // We reuse depth prepass for the scene selection, allow to handle alpha correctly as well as tessellation and vertex animation
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFDepthPass.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Depth prepass"
            Tags{ "LightMode" = "DepthForwardOnly" }

            Cull[_CullMode]

            ZWrite On

            HLSLPROGRAM

            #define WRITE_NORMAL_BUFFER
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassVelocity.hlsl"

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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFDepthPass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"

            ENDHLSL
        }

        // AxF shader always render in forward
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
            // In case of forward we want to have depth equal for opaque mesh
            ZTest [_ZTestDepthEqualForOpaque]
            ZWrite [_ZWrite]
            Cull [_CullModeForward]
            //
            // NOTE: For _CullModeForward, see BaseLitUI and the handling of TransparentBackfaceEnable:
            // Basically, we need to use it to support a TransparentBackface pass before this pass
            // (and it should be placed just before this one) for separate backface and frontface rendering,
            // eg for "hair shader style" approximate sorting, see eg Thorsten Scheuermann writeups on this:
            // http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.607.1272&rep=rep1&type=pdf
            // http://amd-dev.wpengine.netdna-cdn.com/wordpress/media/2012/10/Scheuermann_HairSketchSlides.pdf
            // http://web.engr.oregonstate.edu/~mjb/cs519/Projects/Papers/HairRendering.pdf
            //
            // See Lit.shader and the order of the passes after a DistortionVectors, we have:
            // TransparentDepthPrepass, TransparentBackface, Forward, TransparentDepthPostpass

            HLSLPROGRAM

            #pragma multi_compile _ DEBUG_DISPLAY
            //NEWLITTODO
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // Setup DECALS_OFF so the shader stripper can remove variants
            #pragma multi_compile DECALS_OFF DECALS_3RT DECALS_4RT
            
            // Supported shadow modes per light type
            #pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH

            // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Lighting/Forward.hlsl" : nothing left in there.
            //#pragma multi_compile LIGHTLOOP_SINGLE_PASS LIGHTLOOP_TILE_PASS
            #define LIGHTLOOP_TILE_PASS
            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

            #define SHADERPASS SHADERPASS_FORWARD
            // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
            #ifndef _SURFACE_TYPE_TRANSPARENT
                #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
            #endif
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
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/ShaderPass/AxFSharePass.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForward.hlsl"

            ENDHLSL
        }

    }

    CustomEditor "Experimental.Rendering.HDPipeline.AxFGUI"
}
