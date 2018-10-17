namespace UnityEngine.Rendering.PostProcessing
{
    public abstract class PostProcessEffectRenderer
    {
        protected bool m_ResetHistory = true;

        // Called when the renderer is created. Settings will be set before `Init` is called.
        public virtual void Init()
        {
        }

        // Unused with scriptable render pipelines
        public virtual DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.None;
        }

        public virtual void ResetHistory()
        {
            m_ResetHistory = true;
        }

        public virtual void Release()
        {
            ResetHistory();
        }

        public abstract void Render(PostProcessRenderContext context);

        internal abstract void SetSettings(PostProcessEffectSettings settings);
    }

    public abstract class PostProcessEffectRenderer<T> : PostProcessEffectRenderer
        where T : PostProcessEffectSettings
    {
        public T settings { get; internal set; }

        internal override void SetSettings(PostProcessEffectSettings settings)
        {
            this.settings = (T)settings;
        }
    }
}
