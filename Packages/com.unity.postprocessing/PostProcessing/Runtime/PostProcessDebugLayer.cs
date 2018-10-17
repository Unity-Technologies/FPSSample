using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    public enum DebugOverlay
    {
        None,
        Depth,
        Normals,
        MotionVectors,
        NANTracker,
        ColorBlindnessSimulation,
        _,
        AmbientOcclusion,
        BloomBuffer,
        BloomThreshold,
        DepthOfField
    }

    public enum ColorBlindnessType
    {
        Deuteranopia,
        Protanopia,
        Tritanopia
    }

    [Serializable]
    public sealed class PostProcessDebugLayer
    {
        // Monitors
        public LightMeterMonitor lightMeter;
        public HistogramMonitor histogram;
        public WaveformMonitor waveform;
        public VectorscopeMonitor vectorscope;

        Dictionary<MonitorType, Monitor> m_Monitors;

        // Current frame size
        int frameWidth;
        int frameHeight;

        public RenderTexture debugOverlayTarget { get; private set; }

        // Set to true if the frame that was just drawn as a debug overlay enabled and rendered
        public bool debugOverlayActive { get; private set; }
        
        // This is reset to None after rendering of post-processing has finished
        public DebugOverlay debugOverlay { get; private set; }

        // Overlay settings in a separate class to keep things separated
        [Serializable]
        public class OverlaySettings
        {
            public bool linearDepth = false;

            [Range(0f, 16f)]
            public float motionColorIntensity = 4f;

            [Range(4, 128)]
            public int motionGridSize = 64;

            public ColorBlindnessType colorBlindnessType = ColorBlindnessType.Deuteranopia;

            [Range(0f, 1f)]
            public float colorBlindnessStrength = 1f;
        }

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

        // Per-frame requests
        public void RequestMonitorPass(MonitorType monitor)
        {
            m_Monitors[monitor].requested = true;
        }

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

        // Blits to the debug target and mark this frame as using a debug overlay
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
