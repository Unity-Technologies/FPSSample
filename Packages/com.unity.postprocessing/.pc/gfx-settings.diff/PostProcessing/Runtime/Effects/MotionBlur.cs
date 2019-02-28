using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// This class holds settings for the Motion Blur effect.
    /// </summary>
    [Serializable]
    [PostProcess(typeof(MotionBlurRenderer), "Unity/Motion Blur", false)]
    public sealed class MotionBlur : PostProcessEffectSettings
    {
        /// <summary>
        /// The angle of the rotary shutter. Larger values give longer exposure therefore a stronger
        /// blur effect.
        /// </summary>
        [Range(0f, 360f), Tooltip("The angle of rotary shutter. Larger values give longer exposure.")]
        public FloatParameter shutterAngle = new FloatParameter { value = 270f };

        /// <summary>
        /// The amount of sample points, which affects quality and performances.
        /// </summary>
        [Range(4, 32), Tooltip("The amount of sample points. This affects quality and performance.")]
        public IntParameter sampleCount = new IntParameter { value = 10 };

        /// <inheritdoc />
        public override bool IsEnabledAndSupported(PostProcessRenderContext context)
        {
            return enabled.value
                && shutterAngle.value > 0f
            #if UNITY_EDITOR
                // Don't render motion blur preview when the editor is not playing as it can in some
                // cases results in ugly artifacts (i.e. when resizing the game view).
                && Application.isPlaying
            #endif
                && SystemInfo.supportsMotionVectors
                && RenderTextureFormat.RGHalf.IsSupported()
                && !RuntimeUtilities.isVREnabled;
        }
    }

    internal sealed class MotionBlurRenderer : PostProcessEffectRenderer<MotionBlur>
    {
        enum Pass
        {
            VelocitySetup,
            TileMax1,
            TileMax2,
            TileMaxV,
            NeighborMax,
            Reconstruction
        }

        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;

            if (m_ResetHistory)
            {
                cmd.BlitFullscreenTriangle(context.source, context.destination);
                m_ResetHistory = false;
                return;
            }

            const float kMaxBlurRadius = 5f;
            var vectorRTFormat = RenderTextureFormat.RGHalf;
            var packedRTFormat = RenderTextureFormat.ARGB2101010.IsSupported()
                ? RenderTextureFormat.ARGB2101010
                : RenderTextureFormat.ARGB32;

            var sheet = context.propertySheets.Get(context.resources.shaders.motionBlur);
            cmd.BeginSample("MotionBlur");

            // Calculate the maximum blur radius in pixels.
            int maxBlurPixels = (int)(kMaxBlurRadius * context.height / 100);

            // Calculate the TileMax size.
            // It should be a multiple of 8 and larger than maxBlur.
            int tileSize = ((maxBlurPixels - 1) / 8 + 1) * 8;

            // Pass 1 - Velocity/depth packing
            var velocityScale = settings.shutterAngle / 360f;
            sheet.properties.SetFloat(ShaderIDs.VelocityScale, velocityScale);
            sheet.properties.SetFloat(ShaderIDs.MaxBlurRadius, maxBlurPixels);
            sheet.properties.SetFloat(ShaderIDs.RcpMaxBlurRadius, 1f / maxBlurPixels);

            int vbuffer = ShaderIDs.VelocityTex;
            cmd.GetTemporaryRT(vbuffer, context.width, context.height, 0, FilterMode.Point,
                packedRTFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(BuiltinRenderTextureType.None, vbuffer, sheet, (int)Pass.VelocitySetup);

            // Pass 2 - First TileMax filter (1/2 downsize)
            int tile2 = ShaderIDs.Tile2RT;
            cmd.GetTemporaryRT(tile2, context.width / 2, context.height / 2, 0, FilterMode.Point,
                vectorRTFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(vbuffer, tile2, sheet, (int)Pass.TileMax1);

            // Pass 3 - Second TileMax filter (1/2 downsize)
            int tile4 = ShaderIDs.Tile4RT;
            cmd.GetTemporaryRT(tile4, context.width / 4, context.height / 4, 0, FilterMode.Point,
                vectorRTFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(tile2, tile4, sheet, (int)Pass.TileMax2);
            cmd.ReleaseTemporaryRT(tile2);

            // Pass 4 - Third TileMax filter (1/2 downsize)
            int tile8 = ShaderIDs.Tile8RT;
            cmd.GetTemporaryRT(tile8, context.width / 8, context.height / 8, 0, FilterMode.Point,
                vectorRTFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(tile4, tile8, sheet, (int)Pass.TileMax2);
            cmd.ReleaseTemporaryRT(tile4);

            // Pass 5 - Fourth TileMax filter (reduce to tileSize)
            var tileMaxOffs = Vector2.one * (tileSize / 8f - 1f) * -0.5f;
            sheet.properties.SetVector(ShaderIDs.TileMaxOffs, tileMaxOffs);
            sheet.properties.SetFloat(ShaderIDs.TileMaxLoop, (int)(tileSize / 8f));

            int tile = ShaderIDs.TileVRT;
            cmd.GetTemporaryRT(tile, context.width / tileSize, context.height / tileSize, 0,
                FilterMode.Point, vectorRTFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(tile8, tile, sheet, (int)Pass.TileMaxV);
            cmd.ReleaseTemporaryRT(tile8);

            // Pass 6 - NeighborMax filter
            int neighborMax = ShaderIDs.NeighborMaxTex;
            int neighborMaxWidth = context.width / tileSize;
            int neighborMaxHeight = context.height / tileSize;
            cmd.GetTemporaryRT(neighborMax, neighborMaxWidth, neighborMaxHeight, 0,
                FilterMode.Point, vectorRTFormat, RenderTextureReadWrite.Linear);
            cmd.BlitFullscreenTriangle(tile, neighborMax, sheet, (int)Pass.NeighborMax);
            cmd.ReleaseTemporaryRT(tile);

            // Pass 7 - Reconstruction pass
            sheet.properties.SetFloat(ShaderIDs.LoopCount, Mathf.Clamp(settings.sampleCount / 2, 1, 64));
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Reconstruction);

            cmd.ReleaseTemporaryRT(vbuffer);
            cmd.ReleaseTemporaryRT(neighborMax);
            cmd.EndSample("MotionBlur");
        }
    }
}
