#ifndef UNITY_POSTFX_DITHERING
#define UNITY_POSTFX_DITHERING

TEXTURE2D_SAMPLER2D(_DitheringTex, sampler_DitheringTex);
float4 _Dithering_Coords;

float3 Dither(float3 color, float2 uv)
{
    // Final pass dithering
    // Symmetric triangular distribution on [-1,1] with maximal density at 0
    float noise = SAMPLE_TEXTURE2D(_DitheringTex, sampler_DitheringTex, uv * _Dithering_Coords.xy + _Dithering_Coords.zw).a * 2.0 - 1.0;
    noise = FastSign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

#if UNITY_COLORSPACE_GAMMA
    color += noise / 255.0;
#else
    color = SRGBToLinear(LinearToSRGB(color) + noise / 255.0);
#endif

    return color;
}

#endif // UNITY_POSTFX_DITHERING
