using System;

namespace UnityEngine.Rendering.PostProcessing
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class TrackballAttribute : Attribute
    {
        public enum Mode
        {
            None,
            Lift,
            Gamma,
            Gain
        }

        public readonly Mode mode;

        public TrackballAttribute(Mode mode)
        {
            this.mode = mode;
        }
    }
}
