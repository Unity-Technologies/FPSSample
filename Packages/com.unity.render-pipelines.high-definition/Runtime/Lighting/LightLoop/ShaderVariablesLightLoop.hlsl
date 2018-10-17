#ifdef SHADER_VARIABLES_INCLUDE_CB

    #define MAX_ENV2D_LIGHT 32

    uint _DirectionalLightCount;
    uint _PunctualLightCount;
    uint _AreaLightCount;
    uint _EnvLightCount;
    uint _EnvProxyCount;
    int  _EnvLightSkyEnabled;         // TODO: make it a bool
    int _DirectionalShadowIndex;

    float _MicroShadowOpacity;

    uint _NumTileFtplX;
    uint _NumTileFtplY;

    // these uniforms are only needed for when OPAQUES_ONLY is NOT defined
    // but there's a problem with our front-end compilation of compute shaders with multiple kernels causing it to error
    //#ifdef USE_CLUSTERED_LIGHTLIST
    float4x4 g_mInvScrProjection; // TODO: remove, unused in HDRP

    float g_fClustScale;
    float g_fClustBase;
    float g_fNearPlane;
    float g_fFarPlane;
    int g_iLog2NumClusters; // We need to always define these to keep constant buffer layouts compatible

    uint g_isLogBaseBufferEnabled;
    //#endif

    //#ifdef USE_CLUSTERED_LIGHTLIST
    uint _NumTileClusteredX;
    uint _NumTileClusteredY;
    
    real4 _ShadowAtlasSize;

    float4x4 _Env2DCaptureVP[MAX_ENV2D_LIGHT];

    // TODO: move this elsewhere
    int _DebugSingleShadowIndex;

#else

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"

    StructuredBuffer<uint>  g_vLightListGlobal;      // don't support Buffer yet in unity

    StructuredBuffer<uint>  g_vLayeredOffsetsBuffer;     // don't support Buffer yet in unity
    StructuredBuffer<float> g_logBaseBuffer;            // don't support Buffer yet in unity
                                                        //#endif

    #ifdef USE_INDIRECT
        StructuredBuffer<uint> g_TileFeatureFlags;
    #endif

    StructuredBuffer<DirectionalLightData> _DirectionalLightDatas;
    StructuredBuffer<LightData>            _LightDatas;
    StructuredBuffer<EnvLightData>         _EnvLightDatas;

    // Used by directional and spot lights
    TEXTURE2D_ARRAY(_CookieTextures);

    // Used by point lights
    TEXTURECUBE_ARRAY_ABSTRACT(_CookieCubeTextures);

    // Use texture array for reflection (or LatLong 2D array for mobile)
    TEXTURECUBE_ARRAY_ABSTRACT(_EnvCubemapTextures);
    TEXTURE2D_ARRAY(_Env2DTextures);

    // XRTODO: Need to stereo-ize access
    TEXTURE2D(_DeferredShadowTexture);

#endif
