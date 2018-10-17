namespace UnityEngine.Rendering.PostProcessing
{
    public enum MonitorType
    {
        LightMeter,
        Histogram,
        Waveform,
        Vectorscope
    }

    public abstract class Monitor
    {
        public RenderTexture output { get; protected set; }

        internal bool requested = false;

        public bool IsRequestedAndSupported(PostProcessRenderContext context)
        {
            return requested
                && SystemInfo.supportsComputeShaders
                && !RuntimeUtilities.isAndroidOpenGL
                && ShaderResourcesAvailable(context);
        }

        internal abstract bool ShaderResourcesAvailable(PostProcessRenderContext context);

        internal virtual bool NeedsHalfRes()
        {
            return false;
        }

        protected void CheckOutput(int width, int height)
        {
            if (output == null || !output.IsCreated() || output.width != width || output.height != height)
            {
                RuntimeUtilities.Destroy(output);
                output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    anisoLevel = 0,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false
                };
            }
        }

        internal virtual void OnEnable()
        {
        }

        internal virtual void OnDisable()
        {
            RuntimeUtilities.Destroy(output);
        }

        internal abstract void Render(PostProcessRenderContext context);
    }
}
