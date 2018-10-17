Shader "Hidden/HDRenderPipeline/DebugColorPicker"
{
    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            TEXTURE2D(_DebugColorPickerTexture);
            SAMPLER(sampler_DebugColorPickerTexture);

            float4 _ColorPickerParam; // 4 increasing threshold
            float3 _ColorPickerFontColor;
            float _ApplyLinearToSRGB;
            int _FalseColor;
            float4 _FalseColorThresholds; // 4 increasing threshold

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);

                return output;
            }

            float3 FasleColorRemap(float lum, float4 thresholds)
            {
                //Gradient from 0 to 240 deg of HUE gradient
                const float l = DegToRad(240) / TWO_PI;

                float t = lerp(0.0, l / 3, RangeRemap(thresholds.x, thresholds.y, lum))
                        + lerp(0.0, l / 3, RangeRemap(thresholds.y, thresholds.z, lum))
                        + lerp(0.0, l / 3, RangeRemap(thresholds.z, thresholds.w, lum));

                return HsvToRgb(float3(l - t, 1, 1));
            }

            float4 DisplayPixelInformationAtMousePosition(Varyings input, float4 result, float4 mouseResult, float4 mousePixelCoord)
            {
                bool flipY = ShouldFlipDebugTexture();

                if (mousePixelCoord.z >= 0.0 && mousePixelCoord.z <= 1.0 && mousePixelCoord.w >= 0 && mousePixelCoord.w <= 1.0)
                {
                    // As when we read with the color picker we don't go through the final blit (that current hardcode a conversion to sRGB)
                    // and as our material debug take it into account, we need to a transform here.
                    if (_ApplyLinearToSRGB > 0.0)
                    {
                        mouseResult.rgb = LinearToSRGB(mouseResult.rgb);
                    }

                    // Display message offset:
                    int displayTextOffsetX = 1.5 * DEBUG_FONT_TEXT_WIDTH;
                    int displayTextOffsetY;
                    if (flipY)
                    {
                        displayTextOffsetY = DEBUG_FONT_TEXT_HEIGHT;
                    }
                    else
                    {
                        displayTextOffsetY = -DEBUG_FONT_TEXT_HEIGHT;
                    }

                    uint2 displayUnormCoord = uint2(mousePixelCoord.x + displayTextOffsetX, mousePixelCoord.y + displayTextOffsetY);
                    uint2 unormCoord = input.positionCS.xy;

                    if (_ColorPickerMode == COLORPICKERDEBUGMODE_BYTE || _ColorPickerMode == COLORPICKERDEBUGMODE_BYTE4)
                    {
                        uint4 mouseValue = int4(mouseResult * 255.5);

                        DrawCharacter('R', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawInteger(mouseValue.x, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);

                        if (_ColorPickerMode == COLORPICKERDEBUGMODE_BYTE4)
                        {
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('G', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawInteger(mouseValue.y, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('B', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawInteger(mouseValue.z, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('A', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawInteger(mouseValue.w, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        }
                    }
                    else // float
                    {
                        DrawCharacter('X', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        DrawFloat(mouseResult.x, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        if (_ColorPickerMode == COLORPICKERDEBUGMODE_FLOAT4)
                        {
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('Y', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawFloat(mouseResult.y, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('Z', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawFloat(mouseResult.z, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            displayUnormCoord.x = mousePixelCoord.x + displayTextOffsetX;
                            displayUnormCoord.y += displayTextOffsetY;
                            DrawCharacter('W', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawCharacter(':', _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                            DrawFloat(mouseResult.w, _ColorPickerFontColor, unormCoord, displayUnormCoord, flipY, result.rgb);
                        }
                    }
                }

                return result;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                if (ShouldFlipDebugTexture())
                {
                    input.texcoord.y = 1.0 * _ScreenToTargetScale.y - input.texcoord.y;
                }

                float4 result = SAMPLE_TEXTURE2D(_DebugColorPickerTexture, sampler_DebugColorPickerTexture, input.texcoord);

                //Decompress value if luxMeter is active
                if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER && _ColorPickerMode != COLORPICKERDEBUGMODE_NONE)
                    result.rgb = result.rgb * LUXMETER_COMPRESSION_RATIO;
                    
                if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUMINANCE_METER)
                {
                    result = Luminance(result.rgb);
                }

                if (_FalseColor)
                    result.rgb = FasleColorRemap(Luminance(result.rgb), _FalseColorThresholds);

                if (_ColorPickerMode != COLORPICKERDEBUGMODE_NONE)
                {
                    float4 mousePixelCoord = _MousePixelCoord;
                    if (ShouldFlipDebugTexture())
                    {
                        mousePixelCoord.y = _ScreenSize.y - mousePixelCoord.y;
                        // Note: We must not flip the mousePixelCoord.w coordinate
                    }

                    float4 mouseResult = SAMPLE_TEXTURE2D(_DebugColorPickerTexture, sampler_DebugColorPickerTexture, mousePixelCoord.zw);
                    
                    //Decompress value if luxMeter is active
                    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
                        mouseResult = mouseResult * LUXMETER_COMPRESSION_RATIO;

                    // Reverse debug exposure in order to display the real values.
                    // _DebugExposure will be set to zero if the debug view does not need it so we don't need to make a special case here. It's handled in only one place in C#
                    mouseResult = mouseResult / exp2(_DebugExposure);

                    result = DisplayPixelInformationAtMousePosition(input, result, mouseResult, mousePixelCoord);
                }

                return result;
            }

            ENDHLSL
        }

    }
    Fallback Off
}
