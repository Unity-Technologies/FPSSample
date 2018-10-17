Shader "Hidden/PostProcessing/MotionBlur"
{
    HLSLINCLUDE

        #pragma target 3.0
        #include "../StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        // Camera depth texture
        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);

        // Camera motion vectors texture
        TEXTURE2D_SAMPLER2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture);
        float4 _CameraMotionVectorsTexture_TexelSize;

        // Packed velocity texture (2/10/10/10)
        TEXTURE2D_SAMPLER2D(_VelocityTex, sampler_VelocityTex);
        float2 _VelocityTex_TexelSize;

        // NeighborMax texture
        TEXTURE2D_SAMPLER2D(_NeighborMaxTex, sampler_NeighborMaxTex);
        float2 _NeighborMaxTex_TexelSize;

        // Velocity scale factor
        float _VelocityScale;

        // TileMax filter parameters
        int _TileMaxLoop;
        float2 _TileMaxOffs;

        // Maximum blur radius (in pixels)
        half _MaxBlurRadius;
        float _RcpMaxBlurRadius;

        // Filter parameters/coefficients
        half _LoopCount;

        // -----------------------------------------------------------------------------
        // Prefilter

        // Velocity texture setup
        half4 FragVelocitySetup(VaryingsDefault i) : SV_Target
        {
            // Sample the motion vector.
            float2 v = SAMPLE_TEXTURE2D(_CameraMotionVectorsTexture, sampler_CameraMotionVectorsTexture, i.texcoord).rg;

            // Apply the exposure time and convert to the pixel space.
            v *= (_VelocityScale * 0.5) * _CameraMotionVectorsTexture_TexelSize.zw;

            // Clamp the vector with the maximum blur radius.
            v /= max(1.0, length(v) * _RcpMaxBlurRadius);

            // Sample the depth of the pixel.
            half d = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord));

            // Pack into 10/10/10/2 format.
            return half4((v * _RcpMaxBlurRadius + 1.0) * 0.5, d, 0.0);
        }

        half2 MaxV(half2 v1, half2 v2)
        {
            return dot(v1, v1) < dot(v2, v2) ? v2 : v1;
        }

        // TileMax filter (2 pixel width with normalization)
        half4 FragTileMax1(VaryingsDefault i) : SV_Target
        {
            float4 d = _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);

            half2 v1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.xy).rg;
            half2 v2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.zy).rg;
            half2 v3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.xw).rg;
            half2 v4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.zw).rg;

            v1 = (v1 * 2.0 - 1.0) * _MaxBlurRadius;
            v2 = (v2 * 2.0 - 1.0) * _MaxBlurRadius;
            v3 = (v3 * 2.0 - 1.0) * _MaxBlurRadius;
            v4 = (v4 * 2.0 - 1.0) * _MaxBlurRadius;

            return half4(MaxV(MaxV(MaxV(v1, v2), v3), v4), 0.0, 0.0);
        }

        // TileMax filter (2 pixel width)
        half4 FragTileMax2(VaryingsDefault i) : SV_Target
        {
            float4 d = _MainTex_TexelSize.xyxy * float4(-0.5, -0.5, 0.5, 0.5);

            half2 v1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.xy).rg;
            half2 v2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.zy).rg;
            half2 v3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.xw).rg;
            half2 v4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.zw).rg;

            return half4(MaxV(MaxV(MaxV(v1, v2), v3), v4), 0.0, 0.0);
        }

        // TileMax filter (variable width)
        half4 FragTileMaxV(VaryingsDefault i) : SV_Target
        {
            float2 uv0 = i.texcoord + _MainTex_TexelSize.xy * _TileMaxOffs.xy;

            float2 du = float2(_MainTex_TexelSize.x, 0.0);
            float2 dv = float2(0.0, _MainTex_TexelSize.y);

            half2 vo = 0.0;

            UNITY_LOOP
            for (int ix = 0; ix < _TileMaxLoop; ix++)
            {
                UNITY_LOOP
                for (int iy = 0; iy < _TileMaxLoop; iy++)
                {
                    float2 uv = uv0 + du * ix + dv * iy;
                    vo = MaxV(vo, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rg);
                }
            }

            return half4(vo, 0.0, 0.0);
        }

        // NeighborMax filter
        half4 FragNeighborMax(VaryingsDefault i) : SV_Target
        {
            const half cw = 1.01; // Center weight tweak

            float4 d = _MainTex_TexelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0);

            half2 v1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - d.xy).rg;
            half2 v2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - d.wy).rg;
            half2 v3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - d.zy).rg;

            half2 v4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord - d.xw).rg;
            half2 v5 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord).rg * cw;
            half2 v6 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.xw).rg;

            half2 v7 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.zy).rg;
            half2 v8 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.wy).rg;
            half2 v9 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + d.xy).rg;

            half2 va = MaxV(v1, MaxV(v2, v3));
            half2 vb = MaxV(v4, MaxV(v5, v6));
            half2 vc = MaxV(v7, MaxV(v8, v9));

            return half4(MaxV(va, MaxV(vb, vc)) * (1.0 / cw), 0.0, 0.0);
        }

        // -----------------------------------------------------------------------------
        // Reconstruction

        // Returns true or false with a given interval.
        bool Interval(half phase, half interval)
        {
            return frac(phase / interval) > 0.499;
        }

        // Jitter function for tile lookup
        float2 JitterTile(float2 uv)
        {
            float rx, ry;
            sincos(GradientNoise(uv + float2(2.0, 0.0)) * TWO_PI, ry, rx);
            return float2(rx, ry) * _NeighborMaxTex_TexelSize.xy * 0.25;
        }

        // Velocity sampling function
        half3 SampleVelocity(float2 uv)
        {
            half3 v = SAMPLE_TEXTURE2D_LOD(_VelocityTex, sampler_VelocityTex, uv, 0.0).xyz;
            return half3((v.xy * 2.0 - 1.0) * _MaxBlurRadius, v.z);
        }

        // Reconstruction filter
        half4 FragReconstruction(VaryingsDefault i) : SV_Target
        {
            // Color sample at the center point
            const half4 c_p = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);

            // Velocity/Depth sample at the center point
            const half3 vd_p = SampleVelocity(i.texcoord);
            const half l_v_p = max(length(vd_p.xy), 0.5);
            const half rcp_d_p = 1.0 / vd_p.z;

            // NeighborMax vector sample at the center point
            const half2 v_max = SAMPLE_TEXTURE2D(_NeighborMaxTex, sampler_NeighborMaxTex, i.texcoord + JitterTile(i.texcoord)).xy;
            const half l_v_max = length(v_max);
            const half rcp_l_v_max = 1.0 / l_v_max;

            // Escape early if the NeighborMax vector is small enough.
            if (l_v_max < 2.0) return c_p;

            // Use V_p as a secondary sampling direction except when it's too small
            // compared to V_max. This vector is rescaled to be the length of V_max.
            const half2 v_alt = (l_v_p * 2.0 > l_v_max) ? vd_p.xy * (l_v_max / l_v_p) : v_max;

            // Determine the sample count.
            const half sc = floor(min(_LoopCount, l_v_max * 0.5));

            // Loop variables (starts from the outermost sample)
            const half dt = 1.0 / sc;
            const half t_offs = (GradientNoise(i.texcoord) - 0.5) * dt;
            half t = 1.0 - dt * 0.5;
            half count = 0.0;

            // Background velocity
            // This is used for tracking the maximum velocity in the background layer.
            half l_v_bg = max(l_v_p, 1.0);

            // Color accumlation
            half4 acc = 0.0;

            UNITY_LOOP
            while (t > dt * 0.25)
            {
                // Sampling direction (switched per every two samples)
                const half2 v_s = Interval(count, 4.0) ? v_alt : v_max;

                // Sample position (inverted per every sample)
                const half t_s = (Interval(count, 2.0) ? -t : t) + t_offs;

                // Distance to the sample position
                const half l_t = l_v_max * abs(t_s);

                // UVs for the sample position
                const float2 uv0 = i.texcoord + v_s * t_s * _MainTex_TexelSize.xy;
                const float2 uv1 = i.texcoord + v_s * t_s * _VelocityTex_TexelSize.xy;

                // Color sample
                const half3 c = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv0, 0.0).rgb;

                // Velocity/Depth sample
                const half3 vd = SampleVelocity(uv1);

                // Background/Foreground separation
                const half fg = saturate((vd_p.z - vd.z) * 20.0 * rcp_d_p);

                // Length of the velocity vector
                const half l_v = lerp(l_v_bg, length(vd.xy), fg);

                // Sample weight
                // (Distance test) * (Spreading out by motion) * (Triangular window)
                const half w = saturate(l_v - l_t) / l_v * (1.2 - t);

                // Color accumulation
                acc += half4(c, 1.0) * w;

                // Update the background velocity.
                l_v_bg = max(l_v_bg, l_v);

                // Advance to the next sample.
                t = Interval(count, 2.0) ? t - dt : t;
                count += 1.0;
            }

            // Add the center sample.
            acc += half4(c_p.rgb, 1.0) * (1.2 / (l_v_bg * sc * 2.0));

            return half4(acc.rgb / acc.a, c_p.a);
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // (0) Velocity texture setup
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragVelocitySetup

            ENDHLSL
        }

        // (1) TileMax filter (2 pixel width with normalization)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragTileMax1

            ENDHLSL
        }

        //  (2) TileMax filter (2 pixel width)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragTileMax2

            ENDHLSL
        }

        // (3) TileMax filter (variable width)
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragTileMaxV

            ENDHLSL
        }

        // (4) NeighborMax filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragNeighborMax

            ENDHLSL
        }

        // (5) Reconstruction filter
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragReconstruction

            ENDHLSL
        }
    }
}
