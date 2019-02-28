Shader "Hidden/HDRenderPipeline/preIntegratedFGD_CharlieFabricLambert"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            // ----------------------------------------------------------------------------
            // Importance Sampling
            // ----------------------------------------------------------------------------

            float4 IntegrateCharlieAndFabricLambertFGD(float3 V, float3 N, float roughness, uint sampleCount = 4096)
            {
                float NdotV     = ClampNdotV(dot(N, V));
                float4 acc      = float4(0.0, 0.0, 0.0, 0.0);

                float3x3 localToWorld = GetLocalFrame(N);

                for (uint i = 0; i < sampleCount; ++i)
                {
                    float2 u = Hammersley2d(i, sampleCount);

                    float NdotL;
                    float weightOverPdf;

                    // Ref: Production Friendly Microfacet Sheen BRDF
                    // Paper recommend plain uniform sampling of upper hemisphere instead of importance sampling for Charlie
                    float3 localL = SampleHemisphereUniform(u.x, u.y);
                    float3 L = mul(localL, localToWorld);
                    NdotL = saturate(dot(N, L));

                    if (NdotL > 0.0)
                    {
                        // Sampling weight for each sample
                        // pdf = 1 / 2PI
                        // weight = fr * (N.L) with fr = CharlieV * CharlieD  / PI
                        // weight over pdf is:
                        // weightOverPdf = (CharlieV * CharlieD / PI) * (N.L) / (1 / 2PI)
                        // weightOverPdf = 2 * CharlieV * CharlieD * (N.L)
                        float3 H = normalize(V + L);
                        float NdotH = dot(N, H);
                        // Note: we use V_Charlie and not the approx when computing FGD texture as we can afford it
                        weightOverPdf = 2.0 * V_Charlie(NdotL, NdotV, roughness) * D_CharlieNoPI(NdotH, roughness) * NdotL;

                        // Integral{BSDF * <N,L> dw} =
                        // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
                        // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
                        // (1 - F0) * x + F0 * y = lerp(x, y, F0)
                        float VdotH = dot(V, H);
                        acc.x += weightOverPdf * pow(1 - VdotH, 5);
                        acc.y += weightOverPdf;
                    }

                    // for Fabric Lambert we still use a Cosine importance sampling
                    ImportanceSampleLambert(u, localToWorld, L, NdotL, weightOverPdf);

                    if (NdotL > 0.0)
                    {
                        float fabricLambert = FabricLambertNoPI(roughness);
                        acc.z += fabricLambert * weightOverPdf;
                    }
                }

                // Normalize the accumulated value
                acc /= sampleCount;

                // The specular term is not bound in the [0, 1] space to avoid that we put it to LDR here and back to HDR when reading
                acc.y  = acc.y / (1 + acc.y);

                return acc;
            }

            // ----------------------------------------------------------------------------
            // Pre-Integration
            // ----------------------------------------------------------------------------

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texCoord   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texCoord   = GetFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                // These coordinate sampling must match the decoding in GetPreIntegratedDFG in lit.hlsl, i.e here we use perceptualRoughness, must be the same in shader
                float NdotV                 = input.texCoord.x;
                float perceptualRoughness   = input.texCoord.y;
                float3 V                    = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);
                float3 N                    = float3(0.0, 0.0, 1.0);

                // Pre integrate GGX with smithJoint visibility as well as DisneyDiffuse
                float4 preFGD = IntegrateCharlieAndFabricLambertFGD(V, N, PerceptualRoughnessToRoughness(perceptualRoughness));

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
