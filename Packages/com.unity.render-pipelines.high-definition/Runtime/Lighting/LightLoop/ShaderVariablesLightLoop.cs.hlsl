//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERVARIABLESLIGHTLOOP_CS_HLSL
#define SHADERVARIABLESLIGHTLOOP_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.ShaderVariablesLightLoop:  static fields
//
#define MAX_ENV2DLIGHT (32)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.ShaderVariablesLightLoop
// PackingRules = Exact
    float4 _ShadowAtlasSize;
    float4 _CascadeShadowAtlasSize;
    float4x4 _Env2DCaptureVP[32];
    uint _DirectionalLightCount;
    uint _PunctualLightCount;
    uint _AreaLightCount;
    uint _EnvLightCount;
    uint _EnvProxyCount;
    int _EnvLightSkyEnabled;
    int _DirectionalShadowIndex;
    float _MicroShadowOpacity;
    uint _NumTileFtplX;
    uint _NumTileFtplY;
    float g_fClustScale;
    float g_fClustBase;
    float g_fNearPlane;
    float g_fFarPlane;
    int g_iLog2NumClusters;
    uint g_isLogBaseBufferEnabled;
    uint _NumTileClusteredX;
    uint _NumTileClusteredY;
    uint _CascadeShadowCount;
    int _DebugSingleShadowIndex;
    int _EnvSliceSize;


#endif
