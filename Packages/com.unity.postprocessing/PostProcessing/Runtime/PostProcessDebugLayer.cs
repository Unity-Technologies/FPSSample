using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// A list of debug overlays.
    /// </summary>
    public enum DebugOverlay
    {
        /// <summary>
        /// No overlay.
        /// </summary>
        None,

        /// <summary>
        /// Displays the depth buffer.
        /// </summary>
        Depth,

        /// <summary>
        /// Displays the screen-space normals buffer.
        /// </summary>
        Normals,

        /// <summary>
        /// Displays the screen-space motion vectors.
        /// </summary>
        MotionVectors,

        /// <summary>
        /// Dims the screen and displays NaN and Inf pixels with a bright pink color.
        /// </summary>
        NANTracker,

        /// <summary>
        /// A color blindness simulator.
        /// </summary>
        ColorBlindnessSimulation,

        // Menu item separator for the inspector
        _,

        /// <summary>
        /// Displays the raw ambient occlusion map.
        /// </summary>
        AmbientOcclusion,

        /// <summary>
        /// Displays the bloom buffer.
        /// </summary>
        BloomBuffer,

        /// <summary>
        /// Displays the thresholded buffer used to generate bloom.
        /// </summary>
        BloomThreshold,

        /// <summary>
        /// Displays depth of field helpers.
        /// </summary>
        DepthOfField
    }

    /// <summary>
    /// A list of color blindness types.
    /// </summary>
    public enum ColorBlindnessType
    {
        /// <summary>
        /// Deuteranopia (red-green color blindness).
        /// </summary>
        Deuteranopia,

        /// <summary>
        /// Protanopia (red-green color blindness).
        /// </summary>
        Protanopia,

        /// <summary>
        /// Tritanopia (blue-yellow color blindness).
        /// </summary>
        Tritanopia
    }

    /// <summary>
    /// This class centralizes rendering commands for debug modes.
    /// </summary>
    [Serializable]
    public sealed class PostProcessDebugLayer
    {
        /// <summary>
        /// Light meter renderer.
        /// </summary>
        public LightMeterMonitor lightMeter;

        /// <summary>
        /// Histogram renderer.
        /// </summary>
        public HistogramMonitor histogram;

        /// <summary>
        /// Waveform renderer.
        /// </summary>
        public WaveformMonitor waveform;

        /// <summary>
        /// Vectorscope monitor.
        /// </summary>
        public VectorscopeMonitor vectorscope;

        Dictionary<MonitorType, Monitor> m_Monitors;

        // Current frame size
        int frameWidth;
        int frameHeight;

        /// <summary>
        /// The render target used to render debug overlays in.
        /// </summary>
        public RenderTexture debugOverlayTarget { get; private set; }

        /// <summary>
        /// Returns <c>true</c> if the frame that was just drawn had an active debug overlay.
        /// </summary>
        public bool debugOverlayActive { get; private set; }

        /// <summary>
        /// The debug overlay requested for the current frame. It is reset to <c>None</c> once the
        /// frame has finished rendering.
        /// </summary>
        public DebugOverlay debugOverlay { get; private set; }

        /// <summary>
        /// Debug overlay settings wrapper.
        /// </summary>
        [Serializable]
        public class OverlaySettings
        {
            /// <summary>
            /// Should we remap depth to a linear range?
            /// </summary>
            public bool linearDepth = false;

            /// <summary>
            /// The intensity of motion vector colors.
            /// </summary>
            [Range(0f, 16f)]
            public float motionColorIntensity = 4f;

            /// <summary>
            /// The size of the motion vector grid.
            /// </summary>
            [Range(4, 128)]
            public int motionGridSize = 64;

            /// <summary>
            /// The color blindness type to simulate.
            /// </summary>
            public ColorBlindnessType colorBlindnessType = ColorBlindnessType.Deuteranopia;

            /// <summary>
            /// The strength of the selected color blindness type.
            /// </summary>
            [Range(0f, 1f)]
            public float colorBlindnessStrength = 1f;
        }

        /// <summary>
        /// Debug overlay settings.
        /// </summary>
        public OverlaySettings overlaySettings;

        internal void OnEnable()
        {
            RuntimeUtilities.CreateIfNull(ref lightMeter);
            RuntimeUtilities.CreateIfNull(ref histogram);
            RuntimeUtilities.CreateIfNull(ref waveform);
            RuntimeUtilities.CreateIfNull(ref vectorscope);
            RuntimeUtilities.CreateIfNull(ref overlaySettings);

            m_Monitors = new Dictionary<MonitorType, Monitor>
            {
                { MonitorType.LightMeter, lightMeter },
                { MonitorType.Histogram, histogram },
                { MonitorType.Waveform, waveform },
                { MonitorType.Vectorscope, vectorscope }
            };

            foreach (var kvp in m_Monitors)
                kvp.Value.OnEnable();
        }

        internal void OnDisable()
        {
            foreach (var kvp in m_Monitors)
                kvp.Value.OnDisable();

            DestroyDebugOverlayTarget();
        }

        void DestroyDebugOverlayTarget()
        {
            RuntimeUtilities.Destroy(debugOverlayTarget);
            debugOverlayTarget = null;
        }

        /// <summary>
        /// Requests the drawing of a monitor for the current frame.
        /// </summary>
        /// <param name="monitor">The monitor to request</param>
        public void RequestMonitorPass(MonitorType monitor)
        {
            m_Monitors[monitor].requested = true;
        }

        /// <summary>
        /// Requests the drawing of a debug overlay for the current frame.
        /// </summary>
        /// <param name="mode">The debug overlay to request</param>
        public void RequestDebugOverlay(DebugOverlay mode)
        {
            debugOverlay = mode;
        }

        // Sets the current frame size - used to make sure the debug overlay target is always the
        // correct size - mostly useful in the editor as the user can easily resize the gameview.
        internal void SetFrameSize(int width, int height)
        {
            frameWidth = width;
            frameHeight = height;
            debugOverlayActive = false;
        }

        /// <summary>
        /// Blit a source render target to the debug overlay target.
        /// </summary>
        /// <param name="cmd">The command buffer to send render commands to</param>
        /// <param name="source">The source target</param>
        /// <param name="sheet">The property sheet to use for the blit</param>
        /// <param name="pass">The pass to use for the property sheet</param>
        public void PushDebugOverlay(CommandBuffer cmd, RenderTargetIdentifier source, PropertySheet sheet, int pass)
        {
            if (debugOverlayTarget == null || !debugOverlayTarget.IsCreated() || debugOverlayTarget.width != frameWidth || debugOverlayTarget.height != frameHeight)
            {
                RuntimeUtilities.Destroy(debugOverlayTarget);

                debugOverlayTarget = new RenderTexture(frameWidth, frameHeight, 0, RenderTextureFormat.ARGB32)
                {
                    name = "Debug Overlay Target",
                    anisoLevel = 1,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };
                debugOverlayTarget.Create();
            }

            cmd.BlitFullscreenTriangle(source, debugOverlayTarget, sheet, pass);
            debugOverlayActive = true;
        }

        internal DepthTextureMode GetCameraFlags()
        {
            if (debugOverlay == DebugOverlay.Depth)
                return DepthTextureMode.Depth;

            if (debugOverlay == DebugOverlay.Normals)
                return DepthTextureMode.DepthNormals;

            if (debugOverlay == DebugOverlay.MotionVectors)
                return DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            return DepthTextureMode.None;
        }

        internal void RenderMonitors(PostProcessRenderContext context)
        {
            // Monitors
            bool anyActive = false;
            bool needsHalfRes = false;

            foreach (var kvp in m_Monitors)
            {
                bool active = kvp.Value.IsRequestedAndSupported(context);
                anyActive |= active;
                needsHalfRes |= active && kvp.Value.NeedsHalfRes();
            }

            if (!anyActive)
                return;

            var cmd = context.command;
            cmd.BeginSample("Monitors");

            if (needsHalfRes)
            {
                cmd.GetTemporaryRT(ShaderIDs.HalfResFinalCopy, context.width / 2, context.height / 2, 0, FilterMode.Bilinear, context.sourceFormat);
                cmd.Blit(context.destination, ShaderIDs.HalfResFinalCopy);
            }

            foreach (var kvp in m_Monitors)
            {
                var monitor = kvp.Value;

                if (monitor.requested)
                    monitor.Render(context);
            }

            if (needsHalfRes)
                cmd.ReleaseTemporaryRT(ShaderIDs.HalfResFinalCopy);

            cmd.EndSample("Monitors");
        }

        internal void RenderSpecialOverlays(PostProcessRenderContext context)
        {
            if (debugOverlay == DebugOverlay.Depth)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.debugOverlays);
                sheet.properties.SetVector(ShaderIDs.Params, new Vector4(overlaySettings.linearDepth ? 1f : 0f, 0f, 0f, 0f));
                PushDebugOverlay(context.command, BuiltinRenderTextureType.None, sheet, 0);
            }
            else if (debugOverlay == DebugOverlay.Normals)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.debugOverlays);
                sheet.ClearKeywords();

                if (context.camera.actualRenderingPath == RenderingPath.DeferredLighting)
                    sheet.EnableKeyword("SOURCE_GBUFFER");

                PushDebugOverlay(context.command, BuiltinRenderTextureType.None, sheet, 1);
            }
            else if (debugOverlay == DebugOverlay.MotionVectors)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.debugOverlays);
                sheet.properties.SetVector(ShaderIDs.Params, new Vector4(overlaySettings.motionColorIntensity, overlaySettings.motionGridSize, 0f, 0f));
                PushDebugOverlay(context.command, context.source, sheet, 2);
            }
            else if (debugOverlay == DebugOverlay.NANTracker)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.debugOverlays);
                PushDebugOverlay(context.command, context.source, sheet, 3);
            }
            else if (debugOverlay == DebugOverlay.ColorBlindnessSimulation)
            {
                var sheet = context.propertySheets.Get(context.resources.shaders.debugOverlays);
                sheet.properties.SetVector(ShaderIDs.Params, new Vector4(overlaySettings.colorBlindnessStrength, 0f, 0f, 0f));
                PushDebugOverlay(context.command, context.source, sheet, 4 + (int)overlaySettings.colorBlindnessType);
            }
        }

        internal void EndFrame()
        {
            foreach (var kvp in m_Monitors)
                kvp.Value.requested = false;

            if (!debugOverlayActive)
                DestroyDebugOverlayTarget();

            debugOverlay = DebugOverlay.None;
        }
    }
}
