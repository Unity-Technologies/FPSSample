Shader "HDRenderPipeline/Decal"
{
    Properties
    {
        // Versioning of material to help for upgrading
        [HideInInspector] _HdrpVersion("_HdrpVersion", Float) = 2

		_BaseColor("_BaseColor", Color) = (1,1,1,1)
        _BaseColorMap("BaseColorMap", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}     // Tangent space normal map
        _MaskMap("MaskMap", 2D) = "white" {}
        _DecalBlend("_DecalBlend", Range(0.0, 1.0)) = 0.5
		[ToggleUI] _AlbedoMode("_AlbedoMode", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _NormalBlendSrc("_NormalBlendSrc", Float) = 0.0
		[HideInInspector] _MaskBlendSrc("_MaskBlendSrc", Float) = 1.0
		[HideInInspector] _MaskBlendMode("_MaskBlendMode", Float) = 4.0 // smoothness 3RT default
		[ToggleUI] _MaskmapMetal("_MaskmapMetal", Range(0.0, 1.0)) = 0.0
		[ToggleUI] _MaskmapAO("_MaskmapAO", Range(0.0, 1.0)) = 0.0
		[ToggleUI] _MaskmapSmoothness("_MaskmapSmoothness", Range(0.0, 1.0)) = 1.0
		[HideInInspector] _DecalMeshDepthBias("_DecalMeshDepthBias", Float) = 0.0 
		[HideInInspector] _DrawOrder("_DrawOrder", Int) = 0
        // Stencil state
        [HideInInspector] _DecalStencilRef("_DecalStencilRef", Int) = 8 
        [HideInInspector] _DecalStencilWriteMask("_DecalStencilWriteMask", Int) = 8
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    //#pragma enable_d3d11_debug_symbols

    //-------------------------------------------------------------------------------------
    // Variant
    //-------------------------------------------------------------------------------------
    #pragma shader_feature _COLORMAP
    #pragma shader_feature _NORMALMAP
    #pragma shader_feature _MASKMAP
	#pragma shader_feature _ALBEDOCONTRIBUTION

    #pragma multi_compile_instancing
    // No need to teset for DECALS_3RT we are in decal shader, so there is no OFF state
	#pragma multi_compile _ DECALS_4RT
    //-------------------------------------------------------------------------------------
    // Define
    //-------------------------------------------------------------------------------------
    #define UNITY_MATERIAL_DECAL

    //-------------------------------------------------------------------------------------
    // Include
    //-------------------------------------------------------------------------------------

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Wind.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"


    //-------------------------------------------------------------------------------------
    // variable declaration
    //-------------------------------------------------------------------------------------

    #include "DecalProperties.hlsl"

    // All our shaders use same name for entry point
    #pragma vertex Vert
    #pragma fragment Frag

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline"}

		// c# code relies on the order in which the passes are declared, any change will need to be reflected in DecalUI.cs

		// pass 0 is mesh 3RT mode
		Pass
		{
			Name "DBufferMesh_3RT"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_3RT"} // Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

			ColorMask BA 2	// smoothness/smoothness alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		// enum MaskBlendFlags
		//{
		//	Metal = 1 << 0,
		//	AO = 1 << 1,
		//	Smoothness = 1 << 2,
		//}

		// Projectors
		//
		// 1 - Metal
		// 2 - AO
		// 3 - Metal + AO
		// 4 - Smoothness also 3RT 
		// 5 - Metal + Smoothness
		// 6 - AO + Smoothness
		// 7 - Metal + AO + Smoothness
		//

		Pass
		{
			Name "DBufferProjector_M"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_M"} // Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask R 2	// metal 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_AO"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_AO"} // AO only
													  // back faces with zfail, for cases when camera is inside the decal volume
            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask G 2	// ao
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_MAO"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_MAO"} // AO + Metalness
													   // back faces with zfail, for cases when camera is inside the decal volume
            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RG 2	// metalness + ao
			ColorMask RG 3	// metalness alpha + ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_S"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_S"} // Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
		
			ColorMask BA 2	// smoothness/smoothness alpha
            ColorMask 0 3   // Caution: We need to setup the mask to 0 in case perChannelMAsk is enabled as 4 RT are bind
		
			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferProjector_MS"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_MS"} // Smoothness and Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RBA 2	// metal/smoothness/smoothness alpha 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}


		Pass
		{
			Name "DBufferProjector_AOS"  // Name is not used
			Tags{"LightMode" = "DBufferProjector_AOS"} // AO + Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			// back faces with zfail, for cases when camera is inside the decal volume
			Cull Front
			ZWrite Off
			ZTest Greater
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask GBA 2	// ao, smoothness, smoothness alpha
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

        Pass
        {
            Name "DBufferProjector_MAOS"  // Name is not used
            Tags { "LightMode" = "DBufferProjector_MAOS" } // Metalness AO and Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

            // back faces with zfail, for cases when camera is inside the decal volume
            Cull Front
            ZWrite Off
            ZTest Greater
            // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

            HLSLPROGRAM

            #define SHADERPASS SHADERPASS_DBUFFER_PROJECTOR
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Decal.hlsl"
            #include "ShaderPass/DecalSharePass.hlsl"
            #include "DecalData.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

            ENDHLSL
        }

		// Mesh 
		// 8 - Metal
		// 9 - AO
		// 10 - Metal + AO
		// 11 - Smoothness 
		// 12 - Metal + Smoothness
		// 13 - AO + Smoothness
		// 14 - Metal + AO + Smoothness

		Pass
		{
			Name "DBufferMesh_M"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_M"} // Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask R 2	// metal 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_AO"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_AO"} // AO only

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask G 2	// ao
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_MAO"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_MAO"} // AO + Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RG 2	// metalness + ao
			ColorMask RG 3	// metalness alpha + ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_S"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_S"} // Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha

			ColorMask BA 2	// smoothness/smoothness alpha
            ColorMask 0 3   // Caution: We need to setup the mask to 0 in case perChannelMAsk is enabled as 4 RT are bind

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}


		Pass
		{
			Name "DBufferMesh_MS"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_MS"} // Smoothness and Metalness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask RBA 2	// metal/smoothness/smoothness alpha 
			ColorMask R 3	// metal alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_AOS"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_AOS"} // AO + Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			ColorMask GBA 2	// ao, smoothness, smoothness alpha
			ColorMask G 3	// ao alpha

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DBufferMesh_MAOS"  // Name is not used
			Tags{"LightMode" = "DBufferMesh_MAOS"} // Metalness AO and Smoothness

            Stencil
            {
                WriteMask[_DecalStencilWriteMask]
                Ref[_DecalStencilRef]
                Comp Always
                Pass Replace
            }

			ZWrite Off
			ZTest LEqual
			// using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
			Blend 0 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 1 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 2 SrcAlpha OneMinusSrcAlpha, Zero OneMinusSrcAlpha
			Blend 3 Zero OneMinusSrcColor

			HLSLPROGRAM

			#define SHADERPASS SHADERPASS_DBUFFER_MESH
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Decal.hlsl"
			#include "ShaderPass/DecalSharePass.hlsl"
			#include "DecalData.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDBuffer.hlsl"

			ENDHLSL
		}
	}
    CustomEditor "Experimental.Rendering.HDPipeline.DecalUI"
}
