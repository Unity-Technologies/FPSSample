using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Fog effect with the deferred rendering path.
    /// </summary>
    [Serializable]
    public sealed class Fog
    {
        /// <summary>
        /// If <c>true</c>, enables the internal deferred fog pass. Actual fog settings should be
        /// set in the Lighting panel.
        /// </summary>
        [Tooltip("Enables the internal deferred fog pass. Actual fog settings should be set in the Lighting panel.")]
        public bool enabled = true;

        /// <summary>
        /// Should the fog affect the skybox?
        /// </summary>
        [Tooltip("Mark true for the fog to ignore the skybox")]
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
