Shader "Hidden/HDRenderPipeline/AOResolve"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #pragma enable_d3d11_debug_symbols

        // Target multivalues textures
        Texture2D<float4> _DepthValuesTexture;
        Texture2D<float2> _MultiAOTexture;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID) * _ScreenSize.xy;
            return output;
        }

        float Frag(Varyings input) : SV_Target
        {
            // Generate the matching pixel coordinates
            int2 pixelCoords = int2(input.texcoord);

            // Read the multiple depth values
            float4 depthValues = LOAD_TEXTURE2D(_DepthValuesTexture, pixelCoords);

            // Compute the lerp value between the max and min ao values (and saturate in case maxdepth == mindepth)
            float lerpVal = saturate((depthValues.z - depthValues.y) / (depthValues.x - depthValues.y));

            // Fetch the AO values
            float2 aoValues = LOAD_TEXTURE2D(_MultiAOTexture, pixelCoords);

            // Lerp between Both
            return lerp(aoValues.x, aoValues.y, lerpVal);
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            ZWrite On ZTest Always Blend Off Cull Off

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
