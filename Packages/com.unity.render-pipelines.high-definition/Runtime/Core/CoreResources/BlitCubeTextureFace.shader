Shader "Hidden/SRP/BlitCubeTextureFace"
{
    SubShader
    {
        // Cubemap blit.  Takes a face index.
        Pass
        {

            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            TEXTURECUBE(_InputTex);
            SAMPLER(sampler_InputTex);

            float _FaceIndex;
            float _LoD;

            struct Attributes
            {
                uint vertexID : VERTEXID_SEMANTIC;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 texcoord     : TEXCOORD0;
            };

            static const float3 faceU[6] = { float3(0, 0, -1), float3(0, 0, 1), float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(-1, 0, 0) };
            static const float3 faceV[6] = { float3(0, -1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1), float3(0, -1, 0), float3(0, -1, 0) };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);

                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
                uv = uv * 2 - 1;

                int idx = (int)_FaceIndex;
                float3 transformU = faceU[idx];
                float3 transformV = faceV[idx];

                float3 n = cross(transformV, transformU);
                output.texcoord = n + uv.x * transformU + uv.y * transformV;
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURECUBE_LOD(_InputTex, sampler_InputTex, input.texcoord, _LoD);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
