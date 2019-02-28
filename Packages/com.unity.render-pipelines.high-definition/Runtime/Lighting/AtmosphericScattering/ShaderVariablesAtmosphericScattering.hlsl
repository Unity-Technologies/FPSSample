#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/ShaderVariablesAtmosphericScattering.cs.hlsl"
#else
    TEXTURE3D(_VBufferLighting);
    TEXTURECUBE_ARRAY(_SkyTexture);

    #define _MipFogNear                     _MipFogParameters.x
    #define _MipFogFar                      _MipFogParameters.y
    #define _MipFogMaxMip                   _MipFogParameters.z

    #define _FogDensity                     _FogColorDensity.w
    #define _FogColor                       _FogColorDensity

    #define _LinearFogStart                 _LinearFogParameters.x
    #define _LinearFogOneOverRange          _LinearFogParameters.y
    #define _LinearFogHeightEnd             _LinearFogParameters.z
    #define _LinearFogHeightOneOverRange    _LinearFogParameters.w

    #define _ExpFogDistance                 _ExpFogParameters.x
    #define _ExpFogBaseHeight               _ExpFogParameters.y
    #define _ExpFogHeightAttenuation        _ExpFogParameters.z
#endif

