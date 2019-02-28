namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// The base abstract class for all effect renderer types. If you're writing your own effect you
    /// should rather use <see cref="PostProcessEffectRenderer{T}"/>.
    /// </summary>
    /// <seealso cref="PostProcessEffectRenderer{T}"/>
    public abstract class PostProcessEffectRenderer
    {
        /// <summary>
        /// This member is set to <c>true</c> when <see cref="PostProcessLayer.ResetHistory"/> is
        /// called by the user to reset temporal effects and other history-based effects.
        /// </summary>
        protected bool m_ResetHistory = true;

        /// <summary>
        /// Called when the renderer is created and its associated settings have been set.
        /// </summary>
        /// <seealso cref="PostProcessEffectRenderer{T}.settings"/>
        public virtual void Init()
        {
        }

        /// <summary>
        /// Override this method if your renderer needs access to any of the buffers defined in
        /// <see cref="DepthTextureMode"/>.
        /// </summary>
        /// <returns>The currently set depth texture modes</returns>
        /// <seealso cref="DepthTextureMode"/>
        public virtual DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.None;
        }

        /// <summary>
        /// Resets the history state for this renderer. This is automatically called when
        /// <see cref="PostProcessLayer.ResetHistory"/> is called by the user.
        /// </summary>
        public virtual void ResetHistory()
        {
            m_ResetHistory = true;
        }

        /// <summary>
        /// Override this method to release any resource allocated by your renderer.
        /// </summary>
        public virtual void Release()
        {
            ResetHistory();
        }

        /// <summary>
        /// The render method called by <see cref="PostProcessLayer"/> when the effect is rendered.
        /// </summary>
        /// <param name="context">A context object</param>
        public abstract void Render(PostProcessRenderContext context);

        internal abstract void SetSettings(PostProcessEffectSettings settings);
    }

    /// <summary>
    /// The base abstract class for all effect renderer types.
    /// </summary>
    /// <typeparam name="T">The associated type of settings for this renderer</typeparam>
    public abstract class PostProcessEffectRenderer<T> : PostProcessEffectRenderer
        where T : PostProcessEffectSettings
    {
        /// <summary>
        /// The current state of the effect settings associated with this renderer.
        /// </summary>
        public T settings { get; internal set; }

        internal override void SetSettings(PostProcessEffectSettings settings)
        {
            this.settings = (T)settings;
        }
    }
}
