using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public sealed class Fog
    {
        [Tooltip("Enables the internal deferred fog pass. Actual fog settings should be set in the Lighting panel.")]
        public bool enabled = true;

        [Tooltip("Should the fog affect the skybox?")]
        public bool excludeSkybox = true;

        internal DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth;
        }

        internal bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled
                && RenderSettings.fog
                && !RuntimeUtilities.scriptableRenderPipelineActive
                && context.resources.shaders.deferredFog
                && context.resources.shaders.deferredFog.isSupported
                && context.camera.actualRenderingPath == RenderingPath.DeferredShading;  // In forward fog is already done at shader level
        }

        internal void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(context.resources.shaders.deferredFog);
            sheet.ClearKeywords();

            var fogColor = RuntimeUtilities.isLinearColorSpace ? RenderSettings.fogColor.linear : RenderSettings.fogColor;
            sheet.properties.SetVector(ShaderIDs.FogColor, fogColor);
            sheet.properties.SetVector(ShaderIDs.FogParams, new Vector3(RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));

            var cmd = context.command;
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, excludeSkybox ? 1 : 0);
        }
    }
}
