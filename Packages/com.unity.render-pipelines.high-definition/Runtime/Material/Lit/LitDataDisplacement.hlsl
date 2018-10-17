// Share by Lit and LayeredLit. Return object scaling for displacement map depends if it is vertex (affect vertex displacement) or pixel displacement (affect tiling)
float3 GetDisplacementObjectScale(bool vertexDisplacement)
{
    float3 objectScale = float3(1.0, 1.0, 1.0);

    // TODO: This should be an uniform for the object, this code should be remove once we have it. - Workaround for now
    // To handle object scaling with pixel displacement we need to multiply the view vector by the inverse scale.
    // To Handle object scaling with vertex/tessellation displacement we must multiply displacement by object scale
    // Currently we extract either the scale (ObjectToWorld) or the inverse scale (worldToObject) directly by taking the transform matrix
    float4x4 worldTransform;
    if (vertexDisplacement)
    {
        worldTransform = GetObjectToWorldMatrix();
    }

    else
    {
        worldTransform = GetWorldToObjectMatrix();
    }

    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    // In the specific case of pixel displacement mapping, to get a consistent behavior compare to tessellation we require to not take into account y scale if lock object scale is not enabled
#if !defined(_PIXEL_DISPLACEMENT) || (defined(_PIXEL_DISPLACEMENT_LOCK_OBJECT_SCALE))
    objectScale.y = length(float3(worldTransform._m10, worldTransform._m11, worldTransform._m12));
#endif
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));

    return objectScale;
}

#ifndef LAYERED_LIT_SHADER

// Note: This function is call by both Per vertex and Per pixel displacement
float GetMaxDisplacement()
{
    float maxDisplacement = 0.0;
#if defined(_HEIGHTMAP)
    maxDisplacement = abs(_HeightAmplitude); // _HeightAmplitude can be negative if min and max are inverted, but the max displacement must be positive
#endif
    return maxDisplacement;
}

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP)
    if (layerTexCoord.base.mappingType == UV_MAPPING_TRIPLANAR)
    {
        minUvSize = min(layerTexCoord.base.uvZY * _HeightMap_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base.uvXZ * _HeightMap_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base.uvXY * _HeightMap_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base.uv * _HeightMap_TexelSize.zw, minUvSize);
    }
#endif

    return minUvSize;
}

struct PerPixelHeightDisplacementParam
{
    float2 uv;
};

float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
    // Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
    // Tiling is automatically handled correctly here.
    return SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, param.uv + texOffsetCurrent, lod).r;
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl"

void ApplyDisplacementTileScale(inout float height)
{
    // Inverse tiling scale = 2 / (abs(_BaseColorMap_ST.x) + abs(_BaseColorMap_ST.y)
    // Inverse tiling scale *= (1 / _TexWorldScale) if planar or triplanar
#ifdef _DISPLACEMENT_LOCK_TILING_SCALE
    height *= _InvTilingScale;
#endif
}

float ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
#if defined(_PIXEL_DISPLACEMENT) && defined(_HEIGHTMAP)
    // These variables are known at the compile time.
    bool isPlanar = layerTexCoord.base.mappingType == UV_MAPPING_PLANAR;
    bool isTriplanar = layerTexCoord.base.mappingType == UV_MAPPING_TRIPLANAR;

    // See comment in layered version for details
    float  maxHeight = GetMaxDisplacement();
    ApplyDisplacementTileScale(maxHeight);
    float2 minUvSize = GetMinUvSize(layerTexCoord);
    float  lod       = ComputeTextureLOD(minUvSize);

    // TODO: precompute uvSpaceScale
    float2 invPrimScale = (isPlanar || isTriplanar) ? float2(1.0, 1.0) : _InvPrimScale.xy;
    float  worldScale   = (isPlanar || isTriplanar) ? _TexWorldScale : 1.0;
    float2 uvSpaceScale = invPrimScale * _BaseColorMap_ST.xy * (worldScale * maxHeight);
    float2 scaleOffsetDetails = _DetailMap_ST.xy;

    PerPixelHeightDisplacementParam ppdParam;

    float height = 0; // final height processed
    float NdotV  = 0;

    // planar/triplanar
    float2 uvXZ;
    float2 uvXY;
    float2 uvZY;
    GetTriplanarCoordinate(V, uvXZ, uvXY, uvZY);

    // TODO: support object space planar/triplanar ?

    // We need to calculate the texture space direction. It depends on the mapping.
    if (isTriplanar)
    {
        float planeHeight;

        // Perform a POM in each direction and modify appropriate texture coordinate
        UNITY_BRANCH if (layerTexCoord.triplanarWeights.x >= 0.001)
        {
            ppdParam.uv      = layerTexCoord.base.uvZY;
            float3 viewDirTS = float3(uvZY, abs(V.x));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps  = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset    = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, planeHeight);

            // Apply offset to all triplanar UVSet
            layerTexCoord.base.uvZY    += offset;
            layerTexCoord.details.uvZY += offset * scaleOffsetDetails;
            height += layerTexCoord.triplanarWeights.x * planeHeight;
            NdotV  += layerTexCoord.triplanarWeights.x * viewDirTS.z;
        }

        UNITY_BRANCH if (layerTexCoord.triplanarWeights.y >= 0.001)
        {
            ppdParam.uv      = layerTexCoord.base.uvXZ;
            float3 viewDirTS = float3(uvXZ, abs(V.y));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps  = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset    = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, planeHeight);

            layerTexCoord.base.uvXZ    += offset;
            layerTexCoord.details.uvXZ += offset * scaleOffsetDetails;
            height += layerTexCoord.triplanarWeights.y * planeHeight;
            NdotV  += layerTexCoord.triplanarWeights.y * viewDirTS.z;
        }

        UNITY_BRANCH if (layerTexCoord.triplanarWeights.z >= 0.001)
        {
            ppdParam.uv      = layerTexCoord.base.uvXY;
            float3 viewDirTS = float3(uvXY, abs(V.z));
            float3 viewDirUV = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
            float  unitAngle = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
            int    numSteps  = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
            float2 offset    = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, planeHeight);

            layerTexCoord.base.uvXY    += offset;
            layerTexCoord.details.uvXY += offset * scaleOffsetDetails;
            height += layerTexCoord.triplanarWeights.z * planeHeight;
            NdotV  += layerTexCoord.triplanarWeights.z * viewDirTS.z;
        }
    }
    else
    {
        ppdParam.uv = layerTexCoord.base.uv; // For planar it is uv too, not uvXZ

        // Note: The TBN is not normalize as it is based on mikkt. We should normalize it, but POM is always use on simple enough surfarce that mean it is not required (save 2 normalize). Tag: SURFACE_GRADIENT
        float3 viewDirTS = isPlanar ? float3(uvXZ, V.y) : TransformWorldToTangent(V, input.worldToTangent) * GetDisplacementObjectScale(false).xzy; // Switch from Y-up to Z-up (as we move to tangent space)
        NdotV = viewDirTS.z;

        // Transform the view vector into the UV space.
        float3 viewDirUV    = normalize(float3(viewDirTS.xy * uvSpaceScale, viewDirTS.z)); // TODO: skip normalize
        float  unitAngle    = saturate(FastACosPos(viewDirUV.z) * INV_HALF_PI);            // TODO: optimize
        int    numSteps     = (int)lerp(_PPDMinSamples, _PPDMaxSamples, unitAngle);
        float2 offset       = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirUV, ppdParam, height);

        // Apply offset to all UVSet0 / planar
        layerTexCoord.base.uv += offset;
        // Note: Applying offset on detail uv is only correct if it use the same UVSet or is planar or triplanar. It is up to the user to do the correct thing.
        layerTexCoord.details.uv += offset * scaleOffsetDetails;
    }

    // Since POM "pushes" geometry inwards (rather than extrude it), { height = height - 1 }.
    // Since the result is used as a 'depthOffsetVS', it needs to be positive, so we flip the sign.
    float verticalDisplacement = maxHeight - height * maxHeight;
    return verticalDisplacement / ClampNdotV(NdotV);
#else
    return 0.0;
#endif
}

// Calculate displacement for per vertex displacement mapping
float3 ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
    float height = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, layerTexCoord.base, lod).r - _HeightCenter) * _HeightAmplitude;

    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height);
    // Applying scaling of the object if requested
#ifdef _VERTEX_DISPLACEMENT_LOCK_OBJECT_SCALE
    float3 objectScale = GetDisplacementObjectScale(true);
    // Reminder: mappingType is know statically, so code below is optimize by the compiler
    // Planar and Triplanar are in world space thus it is independent of object scale
    return height.xxx * ((layerTexCoord.base.mappingType == UV_MAPPING_UVSET) ? objectScale : float3(1.0, 1.0, 1.0));
#else
    return height.xxx;
#endif
}

#endif // #ifndef LAYERED_LIT_SHADER
