#ifdef SHADER_VARIABLES_INCLUDE_CB

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/ShaderVariablesLightLoop.cs.hlsl"

#else

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"

    // don't support Buffer yet in unity
    StructuredBuffer<uint>  g_vBigTileLightList;
    StructuredBuffer<uint>  g_vLightListGlobal;
    StructuredBuffer<uint>  g_vLayeredOffsetsBuffer;
    StructuredBuffer<float> g_logBaseBuffer;

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
