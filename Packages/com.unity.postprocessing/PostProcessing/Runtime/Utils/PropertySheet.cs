namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// The post-processing stack is entirely built around the use of <see cref="CommandBuffer"/>
    /// and as such requires the use of <see cref="MaterialPropertyBlock"/> to properly deal with
    /// the deferred nature of <see cref="CommandBuffer"/>.
    /// This wrapper abstracts the creation and destruction of <see cref="MaterialPropertyBlock"/>
    /// and <see cref="Material"/> to make the process easier.
    /// </summary>
    /// <seealso cref="PropertySheetFactory"/>
    public sealed class PropertySheet
    {
        /// <summary>
        /// The actual <see cref="MaterialPropertyBlock"/> to fill.
        /// </summary>
        public MaterialPropertyBlock properties { get; private set; }

        internal Material material { get; private set; }

        internal PropertySheet(Material material)
        {
            this.material = material;
            properties = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Clears all keywords set on the source material.
        /// </summary>
        public void ClearKeywords()
        {
            material.shaderKeywords = null;
        }

        /// <summary>
        /// Enableds a given keyword on the source material.
        /// </summary>
        /// <param name="keyword">The keyword to enable</param>
        public void EnableKeyword(string keyword)
        {
            material.EnableKeyword(keyword);
        }

        /// <summary>
        /// Disables a given keyword on the source material.
        /// </summary>
        /// <param name="keyword">The keyword to disable</param>
        public void DisableKeyword(string keyword)
        {
            material.DisableKeyword(keyword);
        }

        internal void Release()
        {
            RuntimeUtilities.Destroy(material);
            material = null;
        }
    }
}
