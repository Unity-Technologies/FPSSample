real3 ADD_FUNC_SUFFIX(ADD_NORMAL_FUNC_SUFFIX(SampleUVMappingNormal))(TEXTURE2D_ARGS(textureName, samplerName), UVMapping uvMapping, real scale, real param)
{
    if (uvMapping.mappingType == UV_MAPPING_TRIPLANAR)
    {
        real3 triplanarWeights = uvMapping.triplanarWeights;

#ifdef SURFACE_GRADIENT
        // Height map gradient. Basically, it encodes height map slopes along S and T axes.
        real2 derivXplane;
        real2 derivYPlane;
        real2 derivZPlane;
        derivXplane = derivYPlane = derivZPlane = real2(0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            derivXplane = triplanarWeights.x * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param), scale);
        if (triplanarWeights.y > 0.0)
            derivYPlane = triplanarWeights.y * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param), scale);
        if (triplanarWeights.z > 0.0)
            derivZPlane = triplanarWeights.z * UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param), scale);

        // Assume derivXplane, derivYPlane and derivZPlane sampled using (z,y), (z,x) and (x,y) respectively.
        // TODO: Check with morten convention! Do it follow ours ?
        real3 volumeGrad = real3(derivZPlane.x + derivYPlane.y, derivZPlane.y + derivXplane.y, derivXplane.x + derivYPlane.x);
        return SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad);
#else
        real3 val = real3(0.0, 0.0, 0.0);

        if (triplanarWeights.x > 0.0)
            val += triplanarWeights.x * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvZY, param), scale);
        if (triplanarWeights.y > 0.0)
            val += triplanarWeights.y * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXZ, param), scale);
        if (triplanarWeights.z > 0.0)
            val += triplanarWeights.z * UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uvXY, param), scale);

        return normalize(val);
#endif
    }
#ifdef SURFACE_GRADIENT
    else if (uvMapping.mappingType == UV_MAPPING_PLANAR)
    {
        // Note: Planar is on uv coordinate (and not uvXZ)
        real2 derivYPlane = UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
        // See comment above
        real3 volumeGrad = real3(derivYPlane.y, 0.0, derivYPlane.x);
        return SurfaceGradientFromVolumeGradient(uvMapping.normalWS, volumeGrad);
    }
#endif
    else
    {
#ifdef SURFACE_GRADIENT
        real2 deriv = UNPACK_DERIVATIVE_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
        return SurfaceGradientFromTBN(deriv, uvMapping.tangentWS, uvMapping.bitangentWS);
#else
        return UNPACK_NORMAL_FUNC(SAMPLE_TEXTURE_FUNC(textureName, samplerName, uvMapping.uv, param), scale);
#endif
    }
}
