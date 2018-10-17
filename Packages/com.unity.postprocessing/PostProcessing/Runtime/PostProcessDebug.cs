namespace UnityEngine.Rendering.PostProcessing
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Post-process Debug", 1002)]
    public sealed class PostProcessDebug : MonoBehaviour
    {
        public PostProcessLayer postProcessLayer;
        PostProcessLayer m_PreviousPostProcessLayer;

        public bool lightMeter;
        public bool histogram;
        public bool waveform;
        public bool vectorscope;

        public DebugOverlay debugOverlay = DebugOverlay.None;

        Camera m_CurrentCamera;
        CommandBuffer m_CmdAfterEverything;

        void OnEnable()
        {
            m_CmdAfterEverything = new CommandBuffer { name = "Post-processing Debug Overlay" };

#if UNITY_EDITOR
            // Update is only called on object change when ExecuteInEditMode is set, but we need it
            // to execute on every frame no matter what when not in play mode, so we'll use the
            // editor update loop instead...
            UnityEditor.EditorApplication.update += UpdateStates;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= UpdateStates;
#endif

            if (m_CurrentCamera != null)
                m_CurrentCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, m_CmdAfterEverything);

            m_CurrentCamera = null;
            m_PreviousPostProcessLayer = null;
        }

#if !UNITY_EDITOR
        void Update()
        {
            UpdateStates();
        }
#endif

        void Reset()
        {
            postProcessLayer = GetComponent<PostProcessLayer>();
        }

        void UpdateStates()
        {
            if (m_PreviousPostProcessLayer != postProcessLayer)
            {
                // Remove cmdbuffer from previously set camera
                if (m_CurrentCamera != null)
                {
                    m_CurrentCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, m_CmdAfterEverything);
                    m_CurrentCamera = null;
                }

                m_PreviousPostProcessLayer = postProcessLayer;

                // Add cmdbuffer to the currently set camera
                if (postProcessLayer != null)
                {
                    m_CurrentCamera = postProcessLayer.GetComponent<Camera>();
                    m_CurrentCamera.AddCommandBuffer(CameraEvent.AfterImageEffects, m_CmdAfterEverything);
                }
            }

            if (postProcessLayer == null || !postProcessLayer.enabled)
                return;

            // Monitors
            if (lightMeter) postProcessLayer.debugLayer.RequestMonitorPass(MonitorType.LightMeter);
            if (histogram) postProcessLayer.debugLayer.RequestMonitorPass(MonitorType.Histogram);
            if (waveform) postProcessLayer.debugLayer.RequestMonitorPass(MonitorType.Waveform);
            if (vectorscope) postProcessLayer.debugLayer.RequestMonitorPass(MonitorType.Vectorscope);

            // Overlay
            postProcessLayer.debugLayer.RequestDebugOverlay(debugOverlay);
        }

        void OnPostRender()
        {
            m_CmdAfterEverything.Clear();

            if (postProcessLayer == null || !postProcessLayer.enabled || !postProcessLayer.debugLayer.debugOverlayActive)
                return;

            m_CmdAfterEverything.Blit(postProcessLayer.debugLayer.debugOverlayTarget, BuiltinRenderTextureType.CameraTarget);
        }

        void OnGUI()
        {
            if (postProcessLayer == null || !postProcessLayer.enabled)
                return;

            // Some SRPs don't unbind render targets and leave them as-is
            RenderTexture.active = null;

            var rect = new Rect(5, 5, 0, 0);
            var debugLayer = postProcessLayer.debugLayer;
            DrawMonitor(ref rect, debugLayer.lightMeter, lightMeter);
            DrawMonitor(ref rect, debugLayer.histogram, histogram);
            DrawMonitor(ref rect, debugLayer.waveform, waveform);
            DrawMonitor(ref rect, debugLayer.vectorscope, vectorscope);
        }

        void DrawMonitor(ref Rect rect, Monitor monitor, bool enabled)
        {
            if (!enabled || monitor.output == null)
                return;

            rect.width = monitor.output.width;
            rect.height = monitor.output.height;
            GUI.DrawTexture(rect, monitor.output);
            rect.x += monitor.output.width + 5f;
        }
    }
}
