
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    public unsafe struct ShaderVariablesLightLoop
    {
        public const int s_MaxEnv2DLight = 32;

        [HLSLArray(0, typeof(Vector4))]
        public fixed float _ShadowAtlasSize[4];
        [HLSLArray(0, typeof(Vector4))]
        public fixed float _CascadeShadowAtlasSize[4];

        [HLSLArray(s_MaxEnv2DLight, typeof(Matrix4x4))]
        public fixed float _Env2DCaptureVP[s_MaxEnv2DLight * 4 * 4];

        public uint _DirectionalLightCount;

        public uint _PunctualLightCount;
        public uint _AreaLightCount;
        public uint _EnvLightCount;
        public uint _EnvProxyCount;
        public int  _EnvLightSkyEnabled;         // TODO: make it a bool
        public int _DirectionalShadowIndex;

        public float _MicroShadowOpacity;

        public uint _NumTileFtplX;
        public  uint _NumTileFtplY;

        // these uniforms are only needed for when OPAQUES_ONLY is NOT defined
        // but there's a problem with our front-end compilation of compute shaders with multiple kernels causing it to error
        //#ifdef USE_CLUSTERED_LIGHTLIST
//        float4x4 g_mInvScrProjection; // TODO: remove, unused in HDRP

        public float g_fClustScale;
        public float g_fClustBase;
        public float g_fNearPlane;
        public float g_fFarPlane;
        public int g_iLog2NumClusters; // We need to always define these to keep constant buffer layouts compatible

        public uint g_isLogBaseBufferEnabled;
        //#endif

        //#ifdef USE_CLUSTERED_LIGHTLIST
        public uint _NumTileClusteredX;
        public uint _NumTileClusteredY;

        public uint _CascadeShadowCount;

        // TODO: move this elsewhere
        public int _DebugSingleShadowIndex;

        public int _EnvSliceSize;
    }
}

