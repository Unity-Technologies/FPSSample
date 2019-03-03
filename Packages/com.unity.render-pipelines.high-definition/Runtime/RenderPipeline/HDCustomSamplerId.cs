using UnityEngine.Profiling;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum CustomSamplerId
    {
        PushGlobalParameters,
        CopySetDepthBuffer,
        CopyDepthBuffer,
        HTileForSSS,
        Forward,
        RenderSSAO,
        RenderShadows,
        ScreenSpaceShadows,
        BuildLightList,
        BlitToFinalRT,
        Distortion,
        ApplyDistortion,
        DepthPrepass,
        TransparentDepthPrepass,
        GBuffer,
        DBufferRender,
        DBufferPrepareDrawData,
        DBufferNormal,
        DisplayDebugDecalsAtlas,
        DisplayDebugViewMaterial,
        DebugViewMaterialGBuffer,
        BlitDebugViewMaterialDebug,
        SubsurfaceScattering,
        SsrTracing,
        SsrReprojection,
        ForwardPassName,
        ForwardTransparentDepthPrepass,
        RenderForwardError,
        TransparentDepthPostpass,
        ObjectsVelocity,
        CameraVelocity,
        ColorPyramid,
        DepthPyramid,
        PostProcessing,
        RenderDebug,
        ClearBuffers,
        ClearDepthStencil,
        ClearSssLightingBuffer,
        ClearSSSFilteringTarget,
        ClearAndCopyStencilTexture,
        ClearHTile,
        ClearHDRTarget,
        ClearGBuffer,
        ClearSsrBuffers,
        HDRenderPipelineRender,
        CullResultsCull,
        CopyDepth,
        UpdateStencilCopyForSSRExclusion,

        // Profile sampler for tile pass
        TPPrepareLightsForGPU,
        TPPushGlobalParameters,
        TPTiledLightingDebug,
        TPScreenSpaceShadows,
        TPTileSettingsEnableTileAndCluster,
        TPForwardPass,
        TPForwardTiledClusterpass,
        TPDisplayShadows,
        TPRenderDeferredLighting,

        // Misc
        VolumeUpdate,

        Max
    }

    public static class HDCustomSamplerExtension
    {
        static CustomSampler[] s_Samplers;

        public static CustomSampler GetSampler(this CustomSamplerId samplerId)
        {
            // Lazy init
            if (s_Samplers == null)
            {
                s_Samplers = new CustomSampler[(int)CustomSamplerId.Max];

                for (int i = 0; i < (int)CustomSamplerId.Max; i++)
                {
                    var id = (CustomSamplerId)i;
                    s_Samplers[i] = CustomSampler.Create("C#_" + id);
                }
            }

            return s_Samplers[(int)samplerId];
        }
    }
}
