#ifndef UNITY_DEBUG_DISPLAY_INCLUDED
#define UNITY_DEBUG_DISPLAY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/MaterialDebug.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/LightingDebug.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/MipMapDebug.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/ColorPickerDebug.cs.hlsl"

CBUFFER_START(UnityDebugDisplay)
// Set of parameters available when switching to debug shader mode
int _DebugLightingMode; // Match enum DebugLightingMode
int _DebugShadowMapMode;
int _DebugViewMaterial; // Contain the id (define in various materialXXX.cs.hlsl) of the property to display
int _DebugMipMapMode; // Match enum DebugMipMapMode
int _DebugMipMapModeTerrainTexture; // Match enum DebugMipMapModeTerrainTexture
int _ColorPickerMode; // Match enum ColorPickerDebugMode
int _DebugStep;
int _DebugDepthPyramidMip;
float4 _DebugLightingAlbedo; // x == bool override, yzw = albedo for diffuse
float4 _DebugLightingSmoothness; // x == bool override, y == override value
float4 _DebugLightingNormal; // x == bool override
float4 _DebugLightingSpecularColor; // x == bool override, yzw = specular color
float4 _DebugLightingEmissiveColor; // x == bool override, yzw = emissive color
float4 _MousePixelCoord;  // xy unorm, zw norm
float4 _MouseClickPixelCoord;  // xy unorm, zw norm
float _DebugExposure;
CBUFFER_END

float debugShadowAttenuation = 0;

StructuredBuffer<int2>  _DebugDepthPyramidOffsets;

// When displaying lux meter we compress the light in order to be able to display value higher than 65504
// The sun is between 100 000 and 150 000, so we use 4 to be able to cover such a range (4 * 65504)
#define LUXMETER_COMPRESSION_RATIO  4

TEXTURE2D(_DebugFont); // Debug font to write string in shader

void GetPropertiesDataDebug(uint paramId, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEWPROPERTIES_TESSELLATION:
#ifdef TESSELLATION_ON
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_PIXEL_DISPLACEMENT:
#ifdef _PIXEL_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_VERTEX_DISPLACEMENT:
#ifdef _VERTEX_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_TESSELLATION_DISPLACEMENT:
#ifdef _TESSELLATION_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_DEPTH_OFFSET:
#ifdef _DEPTHOFFSET_ON  // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_LIGHTMAP:
#if defined(LIGHTMAP_ON) || defined (DIRLIGHTMAP_COMBINED) || defined(DYNAMICLIGHTMAP_ON)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_INSTANCING:
#if defined(UNITY_INSTANCING_ENABLED)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;
    }
}

float3 GetTextureDataDebug(uint paramId, float2 uv, Texture2D tex, float4 texelSize, float4 mipInfo, float3 originalColor)
{
    float3 outColor = originalColor;

    switch (paramId)
    {
    case DEBUGMIPMAPMODE_MIP_RATIO:
        outColor = GetDebugMipColorIncludingMipReduction(originalColor, tex, texelSize, uv, mipInfo);
        break;
    case DEBUGMIPMAPMODE_MIP_COUNT:
        outColor = GetDebugMipCountColor(originalColor, tex);
        break;
    case DEBUGMIPMAPMODE_MIP_COUNT_REDUCTION:
        outColor = GetDebugMipReductionColor(tex, mipInfo);
        break;
    case DEBUGMIPMAPMODE_STREAMING_MIP_BUDGET:
        outColor = GetDebugStreamingMipColor(tex, mipInfo);
        break;
    case DEBUGMIPMAPMODE_STREAMING_MIP:
        outColor = GetDebugStreamingMipColorBlended(originalColor, tex, mipInfo);
        break;
    }

    return outColor;
}

// DebugFont code assume black and white font with texture size 256x128 with bloc of 16x16
#define DEBUG_FONT_TEXT_WIDTH   16
#define DEBUG_FONT_TEXT_HEIGHT  16
#define DEBUG_FONT_TEXT_COUNT_X 16
#define DEBUG_FONT_TEXT_COUNT_Y 8
#define DEBUG_FONT_TEXT_ASCII_START 32

#define DEBUG_FONT_TEXT_SCALE_WIDTH 10 // This control the spacing between characters (if a character fill the text block it will overlap).

// Only support ASCII symbol from DEBUG_FONT_TEXT_ASCII_START to 126
// return black or white depends if we hit font character or not
// currentUnormCoord is current unormalized screen position
// fixedUnormCoord is the position where we want to draw something, this will be incremented by block font size in provided direction
// color is current screen color
// color of the font to use
// direction is 1 or -1 and indicate fixedUnormCoord block shift
void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, bool flipY, inout float3 color, int direction)
{
    // Are we inside a font display block on the screen ?
    uint2 localCharCoord = currentUnormCoord - fixedUnormCoord;
    if (localCharCoord.x >= 0 && localCharCoord.x < DEBUG_FONT_TEXT_WIDTH && localCharCoord.y >= 0 && localCharCoord.y < DEBUG_FONT_TEXT_HEIGHT)
    {
        if (!flipY)
            localCharCoord.y = DEBUG_FONT_TEXT_HEIGHT - localCharCoord.y;

        asciiValue -= DEBUG_FONT_TEXT_ASCII_START; // Our font start at ASCII table 32;
        uint2 asciiCoord = uint2(asciiValue % DEBUG_FONT_TEXT_COUNT_X, asciiValue / DEBUG_FONT_TEXT_COUNT_X);
        // Unorm coordinate inside the font texture
        uint2 unormTexCoord = asciiCoord * uint2(DEBUG_FONT_TEXT_WIDTH, DEBUG_FONT_TEXT_HEIGHT) + localCharCoord;
        // normalized coordinate
        float2 normTexCoord = float2(unormTexCoord) / float2(DEBUG_FONT_TEXT_WIDTH * DEBUG_FONT_TEXT_COUNT_X, DEBUG_FONT_TEXT_HEIGHT * DEBUG_FONT_TEXT_COUNT_Y);

        #if UNITY_UV_STARTS_AT_TOP
        normTexCoord.y = 1.0 - normTexCoord.y;
        #endif

        float charColor = SAMPLE_TEXTURE2D_LOD(_DebugFont, s_point_clamp_sampler, normTexCoord, 0).r;
        color = color * (1.0 - charColor) + charColor * fontColor;
    }

    fixedUnormCoord.x += DEBUG_FONT_TEXT_SCALE_WIDTH * direction;
}

// Shortcut to not have to file direction
void DrawCharacter(uint asciiValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, bool flipY, inout float3 color)
{
    DrawCharacter(asciiValue, fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, 1);
}

// Draw a signed integer
// Can't display more than 16 digit
// The two following parameter are for float representation
// leading0 is used when drawing frac part of a float to draw the leading 0 (call is in charge of it)
// forceNegativeSign is used to force to display a negative sign as -0 is not recognize
void DrawInteger(int intValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, bool flipY, inout float3 color, int leading0, bool forceNegativeSign)
{
    const uint maxStringSize = 16;

    uint absIntValue = abs(intValue);

    // 1. Get size of the number of display
    int numEntries = min((intValue == 0 ? 0 : log10(absIntValue)) + ((intValue < 0 || forceNegativeSign) ? 1 : 0) + leading0, maxStringSize);

    // 2. Shift curseur to last location as we will go reverse
    fixedUnormCoord.x += numEntries * DEBUG_FONT_TEXT_SCALE_WIDTH;

    // 3. Display the number
    for (uint j = 0; j < maxStringSize; ++j)
    {
        // Numeric value incurrent font start on the second row at 0
        DrawCharacter((absIntValue % 10) + '0', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, -1);
        if (absIntValue  < 10)
            break;
        absIntValue /= 10;
    }

    // 4. Display leading 0
    if (leading0 > 0)
    {
        for (int i = 0; i < leading0; ++i)
        {
            DrawCharacter('0', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, -1);
        }
    }

    // 5. Display sign
    if (intValue < 0 || forceNegativeSign)
    {
        DrawCharacter('-', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, -1);
    }

    // 6. Reset cursor at end location
    fixedUnormCoord.x += (numEntries + 2) * DEBUG_FONT_TEXT_SCALE_WIDTH;
}

void DrawInteger(int intValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, bool flipY, inout float3 color)
{
    DrawInteger(intValue, fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, 0, false);
}

void DrawFloat(float floatValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, bool flipY, inout float3 color)
{
    if (IsNan(floatValue))
    {
        DrawCharacter('N', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color);
        DrawCharacter('a', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color);
        DrawCharacter('N', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color);
    }
    else
    {
        int intValue = int(floatValue);
        bool forceNegativeSign = floatValue >= 0.0f ? false : true;
        DrawInteger(intValue, fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, 0, forceNegativeSign);
        DrawCharacter('.', fontColor, currentUnormCoord, fixedUnormCoord, flipY, color);
        int fracValue = int(frac(abs(floatValue)) * 1e6); // 6 digit
        int leading0 = 6 - (int(log10(fracValue)) + 1); // Counting leading0 to add in front of the float
        DrawInteger(fracValue, fontColor, currentUnormCoord, fixedUnormCoord, flipY, color, leading0, false);
    }
}

// Debug rendering is performed at the end of the frame (after post-processing).
// Debug textures are never flipped upside-down automatically. Therefore, we must always flip manually.
bool ShouldFlipDebugTexture()
{
    #if UNITY_UV_STARTS_AT_TOP
        return (_ProjectionParams.x > 0);
    #else
        return (_ProjectionParams.x < 0);
    #endif
}

#endif
