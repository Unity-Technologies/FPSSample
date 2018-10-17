// Note: This shader is supposed to be removed at some point when Graphics.ConvertTexture can take a RenderTexture as a destination (it's only used by sky manager for now).
Shader "Hidden/BlitCubemap" {
    SubShader {
        // Cubemap blit.  Takes a face index.
        Pass {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURECUBE(_MainTex);
            SAMPLER(sampler_MainTex);
            float _faceIndex;

            struct appdata_t {
                uint vertexID : SV_VertexID;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            static const float3 faceU[6] = { float3(0, 0, -1), float3(0, 0, 1), float3(1, 0, 0), float3(1, 0, 0), float3(1, 0, 0), float3(-1, 0, 0) };
            static const float3 faceV[6] = { float3(0, -1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1), float3(0, -1, 0), float3(0, -1, 0) };

            v2f vert (appdata_t v)
            {
                v2f o;

                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(v.vertexID) * 2.0 - 1.0;

                int idx = (int)_faceIndex;
                float3 transformU = faceU[idx];
                float3 transformV = faceV[idx];

                float3 n = cross(transformV, transformU);
                o.texcoord = n + uv.x * transformU + uv.y * transformV;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return SAMPLE_TEXTURECUBE(_MainTex, sampler_MainTex, i.texcoord);
            }
            ENDHLSL

        }
    }
    Fallback Off
}
