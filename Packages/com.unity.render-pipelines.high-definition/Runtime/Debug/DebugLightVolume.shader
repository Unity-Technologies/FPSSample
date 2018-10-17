Shader "Hidden/HDRenderPipeline/DebugLightVolume"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Range("Range", Vector) = (1.0, 1.0, 1.0, 1.0)
        _Offset("Offset", Vector) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct AttributesDefault
            {
                float3 positionOS : POSITION;
            };

            struct VaryingsDefault
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _Range;
            float3 _Offset;
            float4 _Color;
            float _RequireToFlipInputTexture;

            VaryingsDefault vert(AttributesDefault att) 
            {
                VaryingsDefault output;

                float3 positionRWS = TransformObjectToWorld(att.positionOS.xyz * _Range + _Offset);
                output.positionCS = TransformWorldToHClip(positionRWS);
                if (_RequireToFlipInputTexture > 0.0)
                {
                    output.positionCS.y = 1.0 - output.positionCS.y;
                }
                return output;
            }

            float4 frag(VaryingsDefault varying) : SV_Target
            {
                return _Color;
            }

            ENDHLSL
        }
    }
}