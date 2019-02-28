using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// A <see cref="PropertySheet"/> factory for easy creation and destruction of <see cref="Material"/>
    /// and <see cref="MaterialPropertyBlock"/>.
    /// </summary>
    /// <seealso cref="PropertySheet"/>
    public sealed class PropertySheetFactory
    {
        readonly Dictionary<Shader, PropertySheet> m_Sheets;

        /// <summary>
        /// Creates a new factory.
        /// </summary>
        public PropertySheetFactory()
        {
            m_Sheets = new Dictionary<Shader, PropertySheet>();
        }

        /// <summary>
        /// Gets a <see cref="PropertySheet"/> for a given shader identifier. Sheets are recycled
        /// so you can safely call this method on every frame.
        /// </summary>
        /// <param name="shaderName">The name of the shader to retrieve a sheet for</param>
        /// <returns>A sheet for the given shader</returns>
        /// <remarks>
        /// This method will not work when loading post-processing from an asset bundle. For this
        /// reason it is recommended to use <see cref="Get(Shader)"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the shader is invalid</exception>
        [Obsolete("Use PropertySheet.Get(Shader) with a direct reference to the Shader instead.")]
        public PropertySheet Get(string shaderName)
        {
            var shader = Shader.Find(shaderName);

            if (shader == null)
                throw new ArgumentException(string.Format("Invalid shader ({0})", shaderName));

            return Get(shader);
        }

        /// <summary>
        /// Gets a <see cref="PropertySheet"/> for a given shader instance. Sheets are recycled so
        /// you can safely call this method on every frame.
        /// </summary>
        /// <param name="shader">A shader instance to retrieve a sheet for</param>
        /// <returns>A sheet for the given shader</returns>
        /// <exception cref="ArgumentException">Thrown if the shader is invalid</exception>
        public PropertySheet Get(Shader shader)
        {
            PropertySheet sheet;

            if (shader == null)
                throw new ArgumentException(string.Format("Invalid shader ({0})", shader));

            if (m_Sheets.TryGetValue(shader, out sheet))
                return sheet;

            var shaderName = shader.name;
            var material = new Material(shader)
            {
                name = string.Format("PostProcess - {0}", shaderName.Substring(shaderName.LastIndexOf('/') + 1)),
                hideFlags = HideFlags.DontSave
            };

            sheet = new PropertySheet(material);
            m_Sheets.Add(shader, sheet);
            return sheet;
        }

        /// <summary>
        /// Releases all resources used by this factory.
        /// </summary>
        /// <remarks>
        /// You don't need to call this method when using the builtin factory from
        /// <see cref="PostProcessRenderContext"/>.
        /// </remarks>
        public void Release()
        {
            foreach (var sheet in m_Sheets.Values)
                sheet.Release();

            m_Sheets.Clear();
        }
    }
}
