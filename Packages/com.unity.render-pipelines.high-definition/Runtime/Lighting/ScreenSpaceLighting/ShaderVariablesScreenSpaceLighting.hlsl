#ifdef SHADER_VARIABLES_INCLUDE_CB
    // Buffer pyramid
    float4  _ColorPyramidSize;              // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
    float4  _DepthPyramidSize;              // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
    float4  _CameraMotionVectorsSize;       // (x,y) = Actual Pixel Size, (z,w) = 1 / Actual Pixel Size
    float4  _ColorPyramidScale;             // (x,y) = Screen Scale, z = lod count, w = unused
    float4  _DepthPyramidScale;             // (x,y) = Screen Scale, z = lod count, w = unused
    float4  _CameraMotionVectorsScale;      // (x,y) = Screen Scale, z = lod count, w = unused

                                            // Screen space lighting
    float   _SSRefractionInvScreenWeightDistance;     // Distance for screen space smoothstep with fallback
    float   _SSReflectionInvScreenWeightDistance;     // Distance for screen space smoothstep with fallback
    int     _SSReflectionEnabled;
    int     _SSReflectionProjectionModel;
    int     _SSReflectionHiZRayMarchBehindObject;
    int     _SSRefractionHiZRayMarchBehindObject;

    // Ambiant occlusion
    float4 _AmbientOcclusionParam; // xyz occlusion color, w directLightStrenght
    
    float4 _IndirectLightingMultiplier; // .x indirect diffuse multiplier (use with indirect lighting volume controler)
    
#else
    // Rough refraction texture
    // Color pyramid (width, height, lodcount, Unused)
    TEXTURE2D(_ColorPyramidTexture);
    // Depth pyramid (width, height, lodcount, Unused)
    TEXTURE2D(_DepthPyramidTexture);
    // Ambient occlusion texture
    TEXTURE2D(_AmbientOcclusionTexture);
    TEXTURE2D(_CameraMotionVectorsTexture);
    TEXTURE2D(_SsrLightingTexture);
#endif
