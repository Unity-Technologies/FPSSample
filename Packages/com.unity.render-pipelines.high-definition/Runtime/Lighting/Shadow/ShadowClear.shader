Shader "Hidden/ScriptableRenderPipeline/ShadowClear"
{
    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "ClearShadow"
            ZTest Always
            Cull Off
            ZWrite On

            HLSLPROGRAM

            #pragma vertex Vert_0
            #pragma fragment Frag

            float4 Vert_0( uint vertexID : VERTEXID_SEMANTIC ) : SV_POSITION
            {
                return GetFullScreenTriangleVertexPosition( vertexID, UNITY_RAW_FAR_CLIP_VALUE );
            }

            float4 Frag() : SV_Target{ return 0.0.xxxx; }


            ENDHLSL
        }
    }
    Fallback Off
}
