using System;

namespace UnityEngine.Rendering.PostProcessing
{
    /// <summary>
    /// Use this attribute to draw a trackball in the inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TrackballAttribute : Attribute
    {
        /// <summary>
        /// Trackball modes. These are used to compute and display pre-filtered trackball vales in
        /// the inspector.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Don't display pre-filtered values.
            /// </summary>
            None,

            /// <summary>
            /// Display pre-filtered lift values.
            /// </summary>
            Lift,

            /// <summary>
            /// Display pre-filtered gamma values.
            /// </summary>
            Gamma,

            /// <summary>
            /// Display pre-filtered grain values.
            /// </summary>
            Gain
        }

        /// <summary>
        /// The mode used to display pre-filtered values in the inspector.
        /// </summary>
        public readonly Mode mode;

        /// <summary>
        /// Creates a new attribute.
        /// </summary>
        /// <param name="mode">A mode used to display pre-filtered values in the inspector</param>
        public TrackballAttribute(Mode mode)
        {
            this.mode = mode;
        }
    }
}
