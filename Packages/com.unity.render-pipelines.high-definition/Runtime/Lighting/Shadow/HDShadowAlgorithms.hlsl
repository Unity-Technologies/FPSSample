// Various shadow algorithms
// There are two variants provided, one takes the texture and sampler explicitly so they can be statically passed in.
// The variant without resource parameters dynamically accesses the texture when sampling.

// We can't use multi_compile for compute shaders so we force the shadow algorithm
#if (SHADERPASS == SHADERPASS_DEFERRED_LIGHTING || SHADERPASS == SHADERPASS_VOLUMETRIC_LIGHTING || SHADERPASS == SHADERPASS_VOLUME_VOXELIZATION)
#define SHADOW_LOW // Be careful this require to update GetPunctualFilterWidthInTexels() in C# as well!
#endif

#ifdef SHADOW_LOW
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_3x3(_ShadowAtlasSize.zwxy, posTC, sampleBias, tex, samp)
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_5x5(_CascadeShadowAtlasSize.zwxy, posTC, sampleBias, tex, samp)
#endif
#ifdef SHADOW_MEDIUM
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_5x5(_ShadowAtlasSize.zwxy, posTC, sampleBias, tex, samp)
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCF_Tent_7x7(_CascadeShadowAtlasSize.zwxy, posTC, sampleBias, tex, samp)
#endif
// Note: currently quality settings for PCSS need to be expose in UI and is control in HDLightUI.cs file IsShadowSettings
#ifdef SHADOW_HIGH
#define PUNCTUAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCSS(posTC, posSS, sd.shadowMapSize.xy * _ShadowAtlasSize.zw, sd.atlasOffset, sampleBias, sd.shadowFilterParams0.x, sd.shadowFilterParams0.w, asint(sd.shadowFilterParams0.y), asint(sd.shadowFilterParams0.z), tex, samp, s_point_clamp_sampler)
// Currently PCSS is broken on directional light
#define DIRECTIONAL_FILTER_ALGORITHM(sd, posSS, posTC, sampleBias, tex, samp) SampleShadow_PCSS(posTC, posSS, sd.shadowMapSize.xy * _CascadeShadowAtlasSize.zw, sd.atlasOffset, sampleBias, sd.shadowFilterParams0.x, sd.shadowFilterParams0.w, asint(sd.shadowFilterParams0.y), asint(sd.shadowFilterParams0.z), tex, samp, s_point_clamp_sampler)
#endif

#ifndef PUNCTUAL_FILTER_ALGORITHM
#error "Undefined punctual shadow filter algorithm"
#endif
#ifndef DIRECTIONAL_FILTER_ALGORITHM
#error "Undefined directional shadow filter algorithm"
#endif

float4 EvalShadow_WorldToShadow(HDShadowData sd, float3 positionWS, bool perspProj)
{
    // Note: Due to high VGRP load we can't use the whole view projection matrix, instead we reconstruct it from
    // rotation, position and projection vectors (projection and position are stored in SGPR)
#if 0
    return mul(viewProjection, float4(positionWS, 1));
#else
    if(perspProj)
    {
        positionWS = positionWS - sd.pos;
        float3x3 view = { sd.rot0, sd.rot1, sd.rot2 };
        positionWS = mul(view, positionWS);
    }
    else
    {
        float3x4 view;
        view[0] = float4(sd.rot0, sd.pos.x);
        view[1] = float4(sd.rot1, sd.pos.y);
        view[2] = float4(sd.rot2, sd.pos.z);
        positionWS = mul(view, float4(positionWS, 1.0)).xyz;
    }

    float4x4 proj;
    proj = 0.0;
    proj._m00 = sd.proj[0];
    proj._m11 = sd.proj[1];
    proj._m22 = sd.proj[2];
    proj._m23 = sd.proj[3];
    if(perspProj)
        proj._m32 = -1.0;
    else
        proj._m33 = 1.0;

    return mul(proj, float4(positionWS, 1.0));
#endif
}

// function called by spot, point and directional eval routines to calculate shadow coordinates
float3 EvalShadow_GetTexcoordsAtlas(HDShadowData sd, float2 atlasSizeRcp, float3 positionWS, out float3 posNDC, bool perspProj)
{
    float4 posCS = EvalShadow_WorldToShadow(sd, positionWS, perspProj);
    // Avoid (0 / 0 = NaN).
    posNDC = (perspProj && posCS.w != 0) ? (posCS.xyz / posCS.w) : posCS.xyz;

    // calc TCs
    float3 posTC = float3(posNDC.xy * 0.5 + 0.5, posNDC.z);
    posTC.xy = posTC.xy * sd.shadowMapSize.xy * atlasSizeRcp + sd.atlasOffset;

    return posTC;
}

float3 EvalShadow_GetTexcoordsAtlas(HDShadowData sd, float2 atlasSizeRcp, float3 positionWS, bool perspProj)
{
    float3 ndc;
    return EvalShadow_GetTexcoordsAtlas(sd, atlasSizeRcp, positionWS, ndc, perspProj);
}

float2 EvalShadow_GetTexcoordsAtlas(HDShadowData sd, float2 atlasSizeRcp, float3 positionWS, out float2 closestSampleNDC, bool perspProj)
{
    float4 posCS = EvalShadow_WorldToShadow(sd, positionWS, perspProj);
    // Avoid (0 / 0 = NaN).
    float2 posNDC = (perspProj && posCS.w != 0) ? (posCS.xy / posCS.w) : posCS.xy;

    // calc TCs
    float2 posTC = posNDC * 0.5 + 0.5;
    closestSampleNDC = (floor(posTC * sd.shadowMapSize.xy) + 0.5) * sd.shadowMapSize.zw * 2.0 - 1.0.xx;
    return posTC * sd.shadowMapSize.xy * atlasSizeRcp + sd.atlasOffset;
}

uint2 EvalShadow_GetIntTexcoordsAtlas(HDShadowData sd, float4 atlasSize, float3 positionWS, out float2 closestSampleNDC, bool perspProj)
{
    float2 texCoords = EvalShadow_GetTexcoordsAtlas(sd, atlasSize.zw, positionWS, closestSampleNDC, perspProj);
    return uint2(texCoords * atlasSize.xy);
}

//
//  Biasing functions
//

// helper function to get the world texel size
float EvalShadow_WorldTexelSize(float4 viewBias, float L_dist, bool perspProj)
{
    return perspProj ? (viewBias.w * L_dist) : viewBias.w;
}

// used to scale down view biases to mitigate light leaking across shadowed corners
#if SHADOW_USE_VIEW_BIAS_SCALING != 0
float EvalShadow_ReceiverBiasWeightFlag(int flag)
{
    return (flag & HDSHADOWFLAG_EDGE_LEAK_FIXUP) ? 1.0 : 0.0;
}

bool EvalShadow_ReceiverBiasWeightUseNormalFlag(int flag)
{
    return (flag & HDSHADOWFLAG_EDGE_TOLERANCE_NORMAL) ? true : false;
}

float3 EvalShadow_ReceiverBiasWeightPos(float3 positionWS, float3 normalWS, float3 L, float worldTexelSize, float tolerance, bool useNormal)
{
#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0
    return positionWS + L * worldTexelSize * tolerance;
#else
    return positionWS + (useNormal ? normalWS : L) * worldTexelSize * tolerance;
#endif
}

float EvalShadow_ReceiverBiasWeight(HDShadowData sd, float2 atlasSizeRcp, float2 offset, float4 viewBias, float edgeTolerance, int flags, Texture2D tex, SamplerComparisonState samp, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspProj)
{
    float3 pos = EvalShadow_ReceiverBiasWeightPos(positionWS, normalWS, L, EvalShadow_WorldTexelSize(viewBias, L_dist, perspProj), edgeTolerance, EvalShadow_ReceiverBiasWeightUseNormalFlag(flags));
    float t = SAMPLE_TEXTURE2D_SHADOW(tex, samp, EvalShadow_GetTexcoordsAtlas(sd, atlasSizeRcp, pos, perspProj)).x;
    return lerp(1.0, t, EvalShadow_ReceiverBiasWeightFlag(flags));
}

float EvalShadow_ReceiverBiasWeight(Texture2D tex, SamplerState samp, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspProj)
{
    // only used by PCF filters
    return 1.0;
}
#else // SHADOW_USE_VIEW_BIAS_SCALING != 0
float EvalShadow_ReceiverBiasWeight(HDShadowData sd, float2 atlasSizeRcp, float2 offset, float4 viewBias, float edgeTolerance, int flags, Texture2D tex, SamplerComparisonState samp, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspProj) { return 0; }
float EvalShadow_ReceiverBiasWeight (Texture2D tex, SamplerState samp, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspProj)                                                                                                                { return 0; }
#endif // SHADOW_USE_VIEW_BIAS_SCALING != 0


// receiver bias either using the normal to weight normal and view biases, or just light view biasing
float3 EvalShadow_ReceiverBias(float4 viewBias, float3 normalBias, float3 positionWS, float3 normalWS, float3 L, float L_dist, float lightviewBiasWeight, bool perspProj)
{
#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0 // only light vector based biasing
    float viewBiasScale = viewBias.z;
    return positionWS + L * viewBiasScale * lightviewBiasWeight * EvalShadow_WorldTexelSize(viewBias, L_dist, perspProj);
#else // biasing based on the angle between the normal and the light vector
    float viewBiasMin   = viewBias.x;
    float viewBiasMax   = viewBias.y;
    float viewBiasScale = viewBias.z;
    float normalBiasMin   = normalBias.x;
    float normalBiasMax   = normalBias.y;
    float normalBiasScale = normalBias.z;

    float  NdotL       = dot(normalWS, L);
    float  sine        = sqrt(saturate(1.0 - NdotL * NdotL));
    float  tangent     = abs(NdotL) > 0.0 ? (sine / NdotL) : 0.0;
           sine        = clamp(sine    * normalBiasScale, normalBiasMin, normalBiasMax);
           tangent     = clamp(tangent * viewBiasScale * lightviewBiasWeight, viewBiasMin, viewBiasMax);
    float3 view_bias   = L        * tangent;
    float3 normal_bias = normalWS * sine;
    return positionWS + (normal_bias + view_bias) * EvalShadow_WorldTexelSize(viewBias, L_dist, perspProj);
#endif
}

// sample bias used by wide PCF filters to offset individual taps
#if SHADOW_USE_SAMPLE_BIASING != 0
float EvalShadow_SampleBiasFlag(int flag)
{
    return (flag & SAMPLE_BIAS_SCALE) ? 1.0 : 0.0;
}

float2 EvalShadow_SampleBias_Persp(HDShadowData sd, float3 positionWS, float3 normalWS, float3 tcs)
{
    float3 e1, e2;
    if(abs(normalWS.z) > 0.65)
    {
        e1 = float3(1.0, 0.0, -normalWS.x / normalWS.z);
        e2 = float3(0.0, 1.0, -normalWS.y / normalWS.z);
    }
    else if(abs(normalWS.y) > 0.65)
    {
        e1 = float3(1.0, -normalWS.x / normalWS.y, 0.0);
        e2 = float3(0.0, -normalWS.z / normalWS.y, 1.0);
    }
    else
    {
        e1 = float3(-normalWS.y / normalWS.x, 1.0, 0.0);
        e2 = float3(-normalWS.z / normalWS.x, 0.0, 1.0);
    }

    float4 p1 = EvalShadow_WorldToShadow(sd, positionWS + e1, true);
    float4 p2 = EvalShadow_WorldToShadow(sd, positionWS + e2, true);

    p1.xyz /= p1.w;
    p2.xyz /= p2.w;

    p1.xyz = float3(p1.xy * 0.5 + 0.5, p1.z);
    p2.xyz = float3(p2.xy * 0.5 + 0.5, p2.z);

    p1.xy = p1.xy * sd.shadowMapSize * _ShadowAtlasSize.zw + sd.atlasOffset;
    p2.xy = p2.xy * sd.shadowMapSize * _ShadowAtlasSize.zw + sd.atlasOffset;

    float3 nrm     = cross(p1.xyz - tcs, p2.xyz - tcs);
           nrm.xy /= -nrm.z;

    return isfinite(nrm.xy) ? (EvalShadow_SampleBiasFlag(sd.normalBias.w) * nrm.xy) : 0.0.xx;
}

float2 EvalShadow_SampleBias_Ortho(HDShadowData sd, float3 normalWS)
{
    float3x3 view = float3x3(sd.rot0, sd.rot1, sd.rot2);
    float3 nrm = mul(view, normalWS);

    nrm.x /= sd.proj[0];
    nrm.y /= sd.proj[1];
    nrm.z /= sd.proj[2];

    float2 scale = sd.shadowMapSize * _ShadowAtlasSize.zw;

    nrm.x *= sd.scale.y;
    nrm.y *= sd.scale.x;
    nrm.z *= sd.scale.x * sd.scale.y;

    nrm.xy /= -nrm.z;

    return isfinite(nrm.xy) ? (EvalShadow_SampleBiasFlag(sd.normalBias.w) * nrm.xy) : 0.0.xx;
}
#else // SHADOW_USE_SAMPLE_BIASING != 0
float2 EvalShadow_SampleBias_Persp(float3 positionWS, float3 normalWS, float3 tcs) { return 0.0.xx; }
float2 EvalShadow_SampleBias_Ortho(float3 normalWS)                              { return 0.0.xx; }
#endif // SHADOW_USE_SAMPLE_BIASING != 0


//
//  Point shadows
//
float EvalShadow_PunctualDepth(HDShadowData sd, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, float3 L, float L_dist, bool perspective)
{
    /* bias the world position */
    float recvBiasWeight = EvalShadow_ReceiverBiasWeight(sd, _ShadowAtlasSize.zw, sd.atlasOffset, sd.viewBias, sd.edgeTolerance, sd.flags, tex, samp, positionWS, normalWS, L, L_dist, perspective);
    positionWS = EvalShadow_ReceiverBias(sd.viewBias, sd.normalBias, positionWS, normalWS, L, L_dist, recvBiasWeight, perspective);
    /* get shadowmap texcoords */
    float3 posTC = EvalShadow_GetTexcoordsAtlas(sd, _ShadowAtlasSize.zw, positionWS, perspective);
    /* get the per sample bias */
    float2 sampleBias = EvalShadow_SampleBias_Persp(positionWS, normalWS, posTC);
    /* sample the texture */
    return PUNCTUAL_FILTER_ALGORITHM(sd, positionSS, posTC, sampleBias, tex, samp);
}

//
//  Directional shadows (cascaded shadow map)
//

int EvalShadow_GetSplitIndex(HDShadowContext shadowContext, int index, float3 positionWS, out float alpha, out int cascadeCount)
{
    uint   i = 0;
    float  relDistance = 0.0;
    float3 wposDir, splitSphere;

    HDShadowData sd = shadowContext.shadowDatas[index];
    HDDirectionalShadowData dsd = shadowContext.directionalShadowData;

    // find the current cascade
    for (; i < _CascadeShadowCount; i++)
    {
        float4  sphere  = dsd.sphereCascades[i];
                wposDir = -sphere.xyz + positionWS;
        float   distSq  = dot(wposDir, wposDir);
        relDistance = distSq / sphere.w;
        if (relDistance > 0.0 && relDistance <= 1.0)
        {
            splitSphere = sphere.xyz;
            wposDir    /= sqrt(distSq);
            break;
        }
    }
    int shadowSplitIndex = i < _CascadeShadowCount ? i : -1;

    float3 cascadeDir = dsd.cascadeDirection.xyz;
    cascadeCount     = dsd.cascadeDirection.w;
    float border      = dsd.cascadeBorders[shadowSplitIndex];
          alpha      = border <= 0.0 ? 0.0 : saturate((relDistance - (1.0 - border)) / border);
    float  cascDot    = dot(cascadeDir, wposDir);
          alpha      = lerp(alpha, 0.0, saturate(-cascDot * 4.0));

    return shadowSplitIndex;
}

void LoadDirectionalShadowDatas(inout HDShadowData sd, HDShadowContext shadowContext, int index)
{
    sd.proj = shadowContext.shadowDatas[index].proj;
    sd.pos = shadowContext.shadowDatas[index].pos;
    sd.viewBias = shadowContext.shadowDatas[index].viewBias;
    sd.atlasOffset = shadowContext.shadowDatas[index].atlasOffset;
}

float EvalShadow_CascadedDepth_Blend(HDShadowContext shadowContext, Texture2D tex, SamplerComparisonState samp, float2 positionSS, float3 positionWS, float3 normalWS, int index, float3 L)
{
    float   alpha;
    int     cascadeCount;
    float   shadow = 1.0;
    int     shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, alpha, cascadeCount);

    if (shadowSplitIndex >= 0.0)
    {
        HDShadowData sd = shadowContext.shadowDatas[index];
        LoadDirectionalShadowDatas(sd, shadowContext, index + shadowSplitIndex);
    
        /* normal based bias */
        float3 orig_pos = positionWS;
        float recvBiasWeight = EvalShadow_ReceiverBiasWeight(sd, _CascadeShadowAtlasSize.zw, sd.atlasOffset, sd.viewBias, sd.edgeTolerance, sd.flags, tex, samp, positionWS, normalWS, L, 1.0, false);
        positionWS = EvalShadow_ReceiverBias(sd.viewBias, sd.normalBias, positionWS, normalWS, L, 1.0, recvBiasWeight, false);
    
        /* get shadowmap texcoords */
        float3 posTC = EvalShadow_GetTexcoordsAtlas(sd, _CascadeShadowAtlasSize.zw, positionWS, false);
        /* evalute the first cascade */
        float2 sampleBias = EvalShadow_SampleBias_Ortho(normalWS);
        shadow            = DIRECTIONAL_FILTER_ALGORITHM(sd, positionSS, posTC, sampleBias, tex, samp);
        float  shadow1    = 1.0;
    
        shadowSplitIndex++;
        if (shadowSplitIndex < cascadeCount)
        {
            shadow1 = shadow;
    
            if (alpha > 0.0)
            {
                LoadDirectionalShadowDatas(sd, shadowContext, index + shadowSplitIndex);
                positionWS = EvalShadow_ReceiverBias(sd.viewBias, sd.normalBias, orig_pos, normalWS, L, 1.0, recvBiasWeight, false);
                float3 posNDC;
                posTC = EvalShadow_GetTexcoordsAtlas(sd, _CascadeShadowAtlasSize.zw, positionWS, posNDC, false);
                /* sample the texture */
                sampleBias = EvalShadow_SampleBias_Ortho(normalWS);
    
                UNITY_BRANCH
                if (all(abs(posNDC.xy) <= (1.0 - sd.shadowMapSize.zw * 0.5)))
                    shadow1 = DIRECTIONAL_FILTER_ALGORITHM(sd, positionSS, posTC, sampleBias, tex, samp);
            }
        }
        shadow = lerp(shadow, shadow1, alpha);
    }

    return shadow;
}

float EvalShadow_hash12(float2 pos)
{
    float3 p3  = frac(pos.xyx * float3(443.8975, 397.2973, 491.1871));
           p3 += dot(p3, p3.yzx + 19.19);
    return frac((p3.x + p3.y) * p3.z);
}

// TODO: optimize this using LinearEyeDepth() to avoid having to pass the shadowToWorld matrix
float EvalShadow_SampleClosestDistance_Punctual(HDShadowData sd, Texture2D tex, SamplerState sampl, float3 positionWS, float3 L, float3 lightPositionWS)
{
    float4 closestNDC = { 0,0,0,1 };
    float2 texelIdx = EvalShadow_GetTexcoordsAtlas(sd, _ShadowAtlasSize.zw, positionWS, closestNDC.xy, true);

    // sample the shadow map
    closestNDC.z = SAMPLE_TEXTURE2D_LOD(tex, sampl, texelIdx, 0).x;

    // reconstruct depth position
    float4 closestWS = mul(closestNDC, sd.shadowToWorld);
    float3 occluderPosWS = closestWS.xyz / closestWS.w;

    return distance(occluderPosWS, lightPositionWS);
}
