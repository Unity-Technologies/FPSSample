Shader "Hidden/HDRenderPipeline/CopyStencilBuffer"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 1
        [HideInInspector] _StencilMask("_StencilMask", Int) = 7
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    // #pragma enable_d3d11_debug_symbols

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

    int _StencilRef;

    // Explicit binding not supported on PS4
#if defined(PLATFORM_SUPPORTS_EXPLICIT_BINDING)
    // Explicit binding is needed on D3D since we bind the UAV to slot 1 and we don't have a colour RT bound to fix a D3D warning.
    RW_TEXTURE2D(float, _HTile) : register(u1); // DXGI_FORMAT_R8_UINT is not supported by Unity
    RW_TEXTURE2D(float, _StencilBufferCopy) : register(u1); // DXGI_FORMAT_R8_UINT is not supported by Unity
#else
    RW_TEXTURE2D(float, _HTile); // DXGI_FORMAT_R8_UINT is not supported by Unity
    RW_TEXTURE2D(float, _StencilBufferCopy); // DXGI_FORMAT_R8_UINT is not supported by Unity
#endif
        
    struct Attributes
    {
        uint vertexID : SV_VertexID;
    };

    struct Varyings
    {
        float4 positionCS : SV_Position;
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        return output;
    }

    #pragma vertex Vert

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Name "Pass 0 - Copy stencilRef to output"

            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma fragment Frag

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                return PackByte(_StencilRef);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Pass 1 - Write 1 if value different from stencilRef to output"

            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp NotEqual
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma fragment Frag

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            float4 Frag(Varyings input) : SV_Target // use SV_StencilRef in D3D 11.3+
            {
                return PackByte(1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "Pass 2 - Export HTILE for stencilRef to output"

            Stencil
            {
                ReadMask [_StencilMask]
                Ref  [_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off
            ColorMask 0

            HLSLPROGRAM
            #pragma fragment Frag

            // Force the stencil test before the UAV write.
            [earlydepthstencil]
            void Frag(Varyings input) // use SV_StencilRef in D3D 11.3+
            {
                uint2 positionNDC = (uint2)input.positionCS.xy;
                // There's no need for atomics as we are always writing the same value.
                // Note: the GCN tile size is 8x8 pixels.
                _HTile[positionNDC / 8] = _StencilRef;
            }

            ENDHLSL
        }

        Pass
        {
            // Note, when supporting D3D 11.3+, this can be a one off copy pass.
            // This is essentially the equivalent of Pass 1, but writing to a UAV instead.
            Name "Pass 3 - Initialize Stencil UAV copy with 1 if value different from stencilRef to output"  

            Stencil
            {
                ReadMask[_StencilMask]
                Ref[_StencilRef]
                Comp NotEqual
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma fragment Frag

                // Force the stencil test before the UAV write.
                [earlydepthstencil]
                void Frag(Varyings input)// use SV_StencilRef in D3D 11.3+
                {
                    _StencilBufferCopy[(uint2)input.positionCS.xy] = PackByte(1);
                }

                ENDHLSL
        }

        Pass
        {
            Name "Pass 4 - Update Stencil UAV copy with Stencil Ref"  

            Stencil
            {
                ReadMask[_StencilMask]
                Ref[_StencilRef]
                Comp Equal
                Pass Keep
            }

            Cull   Off
            ZTest  Always
            ZWrite Off
            Blend  Off

            HLSLPROGRAM
            #pragma fragment Frag

                // Force the stencil test before the UAV write.
                [earlydepthstencil]
                void Frag(Varyings input) // use SV_StencilRef in D3D 11.3+
                {
                    uint2 dstPixCoord = (uint2)input.positionCS.xy;
                    uint oldStencilVal = UnpackByte(_StencilBufferCopy[dstPixCoord]);
                    _StencilBufferCopy[dstPixCoord] = PackByte(oldStencilVal | _StencilRef);
                }

                ENDHLSL
        }
    }
    Fallback Off
}
