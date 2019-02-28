VFXUVData GetUVData(VFX_VARYING_PS_INPUTS i) // uvs are provided from interpolants
{
    VFXUVData data = (VFXUVData)0;
#ifdef VFX_VARYING_UV
    data.uvs.xy = i.VFX_VARYING_UV.xy;
#if USE_FLIPBOOK_INTERPOLATION && defined(VFX_VARYING_FRAMEBLEND)
    data.uvs.zw = i.VFX_VARYING_UV.zw;
    data.blend = i.VFX_VARYING_FRAMEBLEND;
#endif
#endif
    return data;
}

VFXUVData GetUVData(VFX_VARYING_PS_INPUTS i,float2 uv) // uvs are provided from ps directly
{
#ifdef VFX_VARYING_FLIPBOOKSIZE
    float2 flipBookSize = i.VFX_VARYING_FLIPBOOKSIZE;
#else
    float2 flipBookSize = float2(1, 1);
#endif

#ifdef VFX_VARYING_INVFLIPBOOKSIZE
    float2 invFlipBookSize = i.VFX_VARYING_INVFLIPBOOKSIZE;
#else
    float2 invFlipBookSize = 1.0f / flipBookSize;
#endif

#ifdef VFX_VARYING_TEXINDEX
    float texIndex = i.VFX_VARYING_TEXINDEX;
#else
    float texIndex = 0.0f;
#endif

    return GetUVData(flipBookSize, invFlipBookSize, uv, texIndex);
}

float4 VFXGetParticleColor(VFX_VARYING_PS_INPUTS i)
{
    float4 color = 1.0f;
    #if VFX_NEEDS_COLOR_INTERPOLATOR
    #ifdef VFX_VARYING_COLOR
    color.rgb *= i.VFX_VARYING_COLOR;
    #endif
    #ifdef VFX_VARYING_ALPHA
    color.a *= i.VFX_VARYING_ALPHA;
    #endif
    #endif
    return color;
}

float VFXGetSoftParticleFade(VFX_VARYING_PS_INPUTS i)
{
    float fade = 1.0f;
    #if USE_SOFT_PARTICLE && defined(VFX_VARYING_INVSOFTPARTICLEFADEDISTANCE)
    float sceneZ = VFXLinearEyeDepth(VFXSampleDepth(i.VFX_VARYING_POSCS));
    fade = saturate(i.VFX_VARYING_INVSOFTPARTICLEFADEDISTANCE * (sceneZ - i.VFX_VARYING_POSCS.w));
    fade = fade * fade * (3.0 - (2.0 * fade)); // Smoothsteping the fade
    #endif
    return fade;
}

float4 VFXGetTextureColor(VFXSampler2D s,VFX_VARYING_PS_INPUTS i)
{
    return SampleTexture(s, GetUVData(i));
}

float4 VFXGetTextureColorWithProceduralUV(VFXSampler2D s, VFX_VARYING_PS_INPUTS i, float2 uv)
{
    return SampleTexture(s, GetUVData(i, uv));
}

float3 VFXGetTextureNormal(VFXSampler2D s,float2 uv)
{
    float4 packedNormal = s.t.Sample(s.s,uv);
    packedNormal.w *= packedNormal.x;
    float3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

float4 VFXGetFragmentColor(VFX_VARYING_PS_INPUTS i)
{
    float4 color = VFXGetParticleColor(i);
    color.a *= VFXGetSoftParticleFade(i);
    return color;
}

void VFXClipFragmentColor(float alpha,VFX_VARYING_PS_INPUTS i)
{
    #if USE_ALPHA_TEST
    #if defined(VFX_VARYING_ALPHATHRESHOLD)
    clip(alpha - i.VFX_VARYING_ALPHATHRESHOLD);
    #else
    clip(alpha - VFX_EPSILON);
    #endif
    #endif
}

float4 VFXApplyFog(float4 color,VFX_VARYING_PS_INPUTS i)
{
    #if USE_FOG && defined(VFX_VARYING_POSCS) && defined(VFX_VARYING_POSWS)
    return VFXApplyFog(color,i.VFX_VARYING_POSCS,i.VFX_VARYING_POSWS);
    #else
    return color;
    #endif
}
