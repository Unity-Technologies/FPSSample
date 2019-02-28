using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Use this attribute to clamp floating point values to a maximum value in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MaxAttribute : Attribute
    {
        /// <summary>
        /// The maximum value the field will be clamped to.
        /// </summary>
        public readonly float max;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="max">The maximum value the field will be clamped to</param>
        public MaxAttribute(float max)
        {
            this.max = max;
        }
    }
}
