Shader "Hidden/HDRenderPipeline/Material/Decal/DecalNormalBuffer"
{

    Properties
    {
        // Stencil state
        [HideInInspector] _DecalNormalBufferStencilRef("_DecalNormalBufferStencilRef", Int) = 0           // set at runtime
        [HideInInspector] _DecalNormalBufferStencilReadMask("_DecalNormalBufferStencilReadMask", Int) = 0 // set at runtime
    }

    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/Decal.hlsl"
		#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

#if defined(PLATFORM_NEEDS_UNORM_UAV_SPECIFIER) && defined(PLATFORM_SUPPORTS_EXPLICIT_BINDING)
        // Explicit binding is needed on D3D since we bind the UAV to slot 1 and we don't have a colour RT bound to fix a D3D warning.
        RW_TEXTURE2D(unorm float4, _NormalBuffer) : register(u1);
#else
        RW_TEXTURE2D(float4, _NormalBuffer);
#endif

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        // Force the stencil test before the UAV write.
        [earlydepthstencil]
        void FragNearest(Varyings input)
        {
            FETCH_DBUFFER(DBuffer, _DBufferTexture, input.texcoord * _ScreenSize.xy);
            DecalSurfaceData decalSurfaceData;
            DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

            float4 GBufferNormal = _NormalBuffer[input.texcoord * _ScreenSize.xy];
            NormalData normalData;
            DecodeFromNormalBuffer(GBufferNormal, uint2(0, 0), normalData);
            normalData.normalWS.xyz = normalize(normalData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
            EncodeIntoNormalBuffer(normalData, uint2(0, 0), GBufferNormal);
            _NormalBuffer[input.texcoord * _ScreenSize.xy] = GBufferNormal;
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            Stencil
            {
                ReadMask[_DecalNormalBufferStencilReadMask]
                Ref[_DecalNormalBufferStencilRef]
                Comp Equal
                Pass Zero   // doesn't really matter, but clear to 0 for debugging
            }

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragNearest
            ENDHLSL
        }
    }

    Fallback Off
}
