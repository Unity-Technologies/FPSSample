Shader "Hidden/HDRenderPipeline/DebugViewTiles"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #define LIGHTLOOP_TILE_PASS

            #pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
            #pragma multi_compile SHOW_LIGHT_CATEGORIES SHOW_FEATURE_VARIANTS

            //-------------------------------------------------------------------------------------
            // Include
            //-------------------------------------------------------------------------------------

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            // Note: We have fix as guidelines that we have only one deferred material (with control of GBuffer enabled). Mean a users that add a new
            // deferred material must replace the old one here. If in the future we want to support multiple layout (cause a lot of consistency problem),
            // the deferred shader will require to use multicompile.
            #define UNITY_MATERIAL_LIT // Need to be define before including Material.hlsl
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl" // This include Material.hlsl
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            //-------------------------------------------------------------------------------------
            // variable declaration
            //-------------------------------------------------------------------------------------

            uint _ViewTilesFlags;
            uint _NumTiles;

            StructuredBuffer<uint> g_TileList;
            Buffer<uint> g_DispatchIndirectBuffer;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                int variant : TEXCOORD0;
            };

#if SHOW_FEATURE_VARIANTS
            Varyings Vert(uint vertexID : SV_VertexID)
            {
                uint quadIndex = vertexID / 6;
                uint quadVertex = vertexID - quadIndex * 6;
                quadVertex = (0x312210 >> (quadVertex<<2)) & 3; //remap [0,5]->[0,3]

                uint2 tileSize = GetTileSize();

                uint variant = 0;
                while (quadIndex >= g_DispatchIndirectBuffer[variant * 3 + 0] && variant < NUM_FEATURE_VARIANTS)
                {
                    quadIndex -= g_DispatchIndirectBuffer[variant * 3 + 0];
                    variant++;
                }

                uint tileIndex = g_TileList[variant * _NumTiles + quadIndex];
                uint2 tileCoord = uint2(tileIndex & 0xFFFF, tileIndex >> 16);
                uint2 pixelCoord = (tileCoord + uint2((quadVertex+1) & 1, (quadVertex >> 1) & 1)) * tileSize;

                float2 clipCoord = (pixelCoord * _ScreenSize.zw) * 2.0 - 1.0;
                if (!ShouldFlipDebugTexture()) // Need to do this negative test to have it work correctly on windows in scene view and game view
                {
                    clipCoord.y *= -1;
                }

                Varyings output;
                output.positionCS = float4(clipCoord, 0, 1.0);
                output.variant = variant;
                return output;
            }
#else
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.variant = 0; // unused
                return output;
            }
#endif

            float4 AlphaBlend(float4 c0, float4 c1) // c1 over c0
            {
                return float4(lerp(c0.rgb, c1.rgb, c1.a), c0.a + c1.a - c0.a * c1.a);
            }

            float4 OverlayHeatMap(uint2 pixCoord, uint n)
            {
                const float4 kRadarColors[12] =
                {
                    float4(0.0, 0.0, 0.0, 0.0),   // black
                    float4(0.0, 0.0, 0.6, 0.5),   // dark blue
                    float4(0.0, 0.0, 0.9, 0.5),   // blue
                    float4(0.0, 0.6, 0.9, 0.5),   // light blue
                    float4(0.0, 0.9, 0.9, 0.5),   // cyan
                    float4(0.0, 0.9, 0.6, 0.5),   // blueish green
                    float4(0.0, 0.9, 0.0, 0.5),   // green
                    float4(0.6, 0.9, 0.0, 0.5),   // yellowish green
                    float4(0.9, 0.9, 0.0, 0.5),   // yellow
                    float4(0.9, 0.6, 0.0, 0.5),   // orange
                    float4(0.9, 0.0, 0.0, 0.5),   // red
                    float4(1.0, 0.0, 0.0, 0.9)    // strong red
                };

                float maxNrLightsPerTile = 31; // TODO: setup a constant for that

                int colorIndex = n == 0 ? 0 : (1 + (int)floor(10 * (log2((float)n) / log2(maxNrLightsPerTile))));
                colorIndex = colorIndex < 0 ? 0 : colorIndex;
                float4 col = colorIndex > 11 ? float4(1.0, 1.0, 1.0, 1.0) : kRadarColors[colorIndex];

                int2 coord = pixCoord - int2(1, 1);

                float4 color = float4(PositivePow(col.xyz, 2.2), 0.3 * col.w);
                if (n >= 0)
                {
                    if (SampleDebugFontNumber(coord, n))        // Shadow
                        color = float4(0, 0, 0, 1);
                    if (SampleDebugFontNumber(coord + 1, n))    // Text
                        color = float4(1, 1, 1, 1);
                }
                return color;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                if (ShouldFlipDebugTexture())
                {
                    input.positionCS.y = _ScreenSize.y - input.positionCS.y;
                }

                // positionCS is SV_Position
                float depth = LOAD_TEXTURE2D(_CameraDepthTexture, input.positionCS.xy).x;
                PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, uint2(input.positionCS.xy) / GetTileSize());

                int2 pixelCoord = posInput.positionSS.xy;
                int2 tileCoord = (float2)pixelCoord / GetTileSize();
                int2 mouseTileCoord = _MousePixelCoord.xy / GetTileSize();
                int2 offsetInTile = pixelCoord - tileCoord * GetTileSize();

                int n = 0;
#ifdef SHOW_LIGHT_CATEGORIES
                for (int category = 0; category < LIGHTCATEGORY_COUNT; category++)
                {
                    uint mask = 1u << category;
                    if (mask & _ViewTilesFlags)
                    {
                        uint start;
                        uint count;
                        GetCountAndStart(posInput, category, start, count);
                        n += count;
                    }
                }
                if (n == 0)
                    n = -1;
#else
                n = input.variant;
#endif

                float4 result = float4(0.0, 0.0, 0.0, 0.0);

                // Tile overlap counter
                if (n >= 0)
                {
                    result = OverlayHeatMap(int2(posInput.positionSS.xy) & (GetTileSize() - 1), n);
                }

#ifdef SHOW_LIGHT_CATEGORIES
                // Highlight selected tile
                if (all(mouseTileCoord == tileCoord))
                {
                    bool border = any(offsetInTile == 0 || offsetInTile == (int)GetTileSize() - 1);
                    float4 result2 = float4(1.0, 1.0, 1.0, border ? 1.0 : 0.5);
                    result = AlphaBlend(result, result2);
                }

                // Print light lists for selected tile at the bottom of the screen
                int maxLights = 32;
                if (tileCoord.y < LIGHTCATEGORY_COUNT && tileCoord.x < maxLights + 3)
                {
                    float depthMouse = LOAD_TEXTURE2D(_CameraDepthTexture, _MousePixelCoord.xy).x;
                    PositionInputs mousePosInput = GetPositionInput(_MousePixelCoord.xy, _ScreenSize.zw, depthMouse, UNITY_MATRIX_I_VP, UNITY_MATRIX_V, mouseTileCoord);

                    uint category = (LIGHTCATEGORY_COUNT - 1) - tileCoord.y;
                    uint start;
                    uint count;
                    GetCountAndStart(mousePosInput, category, start, count);

                    float4 result2 = float4(.1,.1,.1,.9);
                    int2 fontCoord = int2(pixelCoord.x, offsetInTile.y);
                    int lightListIndex = tileCoord.x - 2;

                    int n = -1;
                    if(tileCoord.x == 0)
                    {
                        n = (int)count;
                    }
                    else if(lightListIndex >= 0 && lightListIndex < (int)count)
                    {
                        n = FetchIndex(start, lightListIndex);
                    }

                    if (n >= 0)
                    {
                        if (SampleDebugFontNumber(offsetInTile, n))
                            result2 = float4(0.0, 0.0, 0.0, 1.0);
                        if (SampleDebugFontNumber(offsetInTile + 1, n))
                            result2 = float4(1.0, 1.0, 1.0, 1.0);
                    }

                    result = AlphaBlend(result, result2);
                }
#endif

                return result;
            }

            ENDHLSL
        }
    }
    Fallback Off
}
