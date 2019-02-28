using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Use this attribute to specify a range between a min and a max value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class MinMaxAttribute : Attribute
    {
        /// <summary>
        /// The minimum limit of the user defined range.
        /// </summary>
        public readonly float min;

        /// <summary>
        /// The maximum limit of the user defined range.
        /// </summary>
        public readonly float max;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="min">The minimum limit of the user defined range</param>
        /// <param name="max">The maximum limit of the user defined range</param>
        public MinMaxAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
