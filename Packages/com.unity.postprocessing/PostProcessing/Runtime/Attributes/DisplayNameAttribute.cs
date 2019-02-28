using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Use this attribute to change the label of a field displayed in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class DisplayNameAttribute : Attribute
    {
        /// <summary>
        /// The label displayed in the inspector.
        /// </summary>
        public readonly string displayName;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="displayName">The label to display in the inspector</param>
        public DisplayNameAttribute(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
