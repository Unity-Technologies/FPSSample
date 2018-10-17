Shader "Hidden/HDRenderPipeline/PreIntegratedFGD_Ward"
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
            // Formulas come from -> Walter, B. 2005 "Notes on the Ward BRDF" (https://pdfs.semanticscholar.org/330e/59117d7da6c794750730a15f9a178391b9fe.pdf)
            // The BRDF though, is the one most proeminently used by the AxF materials and is based on the Geisler-Moroder variation of Ward (http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.169.9908&rep=rep1&type=pdf)
            void SampleWardDir( float2   u,
                                float3   V,
                                float3x3 localToWorld,
                                float    roughness,
                            out float3   L,
                            out float    NdotL,
                            out float    NdotH,
                            out float    VdotH )
            {
                // Ward NDF sampling (eqs. 6 & 7 from above paper)
                float    tanTheta = roughness * sqrt(-log( max( 1e-6, u.x )));
                float    phi      = TWO_PI * u.y;

                float    cosTheta = rsqrt(1 + Sq(tanTheta));
                float3   localH = SphericalToCartesian(phi, cosTheta);

                NdotH = cosTheta;

                float3   localV = mul(V, transpose(localToWorld));
                VdotH  = saturate(dot(localV, localH));

                // Compute { localL = reflect(-localV, localH) }
                float3   localL = -localV + 2.0 * VdotH * localH;
                NdotL = localL.z;

                L = mul(localL, localToWorld);
            }

            // weightOverPdf returns the weight (without the Fresnel term) over pdf. Fresnel term must be applied by the caller.
            void ImportanceSampleWard(  float2   u,
                                        float3   V,
                                        float3x3 localToWorld,
                                        float    roughness,
                                        float    NdotV,
                                    out float3   L,
                                    out float    VdotH,
                                    out float    NdotL,
                                    out float    weightOverPdf)
            {
                float    NdotH;
                SampleWardDir( u, V, localToWorld, roughness, L, NdotL, NdotH, VdotH );

                // Importance sampling weight for each sample (eq. 9 from Walter, 2005)
                // pdf = 1 / (4PI * a² * (L.H) * (H.N)^3) * exp( ((N.H)² - 1) / (a² * (N.H)²) )                 <= From Walter, eq. 24 pdf(H) = D(H) . (N.H)
                // fr = (F(N.H) * s) / (4PI * a² * (L.H)² * (H.N)^4) * exp( ((N.H)² - 1) / (a² * (N.H)²) )      <= Moroder-Geisler version
                // weight over pdf is:
                // weightOverPdf = fr * (N.V) / pdf = s * F(N.H) * (N.V) / ((L.H) * (N.H))
                // s * F(N.H) is applied outside the function
                //
                weightOverPdf = NdotV / (VdotH * NdotH);
            }

            float4  IntegrateWardFGD( float3 V, float3 N, float roughness, uint sampleCount = 8192 )
            {
                float   NdotV    = ClampNdotV(dot(N, V));
                float4  acc      = float4(0.0, 0.0, 0.0, 0.0);

                float3x3 localToWorld = GetLocalFrame(N);

                for (uint i = 0; i < sampleCount; ++i)
                {
                    float2  u = Hammersley2d(i, sampleCount);

                    float   VdotH;
                    float   NdotL;
                    float   weightOverPdf;

                    float3  L; // Unused
                    ImportanceSampleWard(   u, V, localToWorld, roughness, NdotV,
                                            L, VdotH, NdotL, weightOverPdf);

                    if ( NdotL > 0.0 )
                    {
                        // Integral{BSDF * <N,L> dw} =
                        // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
                        // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
                        // (1 - F0) * x + F0 * y = lerp(x, y, F0)
                        acc.x += weightOverPdf * pow(1 - VdotH, 5);
                        acc.y += weightOverPdf;
                    }
                }

                acc /= sampleCount;

                return float4(acc.xy, 1.0, 0.0);
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
                float   NdotV               = input.texCoord.x;
                float   perceptualRoughness = input.texCoord.y;
                float3  V                   = float3(sqrt(1 - NdotV * NdotV), 0, NdotV);
                float3  N                   = float3(0.0, 0.0, 1.0);

                float4 preFGD = IntegrateWardFGD(V, N, PerceptualRoughnessToRoughness(perceptualRoughness));

                return float4(preFGD.xyz, 1.0);
            }

            ENDHLSL
        }
    }
    Fallback Off
}
