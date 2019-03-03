using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Use this attribute to clamp floating point values to a minimum value in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MinAttribute : Attribute
    {
        /// <summary>
        /// The minimum value the field will be clamped to.
        /// </summary>
        public readonly float min;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="max">The minimum value the field will be clamped to</param>
        public MinAttribute(float min)
        {
            this.min = min;
        }
    }
}
