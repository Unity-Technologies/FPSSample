using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // RenderRenderPipelineSettings represent settings that are immutable at runtime.
    // There is a dedicated RenderRenderPipelineSettings for each platform

    [Serializable]
    public enum CubeReflectionResolution
    {
        CubeReflectionResolution128 = 128,
        CubeReflectionResolution256 = 256,
        CubeReflectionResolution512 = 512,
        CubeReflectionResolution1024 = 1024,
        CubeReflectionResolution2048 = 2048,
        CubeReflectionResolution4096 = 4096
    }

    [Serializable]
    public enum PlanarReflectionResolution
    {
        PlanarReflectionResolution64 = 64,
        PlanarReflectionResolution128 = 128,
        PlanarReflectionResolution256 = 256,
        PlanarReflectionResolution512 = 512,
        PlanarReflectionResolution1024 = 1024,
        PlanarReflectionResolution2048 = 2048,
        PlanarReflectionResolution4096 = 4096,
        PlanarReflectionResolution8192 = 8192,
        PlanarReflectionResolution16384 = 16384
    }

    [Serializable]
    public enum CookieResolution
    {
        CookieResolution64 = 64,
        CookieResolution128 = 128,
        CookieResolution256 = 256,
        CookieResolution512 = 512,
        CookieResolution1024 = 1024,
        CookieResolution2048 = 2048,
        CookieResolution4096 = 4096,
        CookieResolution8192 = 8192,
        CookieResolution16384 = 16384
    }

    [Serializable]
    public enum CubeCookieResolution
    {
        CubeCookieResolution64 = 64,
        CubeCookieResolution128 = 128,
        CubeCookieResolution256 = 256,
        CubeCookieResolution512 = 512,
        CubeCookieResolution1024 = 1024,
        CubeCookieResolution2048 = 2048,
        CubeCookieResolution4096 = 4096
    }

    [Serializable]
    public class GlobalLightLoopSettings
    {
        public CookieResolution cookieSize = CookieResolution.CookieResolution128;
        public int cookieTexArraySize = 16;
        public CubeCookieResolution pointCookieSize = CubeCookieResolution.CubeCookieResolution128;
        public int cubeCookieTexArraySize = 16;

        public int planarReflectionProbeCacheSize = 2;
        public PlanarReflectionResolution planarReflectionTextureSize = PlanarReflectionResolution.PlanarReflectionResolution1024;
        public int reflectionProbeCacheSize = 64;
        public CubeReflectionResolution reflectionCubemapSize = CubeReflectionResolution.CubeReflectionResolution256;
        public bool reflectionCacheCompressed = false;
        public bool planarReflectionCacheCompressed = false;
        public SkyResolution skyReflectionSize = SkyResolution.SkyResolution256;
        public LayerMask skyLightingOverrideLayerMask = 0;
        public bool supportFabricConvolution = false;

        public int maxDirectionalLightsOnScreen = 16;
        public int maxPunctualLightsOnScreen    = 512;
        public int maxAreaLightsOnScreen        = 64;
        public int maxEnvLightsOnScreen         = 64;
        public int maxDecalsOnScreen            = 512;
    }
}
