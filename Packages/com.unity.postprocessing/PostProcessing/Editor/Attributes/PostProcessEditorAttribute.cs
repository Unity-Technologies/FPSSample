using System;

namespace UnityEditor.Rendering.PostProcessing
{
    /// <summary>
    /// Tells a <see cref="PostProcessEffectEditor{T}"/> class which run-time type it's an editor
    /// for. When you make a custom editor for an effect, you need put this attribute on the editor
    /// class.
    /// </summary>
    /// <seealso cref="PostProcessEffectEditor{T}"/>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PostProcessEditorAttribute : Attribute
    {
        /// <summary>
        /// The type that this editor can edit.
        /// </summary>
        public readonly Type settingsType;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="settingsType">The type that this editor can edit</param>
        public PostProcessEditorAttribute(Type settingsType)
        {
            this.settingsType = settingsType;
        }
    }
}
